using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cmd;

namespace  AgingSystem
{
    public class AgingStatusMetrix
    {
        private static Hashtable m_StatusMetrix = new Hashtable();
        private static AgingStatusMetrix m_object = null;

        public static AgingStatusMetrix Instance()
        {
            if (m_object == null)
                m_object = new AgingStatusMetrix();
            return m_object;
        }

        private AgingStatusMetrix()
        {
            Init();
        }

        private void Init()
        {
            m_StatusMetrix.Clear();
            //m_StatusMetrix.Add(EAgingStatus.Unknown      , "未知状态");
            //m_StatusMetrix.Add(EAgingStatus.Waiting      , "等待接入");
            //m_StatusMetrix.Add(EAgingStatus.PowerOn      , "待机中");
            //m_StatusMetrix.Add(EAgingStatus.Charging     , "老化充电中");
            //m_StatusMetrix.Add(EAgingStatus.DisCharging  , "老化放电中");
            //m_StatusMetrix.Add(EAgingStatus.Recharging   , "老化补电中");
            //m_StatusMetrix.Add(EAgingStatus.AgingComplete, "老化结束");
            m_StatusMetrix.Add(EAgingStatus.Unknown      , "未知状态");
            m_StatusMetrix.Add(EAgingStatus.Waiting      , "等待接入");
            m_StatusMetrix.Add(EAgingStatus.PowerOn      , "待机中");
            m_StatusMetrix.Add(EAgingStatus.Charging     , "老化中");
            m_StatusMetrix.Add(EAgingStatus.DisCharging  , "老化中");
            m_StatusMetrix.Add(EAgingStatus.Recharging   , "老化中");
            m_StatusMetrix.Add(EAgingStatus.AgingComplete, "老化结束");
            m_StatusMetrix.Add(EAgingStatus.Alarm,         "异常报警");
        }

        public string GetAgingStatus(EAgingStatus status)
        {
            return (string)m_StatusMetrix[status];
        }
    }

   
}
