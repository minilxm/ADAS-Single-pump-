using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace AsyncSocket
{
    public class SyncClient : SocketBase
    {
        public event EventHandler<MessageEventArgs> HandleReceivedBuffers;
        private Thread receiveThread;

        public SyncClient(string host, int port, bool blocking = true)
            : base(host, port, blocking)
        {
            receiveThread = new Thread(new ParameterizedThreadStart(FuncReceive), 0);
        }

        public SyncClient(string localHost, int localPort, string remoteHost, int remotePort, bool blocking = true)
            : base(localHost, localPort, remoteHost, remotePort, blocking)
        {
            receiveThread = new Thread(new ParameterizedThreadStart(FuncReceive), 0);
        }


        /// <summary>
        /// 同步连接
        /// </summary>
        public void Connect()
        {
            if (!m_tcpClient.Connected)
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(m_RemoteHost), m_RemotePort);
                try
                {
                    m_tcpClient.Client.Connect(endpoint);
                }
                catch (SocketException e)
                {
                    System.Diagnostics.Debug.WriteLine("请检查服务器是否启动！(" + e.Message + ")");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            if (m_tcpClient.Connected)
            {
                receiveThread.Start(m_tcpClient.Client);
            }
        }

        public override void Close()
        {
            receiveThread.Abort();
            base.Close();
        }

        /// <summary>
        /// 同步发送字节流
        /// </summary>
        /// <param name="buf"></param>
        public override void Send(byte[] buf)
        {
            if (m_tcpClient.Connected)
            {
                int iCount = 0;
                try
                {
                    do
                    {
                        iCount += m_tcpClient.Client.Send(buf, iCount, buf.Length - iCount, SocketFlags.None);
                    }
                    while (iCount < buf.Length);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        private void FuncReceive(object sock)
        {
            Socket client = (Socket)sock;
            byte[] buffer = new byte[BUFFER_SIZE];
            int iCount = 0;
            while(receiveThread.IsAlive)
            {
                try
                {
                    iCount = client.Receive(buffer);     //阻塞在这里，等待数据到来
                }
                catch (ObjectDisposedException)
                {
                    throw new Exception("The System.Net.Sockets.Socket has been closed.");
                }
                catch(SocketException)
                {
                    throw new Exception("An error occurred when attempting to access the socket.");
                }
                catch(Exception e)
                {
                    throw new Exception("FuncReceive Receive Error!" + e.Message);
                }
                System.Diagnostics.Debug.WriteLine("FuncReceive:" + iCount.ToString());
               //如果是同步接收到数据 向外层传递接收到的数据
               if (iCount>0 && HandleReceivedBuffers != null)
                   HandleReceivedBuffers(this, new MessageEventArgs(buffer, 0, iCount));
            }
        }
      
    }
}
