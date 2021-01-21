using System.Net.Sockets;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Standards.ACS1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.LotteryContract
{
    public class LotteryContractState : ContractState
    {
        // ReSharper disable once InconsistentNaming
        internal AEDPoSContractContainer.AEDPoSContractReferenceState AEDPoSContract { get; set; }
        internal TokenContractImplContainer.TokenContractImplReferenceState TokenContract { get; set; }
        
        public StringState TokenSymbol { get; set; }
        
        public SingletonState<long> Price { get; set; }
        
        public SingletonState<long> MaximumAmount { get; set; }
        
        public SingletonState<long> DrawingLag { get; set; }

        public SingletonState<Address> Admin { get; set; }

        public SingletonState<long> CurrentPeriod { get; set; }
        
        public SingletonState<long> SelfIncreasingIdForLottery { get; set; }
        
        public MappedState<long, PeriodBody> Periods { get; set; }
        
        public MappedState<long, Lottery> Lotteries { get; set; }

        public MappedState<Address, long, LotteryList> OwnerToLotteries { get; set; }
        
        public MappedState<string, string> RewardMap { get; set; }

        public SingletonState<StringList> RewardCodeList { get; set; }

        public MappedState<Address, long> BoughtLotteriesCount { get; set; }
        
        public MappedState<Address, long> Staking { get; set; }
        
        public Int64State StakingTotal { get; set; }

        public BoolState IsSuspend { get; set; }

        public MappedState<Address, RegisterDividendDto> Dividends { get; set; }
        
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
        
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        
        public SingletonState<Timestamp> StakingStartTimestamp { get; set; }
        public SingletonState<Timestamp> StakingShutdownTimestamp { get; set; }
    }
}