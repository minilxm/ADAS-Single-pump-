using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace AsyncSocket
{
    public class AsyncClient : SocketBase
    {
        /// <summary>
        /// 由外层类实现接收字节的处理
        /// </summary>
        public event EventHandler<MessageEventArgs> HandleReceivedBuffers;
        public event EventHandler<MessageEventArgsEx> HandleReceivedBuffersEx;

        public string m_LocalIP;
        public int m_LocalPort;
        
        /// <summary>
        /// 绑定的本地IP
        /// </summary>
        public string LocalIP
        {
            get { return m_LocalIP;}
            set { m_LocalIP = value;}
        }

        /// <summary>
        /// 绑定的本地端口
        /// </summary>
        public int LocalPort
        {
            get { return m_LocalPort;}
            set { m_LocalPort = value;}
        }

        public AsyncClient(string host, int port, bool blocking = false)
            : base(host, port, blocking)
        {
        }

        public AsyncClient(string localHost, int localPort, string remoteHost, int remotePort, bool blocking = true)
            : base(localHost, localPort, remoteHost, remotePort, blocking)
        {
            m_LocalIP = localHost;
            m_LocalPort = localPort;
        }

        /// <summary>
        /// 异步连接
        /// </summary>
        public void Connect()
        {
            if (!m_tcpClient.Connected)
            {
                try
                {
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(m_RemoteHost), m_RemotePort);
                    SocketAsyncEventArgs asyncEventArgs = new SocketAsyncEventArgs();
                    asyncEventArgs.RemoteEndPoint = endpoint;
                    asyncEventArgs.Completed += OnConnect;
                    m_tcpClient.Client.ConnectAsync(asyncEventArgs);
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
        }

        public bool IsConnected()
        {
            if (m_tcpClient != null && m_tcpClient.Client != null)
                return m_tcpClient.Connected;
            else
                return false;
        }

        /// <summary>
        /// 异步发送字节流
        /// </summary>
        /// <param name="buf"></param>
        public override void Send(byte[] buf)
        {
            if (m_tcpClient.Connected)
            {
                SocketAsyncEventArgs asyncEventArgs = new SocketAsyncEventArgs();
                asyncEventArgs.SetBuffer(buf, 0, buf.Length); 
                asyncEventArgs.Completed += OnSend;
                try
                {
                    m_tcpClient.Client.SendAsync(asyncEventArgs);
                }
                catch (ArgumentException argExp)
                {
                    throw argExp;
                }
                catch (ObjectDisposedException socketCloseExp)
                {
                    m_tcpClient.Close();
                    throw socketCloseExp;
                }
                catch (InvalidOperationException invalidOperExp)
                {
                    throw invalidOperExp;
                }
                catch (SocketException socketExp)
                {
                    throw socketExp;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Connect)
            {
                System.Diagnostics.Debug.WriteLine("Connected OK");
                HandleReceive();
            }
        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                System.Diagnostics.Debug.WriteLine("Received OK");
                if (e.BytesTransferred > 0)
                {
                    System.Diagnostics.Debug.WriteLine("BytesTransferred = " + e.BytesTransferred
                                                        + "Buffer Length = " + e.Buffer.Length
                                                        + "  " + System.Text.Encoding.Default.GetString(e.Buffer));
                    //如果是异步接收到数据 向外层传递接收到的数据
                    if (HandleReceivedBuffers != null)
                        HandleReceivedBuffers(this, new MessageEventArgs(e.Buffer, 0, e.BytesTransferred));
                    if (HandleReceivedBuffersEx != null)
                        HandleReceivedBuffersEx(this, new MessageEventArgsEx(e.Buffer, 0, e.BytesTransferred, this));
                    HandleReceive();
                }
                else
                {
                    m_tcpClient.Close();
                }
            }
        }

        private void HandleReceive()
        {
            SocketAsyncEventArgs asyncEventArgs = new SocketAsyncEventArgs();
            asyncEventArgs.SetBuffer(new byte[BUFFER_SIZE], 0, BUFFER_SIZE);
            asyncEventArgs.Completed += OnReceive;
            bool bRet = true;
            try
            {
                if (m_tcpClient != null && m_tcpClient.Client != null)
                    bRet = m_tcpClient.Client.ReceiveAsync(asyncEventArgs);
                else
                    bRet = false;
            }
            catch (ArgumentException argExp)
            {
                throw argExp;
            }
            catch (ObjectDisposedException socketCloseExp)
            {
                m_tcpClient.Close();
                throw socketCloseExp;
            }
            catch (InvalidOperationException invalidOperExp)
            {
                throw invalidOperExp;
            }
            catch (SocketException socketExp)
            {
                throw socketExp;
            }
            catch (Exception e)
            {
                throw e;
            }
            if (!bRet)
            {
                System.Diagnostics.Debug.WriteLine("ReceiveAsync:" + asyncEventArgs.BytesTransferred);
                //如果是同步接收到数据 向外层传递接收到的数据
                if (HandleReceivedBuffers != null)
                    HandleReceivedBuffers(this, new MessageEventArgs(asyncEventArgs.Buffer, 0, asyncEventArgs.BytesTransferred));
            }
        }

        private void OnSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Send)
            {
                System.Diagnostics.Debug.WriteLine("Send OK! Transferred = " + e.BytesTransferred);
            }
        }


    }
}
