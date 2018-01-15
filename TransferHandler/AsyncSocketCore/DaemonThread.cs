using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AsyncSocket
{
    class DaemonThread : Object
    {
        private Thread m_thread;
        private AsyncServer m_asyncSocketServer;

        public DaemonThread(AsyncServer asyncSocketServer)
        {
            m_asyncSocketServer = asyncSocketServer;
            m_thread = new Thread(DaemonThreadStart);
            m_thread.Start();
        }

        public void DaemonThreadStart()
        {
            while (m_thread.IsAlive)
            {
                AsyncSocketUserToken[] userTokenArray = null;
                m_asyncSocketServer.AsyncSocketUserTokenList.CopyList(ref userTokenArray);
                for (int i = 0; i < userTokenArray.Length; i++)
                {
                    if (!m_thread.IsAlive)
                        break;
                    try
                    {
                        if ((DateTime.Now - userTokenArray[i].ActiveDateTime).TotalMilliseconds > m_asyncSocketServer.SocketTimeOut) //超时Socket断开
                        {
                            lock (userTokenArray[i])
                            {
                                string msg = ToStringIP(userTokenArray[i].ConnectSocket);
                                Logger.Instance().InfoFormat("DaemonThread 关闭了一个连接,IP={0},timespan={1}", msg, userTokenArray[i].ActiveDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                m_asyncSocketServer.CloseClientSocket(userTokenArray[i]);
                            }
                        }
                    }                    
                    catch (Exception e)
                    {
                        Logger.Instance().ErrorFormat("Daemon thread check timeout socket error, message: {0}", e.Message);
                        Logger.Instance().Error(e.StackTrace);
                    }
                }
#if DEBUG
                for (int i = 0; i < 10 * 1000 / 10; i++) //每分钟检测一次
#else
                for (int i = 0; i < 60 * 1000 / 10; i++) //每分钟检测一次
#endif


                {
                    if (!m_thread.IsAlive)
                        break;
                    Thread.Sleep(10);
                }
            }
        }

        private string ToStringIP(Socket sock)
        {
            if (sock == null || sock.RemoteEndPoint == null)
                return string.Empty;
            string msg = string.Empty;
            IPEndPoint point = sock.RemoteEndPoint as IPEndPoint;
            byte[] address = point.Address.GetAddressBytes();
            if (address != null && address.Length == 4)
                msg = string.Format("{0}.{1}.{2}.{3}", address[0], address[1], address[2], address[3]);
            else
                msg = string.Empty;
            return msg;
        }
        /// <summary>
        /// 退出主程序时一定要调用此函数
        /// </summary>
        public void Close()
        {
            m_thread.Abort();
            m_thread.Join();
        }
    }
}
