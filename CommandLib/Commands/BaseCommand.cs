using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using AsyncSocket;

namespace Cmd
{
    public class BaseCommand : EventArgs
    {
        public event EventHandler<EventArgs> HandleResponse;                  //回调函数
        public event EventHandler<EventArgs> HandleTimeOut;                   //超时处理

        //protected byte[] m_arrStartOfMessage = new byte[2] { 0x0B, 0x1C };   //命令头两个字节
        protected byte   m_Direction            = 0;                            //命令标识（0、1）
        protected byte   m_MessageID            = 0;                            //8位MessageID
        protected byte   m_Channel              = 0xFF;                         //8位通道号
        //protected ushort m_SerialNo             = 0;                            //四位整数的序列号(1001开始),采用静态IP后序列号取消
        protected byte   m_PayloadLength        = 0;                            //8位数据长度
        protected byte   m_PayloadLengthReverse = 0;                            //255-8位数据长度
        protected uint   m_Checksum             = 0;                            //32位检验和
        protected long   m_TimeStamp            = 0;                            //单位：100毫微秒=10（-7）秒
        protected string m_ErrorMsg             = string.Empty;                 //可能有错误信息
        //protected Socket m_RemoteSocket         = null;                         //此处的SOCKET为WIFI模块客户端，由上层应用初始化
        protected AsyncSocketUserToken m_RemoteSocket = null;                   //此处的SOCKET为WIFI模块客户端，由上层应用初始化
        protected byte   m_TryCount             = 0;                            //命令重试的次数,如果没有成功，再发送一次

        #region 属性
        /// <summary>
        /// 命令传输的方向
        /// </summary>
        public byte Direction
        {
            get { return m_Direction; }
            set { m_Direction = value; }
        }

        /// <summary>
        /// 命令ID
        /// </summary>
        public byte MessageID
        {
            get { return m_MessageID; }
            set { m_MessageID = value; }
        }

        /// <summary>
        /// 通道
        /// </summary>
        public byte Channel
        {
            get { return m_Channel; }
            set { m_Channel = value; }
        }

        /// <summary>
        /// WIFI板子序列号
        /// </summary>
        //public ushort SerialNo
        //{
        //    get { return m_SerialNo; }
        //    set { m_SerialNo = value; }
        //}


        /// <summary>
        /// 命令消息长度
        /// </summary>
        public byte PayloadLength
        {
            get { return m_PayloadLength; }
            set { m_PayloadLength = value; }
        }

        /// <summary>
        /// 255-命令消息长度
        /// </summary>
        public byte PayloadLengthReverse
        {
            get { return m_PayloadLengthReverse; }
            set { m_PayloadLengthReverse = value; }
        }

        /// <summary>
        /// 32位检验码
        /// </summary>
        public uint Checksum
        {
            get { return m_Checksum; }
            set { m_Checksum = value; }
        }

        /// <summary>
        /// 命令时间戳
        /// </summary>
        public long TimeStamp
        {
            get { return m_TimeStamp; }
            set { m_TimeStamp = value; }
        }

        /// <summary>
        /// 返回错误信息
        /// </summary>
        public string ErrorMsg
        {
            get { return m_ErrorMsg; }
            set { m_ErrorMsg = value; }
        }

        /// <summary>
        /// 命令重试的次数
        /// </summary>
        public byte TryCount
        {
            get { return m_TryCount; }
            set { m_TryCount = value; }
        }

        /// <summary>
        /// 要发送信息的客户端SOCKET,在发送命令时一定要初始化这个变量
        /// </summary>
        public AsyncSocketUserToken RemoteSocket
        {
            get { return m_RemoteSocket; }
            set { m_RemoteSocket = value; }
        }

        ///// <summary>
        ///// 要发送信息的客户端SOCKET,在发送命令时一定要初始化这个变量
        ///// </summary>
        //public Socket RemoteSocket
        //{
        //    get { return m_RemoteSocket; }
        //    set { m_RemoteSocket = value; }
        //}
        #endregion

        public BaseCommand()
        {
        }

        public BaseCommand(byte messageID)
        {
            m_MessageID = messageID;
        }

        public BaseCommand(byte direction, byte messageID)
        {
            m_Direction = direction;
            m_MessageID = messageID;
        }
 

        /// <summary>
        /// 构造函数,由ADAS发送的命令用此构造函数
        /// </summary>
        /// <param name="direction">命令方向标识</param>
        /// <param name="messageID"></param>
        /// <param name="payloadLength"></param>
        /// <param name="checksum"></param>
        /// <param name="channel">8位通道号通常为0xFF</param>
        public BaseCommand(byte direction, byte messageID, byte payloadLength, uint checksum,  byte channel=0xFF)
        {
            m_Direction     = direction;
            m_MessageID     = messageID;
            m_Checksum      = checksum;
            m_PayloadLength = payloadLength;
            m_PayloadLengthReverse = (byte)(0xFF - payloadLength);
            m_Channel = channel;
        }

      

        /// <summary>
        /// 更新命令数据体长度
        /// </summary>
        /// <param name="length"></param>
        public void UpdatePayloadLength(byte length)
        {
            m_PayloadLength = length;
            m_PayloadLengthReverse = (byte)(0xFF - m_PayloadLength);
        }

        /// <summary>
        /// 取Direction到m_PayloadLengthReverse length之间的字节
        /// </summary>
        /// <returns></returns>
        public virtual List<byte> GetBytes()
        {
            List<byte> buffer = new List<byte>();
            buffer.Add(m_Direction);
            buffer.Add(m_MessageID);
            buffer.Add(m_Channel);
            buffer.Add(m_PayloadLength);
            buffer.Add(m_PayloadLengthReverse);
            return buffer;
        }

        /// <summary>
        /// 取Direction到m_PayloadLengthReverse length之间的字节
        /// </summary>
        /// <returns></returns>
        public virtual List<byte> GetBytesEx()
        {
            List<byte> buffer = new List<byte>();
            buffer.Add(m_Direction);
            buffer.Add(m_MessageID);
            buffer.Add(m_Channel);
            buffer.Add(m_PayloadLength);
            buffer.Add(m_PayloadLengthReverse);
            return buffer;
        }

        public virtual void SetBytes(byte[] payloadData)
        {

        }

        /// <summary>
        /// 复制命令
        /// </summary>
        /// <param name="other"></param>
        public virtual void Copy(BaseCommand other)
        {
            m_Direction            = other.m_Direction;
            m_Channel              = other.m_Channel;
            m_MessageID            = other.m_MessageID;
            m_PayloadLength        = other.m_PayloadLength;
            m_PayloadLengthReverse = other.m_PayloadLengthReverse;
            m_Checksum             = other.m_Checksum;
            m_TimeStamp            = other.m_TimeStamp;
        }

        /// <summary>
        /// 当接受到泵端的响应数据时，调用回调函数
        /// </summary>
        public virtual void InvokeResponse()
        {
            if (HandleResponse != null)
            {
                HandleResponse(this, this);
            }
        }
        
        /// <summary>
        /// 当超时后调用回调函数
        /// </summary>
        public virtual void InvokeTimeOut()
        {
            if (HandleTimeOut != null)
            {
                HandleTimeOut(this, this);
            }
        }
        

    }
}
