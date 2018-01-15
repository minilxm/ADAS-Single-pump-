using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Net.Sockets;
using AsyncSocket;
using Cmd;

namespace Analyse
{
    public class SendOrReceiveBytesArgs : EventArgs
    {
        private bool       m_IsSend  = true;
        private List<byte> m_Buffer  = new List<byte>();

        /// <summary>
        /// 发送或接受的字节流
        /// </summary>
        public  List<byte> Buffer
        {
            get {return m_Buffer;}
            set {m_Buffer = value;}
        }

        public bool IsSend
        {
            get {return m_IsSend;}
            set {m_IsSend = value;}
        }

        public SendOrReceiveBytesArgs()
        {
        }

        public SendOrReceiveBytesArgs(List<byte> buffer, bool bSend=true)
        {
            byte []arrBuf = new byte[buffer.Count];
            buffer.CopyTo(arrBuf);
            m_Buffer.AddRange(arrBuf);
            m_IsSend = bSend;
        }

        public SendOrReceiveBytesArgs(byte[] buffer, bool bSend=true)
        {
            m_IsSend = bSend;
            m_Buffer.Clear();
            m_Buffer.AddRange(buffer);
        }

    }

    public class TimeOutArgs : EventArgs
    {
        private BaseCommand  m_Cmd = null;

        public BaseCommand Cmd
        {
            get { return m_Cmd; }
            set { m_Cmd = value; }
        }

        public TimeOutArgs()
        {
        }

        public TimeOutArgs(BaseCommand cmd)
        {
            m_Cmd = cmd;
        }

    }

    public class ParameterArgs : EventArgs
    {
        public string m_PumpType;
        public string m_Rate;
        public string m_Volume;
        public string m_ChargeTime;
        public string m_DischargeTime;
        public string m_RechargeTime;
        public string m_OccLevel;
         
        public ParameterArgs()
        {
        }

        public ParameterArgs(string pumpType,
                             string rate,
                             string volume,
                             string chargeTime,
                             //string dischargeTime,
                             string rechargeTime,
                             string occLevel
            )
        {
            m_PumpType      =       pumpType;
            m_Rate          =           rate;
            m_Volume        =         volume;
            m_ChargeTime    =     chargeTime;
            //m_DischargeTime =  dischargeTime;
            m_RechargeTime  =   rechargeTime;
            m_OccLevel      = occLevel;
        }

    }


    public class ConfigrationArgs : EventArgs
    {
        private Hashtable m_DockParameter = new Hashtable();    //存放每个货架的配置信息（int 货架号，DefaultParameter）
        public Hashtable DockParameter
        {
            get { return m_DockParameter;}
        }
        
        public ConfigrationArgs()
        {
        }

        public ConfigrationArgs(Hashtable dockParameter)
        {
           m_DockParameter = dockParameter;
        }
    }

    /// <summary>
    /// 连接到ADAS货架上的泵列表
    /// </summary>
    public class SelectedPumpsArgs : EventArgs
    {
        private Hashtable m_SelectedPumps = new Hashtable();    //存放每个货架的泵位置信息（int 货架号，List<Tuple<int,int,int>>(int pumpLocation,int rowNo,int colNo）)
        public Hashtable SelectedPumps
        {
            get { return m_SelectedPumps; }
        }

        public SelectedPumpsArgs()
        {
        }

        public SelectedPumpsArgs(Hashtable selectedPumps)
        {
            m_SelectedPumps = selectedPumps;
        }
    }

    /// <summary>
    /// 新连接的SOCKET还是刚刚关闭的SOCKET
    /// </summary>
    public class SocketConnectArgs : EventArgs
    {
        private bool m_Connected = true;
        //private Socket m_ConnectedSocket;
        private AsyncSocketUserToken m_ConnectedSocket;

        public bool Connected
        {
            get { return m_Connected; }
        }

        //public Socket ConnectedSocket
        //{
        //    get { return m_ConnectedSocket; }
        //}
        public AsyncSocketUserToken ConnectedSocket
        {
            get { return m_ConnectedSocket; }
        }

        public SocketConnectArgs()
        {
        }

        //public SocketConnectArgs(bool connected, Socket connectedSocket)
        //{
        //    m_Connected = connected;
        //    m_ConnectedSocket = connectedSocket;
        //}
        public SocketConnectArgs(bool connected, AsyncSocketUserToken connectedSocket)
        {
            m_Connected = connected;
            m_ConnectedSocket = connectedSocket;
        }
    }



}
