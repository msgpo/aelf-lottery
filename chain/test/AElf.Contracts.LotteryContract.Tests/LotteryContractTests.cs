using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Consensus.Application;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using IBlockTimeProvider = AElf.ContractTestBase.ContractTestKit.IBlockTimeProvider;

namespace AElf.Contracts.LotteryContract
{
    // ReSharper disable InconsistentNaming
    public class LotteryContractTests : LotteryContractTestBase
    {
        private const long Price = 10_00000000;

        private LotteryContractContainer.LotteryContractStub AliceLotteryContractStub =>
            GetLotteryContractStub(AliceKeyPair);

        private TokenContractContainer.TokenContractStub AliceTokenContractStub => GetTokenContractStub(AliceKeyPair);

        private LotteryContractContainer.LotteryContractStub BobLotteryContractStub =>
            GetLotteryContractStub(BobKeyPair);

        private TokenContractContainer.TokenContractStub BobTokenContractStub => GetTokenContractStub(BobKeyPair);

        [Fact]
        public async Task InitializeAndCheckStatus()
        {
            await LotteryContractStub.Initialize.SendAsync(new InitializeInput
            {
                TokenSymbol = "ELF",
                MaximumAmount = 100,
                Price = Price,
                DrawingLag = 1,
                StartTimestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                ShutdownTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10))
            });

            var currentPeriod = await LotteryContractStub.GetCurrentPeriod.CallAsync(new Empty());
            currentPeriod.StartId.ShouldBe(1);
            currentPeriod.RandomHash.ShouldBe(Hash.Empty);

            // Transfer some money to Alice & Bob.
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = AliceAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });

            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = BobAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });

            await AliceTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = LotteryContractAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });

            await BobTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = LotteryContractAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });

            await LotteryContractStub.AddRewardList.SendAsync(new RewardList
            {
                RewardMap =
                {
                    {"啊", "一等奖"},
                    {"啊啊", "二等奖"},
                    {"啊啊啊", "三等奖"}
                }
            });
        }

        [Fact]
        public async Task PrepareDrawWithoutSelling()
        {
            await InitializeAndCheckStatus();

            await LotteryContractStub.SetRewardListForOnePeriod.SendAsync(new RewardsInfo
            {
                Period = 1,
                Rewards =
                {
                    {"啊", 1}
                }
            });
            var result = await LotteryContractStub.PrepareDraw.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Unable to prepare draw because not enough lottery sold.");
        }

        [Fact]
        public async Task BuyTest()
        {
            await InitializeAndCheckStatus();

            {
                var lotteries = await AliceBuy(20, 1);
                lotteries.Count.ShouldBe(20);
            }

            {
                var lotteries = await BobBuy(5, 1);
                lotteries.Count.ShouldBe(5);
            }
        }

        [Fact]
        public async Task PrepareDrawTest()
        {
            await BuyTest(); // buy 25 lotteries
            
            await LotteryContractStub.SetRewardListForOnePeriod.SendAsync(new RewardsInfo
            {
                Period = 1,
                Rewards =
                {
                    {"啊", 1},
                    {"啊啊", 2},
                    {"啊啊啊", 23}
                }
            });
            
            var prepare = await LotteryContractStub.PrepareDraw.SendWithExceptionAsync(new Empty());
            prepare.TransactionResult.Error.ShouldContain("Unable to prepare draw because not enough lottery sold.");
            
            await LotteryContractStub.SetRewardListForOnePeriod.SendAsync(new RewardsInfo
            {
                Period = 1,
                Rewards =
                {
                    {"啊", 1},
                    {"啊啊", 2},
                    {"啊啊啊", 21}
                }
            });
            await LotteryContractStub.PrepareDraw.SendAsync(new Empty());

            var currentPeriodNumber = await LotteryContractStub.GetCurrentPeriodNumber.CallAsync(new Empty());
            currentPeriodNumber.Value.ShouldBe(2);

            var period = await LotteryContractStub.GetPeriod.CallAsync(new Int64Value {Value = 2});
            period.RandomHash.ShouldBe(Hash.Empty);
            period.StartId.ShouldBe(26);

            // {
            //     var lotteries = await AliceBuy(1, 2);
            //     lotteries.Count.ShouldBe(1);
            // }
            //
            // {
            //     var lotteries = await BobBuy(1, 2);
            //     lotteries.Count.ShouldBe(1); // Only return 20 at one time.
            // }

            var result = await LotteryContractStub.PrepareDraw.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("hasn't drew.");
        }

        [Fact]
        public async Task DrawTest()
        {
            await PrepareDrawTest();

            await LotteryContractStub.Draw.SendAsync(new Int64Value {Value = 1});
            var period = await LotteryContractStub.GetPeriod.CallAsync(new Int64Value {Value = 1});
            period.RandomHash.ShouldNotBe(Hash.Empty);
            period.ActualDrawDate.ShouldNotBe(null);
            var rewardResult = await LotteryContractStub.GetRewardResult.CallAsync(new Int64Value
            {
                Value = 1
            });
            var reward =
                rewardResult.RewardLotteries.First(r => r.Owner == AliceAddress && !string.IsNullOrEmpty(r.RewardName));
            const string registrationInformation = "hiahiahia";
            await AliceLotteryContractStub.TakeReward.SendAsync(new TakeRewardInput
            {
                LotteryId = reward.Id,
                RegistrationInformation = registrationInformation
            });

            var lottery = await LotteryContractStub.GetLottery.CallAsync(new Int64Value {Value = reward.Id});
            lottery.RegistrationInformation.ShouldBe(registrationInformation);
        }

        [Fact]
        public async Task DrawTwiceTest()
        {
            await DrawTest(); // buy 25 lotteries

            {
                var draw = await LotteryContractStub.Draw.SendWithExceptionAsync(new Int64Value {Value = 1});
                draw.TransactionResult.Error.ShouldContain("Latest period already drawn.");
            }

            {
                var prepare = await LotteryContractStub.PrepareDraw.SendWithExceptionAsync(new Empty());
                prepare.TransactionResult.Error.ShouldContain("Reward pool cannot be empty.");
            }
            await LotteryContractStub.SetRewardListForOnePeriod.SendAsync(new RewardsInfo
            {
                Period = 2,
                Rewards =
                {
                    {"啊", 1},
                    {"啊啊", 2},
                }
            });

            // in period 2, 26th lottery
            await AliceLotteryContractStub.Buy.SendAsync(new Int64Value
            {
                Value = 1
            });
            
            {
                var prepare = await LotteryContractStub.PrepareDraw.SendWithExceptionAsync(new Empty());
                prepare.TransactionResult.Error.ShouldContain("Unable to prepare draw because not enough lottery sold.");
            }
            
            await LotteryContractStub.SetRewardListForOnePeriod.SendAsync(new RewardsInfo
            {
                Period = 2,
                Rewards =
                {
                    {"啊", 1}
                }
            });
            await LotteryContractStub.PrepareDraw.SendAsync(new Empty());

            //in period 3, 27th lottery
            await AliceLotteryContractStub.Buy.SendAsync(new Int64Value
            {
                Value = 1
            });
            
            var currentPeriodNumber = await LotteryContractStub.GetCurrentPeriodNumber.CallAsync(new Empty());
            currentPeriodNumber.Value.ShouldBe(3);
            
            {
                var period = await LotteryContractStub.GetPeriod.CallAsync(new Int64Value {Value = 3});
                period.RandomHash.ShouldBe(Hash.Empty);
                period.StartId.ShouldBe(27);
            }
            
            await LotteryContractStub.Draw.SendAsync(new Int64Value {Value = 2});

            {
                var period = await LotteryContractStub.GetPeriod.CallAsync(new Int64Value {Value = 2});
                period.RewardIds.Count.ShouldBe(1);

                var lotteryCount = await LotteryContractStub.GetAllLotteriesCount.CallAsync(new Empty());
                
                period.RewardIds.First().ShouldBe(26);
            }
            
            //in period 3, 28th, 29th lottery
            await AliceLotteryContractStub.Buy.SendAsync(new Int64Value
            {
                Value = 2
            });
            
            
            await LotteryContractStub.SetRewardListForOnePeriod.SendAsync(new RewardsInfo
            {
                Period = 3,
                Rewards =
                {
                    {"啊", 3}
                }
            });
            
            await LotteryContractStub.PrepareDraw.SendAsync(new Empty());
            await LotteryContractStub.Draw.SendAsync(new Int64Value {Value = 3});
            
            {
                var period = await LotteryContractStub.GetPeriod.CallAsync(new Int64Value {Value = 3});
                period.RewardIds.Count.ShouldBe(3);
                
                period.RewardIds.ShouldContain(27);
                period.RewardIds.ShouldContain(28);
                period.RewardIds.ShouldContain(29);
            }
        }
        

        private async Task<RepeatedField<Lottery>> AliceBuy(int amount, long period)
        {
            var boughtInformation = (await AliceLotteryContractStub.Buy.SendAsync(new Int64Value
            {
                Value = amount
            })).Output;

            boughtInformation.Amount.ShouldBe(amount);

            var boughtInfo = await LotteryContractStub.GetBoughtLotteries.CallAsync(new GetBoughtLotteriesInput
            {
                Owner = AliceAddress,
                Period = period
            });
            boughtInfo.Lotteries.First().Id.ShouldBe(boughtInformation.StartId);
            return boughtInfo.Lotteries;
        }

        private async Task<RepeatedField<Lottery>> BobBuy(int amount, long period)
        {
            var boughtInformation = (await BobLotteryContractStub.Buy.SendAsync(new Int64Value
            {
                Value = amount
            })).Output;

            boughtInformation.Amount.ShouldBe(amount);

            var boughtOutput = await LotteryContractStub.GetBoughtLotteries.CallAsync(new GetBoughtLotteriesInput
            {
                Owner = BobAddress,
                Period = period
            });
            boughtOutput.Lotteries.Last().Id.ShouldBe(boughtInformation.StartId + boughtInformation.Amount - 1);
            return boughtOutput.Lotteries;
        }

        [Fact]
        public void TestGetRanks()
        {
            var levelsCount = new List<int> {0, 1, 2, 3, 4};
            var ranks = GetRanks(levelsCount);
            ranks.Count.ShouldBe(10);
            ranks.ShouldBe(new List<int> {2, 3, 3, 4, 4, 4, 5, 5, 5, 5});
        }

        [Fact]
        public async Task StakingTest()
        {
            await LotteryContractStub.Initialize.SendAsync(new InitializeInput
            {
                TokenSymbol = "ELF",
                MaximumAmount = 100,
                Price = Price,
                DrawingLag = 1,
                StartTimestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                ShutdownTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10))
            });
            
            
            // Transfer some money to Alice & Bob.
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = AliceAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });
            
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = BobAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });
            
            await AliceTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = LotteryContractAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });

            await BobTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = LotteryContractAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });

            var registerDividend = await AliceLotteryContractStub.RegisterDividend.SendWithExceptionAsync(
                new RegisterDividendDto
                {
                    Receiver = "123"
                });
            registerDividend.TransactionResult.Error.ShouldContain("Not stake yet.");
            await AliceLotteryContractStub.Stake.SendAsync(new Int64Value {Value = 100_000_000});
            
            await AliceLotteryContractStub.RegisterDividend.SendAsync(
                new RegisterDividendDto
                {
                    Receiver = "123"
                });

            var receiver = (await AliceLotteryContractStub.GetRegisteredDividend.CallAsync(AliceAddress)).Receiver;
            receiver.ShouldBe("123");

            {
                var stakingAmount = await AliceLotteryContractStub.GetStakingAmount.CallAsync(AliceAddress);
                stakingAmount.Value.ShouldBe(100_000_000);
            }

            {
                var stakingTotal = await AliceLotteryContractStub.GetStakingTotal.CallAsync(new Empty());
                stakingTotal.Value.ShouldBe(100_000_000);
            }

            await BobLotteryContractStub.Stake.SendAsync(new Int64Value {Value = 1000_000_000});
            {
                var stakingAmount = await AliceLotteryContractStub.GetStakingAmount.CallAsync(BobAddress);
                stakingAmount.Value.ShouldBe(1000_000_000);
            }
            
            {
                var stakingTotal = await AliceLotteryContractStub.GetStakingTotal.CallAsync(new Empty());
                stakingTotal.Value.ShouldBe(1100_000_000);
            }
        }

        [Fact]
        public async Task StakingShutdownTimestampTest()
        {
            await LotteryContractStub.Initialize.SendAsync(new InitializeInput
            {
                TokenSymbol = "ELF",
                MaximumAmount = 100,
                Price = Price,
                DrawingLag = 1,
                StartTimestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                ShutdownTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10))
            });

            // Transfer some money to Alice & Bob.
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = AliceAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });
            
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = BobAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });
            
            await AliceTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = LotteryContractAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });

            await BobTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = LotteryContractAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });
            
            {
                var setStakingShutdown = await AliceLotteryContractStub.SetStakingTimestamp.SendWithExceptionAsync(
                    new SetStakingTimestampInput
                        {Timestamp = Timestamp.FromDateTime(DateTime.UtcNow), IsStartTimestamp = false});
                setStakingShutdown.TransactionResult.Error.ShouldContain("No permission.");
            }
            

            {
                var setStakingShutdown =
                    await LotteryContractStub.SetStakingTimestamp.SendWithExceptionAsync(
                        new SetStakingTimestampInput
                            {Timestamp = Timestamp.FromDateTime(DateTime.UtcNow), IsStartTimestamp = false});
                setStakingShutdown.TransactionResult.Error.ShouldContain("Invalid shutdown timestamp.");
            }

            {
                var time = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10));
                await LotteryContractStub.SetStakingTimestamp.SendAsync(new SetStakingTimestampInput
                    {Timestamp = time, IsStartTimestamp = false});
                var shutdownTimestamp = await LotteryContractStub.GetStakingTimestamp.CallAsync(new Empty());
                shutdownTimestamp.ShutdownTimestamp.ShouldBe(time);
            }
            
            {
                var setStakingShutdown =
                    await LotteryContractStub.SetStakingTimestamp.SendWithExceptionAsync(
                        new SetStakingTimestampInput
                            {Timestamp = Timestamp.FromDateTime(DateTime.UtcNow), IsStartTimestamp = false});
                setStakingShutdown.TransactionResult.Error.ShouldContain("Invalid shutdown timestamp.");
            }
            
            await AliceLotteryContractStub.Stake.SendAsync(new Int64Value {Value = 1000});

            GetRequiredService<IBlockTimeProvider>().SetBlockTime(Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10)));
            
            {
                var stake = await AliceLotteryContractStub.Stake.SendWithExceptionAsync(new Int64Value {Value = 1000});
                stake.TransactionResult.Error.ShouldContain("Staking shutdown.");
            }
            
            {
                var time = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10));
                await LotteryContractStub.SetStakingTimestamp.SendAsync(new SetStakingTimestampInput
                    {Timestamp = time, IsStartTimestamp = false});
                var shutdownTimestamp = await LotteryContractStub.GetStakingTimestamp.CallAsync(new Empty());
                shutdownTimestamp.ShutdownTimestamp.ShouldBe(time);
            }
            
            GetRequiredService<IBlockTimeProvider>().SetBlockTime(Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(1)));
            await AliceLotteryContractStub.Stake.SendAsync(new Int64Value {Value = 1000});
        }
        
        [Fact]
        public async Task StakingStartTimestampTest()
        {
            await LotteryContractStub.Initialize.SendAsync(new InitializeInput
            {
                TokenSymbol = "ELF",
                MaximumAmount = 100,
                Price = Price,
                DrawingLag = 1,
                StartTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(5)),
                ShutdownTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10))
            });

            // Transfer some money to Alice & Bob.
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = AliceAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });
            
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = BobAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });
            
            await AliceTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = LotteryContractAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });

            await BobTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = LotteryContractAddress,
                Symbol = "ELF",
                Amount = 100000000_00000000
            });

            {
                var stake = await AliceLotteryContractStub.Stake.SendWithExceptionAsync(new Int64Value {Value = 1000});
                stake.TransactionResult.Error.ShouldContain("Staking shutdown.");
            }
            
            
            {
                var setStakingShutdown = await AliceLotteryContractStub.SetStakingTimestamp.SendWithExceptionAsync(
                    new SetStakingTimestampInput
                        {Timestamp = Timestamp.FromDateTime(DateTime.UtcNow), IsStartTimestamp = true});
                setStakingShutdown.TransactionResult.Error.ShouldContain("No permission.");
            }
            

            {
                var setStakingShutdown =
                    await LotteryContractStub.SetStakingTimestamp.SendWithExceptionAsync(
                        new SetStakingTimestampInput
                            {Timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(6)), IsStartTimestamp = true});
                setStakingShutdown.TransactionResult.Error.ShouldContain("Invalid start timestamp.");
            }

            {
                var time = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(1));
                await LotteryContractStub.SetStakingTimestamp.SendAsync(new SetStakingTimestampInput
                    {Timestamp = time, IsStartTimestamp = true});
                var shutdownTimestamp = await LotteryContractStub.GetStakingTimestamp.CallAsync(new Empty());
                shutdownTimestamp.StartTimestamp.ShouldBe(time);
            }
            
            {
                var setStakingShutdown =
                    await LotteryContractStub.SetStakingTimestamp.SendWithExceptionAsync(
                        new SetStakingTimestampInput
                            {Timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(2)), IsStartTimestamp = true});
                setStakingShutdown.TransactionResult.Error.ShouldContain("Invalid start timestamp.");
            }
            
            {
                var stake = await AliceLotteryContractStub.Stake.SendWithExceptionAsync(new Int64Value {Value = 1000});
                stake.TransactionResult.Error.ShouldContain("Staking shutdown.");
            }
            GetRequiredService<IBlockTimeProvider>().SetBlockTime(Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(2)));
            await AliceLotteryContractStub.Stake.SendAsync(new Int64Value {Value = 1000});

            {
                var time = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(-1));
                await LotteryContractStub.SetStakingTimestamp.SendAsync(new SetStakingTimestampInput
                    {Timestamp = time, IsStartTimestamp = true});
                var shutdownTimestamp = await LotteryContractStub.GetStakingTimestamp.CallAsync(new Empty());
                shutdownTimestamp.StartTimestamp.ShouldBe(time);
            }
        }

        private List<int> GetRanks(List<int> levelsCount)
        {
            var ranks = new List<int>();

            for (var i = 0; i < levelsCount.Count; i++)
            {
                for (var j = 0; j < levelsCount[i]; j++)
                {
                    ranks.Add(i + 1);
                }
            }

            return ranks;
        }
    }
}