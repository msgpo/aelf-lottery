import React, {memo, useState, useCallback} from 'react';
import {View, StyleSheet} from 'react-native';
import {GStyle, Colors} from '../../../../../assets/theme';
import {TextL} from '../../../../../components/template/CommonText';
import {pTd} from '../../../../../utils/common';
import lotteryUtils from '../../../../../utils/pages/lotteryUtils';
import BetBody from '../../BetBody';
import ConfirmModal from '../../ConfirmModal';
import {LOTTERY_TYPE} from '../../../../../config/lotteryConstant';
import {useStateToProps} from '../../../../../utils/pages/hooks';
import i18n from 'i18n-js';
const lotteryType = LOTTERY_TYPE.TWO_BIT;
const tens = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
const TwoStars = () => {
  const [data] = useState([
    {title: i18n.t('lottery.tenPlace'), playList: tens},
    {title: i18n.t('lottery.onesPlace'), playList: tens},
  ]);
  const [betList, setBetList] = useState([]);
  const {lotteryRewards} = useStateToProps(base => {
    const {lottery} = base;
    return {
      lotteryRewards: lottery.lotteryRewards,
    };
  });
  const bonusAmount = lotteryRewards ? lotteryRewards[lotteryType] : 0;
  const onSelect = useCallback(
    (first, second) => {
      setBetList(lotteryUtils.processingNumber(betList, first, second));
    },
    [betList],
  );
  const onBet = useCallback(() => {
    ConfirmModal.show({
      title: `${i18n.t('lottery.twoStars')}${i18n.t('lottery.directElection')}`,
      data,
      betList,
      lotteryType,
    });
  }, [betList, data]);
  const onTool = useCallback(
    (first, type) => {
      const list = lotteryUtils.processingTool(data, betList, first, type);
      list && setBetList(list);
    },
    [betList, data],
  );
  return (
    <View style={GStyle.container}>
      <TextL style={styles.tipStyle}>
        {i18n.t('lottery.twoStarsTip')}
        {bonusAmount}
        {i18n.t('lottery.unit')}
      </TextL>
      <BetBody
        onTool={onTool}
        betList={betList}
        data={data}
        onBet={onBet}
        onClear={() => setBetList([])}
        bonusAmount={bonusAmount}
        onSelect={onSelect}
      />
    </View>
  );
};
export default memo(TwoStars);

const styles = StyleSheet.create({
  tipStyle: {
    padding: pTd(20),
    color: Colors.fontColor,
  },
  titleBox: {
    paddingVertical: pTd(20),
    alignItems: 'center',
    borderBottomWidth: 1,
    borderBottomColor: Colors.borderColor,
  },
  titleStyle: {
    color: Colors.fontColor,
  },
});
