using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Trans
{
    public class SocketConnection : ConnectionDevice
    {
        #region "Const"
        private const int SOCKET_BUFEER_SIZE=4096;
        #endregion
        #region "Varialbes"
        private Socket m_ConnectedSocket=null;
        IPEndPoint m_ServerInfo=null;
        private byte[] m_SocketReceiveBuffer;
        #endregion
        #region "Event"
        public event EventHandler<DataTransmissionEventArgs> DataReceived;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public SocketConnection()
        {
        }
        #endregion
        #region "Initialize socket device"
        /// <summary>
        /// set the server address, port and protocol type
        /// </summary>
        /// <param name="address">server address</param>
        /// <param name="port">server port</param>
        /// <param name="protocolType">socket protocol type</param>
        public void InitializeDevice(IPAddress address, int port, ProtocolType protocolType)
        {
            InitializeDevice(address, port, SOCKET_BUFEER_SIZE, protocolType);
        }
        /// <summary>
        /// set the server address, port, buffer size and protocol type 
        /// </summary>
        /// <param name="address">server address</param>
        /// <param name="port">server port</param>
        /// <param name="bufferSize">socket buffer size</param>
        /// <param name="protocolType">socket protocol type</param>
        public void InitializeDevice(IPAddress address, int port, int bufferSize, ProtocolType protocolType)
        {
            ResetSocket(protocolType);
            if (null != m_ConnectedSocket)
            {
                m_ServerInfo = new IPEndPoint(address, port);
                if (bufferSize > 0)
                {
                    m_SocketReceiveBuffer = null;
                    m_SocketReceiveBuffer = new Byte[bufferSize];
                }
            }
        }

         /// <summary>
        /// set the Socket ProtocolType:Tcp/Udp
        /// </summary>
        private void ResetSocket(ProtocolType protocolType)
        {
            try
            {
                Close();
            }
            catch (Exception)
            {
            }
            m_ConnectedSocket = null;
            if (null == m_ConnectedSocket || m_ConnectedSocket.ProtocolType != protocolType)
            {
                try
                {
                    switch (protocolType)
                    {
                        case ProtocolType.Tcp:
                            m_ConnectedSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, protocolType);
                            break;
                        case ProtocolType.Udp:
                            m_ConnectedSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, protocolType);
                            break;
                        default:
                            break;
                    }
                }
                catch(Exception excp)
                {
                    m_ConnectedSocket = null;
                    throw new Exception("ResetSocket - ", excp);                   
                }
            }
        }
        #endregion

        #region "Connect/DisConnect"
        /// <summary>
        /// connect socket
        /// </summary>
        public override void Open()
        {
            if (null != m_ConnectedSocket)
            {
                if (!m_ConnectedSocket.Connected)
                {
                    try
                    {
                        m_ConnectedSocket.Connect(m_ServerInfo);
                        m_ConnectedSocket.BeginReceive(m_SocketReceiveBuffer, 0, m_SocketReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), null);                       
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Open - ", e);
                    }
                }
            }
        }
        /// <summary>
        /// disconnect socket
        /// </summary>
        public override void Close()
        {
            if (null != m_ConnectedSocket)
            {
                if (m_ConnectedSocket.Connected)
                {
                    try
                    {
                        if (ProtocolType.Tcp == m_ConnectedSocket.ProtocolType)
                        {
                            //not allow to send and receive
                            m_ConnectedSocket.Shutdown(SocketShutdown.Both);
                            //close socket, not allow to reuse
                            m_ConnectedSocket.Disconnect(false);
                        }
                        m_ConnectedSocket.Close();
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Close - ", e);
                    }
                }
            }
        }

        /// <summary>
        /// whether the socket is open
        /// </summary>
        /// <returns>true:open,otherwise,close</returns>
        public override bool IsOpen()
        {
            bool isOpen = false;
            if (null != m_ConnectedSocket)
            {
                isOpen = m_ConnectedSocket.Connected;
            }
            return isOpen;
        }
        #endregion
        #region "Data Recerived"
        /// <summary>
        /// 异步接收数据
        /// </summary>
        /// <param name="AR"></param>
        private void ReceiveCallBack(IAsyncResult AR)
        {
            try
            {
                if (m_ConnectedSocket!=null && m_ConnectedSocket.Connected)
                {
                    //get received byte count
                    int REnd = m_ConnectedSocket.EndReceive(AR);
                    //save to the buffer
                    if (DataReceived != null)
                    {
                        DataReceived(this, new DataTransmissionEventArgs(m_SocketReceiveBuffer, 0, REnd));
                    }
                    m_ConnectedSocket.BeginReceive(m_SocketReceiveBuffer, 0, m_SocketReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), null);
                }
            }
            catch (Exception exception)
            {
                //throw new Exception(exception.Message, exception);
            }
        }
        #endregion

        #region "Send Data"
        /// <summary>
        /// send out data
        /// </summary>
        /// <param name="data">data to be sent</param>
        public override void SendData(byte[] data)
        {
            if (m_ConnectedSocket!=null && m_ConnectedSocket.Connected)
            {
                try
                {
                    // Writer raw data
                    m_ConnectedSocket.Send(data);
                }
                catch (Exception e)
                {
                    throw new Exception("Write - ", e);
                }
            }
            else
            {
                throw new Exception("Write - Socket is not open");
            }
        }
        #endregion
    }
}
