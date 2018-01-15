using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using System.IO.Ports;
using Cmd;
using Trans;
using System.Net.Sockets;
using System.Net;
using AsyncSocket;

namespace Analyse
{
    /// <summary>
    /// 待发送命令的队列维护
    /// 命令的发送
    /// 命令的解析
    /// 已发送命令的队列维护
    /// 超时机制
    /// </summary>
    public class ProtocolEngine
    {
        //命令消息头
        //private byte[]                   m_StartMessage              = new byte[2]{0x0B,0x1C};

        private ProductID                m_ProductID                 = ProductID.Unknow;
        private static ProtocolEngine    m_Engine                    = null;
        //执行线程
        private Thread                   m_SendCommandProcThread     = null;
        private Thread                   m_WaitCommandProcThread     = null;
        private Thread                   m_AnalysizeProcThread       = null;
        //命令队列
        private CommandQueueEx             m_Queue                   = new CommandQueueEx();                   //发送队列
        private WaitResponseCommandQueueEx m_WaitQueue               = new WaitResponseCommandQueueEx();       //等待响应队列
        private bool                     m_Keeping                   = false;                                //控制发送队列线程启停
        //信号量相关
        private readonly int             m_SemWaitSeconds            = 2;                                    //set wait timeout
        private readonly int             m_SemMaxCount               = 2560;                                 //set Semaphore max count
        private const string             SEMCOMMANDNAME              = "C9GRCmdSemaphore";                   //set Semaphore name
        private const string             SEMRECEIVECOMMANDNAME       = "C9RCCmdSemaphore";                   //set Semaphore name
        private Semaphore                m_SemCommand                = null;
        private Semaphore                m_SemReceiveCommand         = null;                                 //接受泵端命令信号量
        //private AutoResetEvent           m_EventSendCommand          = new AutoResetEvent(false);            //等待命令返回
        //超时相关       
        private static byte              m_SequenceID                = 0;
        private int                      m_Timeout                   = 2000;                                 //set timeout 默认2秒,可从CONFIG文件读取
        //串口相关
        private string                   m_PortName                  = string.Empty;
        //接受泵端数据
        private Hashtable                m_HashReceivedBuffer        = new Hashtable();                     //接收数据HSAH表<SOCKET， SocketBuffer>，每个SOCKET一个BUFFER
        //private List<byte>               m_ReceivedBuffer            = new List<byte>();
        //private List<byte>               m_CommandBuffer             = new List<byte>();                  //分离出一个完整的命令结构
        private const int                HEADLENGTH                  = 5;                                   //部首5字节
        private DeviceType               m_ConnType                  = DeviceType.SerialPort;               //默认是串口
        private AsyncServer              m_Device                    = null;                                //通信类基类(TCP)

        #region 回调函数
        public event EventHandler<SendOrReceiveBytesArgs> ShowBytesOnUI;                                    //回调函数,此函数只能用于GDA
        public event EventHandler<SocketConnectArgs>      SocketConnectOrCloseResponse;                     //回调函数,只用于更新界面是否连接上
        public event EventHandler<SocketConnectArgs>      SendPumpType2Wifi;                                //回调函数,只用于给新连接的WIFI模块发送泵信息
        public event EventHandler<BaseCommand>            UploadAlarm;                                      //回调函数,只用于上报报错信息
        #endregion

        public DeviceType ConnType
        {
            get { return m_ConnType;}
            set { m_ConnType = value;}
        }

        /// <summary>
        /// 设置或读取ProductID
        /// </summary>
        public ProductID PID
        {
            get { return m_ProductID;}
            set { m_ProductID = value;}
        }

        /// <summary>
        /// 读串口
        /// </summary>
        public string PortName
        {
            get { return m_PortName; }
        }

        private ProtocolEngine()
        {
        }

        public static ProtocolEngine Instance()
        {
            if(m_Engine==null)
            { 
                m_Engine = new ProtocolEngine();
            }
            return m_Engine;
        }

        public void InitTcp()
        {
            Init();
            if (m_Device == null)
            {
                m_Device = AsyncServer.Instance();
            }
            m_Device.DataReceived -= ReceiveData;
            m_Device.DataReceived += ReceiveData;
            m_Device.MappingIP -= MapIPFromWifi;
            m_Device.MappingIP += MapIPFromWifi;
            m_Device.AntiMappingIP -= AntiMapIPFromWifi;
            m_Device.AntiMappingIP += AntiMapIPFromWifi;
            m_Device.CreateCommandQueue -= OnCreateCommandQueue;
            m_Device.CreateCommandQueue += OnCreateCommandQueue;
            m_Device.RemoveCommandQueue -= OnRemoveCommandQueue;
            m_Device.RemoveCommandQueue += OnRemoveCommandQueue;
            m_Device.Start();
        }

        /// <summary>
        /// 初始化两个信号量
        /// </summary>
        private void Init()
        {
            //create a semaphore
            bool bCreateNew = false;
            String semCommandName = SEMCOMMANDNAME + DateTime.Now.Ticks.ToString();           //to be different in threads
            m_SemCommand = new Semaphore(0, m_SemMaxCount, semCommandName, out bCreateNew);
            //if semaphore already existed,then open it.
            if (!bCreateNew)
            {
                m_SemCommand = Semaphore.OpenExisting(semCommandName);
            }

            String semRcCommandName = SEMRECEIVECOMMANDNAME + DateTime.Now.Ticks.ToString();           //to be different in threads
            m_SemReceiveCommand = new Semaphore(0, m_SemMaxCount, semRcCommandName, out bCreateNew);
            //if semaphore already existed,then open it.
            if (!bCreateNew)
            {
                m_SemReceiveCommand = Semaphore.OpenExisting(semRcCommandName);
            }
        }

        /// <summary>
        /// 设置命令超时时间：毫秒
        /// </summary>
        /// <param name="timeout">默认为2000毫秒</param>
        public void SetTimeOut(int timeout = 5000)
        {
            m_Timeout = timeout;
        }

        /// <summary>
        /// 此函数只用于TCP连接服务是否启动
        /// </summary>
        /// <returns></returns>
        public bool IsStart()
        {
            if (m_Device != null && m_Device is AsyncServer)
                return m_Device.IsStart();
            else
                return false;
        }
        
        public void CloseConnection()
        {
            Stop();
        }

        /// <summary>
        /// 开启协议解析发送引擎，包括三个子线程
        /// </summary>
        public void Start()
        {
            if (!m_Keeping && m_Device!=null /*&& m_Device.IsOpen()*/)
            {
                ClearAllCommand();
                m_Keeping = true;
                m_SendCommandProcThread = new Thread(new ThreadStart(SendCommandProc));
                m_WaitCommandProcThread = new Thread(new ThreadStart(WaitCommandProc));
                m_AnalysizeProcThread   = new Thread(new ThreadStart(AnalysizeProc));
                m_SendCommandProcThread.Start();
                m_WaitCommandProcThread.Start();
                m_AnalysizeProcThread.Start();
             }
        }
        
        /// <summary>
        /// 关闭协议解析及发送引擎
        /// </summary>
        public void Stop()
        {
            if (m_Keeping)
            {
                m_Keeping = false;
                m_Queue.ClearQueue();
                //close semaphore
                m_SemCommand.Close();           
                m_SemReceiveCommand.Close();
                m_SemCommand.Dispose();
                m_SemReceiveCommand.Dispose();
                m_SemCommand = null;
                m_SemReceiveCommand = null;
                //阻塞主线程，等待自然死亡
                m_SendCommandProcThread.Join(); 
                m_WaitCommandProcThread.Join();
                m_AnalysizeProcThread.Abort();
                //m_AnalysizeProcThread.Join();
                m_SendCommandProcThread = null;
                m_WaitCommandProcThread = null;
                //m_AnalysizeProcThread = null;
                if(m_Device!=null)
                { 
                    m_Device.Exit();
                    m_Device = null;
                }
            }
        }

        public void ClearAllCommand()
        {
            m_Queue.ClearQueue();
            m_WaitQueue.ClearQueue();
        }

        public void AddCommand(long ip, BaseCommand item)
        {
            lock (m_Queue)
            {
                m_Queue.Enqueue(ip, item);
            }
            try
            {
                if (m_SemCommand != null)
                    m_SemCommand.Release(1); //when a Command was added,mSemCommand increase
            }
            catch (System.Exception ex)
            {
                Logger.Instance().Fatal("ProtocolEngine => AddCommand() =>" + ex.Message);
                //throw ex;//add log here
            }
            
        }

        /// <summary>
        /// 发送队列线程函数
        /// </summary>
        private void SendCommandProc()
        {
            //线程开始时，设置有信号状态
            //m_EventSendCommand.Reset();
            long ip = 0;
            while (m_Keeping && m_SemCommand != null)
            {
                if(!m_SemCommand.WaitOne(m_SemWaitSeconds * 1000))          //队列中有新命令加入时执行
                    continue;
                if (m_Queue.Count > 0)
                {
                    BaseCommand currentCommand = null;
                    lock (m_Queue)
                    {
                        currentCommand = m_Queue.Peek(ref ip);
                    }
                    if(currentCommand==null)
                        continue;
                    bool succeed = false;
                    List<byte> cmdByteList = currentCommand.GetBytes();
                    byte[] commandBytes =  new byte[cmdByteList.Count];
                    cmdByteList.CopyTo(commandBytes);
                    byte[] charCommandBytes = Hex2Char(commandBytes);
                    if(m_Device!=null && m_Device.IsStart())
                    {
                        if (currentCommand.RemoteSocket == null)
                            Logger.Instance().Error("SendCommandProc() currentCommand.RemoteSocket is null");
                        else
                            succeed = m_Device.SendAsyncEvent(currentCommand.RemoteSocket, charCommandBytes, 0, charCommandBytes.Length);
                        if(null != ShowBytesOnUI)
                            ShowBytesOnUI(this, new SendOrReceiveBytesArgs(charCommandBytes));
                    }
                    else
                    { 
                        succeed = false;
                        Logger.Instance().Error("Device is closed");
                    }
                    lock (m_Queue)
                    {
                        m_Queue.Dequeue(ip, currentCommand);   //不管发送是否成功，都要移除
                    }
                    if (succeed && currentCommand.Direction == 0)       //如果只是对WIFI模块的命令响应，就不用加入等待回应队列了
                    {
                        lock(m_WaitQueue)
                        {
                            m_WaitQueue.Enqueue(ip,currentCommand);
                        }
                    }
                    //bool bRet = m_EventSendCommand.WaitOne(m_Timeout);    //这里不用等待泵端回应，直接发送命令
                }
                
            }
        }

        /// <summary>
        /// 超时判断线程，定时扫描队列中第一个命令，如果超时，丢弃
        /// </summary>
        private void WaitCommandProc()
        {
            long span = 0;
            while (m_Keeping)
            {
                lock (m_WaitQueue)
                {
                    List<Tuple<long, BaseCommand>> currentCommands = m_WaitQueue.Peek();
                    foreach(var cmd in currentCommands)
                    {
                        span = DateTime.Now.Ticks - cmd.Item2.TimeStamp;
                        if (span > (long)m_Timeout * 10000)
                        {
                            //超时了向上层发送超时信息
                            m_WaitQueue.Dequeue(cmd.Item1, cmd.Item2);
                            cmd.Item2.ErrorMsg = "TimeOut";
                            Logger.Instance().ErrorFormat("Command TimeOut!!! MessageID = {0}, Direction={1}", cmd.Item2.MessageID, cmd.Item2.Direction);
#if !DEBUG
                            //add 20170710 这里重试没有意义，相对于控制器来说，如果失联，在如此短的时间不可能再重试成功
                            //重试2次
                            //if (cmd.Item2.TryCount <= 1)
                            //{
                            //    cmd.Item2.TryCount++;
                            //    AddCommand(cmd.Item1, cmd.Item2);
                            //    Logger.Instance().InfoFormat("Command Retry, MessageID = {0}, Direction={1}, TryCount={2}", cmd.Item2.MessageID, cmd.Item2.Direction, cmd.Item2.TryCount);
                            //}
                            //if(cmd.Item2.TryCount>=2)
                            //    cmd.Item2.InvokeTimeOut();
#else

#endif
                            cmd.Item2.InvokeTimeOut();
                        }
                    }
                    
                }
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// 解析数据包，生成Command对象,并调用HandleReceivedCommand方法
        /// </summary>
        private void AnalysizeProc()
        {
            //int iHeadReadCount = 0;
            while (m_Keeping && m_SemReceiveCommand!=null)
            {
                //如果等待超时，继续下一次等待
                if(!m_SemReceiveCommand.WaitOne(m_Timeout))
                    continue;
                SocketBuffer buffer = FindSocketBuffer();
                if(buffer==null)
                {
                    Logger.Instance().Error("AnalysizeProc() FindSocketBuffer错误，未能找到相关SocketBuffer");
                    continue;
                }
                if (buffer.ReceivedBuffer.Count <= 0)
                    continue;
                //读头部固定5字节
                buffer.HeadReadCount = CatchStartMessage(buffer);
                if (buffer.HeadReadCount < HEADLENGTH)
                    continue;
                
                if(CatchPayloadDataAndChecksum(buffer)<0)
                    continue;
                else
                {
                    //解析完成，所有数据清零
                    BaseCommand cmd = CreateCommand(buffer.CommandBuffer);
                    buffer.HeadReadCount = 0;
                    if (null != ShowBytesOnUI)
                        ShowBytesOnUI(this, new SendOrReceiveBytesArgs(buffer.CommandBuffer, false));
                    buffer.CommandBuffer.Clear();
                    cmd.RemoteSocket = buffer.RemoteSock;
                    //每次有新的命令到来，检查下控制器，是否有SocketToken==null的情况，如果有，则更新
                    Controller controller = ControllerManager.Instance().Get(buffer.RemoteSock);
                    if(controller!=null && controller.SocketToken==null)
                        controller.SocketToken = buffer.RemoteSock;

                    HandleReceivedCommand(ControllerManager.GetLongIPFromSocket(buffer.RemoteSock), cmd);
                }
            }
        }

        private SocketBuffer FindSocketBuffer()
        {
            SocketBuffer buffer = null;
            lock (m_HashReceivedBuffer)
            {
                foreach(DictionaryEntry de in m_HashReceivedBuffer)
                {
                    if(((SocketBuffer)de.Value).IsReady)
                    {
                        buffer = (SocketBuffer)(de.Value);
                        buffer.IsReady = false;
                        break;
                    }
                    else
                        continue;
                }
            }
            return buffer;
        }

        private BaseCommand CreateCommand(List<byte> CommandBuffer)
        {
            byte[] cmdBytes = null;
            lock(CommandBuffer)
            { 
                if(CommandBuffer.Count<9)
                { 
                    Logger.Instance().Error("CreateCommand() Error! CommandBuffer.Count<10");
                    return null;
                }
                cmdBytes = new byte[CommandBuffer.Count];
                CommandBuffer.CopyTo(cmdBytes);
            }
            byte msgID = cmdBytes[1];

            BaseCommand cmd = CommandFactory.CreateCommand(msgID);
            if(cmd==null)
            {
                string msg = string.Format("CreateCommand() Error! MessageID={0}", msgID);
                Logger.Instance().Error(msg);
                return null;
            }
            cmd.Direction = cmdBytes[0];
            cmd.Channel = cmdBytes[2];
            //分析命令数据长度
            cmd.PayloadLength = cmdBytes[3];
            cmd.PayloadLengthReverse = cmdBytes[4];
            //分离出最后4字节的Checksum
            cmd.Checksum = (uint)(cmdBytes[cmdBytes.Length - 1] << 24);
            cmd.Checksum += (uint)(cmdBytes[cmdBytes.Length - 2] << 16);
            cmd.Checksum += (uint)(cmdBytes[cmdBytes.Length - 3] << 8);
            cmd.Checksum += (uint)(cmdBytes[cmdBytes.Length - 4]);
            //重新计算泵端传入的数据checksum，如果对应不上，记录日志，由上层应用酌情处理，但不能作超时处理
            uint checksum = CRC32.CalcCRC32Partial(cmdBytes, cmdBytes.Length - 4, CRC32.CRC32_SEED);
            checksum ^= CRC32.CRC32_SEED;
//在DUBUG状态下就不要去检查checksum
#if !DEBUG
            if( (cmd.Checksum^checksum) != 0 )
            {
                cmd.ErrorMsg="Checksum Error";
                string buffer2String = BytesToString(cmdBytes, 0, cmdBytes.Length);
                Logger.Instance().ErrorFormat("ProtocolEngine::CreateCommand() Checksum error, buffer={0}", buffer2String);
            }
#endif

            //将PayloadData提取出来，方便生成字段
            byte[] arrFieldsBuf = new byte[cmd.PayloadLength];
            int index = cmdBytes.Length - arrFieldsBuf.Length - 4;
            if(index <= 0)
            {
                string msg = string.Format("CreateCommand() Error! index<=0 cmdBytes.Length={0} arrFieldsBuf.Length={1}", cmdBytes.Length, arrFieldsBuf.Length);
                Logger.Instance().Error(msg);
                return null;
            }
            Array.Copy(cmdBytes, index, arrFieldsBuf, 0, arrFieldsBuf.Length);
            //将字段填进命令
            cmd.SetBytes(arrFieldsBuf);
            return cmd;
        }

        /// <summary>
        /// 捕捉命令头7个字节,目前只能做到部分容错
        /// </summary>
        /// <returns></returns>
        private int CatchStartMessage(SocketBuffer buffer)
        {
            int iCount = 0;
            lock(buffer)
            {
                //读头部5个固定字节（读到Payload length）
                iCount =  buffer.ReceivedBuffer.Count;
                if(iCount<7)
                {
                    Logger.Instance().ErrorFormat("CatchStartMessage() Error,数据头长度小于7 buffer.ReceivedBuffer.Count={0}", iCount);
                    return -1;
                }
                byte []temp = new byte[HEADLENGTH];
                buffer.ReceivedBuffer.CopyTo(0, temp, 0, HEADLENGTH);
                buffer.CommandBuffer.Clear();
                buffer.CommandBuffer.AddRange(temp);
                buffer.ReceivedBuffer.RemoveRange(0, HEADLENGTH);        //读完数据后，立即移走
            }
            return buffer.CommandBuffer.Count;
        }

        /// <summary>
        /// 读数据体及Checksum
        /// </summary>
        /// <returns></returns>
        private int CatchPayloadDataAndChecksum(SocketBuffer buffer)
        {
            if (buffer.CommandBuffer.Count < 5) //这里应该报错。因为部首5字节不完整
            {
                Logger.Instance().Error("CatchPayloadDataAndChecksum Error! buffer.CommandBuffer.Count<6");
                return -1;
            }
            lock(buffer.ReceivedBuffer)
            { 
                byte payloadLength = buffer.CommandBuffer[3];
                int iCount = buffer.ReceivedBuffer.Count;
                if(iCount < payloadLength + 4)
                {
                    //数据体长度+Checksum（4字节），缓存中数量不够，重新等下一次信号量
                    Logger.Instance().Error("CatchPayloadDataAndChecksum Error! buffer.ReceivedBuffer.Count < payloadLength + 4");
                    return -2;
                }
                buffer.CommandBuffer.AddRange(buffer.ReceivedBuffer.GetRange(0,payloadLength + 4));
                //temp.Clear();
                buffer.ReceivedBuffer.RemoveRange(0, payloadLength + 4);        //读完数据后，立即移走
            }
            return 0;
        }

        /// <summary>
        /// 生成序列号从0到255
        /// </summary>
        /// <returns></returns>
        private byte MakeSequenceID()
        {
            m_SequenceID = (byte)((++m_SequenceID)%256);
            return m_SequenceID;
        }

        /// <summary>
        /// 处理泵端响应的命令机制为：
        /// 1、如果不是等待队列中第一条命令，丢弃
        /// 2、如果等待队列中第一条命令超时，丢弃
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleReceivedCommand(long ip, BaseCommand cmd)
        {   
            if(cmd == null)
            { 
                Logger.Instance().Error("HandleReceivedCommand Failed! Because cmd is null");
                return;
            }
            if (cmd.Direction==1)   
            {   
                //客户端的回应消息
                #region
                lock (m_WaitQueue)
                {
                    BaseCommand currentCommand = m_WaitQueue.Peek(ip);
                    if (currentCommand != null)
                    {
                        if (cmd.MessageID == currentCommand.MessageID)
                        {
                            //命令匹配，执行回函数
                            currentCommand.Copy(cmd);           //需要全拷贝吗？这里只需要泵端传入的数据而已
                            currentCommand.InvokeResponse();
                            m_WaitQueue.Dequeue(ip, currentCommand);
                        }
                        else
                        {
                            if ((DateTime.Now.Ticks - currentCommand.TimeStamp) > (long)m_Timeout * 10000)
                            {
                                //超时了向上层发送超时信息
                                m_WaitQueue.Dequeue(ip, currentCommand);
                            }
                        }
                    }
                    
                }
                #endregion
            }
            else
            {
                //客户端主动上报的消息
                //这里要处理目前只有一个命令是主动上报的，就是上传报警信息
                if(UploadAlarm!=null)
                {
                    UploadAlarm(this, cmd);
                }
            }
            
        }

        /// <summary>
        /// 响应串口层上传的数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ReceiveData(object sender, DataTransmissionEventArgs args)
        {
            AsyncSocketUserToken socket = args.Token;
            if(socket==null)
                return;
            long ip = ControllerManager.GetLongIPFromSocket(socket);
            SocketBuffer buffer = null;
            lock (m_HashReceivedBuffer)
            {
                if(m_HashReceivedBuffer.ContainsKey(ip))
                   buffer = (SocketBuffer)m_HashReceivedBuffer[ip];
                else
                {
                   buffer = new SocketBuffer();
                   m_HashReceivedBuffer.Add(ip, buffer);
                }
                buffer.RemoteSock = socket;
                //lxm 20161013修改，传入的字节流要进行一次转换，将字符字节两两合成一个16进制数据,比如：'F'和'5' 合成0xF5
                buffer.ReceivedCharBuffer.AddRange(args.EventData);
                args.EventData.Clear();
                buffer.IsReady = Char2Hex(buffer);
                if (buffer.IsReady)
                    m_SemReceiveCommand.Release(1);
            }
        }


        /// <summary>
        /// 接收到的字符转成
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool Char2Hex(SocketBuffer buffer)
        {
            List<byte> charBuffer = buffer.ReceivedCharBuffer;
            int headIndex = charBuffer.IndexOf(0x02);
            int tailIndex = charBuffer.IndexOf(0x03);
            long ip = ControllerManager.GetLongIPFromSocket(buffer.RemoteSock);
            string szIP = ControllerManager.Long2IP(ip);

            if (headIndex < 0)
            {
                //如果连包头都不存在，那么整个包都删除，
                string str = BufferToString(charBuffer);
                Logger.Instance().ErrorFormat("Char2Hex()->找不到包头字节0x02, charBuffer长度为{0},IP={1},缓冲区数据={2}, 清空!", charBuffer.Count, szIP, str);
                buffer.ReceivedBuffer.Clear();
                return false;
            }
            else
            {
                if(tailIndex<0)
                {
                    //如果包尾不存在，继续
                    Logger.Instance().ErrorFormat("Char2Hex()->找不到包尾字节0x03,此包不完整，等待下一次数据到来，charBuffer长度为{0}", charBuffer.Count);
                    return false;
                }
            }

            //0x02和0x03之间的数据必须是偶数个的
            if(tailIndex - headIndex < 10 || (tailIndex - headIndex -1)%2!=0)
            {
                charBuffer.RemoveRange(headIndex, tailIndex - headIndex + 1);
                Logger.Instance().ErrorFormat("Char2Hex()->包头与包尾之间字节数不为偶数或数量小于10，从缓冲区中移动这段数据，并保留余下数据,headIndex={0},tailIndex={1}", headIndex, tailIndex);
                return false;
            }
            byte[] temp = new byte[tailIndex-headIndex-1];
            charBuffer.CopyTo(headIndex+1, temp, 0, temp.Length);
            charBuffer.RemoveRange(headIndex, tailIndex - headIndex + 1);
            int length = temp.Length;
            int iLoop=0;
            byte byteHigh = 0x00;
            byte byteLow = 0x00;
            while(iLoop+1<length)
            {
                if(temp[iLoop]>=0x30 && temp[iLoop]<=0x39)
                   byteHigh = (byte)((temp[iLoop]-0x30)<<4);
                else if(temp[iLoop]>=0x41 && temp[iLoop]<=0x46)
                    byteHigh = (byte)((temp[iLoop] - 0x37) << 4);
                else if (temp[iLoop] >= 0x61 && temp[iLoop] <= 0x66)
                    byteHigh = (byte)((temp[iLoop] - 0x57) << 4);
                else
                {
                    Logger.Instance().ErrorFormat("Char2Hex 错误，出现0~9，A~F以外的字符 temp[iLoop]={0}， iLoop={1}", temp[iLoop], iLoop);
                    buffer.ReceivedBuffer.Clear();
                    return false;
                }

                if (temp[iLoop+1] >= 0x30 && temp[iLoop+1] <= 0x39)
                    byteLow = (byte)(temp[iLoop+1] - 0x30);
                else if (temp[iLoop+1] >= 0x41 && temp[iLoop+1] <= 0x46)
                    byteLow = (byte)(temp[iLoop+1] - 0x37);
                else if (temp[iLoop+1] >= 0x61 && temp[iLoop+1] <= 0x66)
                    byteLow = (byte)(temp[iLoop+1] - 0x57);
                else
                {
                    Logger.Instance().ErrorFormat("Char2Hex 错误，出现0~9，A~F以外的字符 temp[iLoop]={0}， iLoop={1}", temp[iLoop], iLoop);
                    buffer.ReceivedBuffer.Clear();
                    return false;
                }
                byteHigh &=0xF0;
                byteLow  &=0x0F;
                buffer.ReceivedBuffer.Add((byte)(byteHigh+byteLow));
                iLoop = iLoop+2;
            }
            Logger.Instance().InfoFormat("IP={0}, Char2Hex() Converted Char bytes={1}", szIP, BufferToString(temp));
            return true;
        }

        private byte[] Hex2Char(byte[] buffer)
        {
            List<byte> charBuffer = new List<byte>();
            charBuffer.Add(0x02);
            //charBuffer[length-1] = 0x03;
            byte byteHigh = 0x00;
            byte byteLow = 0x00;
            for(int iLoop = 0;iLoop<buffer.Length;iLoop++)
            {
                byteHigh = (byte)(buffer[iLoop] >> 4 & 0x0F);
                byteLow = (byte)(buffer[iLoop] & 0x0F);

                if (byteHigh >= 0x00 && byteHigh <= 0x09)
                    charBuffer.Add((byte)(byteHigh+0x30));
                else if (byteHigh >= 0x0A && byteHigh <= 0x0F)
                    charBuffer.Add((byte)(byteHigh+0x37));
                else
                    break;

                if (byteLow >= 0x00 && byteLow <= 0x09)
                    charBuffer.Add((byte)(byteLow + 0x30));
                else if (byteLow >= 0x0A && byteLow <= 0x0F)
                    charBuffer.Add((byte)(byteLow + 0x37));
                else
                    break;
            }
            charBuffer.Add(0x03);
            Logger.Instance().InfoFormat("Hex2Char() Raw bytes={0}", BufferToString(buffer));
            //Logger.Instance().InfoFormat("Hex2Char() Converted Char bytes={0}", BufferToString(charBuffer));
            return charBuffer.ToArray();
        }

        private string BufferToString(List<byte> buffer)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in buffer)
            {
                sb.Append(b.ToString("X2"));
                sb.Append(b.ToString(" "));
            }
            sb.Append("\r\n");
            return sb.ToString();
        }

        private string BufferToString(byte[] buffer)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in buffer)
            {
                sb.Append(b.ToString("X2"));
                sb.Append(b.ToString(" "));
            }
            //sb.Append("\r\n");
            return sb.ToString();
        }

        /// <summary>
        /// 发送给WIFI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MapIPFromWifi(object sender, DataTransmissionEventArgs args)
        {
            if(args.Token.ConnectSocket!=null)
            {
                AsyncSocketUserToken socket = args.Token;
                long ip = ControllerManager.GetLongIPFromSocket(socket);
                Controller controller = ControllerManager.Instance().Get(ip);
                if (controller != null)
                {
                    if (controller.SocketToken != null)
                        AsyncServer.Instance().CloseClientSocketEx(controller.SocketToken);
                    controller.SocketToken = socket;
                    if (SocketConnectOrCloseResponse != null)
                        SocketConnectOrCloseResponse(this, new SocketConnectArgs(true, socket));
                    //新连接的客户端要对它发送泵类型指令
                    if (SendPumpType2Wifi != null)
                        SendPumpType2Wifi(this, new SocketConnectArgs(true, socket));
                }
                else
                {
                    if (m_Device!=null)
                        m_Device.CloseClientSocket(args.Token);
                    Logger.Instance().ErrorFormat("MapIPFromWifi()->ControllerManager.Instance().Get 错误，IP={0}", ip);
                    return;
                }
            }
        }

         /// <summary>
        /// 删除序列号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void AntiMapIPFromWifi(object sender, DataTransmissionEventArgs args)
        {
            if(args.Token!=null)
            {
                AsyncSocketUserToken socket = args.Token;
                if (socket.ConnectSocket == null || socket.ConnectSocket.RemoteEndPoint == null)
                    return;
                if (SocketConnectOrCloseResponse != null)
                    SocketConnectOrCloseResponse(this, new SocketConnectArgs(false, socket));
                long ip = ControllerManager.GetLongIPFromSocket(socket);
                Controller controller = ControllerManager.Instance().Get(ip);
                if (controller != null)
                    controller.SocketToken = null;
                lock (m_HashReceivedBuffer)
                {
                    if (m_HashReceivedBuffer.ContainsKey(ip))
                        m_HashReceivedBuffer.Remove(ip);
                }
            }
        }

        /// <summary>
        /// 连接建立时，要将对应的命令队列添加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnCreateCommandQueue(object sender, DataTransmissionEventArgs args)
        {
            AsyncSocketUserToken socket = args.Token;
            if (socket != null)
            {
                long ip = ControllerManager.GetLongIPFromSocket(socket);
                m_Queue.CreateQueue(ip);
                m_WaitQueue.CreateQueue(ip);
            }
        }

        /// <summary>
        /// 连接关闭时，要将对应的命令队列清除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnRemoveCommandQueue(object sender, DataTransmissionEventArgs args)
        {
            AsyncSocketUserToken socket = args.Token;
            if (socket != null)
            {
                long ip = ControllerManager.GetLongIPFromSocket(socket);
                m_Queue.ClearQueue(ip);
                m_WaitQueue.ClearQueue(ip);
            }
        }
        
        /// <summary>
        /// 序列号收到后更新MAP
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommandResponse(object sender, EventArgs e)
        {
            //if(e is CmdUploadSN)
            //{
            //    CmdUploadSN cmd = e as CmdUploadSN;
            //    ushort sn =  cmd.SerialNo;
            //    //string strSN = Encoding.Default.GetString(sn, 0, sn.Length);
            //    //System.Diagnostics.Debug.WriteLine(cmd.SN.Count);
            //    Controller controller = SNManager.Instance().Get(sn);
            //    if (controller != null)
            //    {
            //        controller.SocketToken = cmd.RemoteSocket;
            //        if (SocketConnectOrCloseResponse != null)
            //            SocketConnectOrCloseResponse(this, new SocketConnectArgs(true, cmd.RemoteSocket));
            //    }
            //    else
            //    {
            //        Logger.Instance().ErrorFormat("CommandResponse()->SNManager.Instance().Get 错误，sn={0}", sn);
            //        return;
            //    }
            //}
        }

        /// <summary>
        /// 将字节转换成16进制字符，并形成字符串，此函数只用于记录日志
        /// </summary>
        /// <param name="buffer">要转换的数组</param>
        /// <param name="start">起始下标</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        private string BytesToString(List<byte> buffer, int start, int length)
        {
            StringBuilder sb = new StringBuilder();
            for(int i=start; i<start+length; i++)
            {
                sb.Append(buffer[i].ToString("X2"));
                sb.Append(" ");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将字节转换成16进制字符，并形成字符串，此函数只用于记录日志
        /// </summary>
        /// <param name="buffer">要转换的数组</param>
        /// <param name="start">起始下标</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        private string BytesToString(byte[] buffer, int start, int length)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = start; i < start + length; i++)
            {
                sb.Append(buffer[i].ToString("X2"));
                sb.Append(" ");
            }
            return sb.ToString();
        }

    }

    public class SocketBuffer
    {
        private bool       m_IsReady        = false;
        private List<byte> m_ReceivedBuffer = new List<byte>();
        private List<byte> m_ReceivedCharBuffer = new List<byte>();
        private AsyncSocketUserToken m_RemoteSock = null;
        //private Socket     m_RemoteSock     = null;
        private List<byte> m_CommandBuffer  = new List<byte>();   //分离出一个完整的命令结构
        private int        m_HeadReadCount  = 0;                 //

        /// <summary>
        /// 数据是否到达并且可读
        /// </summary>
        public bool IsReady
        {
            get { return m_IsReady;}
            set { m_IsReady = value;}
        }

        /// <summary>
        /// 接收到的数据缓冲区
        /// </summary>
        public List<byte> ReceivedBuffer
        {
            get { return m_ReceivedBuffer; }
            set { m_ReceivedBuffer = value; }
        }

        /// <summary>
        /// 接收到的数据缓冲区以字符0~9和A~F表示
        /// 所有新收到的数据必须进一步转换成原来协议上规定的格式
        /// </summary>
        public List<byte> ReceivedCharBuffer
        {
            get { return m_ReceivedCharBuffer; }
            set { m_ReceivedCharBuffer = value; }
        }

        /// <summary>
        /// 客户端SOCKET
        /// </summary>
        public AsyncSocketUserToken RemoteSock
        {
            get { return m_RemoteSock; }
            set { m_RemoteSock = value; }
        }

        ///// <summary>
        ///// 客户端SOCKET
        ///// </summary>
        //public Socket RemoteSock
        //{
        //    get { return m_RemoteSock; }
        //    set { m_RemoteSock = value; }
        //}


        /// <summary>
        /// 临时数据缓冲区
        /// </summary>
        public List<byte> CommandBuffer
        {
            get { return m_CommandBuffer; }
            set { m_CommandBuffer = value; }
        }

        /// <summary>
        /// 读头字节数量
        /// </summary>
        public int HeadReadCount
        {
            get { return m_HeadReadCount; }
            set { m_HeadReadCount = value; }
        }

        public SocketBuffer()
        { }

        public SocketBuffer(bool isReady, List<byte> buffer, AsyncSocketUserToken remoteSock)
        {
            m_ReceivedBuffer.Clear();
            m_IsReady        = isReady;
            m_ReceivedBuffer.AddRange(buffer);  //不要引用，而是要复制
            m_RemoteSock     = remoteSock;
        }



    }
   
}
