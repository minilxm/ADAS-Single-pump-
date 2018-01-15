using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace AsyncSocket
{
    public class SocketBase
    {
        protected TcpClient m_tcpClient;    //client tcp
        protected string m_RemoteHost = string.Empty;
        protected int m_RemotePort;
        protected const int BUFFER_SIZE = 10240;

        /// <summary>
        /// 要连接到的服务器IP
        /// </summary>
        public string RemoteHost
        {
            get { return m_RemoteHost; }
            set { m_RemoteHost = value; }
        }

        /// <summary>
        /// 要连接到的服务器端口
        /// </summary>
        public int RemotePort
        {
            get { return m_RemotePort; }
            set { m_RemotePort = value; }
        }

        public SocketBase(string host, int port, bool blocking = false)
        {
            InitSocket(host, port, blocking);
        }

        /// <summary>
        /// 自定义客户端的固定IP和端口号
        /// </summary>
        /// <param name="localHost"></param>
        /// <param name="localPort"></param>
        /// <param name="remoteHost"></param>
        /// <param name="remotePort"></param>
        /// <param name="blocking"></param>
        public SocketBase(string localHost, int localPort, string remoteHost, int remotePort, bool blocking = false)
        {
            InitSocketEx(localHost, localPort, remoteHost, remotePort);
        }

        public Socket GetSocket()
        {
            return m_tcpClient.Client;
        }

        public virtual void Send(byte[] buf)
        {

        }

        /// <summary>
        /// 异步发送字符串,编码格式为当前操作系统的默认格式，英文系统为ANSI,中文为GB2312
        /// </summary>
        /// <param name="str"></param>
        public virtual void Send(string str)
        {
            byte[] buffer = System.Text.Encoding.Default.GetBytes(str);
            Send(buffer);
        }

        /// <summary>
        /// 发送UTF8格式
        /// </summary>
        /// <param name="str"></param>
        public virtual void SendUTF8(string str)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(str);
            Send(buffer);
        }

        /// <summary>
        /// 发送ASCII格式
        /// </summary>
        /// <param name="str"></param>
        public virtual void SendASCII(string str)
        {
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(str);
            Send(buffer);
        }

        /// <summary>
        /// 发送Unicode16格式
        /// </summary>
        /// <param name="str"></param>
        public virtual void SendUnicode16(string str)
        {
            byte[] buffer = System.Text.Encoding.Unicode.GetBytes(str);
            Send(buffer);
        }

        public virtual void Close()
        {
            if (m_tcpClient.Connected)
                m_tcpClient.Close();
        }

        /// <summary>
        /// 传入服务器IP和端口号，
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="bBlock"></param>
        private void InitSocket(string host, int port, bool blocking = false)
        {
            m_tcpClient = new TcpClient();
            m_tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); //地址重用
            m_tcpClient.Client.Blocking = blocking; //阻塞或不阻塞
            m_RemoteHost = host;
            m_RemotePort = port;
        }

       

        /// <summary>
        /// 传入服务器IP和端口号
        /// </summary>
        /// <param name="localHost">绑定本地固定地址</param>
        /// <param name="localPort">绑定本地固定端口号</param>
        /// <param name="remoteHost">服务器IP</param>
        /// <param name="remotePort">服务器端口号</param>
        /// <param name="blocking">阻塞选项</param>
        private void InitSocketEx(string localHost, int localPort, string remoteHost, int remotePort, bool blocking = false)
        {
            m_tcpClient = new TcpClient();
            IPAddress addr = IPAddress.Parse(localHost);
            IPEndPoint customPoint = new IPEndPoint(addr, localPort);
            m_tcpClient.Client.Bind(customPoint);
            m_tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); //地址重用
            m_tcpClient.Client.Blocking = blocking; //阻塞或不阻塞
            m_RemoteHost = remoteHost;
            m_RemotePort = remotePort;
        }
    }
}
