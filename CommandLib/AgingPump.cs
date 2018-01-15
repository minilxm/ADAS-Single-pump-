using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cmd
{
    /// <summary>
    /// 老化泵类，主要存放正在老化的泵信息
    /// </summary>
    public class AgingPump
    {
        private int       m_DockNo;
        private int       m_RowNo;
        private byte      m_Channel;                                             //这个泵的通道号，由报警信息上传得到
        private string    m_PumpType                = string.Empty;              //机器型号
        private EAgingStatus    m_AgingStatus       = EAgingStatus.Unknown;      //当前老化状态（含红色报警）
        private EAgingStatus    m_RedAlarmStatus    = EAgingStatus.Unknown;      //有红色报警
        private DateTime  m_BeginAgingTime          = DateTime.MinValue;         //老化开始时间
        private DateTime  m_EndAgingTime            = DateTime.MinValue;         //老化结束时间
        private DateTime  m_BeginDischargeTime      = DateTime.MinValue;         //放电开始时间
        private DateTime  m_BeginLowVoltageTime     = DateTime.MinValue;         //低电报警开始时间
        private DateTime  m_BeginBattaryDepleteTime = DateTime.MinValue;         //电池耗尽报警开始时间
        private string    m_TotalAgingTime          = string.Empty;              //老化总时长（单位：小时）
        private string    m_TotalDischargeTime      = string.Empty;              //放电总时长
        private string    m_TotalBattaryDepleteTime = string.Empty;              //耗尽总时长
        private string    m_AgingResult             = string.Empty;              //老化结果
        private uint      m_Alarm                   = 0;                         //报警信息，每一位代表一个报警
        private int       m_LostTimes               = 0;                         //失联次数，当发生某种不可预测的打入原因，泵端在老化的中间阶段一直没反应，每次上报信息时都会记录它，达到5次以上者，认为不无效的泵

        
        private DateTime m_BeginRechargeTime        = DateTime.MinValue;         //补电开始时间,20170709,补电操作由补电线程来完成，控制器正常情况下收到命令后会进行补电操作，控制器命令返回时记录补电时间

        /// <summary>
        /// 失联次数
        /// </summary>
        public int LostTimes
        {
            get { return m_LostTimes; }
            set { m_LostTimes = value; }
        }
        /// <summary>
        /// 机架
        /// </summary>
        public int DockNo
        {
            get { return m_DockNo; }
            set { m_DockNo = value; }
        }
        /// <summary>
        /// 所在行号
        /// </summary>
        public int RowNo
        {
            get { return m_RowNo; }
            set { m_RowNo = value; }
        }
        /// <summary>
        /// 所在列号
        /// </summary>
        public byte Channel
        {
            get { return m_Channel; }
            set { m_Channel = value; }
        }
        /// <summary>
        /// 机器型号
        /// </summary>
        public string PumpType
        {
            get { return m_PumpType; }
            set { m_PumpType = value; }
        }
        /// <summary>
        /// 老化状态
        /// </summary>
        public EAgingStatus AgingStatus
        {
            get { return m_AgingStatus; }
            set { m_AgingStatus = value; }
        }
        /// <summary>
        /// 红色状态
        /// </summary>
        public EAgingStatus RedAlarmStatus
        {
            get { return m_RedAlarmStatus; }
            set { m_RedAlarmStatus = value; }
        }
        /// <summary>
        /// 老化开始时间
        /// </summary>
        public DateTime BeginAgingTime
        {
            get { return m_BeginAgingTime; }
            set { m_BeginAgingTime = value; }
        }
        /// <summary>
        /// 老化结束时间
        /// </summary>
        public DateTime EndAgingTime
        {
            get { return m_EndAgingTime; }
            set { m_EndAgingTime = value; }
        }
        /// <summary>
        /// 放电开始时间
        /// </summary>
         public DateTime BeginDischargeTime
        {
            get { return m_BeginDischargeTime; }
            set { m_BeginDischargeTime = value; }
        }
        /// <summary>
        /// 低电报警开始时间
        /// </summary>
         public DateTime BeginLowVoltageTime
        {
            get { return m_BeginLowVoltageTime; }
            set { m_BeginLowVoltageTime = value; }
        }
        /// <summary>
        /// 电池耗尽报警开始时间
        /// </summary>
        public DateTime BeginBattaryDepleteTime
        {
            get { return m_BeginBattaryDepleteTime; }
            set { m_BeginBattaryDepleteTime = value; }
        }

        /// <summary>
        /// 补电开始时间
        /// </summary>
        public DateTime BeginRechargeTime
        {
            get { return m_BeginRechargeTime; }
            set { m_BeginRechargeTime = value; }
        }

        /// <summary>
        /// 老化总时长（单位：小时）
        /// </summary>
        public string TotalAgingTime
        {
            get { return m_TotalAgingTime; }
            set { m_TotalAgingTime = value; }
        }
        /// <summary>
        /// 放电总时长
        /// </summary>
        public string TotalDischargeTime
        {
            get { return m_TotalDischargeTime; }
            set { m_TotalDischargeTime = value; }
        }
        /// <summary>
        /// 耗尽总时长
        /// </summary>
        public string TotalBattaryDepleteTime
        {
            get { return m_TotalBattaryDepleteTime; }
            set { m_TotalBattaryDepleteTime = value; }
        }
        /// <summary>
        /// 老化结果
        /// </summary>
        public string AgingResult
        {
            get { return m_AgingResult; }
            set { m_AgingResult = value; }
        }
        ///// <summary>
        ///// 报警信息列表
        ///// </summary>
        //public Hashtable PumpAlarmMetrix
        //{
        //    get { return m_AlarmMetrix; }
        //    set { m_AlarmMetrix = value; }
        //}

        /// <summary>
        /// 报警信息
        /// </summary>
        public uint Alarm
        {
            get { return m_Alarm; }
            set { m_Alarm = value; }
        }

        public string GetAlarmString()
        {
            //Cmd.AlarmMetrix.Instance().
             ProductID pid  = ProductID.Unknow;
            if(Enum.IsDefined(typeof(ProductID), m_PumpType))
                pid = (ProductID)Enum.Parse(typeof(ProductID), m_PumpType);
            else
            {
                Logger.Instance().ErrorFormat("泵类型转换出错，不支持的类型 PumpType ={0}",m_PumpType);
                return string.Empty;
            }
            Hashtable alarmMetrix = null;
            #region //查询所属报警的映射表
            switch (pid)
            {
                case ProductID.GrasebyC6:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC6;
                    break;
                case ProductID.GrasebyC6T:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC6T;
                    break;
                case ProductID.GrasebyC8:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC8;
                    break;
                case ProductID.GrasebyF6:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixF6;
                    break;
                case ProductID.GrasebyF8://C8和F8是一样
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC8;
                    break;
                case ProductID.Graseby1200:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrix1200;
                    break;
                case ProductID.Graseby1200En:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrix1200En;
                    break;
                case ProductID.Graseby2000:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrix2000;
                    break;
                case ProductID.Graseby2100:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrix2100;
                    break;
                default:
                    break;
            }
            #endregion
            StringBuilder sb = new StringBuilder();
            int iCount = 0;
            foreach(DictionaryEntry dic in alarmMetrix)
            {
                uint alarmID = (uint)dic.Key;
                if((alarmID & m_Alarm)==alarmID)
                {
                    sb.Append(dic.Value);
                    sb.Append(";");
                    ++iCount;
                    if(iCount%2==0)
                        sb.Append("\n");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 判断是否通过的标准就是除了低电和耗尽，其他的报警都算不通过
        /// </summary>
        /// <returns></returns>
        public bool IsPass()
        {
            ProductID pid  = ProductID.Unknow;
            if(Enum.IsDefined(typeof(ProductID), m_PumpType))
                pid = (ProductID)Enum.Parse(typeof(ProductID), m_PumpType);
            else
            {
                Logger.Instance().ErrorFormat("泵类型转换出错，不支持的类型 PumpType ={0}",m_PumpType);
                return false;
            }
            Hashtable alarmMetrix = null;
            uint depletealArmIndex = 0, lowVolArmIndex = 0;//耗尽和低电压索引
            #region //查询耗尽报警索引
            switch (pid)
            {
                case ProductID.GrasebyC6:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC6;
                    depletealArmIndex = 0x00000008;
                    lowVolArmIndex = 0x00000100;
                    break;
                case ProductID.GrasebyC6T:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC6T;
                    depletealArmIndex = 0x00000008;
                    lowVolArmIndex = 0x00000100;
                    break;
                case ProductID.GrasebyC8:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC8;
                    depletealArmIndex = 0x00010000;
                    lowVolArmIndex = 0x00000001;
                    break;
                case ProductID.GrasebyF6:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixF6;
                    depletealArmIndex = 0x00000008;
                    lowVolArmIndex = 0x00000100;
                    break;
                case ProductID.GrasebyF8://C8和F8是一样
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC8;
                    depletealArmIndex = 0x00010000;
                    lowVolArmIndex = 0x00000001;
                    break;
                case ProductID.Graseby1200:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrix1200;
                    depletealArmIndex = 0x00000010;
                    lowVolArmIndex = 0x00004000;
                    break;
                case ProductID.Graseby1200En:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrix1200En;
                    depletealArmIndex = 0x00000020;
                    lowVolArmIndex = 0x00008000;
                    break;
                case ProductID.Graseby2000:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrix2000;
                    depletealArmIndex = 0x00000008;
                    lowVolArmIndex = 0x00000100;
                    break;
                case ProductID.Graseby2100:
                    alarmMetrix = AlarmMetrix.Instance().AlarmMetrix2100;
                    depletealArmIndex = 0x00000008;
                    lowVolArmIndex = 0x00000100;
                    break;
                default:
                    break;
            }
                #endregion

            uint filterAlarm = m_Alarm & (~(depletealArmIndex | lowVolArmIndex));
            if(filterAlarm>0)
                return false;
            else
                return true;
        }
    }
}
