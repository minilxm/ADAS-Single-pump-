using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using Trans;

namespace AsyncSocket
{
    /// <summary>
    /// 本类需要在Config文件中配置的变量有ParallelNum、m_Wait4ClientTimeOut
    /// </summary>
    public class AsyncServer
    {
        public event EventHandler<EventArgs>                 ResponseAffterExit;           //当服务关闭时，通知上层应用 
        public event EventHandler<DataTransmissionEventArgs> DataReceived;
        public event EventHandler<DataTransmissionEventArgs> MappingIP;                     //请求序列号 
        public event EventHandler<DataTransmissionEventArgs> AntiMappingIP;                      //连接断开删除对应序列号 
        public event EventHandler<DataTransmissionEventArgs> CreateCommandQueue;            //当新连接到来时，将命令队列添加一个 
        public event EventHandler<DataTransmissionEventArgs> RemoveCommandQueue;            //当连接关闭时，将命令队列移除一个 

        private bool                     m_bStartAccept                 = true;             //是否停止接受新的连接
        private bool                     m_IsStarted                    = false;
        private static AsyncServer       m_Server                       = null;
        private Socket                   m_ListenSocket;
        private int                      m_TcpServerPort                = 20160;            //默认TCP服务端口号
        private int                      m_NumConnections               = 80;              //最大支持连接个数
        private int                      m_ReceiveBufferSize;                               //每个连接接收缓存大小
        private Semaphore                m_MaxNumberAcceptedClients;                        //限制访问接收连接的线程数，用来控制最大并发数
        private int                      m_SocketTimeOut                = 4 * 60 * 1000;   //如果客户端在4分钟内没有数据交流，则表示断开
        private DaemonThread             m_DaemonThread;
        private AsyncSocketUserTokenPool m_AsyncSocketUserTokenPool;
        private AsyncSocketUserTokenList m_AsyncSocketUserTokenList;

        public AsyncSocketUserTokenList AsyncSocketUserTokenList
        {
            get { return m_AsyncSocketUserTokenList; }
        }

        /// <summary>
        /// 客户端与服务端的超时时间
        /// </summary>
        public int SocketTimeOut
        {
            get { return m_SocketTimeOut; }
            set { m_SocketTimeOut = value; }
        }

        /// <summary>
        /// 由外部传入TCP服务端口号
        /// </summary>
        public int TcpServerPort
        {
            get { return m_TcpServerPort; }
            set { m_TcpServerPort = value; }
        }

        public static AsyncServer Instance()
        {
            if (m_Server == null)
                m_Server = new AsyncServer();
            return m_Server;
        }

        private AsyncServer()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            int parallelNum = 0, wait4ClientTimeOut = 0;
            //默认可以同时接收500个连接请求。收到请求后如果数量超过1个，则弹出列表供用户选择
            if (!(int.TryParse(config.AppSettings.Settings["ParallelNum"].Value, out parallelNum)))
                parallelNum = 500;

            m_NumConnections           = parallelNum;
            m_ReceiveBufferSize        = ProtocolConst.ReceiveBufferSize;
            m_AsyncSocketUserTokenPool = new AsyncSocketUserTokenPool(parallelNum);
            m_AsyncSocketUserTokenList = new AsyncSocketUserTokenList();
            m_MaxNumberAcceptedClients = new Semaphore(parallelNum, parallelNum);
        }
    

        /// <summary>
        /// 启动TCP服务，以及幽灵线程（判断是否断开连接）
        /// </summary>
        /// <param name="localEndPoint"></param>
        public void Start()
        {
            if(!m_IsStarted)
            {
                Init();
                IPEndPoint listenPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), m_TcpServerPort);
                try
                { 
                    if( m_ListenSocket==null )
                        m_ListenSocket = new Socket(listenPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    m_ListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    m_ListenSocket.Bind(listenPoint);
                    m_ListenSocket.Listen(m_NumConnections);
                    Logger.Instance().InfoFormat("Start listen socket {0} success", listenPoint.ToString());
                    StartAccept(null);
                    m_DaemonThread = new DaemonThread(this);
                }
                catch(Exception e)
                {
                    //启动服务失败，记录日志
                    m_IsStarted = false;
                    Logger.Instance().ErrorFormat("AsyncServer::Start() Failed Message={0}", e.Message);
                }
                m_IsStarted = true;
            }
            else
                Logger.Instance().Info("Server has been started.");
        }

        public bool IsStart()
        {
            return m_IsStarted;
        }
        /// <summary>
        /// 针对单一SOCKET进行数据发送
        /// </summary>
        /// <param name="connectSocket">需要发送的Socket对象</param>
        /// <param name="sendEventArgs">Socket事件</param>
        /// <param name="buffer">数据区</param>
        /// <param name="offset">数据起始下标</param>
        /// <param name="count">数据字节长度</param>
        /// <returns></returns>
        public bool SendAsyncEvent(Socket connectSocket, SocketAsyncEventArgs sendEventArgs, byte[] buffer, int offset, int count)
        {
            if (connectSocket == null)
                return false;
            sendEventArgs.SetBuffer(buffer, offset, count);
            bool willRaiseEvent = connectSocket.SendAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                return ProcessSend(sendEventArgs);
            }
            else
                return true;
        }

        /// <summary>
        /// 针对单一SOCKET进行数据发送
        /// </summary>
        /// <param name="connectSocket">需要发送的Socket对象</param>
        /// <param name="sendEventArgs">Socket事件</param>
        /// <param name="buffer">数据区</param>
        /// <param name="offset">数据起始下标</param>
        /// <param name="count">数据字节长度</param>
        /// <returns></returns>
        public bool SendAsyncEvent(AsyncSocketUserToken connectSocket, SocketAsyncEventArgs sendEventArgs, byte[] buffer, int offset, int count)
        {
            if (connectSocket == null)
                return false;
            sendEventArgs.SetBuffer(buffer, offset, count);
            bool willRaiseEvent = connectSocket.ConnectSocket.SendAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                return ProcessSend(sendEventArgs);
            }
            else
                return true;
        }

        /// <summary>
        /// 针对单一SOCKET进行数据发送
        /// </summary>
        /// <param name="connectSocket">需要发送的目标Socket对象</param>
        /// <param name="buffer">数据区</param>
        /// <param name="offset">数据起始下标</param>
        /// <param name="count">数据字节长度</param>
        /// <returns></returns>
        public bool SendAsyncEvent(Socket connectSocket, byte[] buffer, int offset, int count)
        {
            if (connectSocket == null)
                return false;
            SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();
            sendEventArgs.SetBuffer(buffer, offset, count);
            bool willRaiseEvent = connectSocket.SendAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                return ProcessSend(sendEventArgs);
            }
            else
                return true;
        }

        public bool SendAsyncEvent(AsyncSocketUserToken connectSocket, byte[] buffer, int offset, int count)
        {
            if (connectSocket == null || connectSocket.ConnectSocket==null)
                return false;
            SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();
            sendEventArgs.SetBuffer(buffer, offset, count);

            bool willRaiseEvent = connectSocket.ConnectSocket.SendAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                return ProcessSend(sendEventArgs);
            }
            else
                return true;
        }

        /// <summary>
        /// 关闭单个客户端
        /// </summary>
        /// <param name="userToken"></param>
        public void CloseClientSocket(AsyncSocketUserToken userToken)
        {
            if (userToken == null || userToken.ConnectSocket == null)
                return;
            if (AntiMappingIP != null)
                AntiMappingIP(this, new DataTransmissionEventArgs(userToken));
            if (RemoveCommandQueue != null)
            {
                RemoveCommandQueue(this, new DataTransmissionEventArgs(userToken));
            }
            string socketInfo = string.Format("Local Address: {0} Remote Address: {1}", userToken.ConnectSocket.LocalEndPoint,
                userToken.ConnectSocket.RemoteEndPoint);
            Logger.Instance().InfoFormat("Client connection disconnected. {0}", socketInfo);
            try
            {
                if(userToken.ConnectSocket!=null)
                    userToken.ConnectSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                Logger.Instance().ErrorFormat("CloseClientSocket()-> Disconnect client {0} error, message: {1}", socketInfo, e.Message);
            }
            try
            {
                if (userToken.ConnectSocket != null)
                {
                    userToken.ConnectSocket.Close();
                    userToken.ConnectSocket = null; //释放引用，并清理缓存，包括释放协议对象等资源
                }
            }
            catch
            {
                Logger.Instance().Error("CloseClientSocket()-> userToken.ConnectSocket.Close() 出错");
            }
            m_MaxNumberAcceptedClients.Release();
            if (userToken != null)
            {
                m_AsyncSocketUserTokenPool.Push(userToken);
                m_AsyncSocketUserTokenList.Remove(userToken);
            }

            ///当最后一个连接被关闭时，停止服务端，需要用户重新发送广播后再建立连接
            //if(m_AsyncSocketUserTokenList.Count==0)
            //{
            //    Exit();
            //}
        }

        public void CloseClientSocketEx(AsyncSocketUserToken userToken)
        {
            if (userToken==null || userToken.ConnectSocket == null)
                return;
            string socketInfo = string.Format("Local Address: {0} Remote Address: {1}", userToken.ConnectSocket.LocalEndPoint,
              userToken.ConnectSocket.RemoteEndPoint);
            Logger.Instance().InfoFormat("CloseClientSocketEx()->Client connection disconnected. {0}", socketInfo);
            try
            {
                if (userToken.ConnectSocket != null)
                    userToken.ConnectSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                Logger.Instance().ErrorFormat("CloseClientSocketEx()->Disconnect client {0} error, message: {1}", socketInfo, e.Message);
            }
            userToken.ConnectSocket.Close();
            userToken.ConnectSocket = null; //释放引用，并清理缓存，包括释放协议对象等资源

            m_MaxNumberAcceptedClients.Release();
            m_AsyncSocketUserTokenPool.Push(userToken);
            m_AsyncSocketUserTokenList.Remove(userToken);
            userToken = null;
        }

        /// <summary>
        /// 服务停止时，必须调用
        /// </summary>
        public void Exit()
        {
            m_DaemonThread.Close();
            lock (m_AsyncSocketUserTokenList)
            {
                AsyncSocketUserToken[] userTokenArray = null;
                m_AsyncSocketUserTokenList.CopyList(ref userTokenArray);
                for (int i = 0; i < userTokenArray.Length; i++)
                    CloseClientSocket(userTokenArray[i]);
            }
            m_IsStarted = false;
            m_bStartAccept = true;
            if (m_ListenSocket != null)
            {
                m_ListenSocket.Close();
                m_ListenSocket.Dispose();
                m_ListenSocket = null;
            }
            GC.Collect();
            if (ResponseAffterExit != null)
                ResponseAffterExit(this, null);
        }

        public void CloseAllClient()
        {
            lock (m_AsyncSocketUserTokenList)
            {
                AsyncSocketUserToken[] userTokenArray = null;
                m_AsyncSocketUserTokenList.CopyList(ref userTokenArray);
                for (int i = 0; i < userTokenArray.Length; i++)
                    CloseClientSocket(userTokenArray[i]);
                m_AsyncSocketUserTokenList.RemoveAll();
            }
        }

        /// <summary>
        /// 初始化连接池，及连接个数
        /// </summary>
        private void Init()
        {
            m_AsyncSocketUserTokenPool.Clear();
            AsyncSocketUserToken userToken;
            for (int i = 0; i < m_NumConnections; i++) //按照连接数建立读写对象
            {
                userToken = new AsyncSocketUserToken(m_ReceiveBufferSize);
                userToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                userToken.SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                m_AsyncSocketUserTokenPool.Push(userToken);
            }
        }

        /// <summary>
        /// 停止新的连接
        /// </summary>
        private void StopAccept()
        {
            m_bStartAccept = false;
        }

        /// <summary>
        /// 接收新的TCP客户的连接
        /// </summary>
        /// <param name="acceptEventArgs"></param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            if(!m_bStartAccept)
            {
                Logger.Instance().Info("StartAccept()->StartAccept Stopped");
                return;
            }
            if (acceptEventArgs == null)
            {
                acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            }
            else
                acceptEventArgs.AcceptSocket = null;    //释放上次绑定的Socket，等待下一个Socket连接
            m_MaxNumberAcceptedClients.WaitOne();       //获取信号量
            if (m_ListenSocket == null)
                return;
            bool willRaiseEvent = m_ListenSocket.AcceptAsync(acceptEventArgs);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArgs);
            }
        }

        /// <summary>
        /// 接受事件完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="acceptEventArgs"></param>
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs acceptEventArgs)
        {
            try
            {
                if (!m_bStartAccept)
                {
                    Logger.Instance().Info("OnAcceptCompleted()->StartAccept Stopped");
                    return;
                }
                else
                    ProcessAccept(acceptEventArgs);
            }
            catch (Exception e)
            {
                Logger.Instance().ErrorFormat("Accept client {0} error, message: {1}", acceptEventArgs.AcceptSocket, e.Message);
                Logger.Instance().Error(e.StackTrace);
            }
            finally
            {
                //StartAccept(acceptEventArgs); //把当前异步事件释放，等待下次连接
            }
        }

        /// <summary>
        /// 处理新连接事件
        /// </summary>
        /// <param name="acceptEventArgs"></param>
        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            Logger.Instance().InfoFormat("Client connection accepted. Local Address: {0}, Remote Address: {1}",
                acceptEventArgs.AcceptSocket.LocalEndPoint, acceptEventArgs.AcceptSocket.RemoteEndPoint);

            AsyncSocketUserToken userToken = m_AsyncSocketUserTokenPool.Pop();  //从备用池里取出来，主要是为了控制并发数量
            m_AsyncSocketUserTokenList.Add(userToken);                          //添加到正在连接列表
            userToken.ConnectSocket = acceptEventArgs.AcceptSocket;
            userToken.ConnectDateTime = DateTime.Now;
            userToken.ActiveDateTime = DateTime.Now;
            //第一次连接要请求序列号
            if(MappingIP!=null)
            {
                MappingIP(this, new DataTransmissionEventArgs(userToken));
            }
            if(CreateCommandQueue!=null)
            {
                CreateCommandQueue(this, new DataTransmissionEventArgs(userToken));
            }
            try
            {
                //如果 I/O 操作挂起，将返回 true。 操作完成时，将引发 e 参数的 SocketAsyncEventArgs.Completed 事件。 
                //如果 I/O 操作同步完成，将返回 false。 在这种情况下，将不会引发 e 参数的 SocketAsyncEventArgs.Completed 事件，并且可能在方法调用返回后立即检查作为参数传递的 e 对象以检索操作的结果。 
                bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                if (!willRaiseEvent)
                {
                    lock (userToken)
                    {
                        ProcessReceive(userToken.ReceiveEventArgs);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Instance().ErrorFormat("Accept client {0} error, message: {1}", userToken.ConnectSocket, e.Message);
                Logger.Instance().Error(e.StackTrace);
            }
            if (m_bStartAccept)
            {
                StartAccept(acceptEventArgs); //把当前异步事件释放，等待下次连接
            }
            else
            {
                Logger.Instance().Info("ProcessAccept()->StartAccept Stopped");
            }
        }

        /// <summary>
        /// IO事件完成，包括远程SOCKET断开，数据到达
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="asyncEventArgs"></param>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            AsyncSocketUserToken userToken = asyncEventArgs.UserToken as AsyncSocketUserToken;
            userToken.ActiveDateTime = DateTime.Now;
            try
            {
                lock (userToken)
                {
                    if (asyncEventArgs.LastOperation == SocketAsyncOperation.Receive)
                        ProcessReceive(asyncEventArgs);
                    else if (asyncEventArgs.LastOperation == SocketAsyncOperation.Send)
                        ProcessSend(asyncEventArgs);
                    else
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (Exception e)
            {
                Logger.Instance().ErrorFormat("OnIOCompleted {0} error, message: {1}", userToken.ConnectSocket, e.Message);
                Logger.Instance().Error(e.StackTrace);
            }
        }

        /// <summary>
        /// 处理数据接收
        /// </summary>
        /// <param name="receiveEventArgs"></param>
        private void ProcessReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            AsyncSocketUserToken userToken = receiveEventArgs.UserToken as AsyncSocketUserToken;
            if (userToken.ConnectSocket == null)
                return;
            userToken.ActiveDateTime = DateTime.Now;
            if (userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                if (userToken.ReceiveEventArgs.BytesTransferred > 0)
                {
                    int offset = userToken.ReceiveEventArgs.Offset;
                    int count = userToken.ReceiveEventArgs.BytesTransferred;
                    if (count <= 0)
                    {
                        bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                        if (!willRaiseEvent)
                            ProcessReceive(userToken.ReceiveEventArgs);
                    }
                    else
                    {
                        if (DataReceived != null)
                        {
                            DataReceived(this, new DataTransmissionEventArgs(userToken.ReceiveEventArgs.Buffer, offset, count, userToken));
                        }
                        bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                        if (!willRaiseEvent)
                            ProcessReceive(userToken.ReceiveEventArgs);
                    }
                }
                else
                {
                    Logger.Instance().ErrorFormat("ProcessReceive()->BytesTransferred = {0}", userToken.ReceiveEventArgs.BytesTransferred);
                }
            }
            else if (userToken.ReceiveEventArgs.SocketError == SocketError.ConnectionReset)
            {
                Logger.Instance().ErrorFormat("ProcessReceive()->userToken.ReceiveEventArgs.SocketError={0}", userToken.ReceiveEventArgs.SocketError);
                CloseClientSocket(userToken);
            }
            else
            {
                Logger.Instance().ErrorFormat("ProcessReceive()->userToken.ReceiveEventArgs.SocketError={0}", userToken.ReceiveEventArgs.SocketError);
            }

        }

        /// <summary>
        /// 处理数据发送
        /// </summary>
        /// <param name="sendEventArgs"></param>
        /// <returns></returns>
        private bool ProcessSend(SocketAsyncEventArgs sendEventArgs)
        {
            AsyncSocketUserToken userToken = sendEventArgs.UserToken as AsyncSocketUserToken;
            if (userToken==null || userToken.AsyncSocketInvokeElement == null)
                return false;
            userToken.ActiveDateTime = DateTime.Now;
            if (sendEventArgs.SocketError == SocketError.Success)
                return userToken.AsyncSocketInvokeElement.SendCompleted(); //调用子类回调函数
            else
            {
                CloseClientSocket(userToken);
                return false;
            }
        }

        #region 定时器这里用不着 
        /*
        private void StopTimer()
        {
            m_Wait4ClientTimer.Stop();              //停止时钟，以后进入连接的客户端被忽略
            StopAccept();                           //停止新的连接
        }

        private void OnTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            StopTimer();
            AsyncSocketUserToken[] userTokenArray = null;
            lock (m_AsyncSocketUserTokenList)
            {
                m_AsyncSocketUserTokenList.CopyList(ref userTokenArray);
            }
            ClientInfoArgs clientInfo = new ClientInfoArgs();
            if(userTokenArray.Length>0)
            {
                 for (int i = 0; i < userTokenArray.Length; i++)
                 {
                     if(userTokenArray[i].ConnectSocket.RemoteEndPoint is IPEndPoint)
                     {
                         IPEndPoint endpoint = userTokenArray[i].ConnectSocket.RemoteEndPoint as IPEndPoint;
                         clientInfo.ClientPoint.Add(new IPEndPoint(endpoint.Address, endpoint.Port));
                     }
                 }
                //将接收到的IP上传给上层
                if( null!=ShowAndFilterConnectedClients )
                    ShowAndFilterConnectedClients(this, clientInfo);
            }
            else
            {
                Exit();
            }
        }
        */
        #endregion

        ///// <summary>
        ///// 踢出除选中的客户端以外的连接
        ///// </summary>
        ///// <param name="point"></param>
        //public void ExcludeOtherClientSocket(IPEndPoint point)
        //{
        //    lock (m_AsyncSocketUserTokenList)
        //    {
        //        int index = m_AsyncSocketUserTokenList.Find(point);
        //        AsyncSocketUserToken[] userTokenArray = null;
        //        m_AsyncSocketUserTokenList.CopyList(ref userTokenArray);
        //        for (int i = 0; i < userTokenArray.Length; i++)
        //        { 
        //            if(i!=index)
        //                CloseClientSocket(userTokenArray[i]);
        //        }
        //        m_AsyncSocketUserTokenList.RemoveAllWithout(point);
        //    }
        //}
    }
}
