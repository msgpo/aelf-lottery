import React from 'react';
import {
  createStackNavigator,
  CardStyleInterpolators,
} from '@react-navigation/stack';
import {NavigationContainer} from '@react-navigation/native';

import Referral from '../template/Referral';
//Security Lock
import SecurityLock from '../template/SecurityLock';
import Tab from './Tab';
import navigationService from '../../utils/common/navigationService';

import LoginNav from '../template/Login/stackNav';
import MineNav from '../template/Mine/stackNav';
import TermsNav from '../template/Terms/stackNav';
import HomeNav from '../template/Home/stackNav';
import DrawNav from '../template/Draw/stackNav';
import LeaderBoardNav from '../template/LeaderBoard/stackNav';
const Stack = createStackNavigator();

const stackNav = [
  {name: 'Referral', component: Referral},
  {
    name: 'SecurityLock',
    component: SecurityLock,
    options: {
      gestureEnabled: false,
    },
  },
  {name: 'Tab', component: Tab},
  ...HomeNav,
  ...TermsNav,
  ...LoginNav,
  ...MineNav,
  ...LeaderBoardNav,
  ...DrawNav,
];
const NavigationMain = () => (
  <NavigationContainer ref={navigationService.setTopLevelNavigator}>
    <Stack.Navigator
      initialRouteName="Referral"
      screenOptions={{
        cardStyleInterpolator: CardStyleInterpolators.forHorizontalIOS,
        header: () => null,
      }}>
      {stackNav.map((item, index) => (
        <Stack.Screen key={index} {...item} />
      ))}
    </Stack.Navigator>
  </NavigationContainer>
);
export default NavigationMain;