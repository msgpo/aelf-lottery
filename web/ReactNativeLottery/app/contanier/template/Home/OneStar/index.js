import React, {memo, useState, useCallback} from 'react';
import {View, StyleSheet} from 'react-native';
import {CommonHeader} from '../../../../components/template';
import {GStyle, Colors} from '../../../../assets/theme';
import {TextL} from '../../../../components/template/CommonText';
import {pTd} from '../../../../utils/common';
import lotteryUtils from '../../../../utils/pages/lotteryUtils';
import BetBody from '../BetBody';
import ConfirmModal from '../ConfirmModal';
import {LOTTERY_TYPE} from '../../../../config/lotteryConstant';
import {useStateToProps} from '../../../../utils/pages/hooks';
import i18n from 'i18n-js';
const lotteryType = LOTTERY_TYPE.ONE_BIT;
const OneStar = () => {
  const [data] = useState([
    {
      title: i18n.t('lottery.onesPlace'),
      playList: ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'],
    },
  ]);
  const [betList, setBetList] = useState([]);
  const [multiplied, setMultiplied] = useState(1);
  const {lotteryRewards} = useStateToProps(base => {
    const {lottery} = base;
    return {
      lotteryRewards: lottery.lotteryRewards,
    };
  });
  const bonusAmount = lotteryRewards?.[lotteryType] ?? 1;
  const onSelect = useCallback(
    (first, second) => {
      setBetList(lotteryUtils.processingNumber(betList, first, second));
    },
    [betList],
  );
  const onBet = useCallback(() => {
    ConfirmModal.show({
      title: `${i18n.t('lottery.oneStar')}${i18n.t('lottery.directElection')}`,
      data,
      betList,
      lotteryType,
      multiplied,
    });
  }, [betList, data, multiplied]);
  const onTool = useCallback(
    (first, type) => {
      const list = lotteryUtils.processingTool(data, betList, first, type);
      list && setBetList(list);
    },
    [betList, data],
  );
  return (
    <View style={GStyle.container}>
      <CommonHeader title={i18n.t('lottery.oneStar')} canBack>
        <View style={styles.titleBox}>
          <TextL style={styles.titleStyle}>
            {i18n.t('lottery.directElection')}
          </TextL>
        </View>
        <TextL style={styles.tipStyle}>
          {i18n.t('lottery.oneStarTip')}
          {bonusAmount}
          {i18n.t('lottery.unit')}
        </TextL>
        <BetBody
          data={data}
          onBet={onBet}
          onTool={onTool}
          betList={betList}
          onSelect={onSelect}
          multiplied={multiplied}
          bonusAmount={bonusAmount}
          setMultiplied={setMultiplied}
          onClear={() => {
            setBetList([]);
            setMultiplied(1);
          }}
        />
      </CommonHeader>
    </View>
  );
};
export default memo(OneStar);

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