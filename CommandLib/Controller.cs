using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using AsyncSocket;

namespace Cmd
{
    public class Controller
    {
        private long            m_IP;
        private int             m_DockNo;
        private int             m_RowNo;
        private DateTime        m_BeginAginTime                  = DateTime.MinValue;
        private DateTime        m_BeginDischargeTime             = DateTime.MinValue;
        //private Socket          m_SocketToken                  = null;
        private AsyncSocketUserToken m_SocketToken = null;       //20170312, Socket由AsyncSocketUserToken替代
        private DateTime        m_SocketConnectTimestamp         = DateTime.MinValue;        //记录下控制器的连入时间
        private List<AgingPump> m_AgingPumpList                  = new List<AgingPump>();    //正在老化的泵列表,每个控制器至多有8个泵，根据老化报警信息，初始化这个列表
        //private string          m_AgingStatus                  = string.Empty;
        private EAgingStatus    m_AgingStatus                    = EAgingStatus.Unknown;
        //private EAgingStatus    m_RedAlarmStatus               = EAgingStatus.Unknown;  //控制器下属的泵中有红色报警
        private bool            m_IsRunning                      = false; //控制器下的所有泵是否全部运行中，true:running,false:stop

        /// <summary>
        /// 控制器的连入时间
        /// </summary>
        public DateTime SocketConnectTimestamp
        {
            get { return m_SocketConnectTimestamp; }
            set { m_SocketConnectTimestamp = value; }
        }

        /// <summary>
        /// 老化状态,在开始时先寄存在这里
        /// </summary>
        public EAgingStatus AgingStatus
        {
            get { return m_AgingStatus; }
            set { m_AgingStatus = value; }

        }

        /// <summary>
        /// 控制器下的所有泵是否全部运行中
        /// </summary>
        public bool IsRunning
        {
            get { return m_IsRunning; }
            set { m_IsRunning = value; }
        }


        /// <summary>
        /// 红色报警
        /// </summary>
        //public EAgingStatus RedAlarmStatus
        //{
        //    get { return m_RedAlarmStatus; }
        //    set { m_RedAlarmStatus = value; }

        //}

        /// <summary>
        /// 老化开始时间，在开始时先寄存在这里。
        /// </summary>
        public DateTime BeginAginTime
        {
            get { return m_BeginAginTime; }
            set { m_BeginAginTime = value; }
        }
        /// <summary>
        /// 老化放电时间，在开始时先寄存在这里。
        /// </summary>
        public DateTime BeginDischargeTime
        {
            get { return m_BeginDischargeTime; }
            set { m_BeginDischargeTime = value; }
        }
        /// <summary>
        /// 货架编号(1~N)
        /// </summary>
        public int DockNo
        {
            get { return m_DockNo; }
            set { m_DockNo = value; }
        }

        /// <summary>
        /// WIFI模块所在货架的行号（1~5）
        /// </summary>
        public int RowNo
        {
            get { return m_RowNo; }
            set { m_RowNo = value; }
        }

        /// <summary>
        /// WIFI模块IP
        /// </summary>
        public long IP
        {
            get { return m_IP; }
            set { m_IP = value; }
        }

        /// <summary>
        /// WIFI模块连接到ASAD的Socket
        /// </summary>
        //public Socket SocketToken
        //{
        //    get { return m_SocketToken; }
        //    set { m_SocketToken = value; }
        //}

        public AsyncSocketUserToken SocketToken
        {
            get { return m_SocketToken; }
            set { m_SocketToken = value; }
        }

        /// <summary>
        /// 正在老化的泵列表，这个列表与用户在界面上勾选的无关，从报警信息中抽取信息，填充的
        /// </summary>
        public List<AgingPump> AgingPumpList
        {
            get { return m_AgingPumpList;}
            set { m_AgingPumpList = value;}
        }

        /// <summary>
        /// 控制器的IP，所在货架编号，所在货架行号，以及它的连接SOCKET
        /// </summary>
        /// <param name="ip">控制器的IP</param>
        /// <param name="dockNo">所在货架编号</param>
        /// <param name="rowNo">所在货架行号</param>
        /// <param name="token">连接SOCKET</param>
        public Controller(long ip, int dockNo, int rowNo, AsyncSocketUserToken token = null)
        {
            m_IP = ip;
            m_DockNo = dockNo;
            m_RowNo = rowNo;
            m_SocketToken = token;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip">控制器的I</param>
        /// <param name="dockNo">所在货架编号</param>
        /// <param name="rowNo">所在货架行号</param>
        /// <param name="pumpList">正在老化的信息列表</param>
        /// <param name="token">连接SOCKET</param>
        public Controller(long ip, int dockNo, int rowNo, List<AgingPump> pumpList, AsyncSocketUserToken token = null)
        {
            m_IP            = ip;
            m_DockNo        = dockNo;
            m_RowNo         = rowNo;
            m_SocketToken   = token;
            m_AgingPumpList = pumpList;
        }

        /// <summary>
        /// 通过货架号，行号和通道号可以唯一定位一个正在老化的泵
        /// </summary>
        /// <param name="dockNo"></param>
        /// <param name="rowNo"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public AgingPump FindPump(int dockNo, int rowNo, byte channel)
        {
            AgingPump pump = m_AgingPumpList.Find((x)=>{return x.DockNo==dockNo && x.RowNo==rowNo && x.Channel==channel;});
            return pump;
        }

        public List<AgingPump> SortAgingPumpList()
        {
            return this.AgingPumpList.OrderBy(x=>x.Channel).ToList<AgingPump>();
        }

    }
   
}
