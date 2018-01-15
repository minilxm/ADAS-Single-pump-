using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace AsyncSocket
{
    /// <summary>
    /// 必须是同步SOCKET
    /// </summary>
    public class SerializeSocket
    {
        private Socket m_Socket;

        public SerializeSocket(Socket socket)
        {
            m_Socket = socket;
        }

        private int Read(byte[] buf, int length)
        {
            int iCount = 0;
            if (m_Socket.Connected)
            {
                do
                {
                    try
                    {
                        iCount += m_Socket.Receive(buf, iCount, length - iCount, SocketFlags.None);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("SerializeSocket::Read() Error." + ex.Message);
                    }
                }
                while (iCount < length);
            }
            else
            {
                throw new Exception("Socket disconnected");
            }
            return iCount;
        }

        private void Write(byte[] buf)
        {
            int iCount = 0;
            if (m_Socket.Connected)
            {
                do
                {
                    try
                    {
                        iCount += m_Socket.Send(buf, iCount, buf.Length - iCount, SocketFlags.None);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("SerializeSocket::Write() Error." + ex.Message);
                    }
                }
                while (iCount < buf.Length);
            }
            else
            {
                throw new Exception("Socket disconnected");
            }
        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            //if (e.LastOperation == SocketAsyncOperation.Receive)
            //{
            //    //e.Buffer
            //    System.Diagnostics.Debug.WriteLine("OnReceive OK! Transferred = " + e.BytesTransferred);
            //    asyncEvent.UserToken = e.Buffer;
            //}
        }

        private void OnSend(object sender, SocketAsyncEventArgs e)
        {
            //if (e.LastOperation == SocketAsyncOperation.Send)
            //{
            //    //e.Buffer
            //    System.Diagnostics.Debug.WriteLine("OnSend OK! Transferred = " + e.BytesTransferred);
            //}
        }

        public SerializeSocket In(ref int iVal)
        {
            byte[] buf = new byte[sizeof(int)];
            Read(buf, sizeof(int));
            iVal = BitConverter.ToInt32(buf, 0);
            return this;
        }

        public SerializeSocket Out(int iVal)
        {
            byte[] buf = BitConverter.GetBytes(iVal);
            Write(buf);
            return this;
        }

    }
}
