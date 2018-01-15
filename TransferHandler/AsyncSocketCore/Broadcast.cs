using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace AsyncSocket
{
    public class Broadcast
    {
        public event EventHandler<MessageEventArgs> HandleReceivedMessage;
        private Socket m_Socket;
        private const int BUFFERSIZE = 1024;    //不要超过以太网MTU=1472

        public Broadcast(bool bUdpServer = false)
        {
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            if (bUdpServer)
            {
                m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            else
            {
                //广播设置
                m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            }
        }

        public void SendBroadcastMsg(byte[] buffer, int port)
        {
            int iCount = 0;
            do
            {
                iCount += m_Socket.SendTo(buffer, iCount, buffer.Length - iCount, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port));
            }
            while (iCount < buffer.Length);
        }

        /// <summary>
        /// 开启一个接收广播的服务端
        /// </summary>
        /// <param name="port"></param>
        public void StartBroadcastServer(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            m_Socket.Bind(localEndPoint);
            HandleReceive();
        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.ReceiveFrom)
            {
                //将接收到的数据向外层传递
                if (HandleReceivedMessage!=null)
                {
                    HandleReceivedMessage(this, new MessageEventArgs(e.Buffer, 0, e.BytesTransferred));
                }
                //e.Buffer
                System.Diagnostics.Debug.WriteLine("Broadcast BytesTransferred = " + e.BytesTransferred);
                HandleReceive();
            }
        }

        private void HandleReceive()
        {
            SocketAsyncEventArgs asyncEvent = new SocketAsyncEventArgs();
            asyncEvent.Completed += OnReceive;
            asyncEvent.SetBuffer(new byte[BUFFERSIZE], 0, BUFFERSIZE);
            asyncEvent.RemoteEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
            m_Socket.ReceiveFromAsync(asyncEvent);
        }

    }//end class
}
