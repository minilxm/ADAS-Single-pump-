using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Excel = Microsoft.Office.Interop.Excel;
using Cmd;
using Analyse;
using log4net;

namespace  AgingSystem
{
    /// <summary>
    /// 货架
    /// </summary>
    public partial class AgingDock : UserControl
    {
        public event EventHandler<ConfigrationArgs>     OnConfigrationCompleted;
        public event EventHandler<SelectedPumpsArgs>    OnPumpSelected;
        public delegate void DeleStartTimer();
        public delegate void DeleShowRedAlarm();
        public delegate void DeleEnableControls(bool bEnabled);
        public delegate void DeleSetBackGround(System.Windows.Media.Color color, EAgingStatus agingStatus);
        public delegate void DeleSetTitleAndEnable(string title, bool enable);
        

        private int                   m_TimeOut            = 5000;                                       //超时5秒,从配置文件读取
        private int                   m_DockNo             = 0;                                          //货架号
        private System.Timers.Timer   m_ChargeTimer        = new System.Timers.Timer();                  //充电时钟
        private System.Timers.Timer   m_RechargeTimer      = new System.Timers.Timer();                  //放电电时钟
        private Hashtable             m_DockParameter      = new Hashtable();                            //存放每个货架的配置信息（int 货架号，AgingParameter）
        private Hashtable             m_HashPumps          = new Hashtable();                            //单个货架上的选中的泵位置信息列表<DockNO, List<Tuple<int,int,int>>(int pumpLocation,int rowNo,int colNo )
        private Hashtable             m_HashRowNo          = new Hashtable();                            //单个货架上的选中的泵通道编号，即一个控制器有命令中的通道编号（int 行号,int 通道号位计算）
        private CommandManage         m_CmdManager         = new CommandManage();                        //发送命令对象，通过它来发送所有命令
        private AutoResetEvent        m_EventSendPumpType  = new AutoResetEvent(false);                  //发送CmdSendPumpType命令事件
        private AutoResetEvent        m_EventCmdCharge     = new AutoResetEvent(false);                  //发送CmdCharge命令事件
        private AutoResetEvent        m_EventCmdDischarge  = new AutoResetEvent(false);                  //发送CmdDisCharge命令事件
        private bool                  m_bStartAging        = false;                                      //老化开始标识，true:开始，false停止
        private DateTime              m_StartAgingTime     = DateTime.MinValue;                          //老化开始时间戳
        private DateTime              m_StoptAgingTime     = DateTime.MinValue;                          //老化结束时间戳
        //private DateTime              m_StartRechargeTime  = DateTime.MinValue;                          //老化补电开始时间戳,最后一个泵的开始补电算起
        private DateTime              m_StopRechargeTime   = DateTime.MinValue;                          //老化补电结束时间戳
        private DateTime              m_StartDischargeTime = DateTime.MinValue;                          //老化放电开始时间戳
        private DateTime              m_StopDischargeTime  = DateTime.MinValue;                          //老化放电结束时间戳
        private List<Controller>      m_Controllers        = new List<Controller>();                     //控制器列表,每个货架都有一定数量的控制器，每个控制器都有一定数量的泵
        private EAgingStatus          m_AgingStatus        = EAgingStatus.Unknown;                       //机架也有状态
        private System.Windows.Media.Color  m_DefaultColor = Colors.Gray;                                //机架默认颜色
        private Thread                m_TaskThread         = null;                                       //任务线程，防止界面卡死
                                                                                                         //以下全是与CheckPumpRunningStatus相关的定义
        private List<Thread> m_CheckPumpRunningStatusThreads = new List<Thread>();                       //新建几个线程，每个线程对应一个控制器，当点击开始老化后，5个线程在60分钟内负责检查所有泵是否启动，如未启动，则需要重新发送启动命令
        private List<Thread> m_CheckPumpStopStatusThreads = new List<Thread>();                          //新建几个线程，每个线程对应一个控制器，当点击停止后，5个线程在5分钟内负责检查所有泵是否停止
        private AutoResetEvent        m_EventCheckPumpRunningStatus = new AutoResetEvent(false);         //启动成功以后此事件有信号，触发检查泵状态线程
        private bool                  m_bKeepCheckPumpStatus = true;
        private bool                  m_bKeepCheckPumpStopStatus = false;                                //用于检查停止后，泵有没有停止
        private bool                  m_bCanStartCheck              = false;                             //是否可以启动检查线程
        private ProductID             m_CurrentProductID            = ProductID.Unknow;
        private ushort                m_QueryInterval               = 60;
        private System.Timers.Timer   m_CheckPumpStatusTimer        = new System.Timers.Timer();         //设置检查泵状态持续时钟，主要用于计时，超时将置一个状态
        private System.Timers.Timer   m_CheckPumpStopStatusTimer        = new System.Timers.Timer();     //设置检查泵停止状态持续时钟，主要用于计时，超时将置一个状态
        private DateTime              m_StartCheckPumpStatusTime    = DateTime.Now;                      //开始检查状态时间，根据此时间计时
        private DateTime              m_StartCheckPumpStopStatusTime= DateTime.Now;                      //开始检查停止状态时间，根据此时间计时
        private AutoResetEvent        m_EventAllPumpStoped          = new AutoResetEvent(false);         //所有泵都停止了，或者超时了
        
        //以上全是与CheckPumpRunningStatus相关的定义

        //以下定义老化放电线程必须的变量 20170701
        private bool m_bKeepDisCharge = true;
        private System.Timers.Timer m_CheckDisChargeTimer = new System.Timers.Timer();         //设置检查放电持续时钟，主要用于计时，大约30分钟
        private DateTime m_StartCheckDisChargeTime = DateTime.Now;                             //开始线程开始放电时间，根据此时间倒计时
        private Hashtable m_HashDisChargeController = new Hashtable();                         //<ip,bool>控制器IP，是否已经放电

        //以下定义老化补电线程必须变量
        private DepletePumpManager m_DepleteManager = new DepletePumpManager();                //多线程共同访问需要加锁
        private bool m_bKeepReCharge = true;

        private DateTime m_LastRechargeTime = DateTime.MinValue;                                //货架上最后一个补电开始时间



        /// <summary>
        /// 货架号
        /// </summary>
        public int DockNo
        {
            get { return m_DockNo;}
            set { m_DockNo = value;}
        }

        /// <summary>
        /// 某个货车架上的选中的通道号
        /// </summary>
        public Hashtable ChannelHashRowNo
        {
            get { return m_HashRowNo;}
        }

        /// <summary>
        /// 货架状态
        /// </summary>
        public EAgingStatus AgingStatus
        {
            get { return m_AgingStatus; }
            set { m_AgingStatus = value; }
        }

        /// <summary>
        /// 机架默认颜色
        /// </summary>
        public System.Windows.Media.Color DefaultColor
        {
            get { return m_DefaultColor; }
            set { m_DefaultColor = value; }
        }

        /// <summary>
        /// 是否已开始老化
        /// </summary>
        public bool IsStartAging
        {
            get { return m_bStartAging;}
            set { m_bStartAging = value;}
        }

        public AgingDock()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置等待命令返回超时时间
        /// </summary>
        /// <param name="timeout"></param>
        public void SetTimeOut(int timeout)
        {
            m_TimeOut = timeout;
        }

        private void Dock_Loaded(object sender, RoutedEventArgs e)
        {
            //dockGrid.Background = new SolidColorBrush(Colors.Gray);
            //从全局变量中找到自己的控制器，一开始所有控制器都是没有连接的，
            //对象引用
            m_Controllers = ControllerManager.Instance().Get(m_DockNo); 
        }

        public BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            Bitmap bitmapSource = new Bitmap(bitmap.Width, bitmap.Height);
            int i, j;
            for (i = 0; i < bitmap.Width; i++)
                for (j = 0; j < bitmap.Height; j++)
                {
                    System.Drawing.Color pixelColor = bitmap.GetPixel(i, j);
                    System.Drawing.Color newColor = System.Drawing.Color.FromArgb(pixelColor.R, pixelColor.G, pixelColor.B);
                    bitmapSource.SetPixel(i, j, newColor);
                }
            MemoryStream ms = new MemoryStream();
            bitmapSource.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(ms.ToArray());
            bitmapImage.EndInit();
            return bitmapImage;
        }

        public BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        {
            IntPtr ptr = bitmap.GetHbitmap();
            BitmapSource result = Imaging.CreateBitmapSourceFromHBitmap(ptr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(ptr);
            return result;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public void SetTitle(string title)
        {
            lbDockName.Content = title;
        }

        #region 回调函数
        /// <summary>
        /// 选择需要老化的泵
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectedPumps(object sender, SelectedPumpsArgs e)
        {
            m_HashPumps = e.SelectedPumps;
            GenChannel();
        }

        /// <summary>
        /// 保存配置信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSaveConfigration(object sender, ConfigrationArgs e)
        {
            m_DockParameter = e.DockParameter;
        }

        /// <summary>
        /// 回应命令响应,回应的命令中全带有序列号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommandResponse(object sender, EventArgs e)
        {
            if (e is CmdSendPumpType)
            {
                CmdSendPumpType cmd = e as CmdSendPumpType;
                HandleCmdSendPumpTypeResponse(cmd);
            }
            else if (e is CmdCharge)
            {
                CmdCharge cmd = e as CmdCharge;
                HandleCmdChargeResponse(cmd);
            }
            else if (e is CmdDischarge)
            {
                CmdDischarge cmd = e as CmdDischarge;
                HandleCmdDischargeResponse(cmd);
            }
        }

        /// <summary>
        /// 接收到了客户端的确认消息
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleCmdSendPumpTypeResponse(CmdSendPumpType cmd)
        {
            if (cmd != null && cmd.RemoteSocket != null)
            {
                m_EventSendPumpType.Set();//命令返回，设置有信号状态
            }
            else
            {
                Logger.Instance().ErrorFormat("AgingDock::HandleCmdSendPumpTypeResponse() Error, 参数为null");
                return;
            }
        }

         /// <summary>
        /// 接收到了客户端的确认消息
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleCmdChargeResponse(CmdCharge cmd)
        {
            if (cmd != null && cmd.RemoteSocket != null)
            {
                m_EventCmdCharge.Set();//命令返回，设置有信号状态
            }
            else
            {
                Logger.Instance().ErrorFormat("AgingDock::HandleCmdChargeResponse() Error, 参数为null");
                return;
            }
        }
       
        /// <summary>
        /// 收到了CmdDischarge命令
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleCmdDischargeResponse(CmdDischarge cmd)
        {
            if (cmd != null && cmd.RemoteSocket != null)
            {
                m_EventCmdDischarge.Set();//命令返回，设置有信号状态
            }
            else
            {
                Logger.Instance().ErrorFormat("AgingDock::HandleCmdDischargeResponse() Error, 参数为null");
                return;
            }
        }
        
        /// <summary>
        /// 回应命令响应,回应的命令中全带有序列号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommandTimeoutResponse(object sender, EventArgs e)
        {
            if (e is CmdSendPumpType)
            {
                CmdSendPumpType cmd = e as CmdSendPumpType;
                HandleTimeoutCmdSendPumpTypeResponse(cmd);
            }
            else if (e is CmdCharge)
            {
                CmdCharge cmd = e as CmdCharge;
                HandleTimeoutCmdChargeResponse(cmd);
            }
            else if (e is CmdDischarge)
            {
                CmdDischarge cmd = e as CmdDischarge;
                HandleTimeoutCmdDischargeResponse(cmd);
            }
        }

        /// <summary>
        /// CmdSendPumpType命令超时处理
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleTimeoutCmdSendPumpTypeResponse(CmdSendPumpType cmd)
        {
            if (cmd != null)
            {
                long ip = ControllerManager.GetLongIPFromSocket(cmd.RemoteSocket);
                int row = ControllerManager.Instance().Get(ip).RowNo;
                int dockNo = ControllerManager.Instance().Get(ip).DockNo;
                string msg = string.Format("推送泵信息时，{0}号货架第{1}层控制器无响应！", dockNo, row);
                MessageBox.Show(msg);
            }
            else
            {
                Logger.Instance().ErrorFormat("AgingDock::HandleTimeoutCmdSendPumpTypeResponse() Error, 参数为null");
                return;
            }
        }

        /// <summary>
        /// CmdCharge命令超时处理
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleTimeoutCmdChargeResponse(CmdCharge cmd)
        {
            if (cmd != null)
            {
                long ip = ControllerManager.GetLongIPFromSocket(cmd.RemoteSocket);
                int row = ControllerManager.Instance().Get(ip).RowNo;
                int dockNo = ControllerManager.Instance().Get(ip).DockNo;
                string msg = string.Format("{0}号货架第{1}层控制器老化充电无响应！", dockNo, row);
                MessageBox.Show(msg);
            }
            else
            {
                Logger.Instance().ErrorFormat("AgingDock::HandleTimeoutCmdChargeResponse() Error, 参数为null");
                return;
            }
        }

        /// <summary>
        /// CmdDisCharge命令超时处理
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleTimeoutCmdDischargeResponse(CmdDischarge cmd)
        {
            if (cmd != null)
            {
                long ip = ControllerManager.GetLongIPFromSocket(cmd.RemoteSocket);
                int row = ControllerManager.Instance().Get(ip).RowNo;
                int dockNo = ControllerManager.Instance().Get(ip).DockNo;
                string msg = string.Format("{0}号货架第{1}层控制器老化放电无响应！", dockNo, row);
                MessageBox.Show(msg);
            }
            else
            {
                Logger.Instance().ErrorFormat("AgingDock::HandleTimeoutCmdChargeResponse() Error, 参数为null");
                return;
            }
        }
        
        #endregion

        #region Timer
        /// <summary>
        /// 启动充电时钟
        /// </summary>
        private void StartChargeTimer()
        {
            StopChargeTimer();
#if DEBUG
            m_ChargeTimer.Interval = 10*1000;//parameter.ChargeTime * 3600 * 1000;
#else
            m_ChargeTimer.Interval = 2*60*1000;
#endif
            m_ChargeTimer.Elapsed += OnChargeTimer;
            m_ChargeTimer.Start();
        }
        private void StopChargeTimer()
        {
            m_ChargeTimer.Elapsed -= OnChargeTimer;
            m_ChargeTimer.Stop();
        }
        private void OnChargeTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimeSpan span = DateTime.Now - m_StartAgingTime;
            AgingParameter parameter = m_DockParameter[m_DockNo] as AgingParameter;
#if DEBUG
            if (span.TotalMinutes >= 0.2)
#else
            if(span.TotalMinutes >= (double)parameter.ChargeTime * 60)

#endif
            {
                Logger.Instance().Info("充电时间已满，准备开始老化放电！");
                StopChargeTimer();
                //此处调用老化放电命令，因为上电时间是同时的，所以，放电时间也是同时的，不用每个泵单独发
                //m_CmdManager.SendCmdDischarge()
                //找出那些已经连接的SOCKET
                //List<Controller> controller = ControllerManager.Instance().Get(m_DockNo);
                if (m_Controllers.Count <= 0)
                {
                    MessageBox.Show("配置文件出错，未检测到相关货架的配置文件！");
                    return;
                }
                //启动放电线程，30分钟倒计时，处理放电。给每个控制器发送放电命令后等待反馈，超时后进入下一个发送周期
                CreateCheckDisChargeThread();
                //int iChannel = 0;
                //byte channel = 0;
                //List<Controller> controllers = m_Controllers.FindAll((x) => { return x.SocketToken != null; });
                //if (controllers != null && controllers.Count > 0)
                //{
                //for (int i = 0; i < controllers.Count; i++)
                //{
                //    if(!m_HashRowNo.ContainsKey(controllers[i].RowNo))
                //        continue;
                //    iChannel = (int)m_HashRowNo[controllers[i].RowNo];
                //    channel = (byte)(iChannel & 0x000000FF);
                //    //老化放电命令不要回调函数，因为控制器可能在发送放电命令时掉线了
                //    //如果没有回应就不发了，等控制器重新连接上时，它会自动发送报警数据包，此包中带有泵的电源信息，
                //    //如果是AC电源，则表示还没有进行放电操作，重新发送放电指令。
                //    m_CmdManager.SendCmdDischarge(controllers[i].SocketToken, null, null, channel);   
                //}
                //foreach(var obj in controllers)
                //{
                //    if(obj.SocketToken!=null)
                //    { 
                //        obj.BeginDischargeTime = DateTime.Now;
                //        foreach(var agingPump in obj.AgingPumpList)
                //        {
                //            if(agingPump!=null)
                //            { 
                //                agingPump.AgingStatus = EAgingStatus.DisCharging;//"老化放电中";
                //                agingPump.BeginDischargeTime = DateTime.Now;
                //            }
                //        }
                //    }
                //}
                this.Dispatcher.BeginInvoke(new DeleSetBackGround(SetStatusAndBackground), new object[] { Colors.Yellow, EAgingStatus.DisCharging/*"老化放电中"*/});

                //}
            }
        }
        /// <summary>
        /// 启动补电时钟
        /// </summary>
        private void StartRechargeTimer()
        {
            if(m_RechargeTimer.Enabled)
                return;
            StopRechargeTimer();
#if DEBUG
            m_RechargeTimer.Interval = 2*1000;//parameter.ChargeTime * 3600 * 1000;
#else
            m_ChargeTimer.Interval = 2*60*1000;
#endif
            m_RechargeTimer.Elapsed += OnRechargeTimer;
            m_RechargeTimer.Start();
        }
        private void StopRechargeTimer()
        {
            if(!m_RechargeTimer.Enabled)
                return;
            m_RechargeTimer.Elapsed -= OnRechargeTimer;
            m_RechargeTimer.Stop();
        }
        private void OnRechargeTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            AgingParameter parameter = m_DockParameter[m_DockNo] as AgingParameter;

            #region //其他泵先进行补电，每次要更新老化结束时间，当达到老化时间后不再更新
            foreach (var controller in m_Controllers)
            {
                if(controller!=null)
                {
                    foreach(var pump in controller.AgingPumpList)
                    {
                        //耗尽时间出现时说明已经收到报警包，但并不能说明补电开始。20170709
                        if (pump != null && pump.BeginBattaryDepleteTime.Year > 2000 && pump.BeginRechargeTime.Year > 2000 && pump.EndAgingTime.Year < 2000)
                        {
                            TimeSpan timespan = DateTime.Now - pump.BeginRechargeTime;
#if DEBUG 
                            if(timespan.TotalMinutes >= 0.2)
#else
                                 if(timespan.TotalMinutes >= (double)parameter.RechargeTime * 60)
#endif
                            {
                                pump.EndAgingTime = DateTime.Now;
                                pump.AgingStatus = EAgingStatus.AgingComplete;//"老化结束";
                            }
                        }
                    }
                }
            }
            #endregion

            
            //if(m_StartRechargeTime.Year>2000)
            if(IsAgingCompleted())
            {
                //这是最后一个泵的补电开始时间
                TimeSpan lastPumpRechargespan = DateTime.Now - m_LastRechargeTime;
#if DEBUG
                if(lastPumpRechargespan.TotalMinutes >= 0.2)
#else
                if(lastPumpRechargespan.TotalMinutes >= (double)parameter.RechargeTime * 60)
#endif
                {
                    StopRechargeTimer();
                    this.Dispatcher.BeginInvoke(new DeleSetBackGround(SetStatusAndBackground), new object[]{Colors.Green, EAgingStatus.AgingComplete/*"老化结束"*/});
                    foreach(var obj in m_Controllers)
                    {
                        if(obj.SocketToken!=null)
                        { 
                            foreach(var agingPump in obj.AgingPumpList)
                            {
                                if(agingPump!=null)
                                { 
                                    if(agingPump.EndAgingTime.Year>2000)
                                    {
                                        //已经老化结束了，不用赋值
                                    }
                                    else
                                    {
                                        agingPump.EndAgingTime = DateTime.Now;
                                        agingPump.AgingStatus = EAgingStatus.AgingComplete;//"老化结束";
                                    }
                                }
                            }
                        }
                    }
                    //关闭补电线程 20170708
                    StopCheckReChargeThread();
                    string fileName = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss_fff")+".xlsx";
                    m_StoptAgingTime = DateTime.Now;
                    ExportExcel(fileName);
                }
            }
        }
        private void SetStatusAndBackground(System.Windows.Media.Color color, EAgingStatus agingStatus)
        {
            dockGrid.Background = new SolidColorBrush(color);
            this.lbStatus.Content = AgingStatusMetrix.Instance().GetAgingStatus(agingStatus);
            m_AgingStatus = agingStatus;
        }

        /// <summary>
        /// 当停止时，禁用界面操作
        /// </summary>
        /// <param name="color"></param>
        /// <param name="agingStatus"></param>
        private void SetTitleAndEnable(string title, bool enable = true)
        {
            this.IsEnabled = enable;
            this.lbStatus.Content = title;
        }

        private void StartCheckPumpStatusTimer()
        {
            m_StartCheckPumpStatusTime = DateTime.Now;
            StopCheckPumpStatusTimer();
            m_CheckPumpStatusTimer.Interval = 2000;
            m_CheckPumpStatusTimer.Elapsed += OnCheckPumpStatusTimer;
            m_CheckPumpStatusTimer.Start();
        }
        private void StopCheckPumpStatusTimer()
        {
            m_CheckPumpStatusTimer.Elapsed -= OnCheckPumpStatusTimer;
            m_CheckPumpStatusTimer.Stop();
        }
        private void OnCheckPumpStatusTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimeSpan ts = DateTime.Now - m_StartCheckPumpStatusTime;
            if(ts.TotalSeconds>=DockWindow.m_CheckPumpStatusMaxMunites)
            {
                m_bKeepCheckPumpStatus = false;
                StopCheckPumpStatusTimer();
            }
        }

        private void StartCheckPumpStopStatusTimer()
        {
            m_StartCheckPumpStopStatusTime = DateTime.Now;
            StopCheckPumpStopStatusTimer();
            m_CheckPumpStopStatusTimer.Interval = 2000;
            m_CheckPumpStopStatusTimer.Elapsed += OnCheckPumpStopStatusTimer;
            m_CheckPumpStopStatusTimer.Start();
        }
        private void StopCheckPumpStopStatusTimer()
        {
            m_CheckPumpStopStatusTimer.Elapsed -= OnCheckPumpStopStatusTimer;
            m_CheckPumpStopStatusTimer.Stop();
        }
        private void OnCheckPumpStopStatusTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimeSpan ts = DateTime.Now - m_StartCheckPumpStopStatusTime;
            if (ts.TotalSeconds >= DockWindow.m_CheckPumpStopStatusMaxMunites)
            {
                //超时了
                m_bKeepCheckPumpStopStatus = false;
                StopCheckPumpStopStatusTimer();
                m_EventAllPumpStoped.Set();
            }
            if (m_CheckPumpStopStatusThreads.Count == 0)
                Thread.Sleep(2000);
            bool bFind = false;
            //当所有线程状态为信息的时候，发送一个状态给停止线程，可以进行界面上的操作
            foreach(var thread in m_CheckPumpStopStatusThreads)
            {
                if(thread!=null)
                {
                    if(thread.ThreadState==ThreadState.Aborted
                       ||thread.ThreadState==ThreadState.AbortRequested
                       || thread.ThreadState==ThreadState.Stopped
                       || thread.ThreadState==ThreadState.StopRequested
                       || thread.ThreadState==ThreadState.Unstarted
                        )
                    {
                        continue;
                    }
                    else
                    {
                        bFind = true;
                        break;
                    }
                }
            }
            if(!bFind)
            {
                //所有线程都死了，可以发送一个信号给ProcStopAging，让它继续操作界面
                m_bKeepCheckPumpStopStatus = false;
                StopCheckPumpStopStatusTimer();
                m_EventAllPumpStoped.Set();
            }
        }

        //检查放电是否成功返回时钟，30分钟时间去执行,20170701
        private void StartCheckDisChargeTimer()
        {
            StopCheckDisChargeTimer();
            m_StartCheckDisChargeTime = DateTime.Now;
            m_bKeepDisCharge = true;
            m_CheckDisChargeTimer.Interval = 5000;
            m_CheckDisChargeTimer.Elapsed += OnCheckDisChargeTimer;
            m_CheckDisChargeTimer.Start();
        }
        private void StopCheckDisChargeTimer()
        {
            m_bKeepDisCharge = false;
            m_CheckDisChargeTimer.Elapsed -= OnCheckDisChargeTimer;
            m_CheckDisChargeTimer.Stop();
        }
        private void OnCheckDisChargeTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimeSpan ts = DateTime.Now - m_StartCheckDisChargeTime;
            if (m_bKeepDisCharge==false || ts.TotalSeconds >= DockWindow.m_CheckDisChargeMaxMunites)
            {
                m_bKeepDisCharge = false;
                StopCheckDisChargeTimer();
            }
        }


        #endregion

        #region button click

        public bool CanStart()
        {
            if(this.m_bStartAging)
            {
                return false;
            }
            AgingParameter para = m_DockParameter[m_DockNo] as AgingParameter;
            ProductID pid = ProductID.Unknow;
            if (para != null)
            {
                if (Enum.IsDefined(typeof(ProductID), para.PumpType))
                    pid = (ProductID)Enum.Parse(typeof(ProductID), para.PumpType);
                else
                {
                    Logger.Instance().ErrorFormat("OnStartClick()->para.PumpType is not defined! para.PumpType={0}", para.PumpType);
                    //MessageBox.Show("泵型号出错，非法的泵类型!");
                    return false;
                }
            }
            else
            {
                Logger.Instance().Debug("OnStartClick()->AgingParameter is null");
                //MessageBox.Show("开始前先配置老化参数!");
                return false;
            }
            List<Controller> controller = ControllerManager.Instance().Get(m_DockNo);
            if (controller.Count <= 0)
            {
                //MessageBox.Show("配置文件出错，未检测到相关货架的配置文件！");
                return false;
            }
            List<Tuple<int, int, int>> selectedPumps = m_HashPumps[m_DockNo] as List<Tuple<int, int, int>>;
            if (selectedPumps == null || selectedPumps.Count <= 0)
            {
                //MessageBox.Show("请选择需要老化的泵！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 开始老化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnStartClick(object sender, RoutedEventArgs e)
        {
           StartAging();
        }

        public void StartAging()
        {
            //关闭泵停止状态检查线程
            if (m_CheckPumpStopStatusThreads.Count > 0)
            {
                foreach (var th in m_CheckPumpStopStatusThreads)
                {
                    if (th.ThreadState == ThreadState.Running
                       || th.ThreadState == ThreadState.Suspended
                       )
                        th.Abort();
                }
            }
            m_CheckPumpStopStatusThreads.Clear();

            if (m_TaskThread == null)
            {
                m_TaskThread = new Thread(new ThreadStart(ProcStartAging));
            }
            if (m_TaskThread.ThreadState == ThreadState.Unstarted)
            {
                m_bKeepCheckPumpStatus = true;
                m_TaskThread.Start();
            }
            else if (m_TaskThread.ThreadState == ThreadState.Aborted
                || m_TaskThread.ThreadState == ThreadState.Stopped
                )
            {
                m_TaskThread = new Thread(new ThreadStart(ProcStartAging));
                m_bKeepCheckPumpStatus = true;
                m_TaskThread.Start();
            }
            //补电线程启动，等待队列中有新的耗尽泵进入
            m_bKeepReCharge = true;
            CreateCheckReChargeThread();
        }

        private void ProcStartAging()
        {
            #region 初始化参数
            InitAllParamater();
            foreach (var obj in m_Controllers)
            {
                obj.AgingPumpList.Clear();
            }
            AgingParameter para = m_DockParameter[m_DockNo] as AgingParameter;
            ProductID pid = ProductID.Unknow;
            if (para != null)
            {
                if (Enum.IsDefined(typeof(ProductID), para.PumpType))
                    pid = (ProductID)Enum.Parse(typeof(ProductID), para.PumpType);
                else
                {
                    Logger.Instance().ErrorFormat("OnStartClick()->para.PumpType is not defined! para.PumpType={0}", para.PumpType);
                    MessageBox.Show("泵型号出错，非法的泵类型!");
                    return;
                }
            }
            else
            {
                Logger.Instance().Debug("OnStartClick()->AgingParameter is null");
                MessageBox.Show("开始前先配置老化参数!");
                return;
            }
            List<Controller> controller = ControllerManager.Instance().Get(m_DockNo);
            if (controller.Count <= 0)
            {
                MessageBox.Show("配置文件出错，未检测到相关货架的配置文件！");
                return;
            }
            List<Tuple<int, int, int>> selectedPumps = m_HashPumps[m_DockNo] as List<Tuple<int, int, int>>;
            if (selectedPumps == null || selectedPumps.Count <= 0)
            {
                MessageBox.Show("请选择需要老化的泵！");
                return;
            }
            #endregion
            //找出那些已经连接的SOCKET
            List<Controller> controllers = controller.FindAll((x) => { return x.SocketToken != null; });
            int controllerCount = 0, counter = 0;
            if (controllers != null && controllers.Count>0)
                controllerCount = controllers.Count;
            else
            {
                MessageBox.Show("未发现已连接的控制器！");
                return;
            }
            OnEnableControls(false);
            if (controllerCount > 0)
            {
                #region//在开始老化之前，要给每个WIFI模块发送一条指令，声明它所连接的泵型号,待这个命令有回应之后才能进行下一步操作
                Controller currentObj = null;
                int iChannel = 0;
                byte channel = 0;
                m_EventSendPumpType.Reset();//先设置无信号状态
                m_EventCmdCharge.Reset();//先设置无信号状态
                #region//将泵类型命令参数保存在成员变量中，方便检查泵状态时使用
                m_CurrentProductID = pid;
                m_QueryInterval = (ushort)DockWindow.m_QueryInterval;
                #endregion
                for (int i = 0; i < controllers.Count; i++)
                {
                    currentObj = controllers[i];
                    if(!m_HashRowNo.ContainsKey(currentObj.RowNo))
                        continue;
                    iChannel = (int)m_HashRowNo[currentObj.RowNo];
                    channel = (byte)(iChannel & 0x000000FF);
                    if (currentObj.SocketToken != null)
                        m_CmdManager.SendPumpType(pid, (ushort)DockWindow.m_QueryInterval, currentObj.SocketToken, CommandResponse, CommandTimeoutResponse, channel);
                    else
                    {
                        Logger.Instance().ErrorFormat("ProcStartAging()->发送泵信息命令时，{0}号货架第{1}层控制器SocketToken为null", currentObj.DockNo, currentObj.RowNo);
                        continue;
                    }
                    if (m_EventSendPumpType.WaitOne(m_TimeOut))//等待3秒，如果前一条命令没有返回就不用发了
                    {
                        decimal rate = para.Rate;
                        decimal volume = para.Volume;
                        if (currentObj.SocketToken!=null)
                            m_CmdManager.SendCmdCharge(rate, volume, currentObj.SocketToken, CommandResponse, CommandTimeoutResponse, channel);
                        else
                        {
                            Logger.Instance().ErrorFormat("ProcStartAging()->发送老化充电命令时，{0}号货架第{1}层控制器SocketToken为null", currentObj.DockNo, currentObj.RowNo);
                            continue;
                        }
                        if(m_EventCmdCharge.WaitOne(m_TimeOut))            //如果开始命令有返回，表示成功启动
                        {
                            counter++;                                     //统计已经发送过的开始命令数量
                            currentObj.BeginAginTime = DateTime.Now;       //因为老化开始时间
                            currentObj.AgingStatus = EAgingStatus.Charging;//老化充电中
                            if(!m_bStartAging)                             //此处只能调用一次，定时器也只能开一次
                            {
                                m_bStartAging = true;
                                m_StartAgingTime = DateTime.Now;
                                OnEnableControls(false);
                                StartChargeTimer();//开始老化充电时钟开启，然后才能接收报警信息，监控泵的动态，一旦出现低电，立即对此泵发送，补电命令
                                this.Dispatcher.BeginInvoke(new DeleSetBackGround(SetStatusAndBackground), new object[] { Colors.Yellow, EAgingStatus.Charging/*"老化充电中"*/ });
                            }
                        }
                        else
                        {
                            Logger.Instance().ErrorFormat("{0}号货架第{1}层控制器无法响应老化开始命令，请排除故障后重新开始！", currentObj.DockNo, currentObj.RowNo);
                            continue;
                        }
                    }
                    else
                    {
                        Logger.Instance().ErrorFormat("{0}号货架第{1}层控制器无法响应泵信息命令，请排除故障后重新开始！", currentObj.DockNo, currentObj.RowNo);
                        continue;
                    }
                }
                #endregion
            }
            if(counter<=0)
            {
                OnEnableControls(true);
            }
            else
            {
                //可以启动检查线程
                m_bCanStartCheck = true;
                CreateCheckPumpStatusThread();
            }
        }

        /// <summary>
        /// 停止老化发送完成命令，让泵停止，然后断开所有本架子上的客户端连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要停止老化吗？", "提示", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                //关闭补电线程 20170708
                StopCheckReChargeThread();
                m_bKeepCheckPumpStatus = false;
                StopAging();
                StartCheckPumpStopStatusTimer();//启动时钟
            }
            else
            {
                return;
            }
        }

        public void StopAging()
        {
            //停止之前的启动检查线程
            if (m_CheckPumpRunningStatusThreads.Count > 0)
            {
                foreach (var th in m_CheckPumpRunningStatusThreads)
                {
                    if (th.ThreadState == ThreadState.Running
                       || th.ThreadState == ThreadState.Suspended
                       )
                        th.Abort();
                }
            }
            m_CheckPumpRunningStatusThreads.Clear();
            Thread stopThread = new Thread(ProcStopAging);
            stopThread.Start();
            //如果是正常老化结束，不用启动检查线程
            if (this.AgingStatus != EAgingStatus.AgingComplete)
            {
                m_bKeepCheckPumpStopStatus = true;
                CreateCheckPumpStopStatusThread();
            }
        }

        private void ProcStopAging()
        {
            if(this.AgingStatus==EAgingStatus.PowerOn
                ||this.AgingStatus==EAgingStatus.Unknown
                ||this.AgingStatus==EAgingStatus.Waiting
                )
                return;
            if(m_TaskThread!=null&&(m_TaskThread.ThreadState==ThreadState.Running||m_TaskThread.ThreadState==ThreadState.Suspended))
            {
                m_TaskThread.Abort();
            }
            if (this.AgingStatus != EAgingStatus.AgingComplete)
            {
                this.Dispatcher.BeginInvoke(new DeleSetTitleAndEnable(SetTitleAndEnable), new object[] { "正在停止泵......", false });
            }
            int iChannel = 0;
            byte channel = 0;
            foreach (var controller in m_Controllers)
            {
                if (controller != null && controller.SocketToken != null)
                {
                    if (!m_HashRowNo.ContainsKey(controller.RowNo))
                        continue;
                    iChannel = (int)m_HashRowNo[controller.RowNo];
                    channel = (byte)(iChannel & 0x000000FF);
                    m_CmdManager.SendCmdFinishAging(controller.SocketToken, null, null, channel);
                }
            }
            //中途停止也可以保存EXCEL
            if (this.AgingStatus != EAgingStatus.AgingComplete)
            {
                string fileName = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss_fff") + ".xlsx";
                ExportExcel(fileName);
            }
            if (this.AgingStatus != EAgingStatus.AgingComplete)
            {
                //这里必须接受到了一个信号才能有所动作
                if (m_EventAllPumpStoped.WaitOne(DockWindow.m_CheckPumpStopStatusMaxMunites * 1000))
                {

                }
                else
                {
                    MessageBox.Show("停止泵过程超时，请手动停止！");
                }
                this.Dispatcher.BeginInvoke(new DeleSetTitleAndEnable(SetTitleAndEnable), new object[] { "老化结束", true });
            }
            OnEnableControls(true);
            Thread.Sleep(2000);
            //停止时要发送老化结束命令
            //停止老化，清除所有信息，包括已经连接上的控制器
            InitAllParamater(false);
            //清除控制器下的所有报警信息，等待新的控制器上传。
            foreach (var controller in m_Controllers)
            {
                if (controller != null && controller.SocketToken != null)
                {
                    controller.IsRunning = false;
                    AsyncSocket.AsyncServer.Instance().CloseClientSocket(controller.SocketToken);
                }
            }
            this.Dispatcher.BeginInvoke(new DeleSetBackGround(SetStatusAndBackground), new object[] { Colors.Blue, EAgingStatus.PowerOn });
        }

        /// <summary>
        ///弹出配置界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConfigClick(object sender, RoutedEventArgs e)
        {
            int dockNo = 0;
            if (Int32.TryParse(Tag.ToString(), out dockNo))
            {
                Configuration cfg = new Configuration(dockNo);
                cfg.OnSaveConfigration += OnSaveConfigration;
                cfg.OnSelectedPumps += OnSelectedPumps;
                bool? bRet = cfg.ShowDialog();
                cfg = null;
                GC.Collect();
                if (bRet.HasValue && bRet == true)
                {
                    if (OnConfigrationCompleted != null)
                        OnConfigrationCompleted(this, new ConfigrationArgs(m_DockParameter));
                    AgingParameter para = m_DockParameter[m_DockNo] as AgingParameter;
                    lbPumpType.Content = "泵型号:" + para.PumpType;
                }
                if (OnPumpSelected != null)
                {
                    OnPumpSelected(this, new SelectedPumpsArgs(m_HashPumps));
                    string strTemp = string.Format("泵数量:{0}/{1}", ((List<Tuple<int,int,int>>)m_HashPumps[dockNo]).Count, DockInfoManager.Instance().Get(dockNo));
                    lbPumpCount.Content = strTemp;
                }
            }
            else
            {
                MessageBox.Show("货架编号为空，无法配置参数！");
            }
        }

        private void OnDetailClick(object sender, RoutedEventArgs e)
        {
            if(m_DockParameter[m_DockNo]==null || m_HashPumps[m_DockNo]==null)
                return;
            if(((List<Tuple<int,int,int>>)m_HashPumps[m_DockNo]).Count==0)
                return;
            int dockNo = 0;
            if (Int32.TryParse(Tag.ToString(), out dockNo))
            {
                List<AgingPump> agingPumps = new List<AgingPump>();
                for(int i=0;i<m_Controllers.Count;i++)
                {
                    agingPumps.AddRange(m_Controllers[i].AgingPumpList);
                }
                DetailList detailList = new  DetailList(m_DockNo, (AgingParameter)m_DockParameter[m_DockNo], (List<Tuple<int,int,int>>)m_HashPumps[m_DockNo], agingPumps);
                detailList.ShowDialog();
                detailList = null;
                GC.Collect();
            }
            else
            {
                //MessageBox.Show("货架编号为空，无法配置参数！");
            }
        }
        #endregion

        #region 群发送命令函数
        private void SendSendPumpTypeCommand(CmdSendPumpType cmd)
        {
            AgingParameter para = m_DockParameter[m_DockNo] as AgingParameter;
            ProductID pid = ProductID.Unknow;
            if (para != null)
            {
                if (Enum.IsDefined(typeof(ProductID), para.PumpType))
                    pid = (ProductID)Enum.Parse(typeof(ProductID), para.PumpType);
                else
                {
                    Logger.Instance().ErrorFormat("SendSendPumpTypeCommand()->para.PumpType is not defined! para.PumpType={0}", para.PumpType);
                    MessageBox.Show("泵型号出错，非法的泵类型!");
                    return;
                }
            }
            else
            {
                Logger.Instance().Debug("SendSendPumpTypeCommand()->AgingParameter is null");
                MessageBox.Show("开始前先配置老化参数!");
                return;
            }
            List<Controller> controller = ControllerManager.Instance().Get(m_DockNo);
            if (controller.Count <= 0)
            {
                MessageBox.Show("配置文件出错，未检测到相关货架的配置文件！");
                return;
            }
             //找出那些已经连接的SOCKET
            List<Controller> ipMap = controller.FindAll((x) => { return x.SocketToken != null; });
            if (ipMap != null && ipMap.Count > 0)
            {
                #region//在开始老化之前，要给每个WIFI模块发送一条指令，声明它所连接的泵型号,待这个命令有回应之后才能进行下一步操作
                Controller currentObj = null, preObj = null;
                m_EventSendPumpType.Set();//先设置有信号状态
                for (int i = 0; i < ipMap.Count; i++)
                {
                    currentObj = ipMap[i];
                    if (m_EventSendPumpType.WaitOne(m_TimeOut))//等待3秒，如果前一条命令没有返回就不用发了
                    {
                        m_CmdManager.SendPumpType(pid, (ushort)DockWindow.m_QueryInterval, currentObj.SocketToken, CommandResponse, CommandTimeoutResponse, (byte)m_HashRowNo[currentObj.RowNo]);
                        preObj = currentObj;
                    }
                    else
                    {
                        if (preObj != null)
                        {
                            string msg = string.Format("{0}号货架第{1}层控制器无响应，请排除故障后重新开始！", preObj.DockNo, preObj.RowNo);
                            Logger.Instance().Error(msg);
                            return;
                        }
                    }
                }
                #endregion
            }

        }

        private void SendCmdDischarge()
        {
            AgingParameter para = m_DockParameter[m_DockNo] as AgingParameter;
            ProductID pid = ProductID.Unknow;
            if (para != null)
            {
                if (Enum.IsDefined(typeof(ProductID), para.PumpType))
                    pid = (ProductID)Enum.Parse(typeof(ProductID), para.PumpType);
                else
                {
                    Logger.Instance().ErrorFormat("SendSendPumpTypeCommand()->para.PumpType is not defined! para.PumpType={0}", para.PumpType);
                    MessageBox.Show("泵型号出错，非法的泵类型!");
                    return;
                }
            }
            else
            {
                Logger.Instance().Debug("SendSendPumpTypeCommand()->AgingParameter is null");
                MessageBox.Show("开始前先配置老化参数!");
                return;
            }
            List<Controller> controller = ControllerManager.Instance().Get(m_DockNo);
            if (controller.Count <= 0)
            {
                MessageBox.Show("配置文件出错，未检测到相关货架的配置文件！");
                return;
            }
             //找出那些已经连接的SOCKET
            List<Controller> ipMap = controller.FindAll((x) => { return x.SocketToken != null;});
            if (ipMap != null && ipMap.Count > 0)
            {
                #region//在开始老化之前，要给每个WIFI模块发送一条指令，声明它所连接的泵型号,待这个命令有回应之后才能进行下一步操作
                Controller currentObj = null, preObj = null;
                m_EventCmdDischarge.Set();//先设置有信号状态
                for (int i = 0; i < ipMap.Count; i++)
                {
                    currentObj = ipMap[i];
                    if (m_EventCmdDischarge.WaitOne(m_TimeOut))//等待3秒，如果前一条命令没有返回就不用发了
                    {
                        m_CmdManager.SendCmdDischarge(currentObj.SocketToken, CommandResponse, CommandTimeoutResponse);
                        preObj = currentObj;
                    }
                    else
                    {
                        if (preObj != null)
                        {
                            string msg = string.Format("{0}号货架第{1}层控制器CmdDischarge无响应，请排除故障后重新开始！", preObj.DockNo, preObj.RowNo);
                            Logger.Instance().Error(msg);
                            return;
                        }
                    }
                }
                //等待最后的返回
                if(m_EventCmdDischarge.WaitOne(m_TimeOut))
                {
                    Logger.Instance().Info("发送CmdDischarge命令全部收到回应！");
                    
                }
                else
                {
                    if (preObj != null)
                    {
                        string msg = string.Format("{0}号货架第{1}层控制器无法响应放电命令，请排除故障后重新开始！", preObj.DockNo, preObj.RowNo);
                        Logger.Instance().Error(msg);
                        return;
                    }
                }
                #endregion
            }

        }
        #endregion

        private void EnableControls(bool bEnabled = false)
        {
            btnConfig.IsEnabled = bEnabled;
            btnStart.IsEnabled = bEnabled;
            btnStop.IsEnabled = !bEnabled;
        }

        private void OnEnableControls(bool bEnabled = false)
        {
            this.Dispatcher.Invoke(new DeleEnableControls(EnableControls), new object[]{bEnabled});
        }

        /// <summary>
        /// 清空所有变量
        /// </summary>
        private void InitAllParamater(bool bStart = true)
        {
            m_ChargeTimer.Stop();       
            m_RechargeTimer.Stop();       
            m_AgingStatus = EAgingStatus.PowerOn;//可以开始，肯定是已经上电状态
            m_bStartAging        = false;       
            m_StartAgingTime     = DateTime.MinValue; 
            m_StoptAgingTime     = DateTime.MinValue;
            m_LastRechargeTime   = DateTime.MinValue;    
            m_StopRechargeTime   = DateTime.MinValue;    
            m_StartDischargeTime = DateTime.MinValue;    
            m_StopDischargeTime  = DateTime.MinValue;
            //清除控制器下的所有报警信息，等待新的控制器上传。
            foreach(var controller in m_Controllers)
            {
                if(controller!=null)
                {
                    controller.AgingPumpList.Clear();
                    if(!bStart) //如果是停止就清空，开始的时候不要清空
                    { 
                        controller.SocketConnectTimestamp = DateTime.MinValue;
                        controller.BeginAginTime = DateTime.MinValue;
                        controller.BeginDischargeTime = DateTime.MinValue;
                    }
                }
            }
        }

        /// <summary>
        /// 更新报警信息,一条完整的报警信息，是一个控制器的信息，如果中途有泵无响应，那么要删除它
        /// </summary>
        /// <param name="cmd"></param>
        public void UpdateAlarmInfo(GetAlarm cmd)
        {
            #region 不处理报警信息
            if (this.m_StartAgingTime.Year < 2000)
            {
                //还没开始老化呢，怎么能接收报警数据呢！
                Logger.Instance().Info("老化尚未开始，控制器上报的Alarm不处理!");
                return;
            }
            if (this.AgingStatus == EAgingStatus.AgingComplete)
            {
                //还没开始老化呢，怎么能接收报警数据呢！
                Logger.Instance().Info("老化结束，控制器上报的Alarm不处理!");
                return;
            }
            if (this.m_StoptAgingTime.Year > 2000)
            {
                //还没开始老化呢，怎么能接收报警数据呢！
                Logger.Instance().Info("老化已停止，控制器上报的Alarm不处理!");
                return;
            }
            #endregion
            if (cmd != null && cmd.PumpPackages != null && cmd.RemoteSocket != null)
            {
                Controller controller = m_Controllers.Find((x) => { return x.SocketToken != null && x.SocketToken.IP == cmd.RemoteSocket.IP; });
                if (controller == null)
                {
                    Logger.Instance().ErrorFormat("UpdateAlarmInfo()->控制器不存在货架中,Socket没找到 IP={0}", cmd.RemoteSocket.IPString);
                    return;
                }
                int dockNo = controller.DockNo;
                int rowNo = controller.RowNo;
                int count = cmd.PumpPackages.Count; //此次上报的泵数据包数量
                StringBuilder sb = new StringBuilder();
                foreach(var p in cmd.PumpPackages)
                {
                    sb.Append(p.Chanel.ToString());
                    sb.Append(",");
                }
                Logger.Instance().InfoFormat("收到货架编号为{0},第{1}行控制器上报信息,数据包数量={2},通道编号={3}", dockNo, rowNo, count, sb.ToString().TrimEnd(','));
                AgingPump info = null;
                AgingParameter para = m_DockParameter[dockNo] as AgingParameter;
                ProductID pid = ProductID.Unknow;
                if (Enum.IsDefined(typeof(ProductID), para.PumpType))
                    pid = (ProductID)Enum.Parse(typeof(ProductID), para.PumpType);
                else
                {
                    Logger.Instance().ErrorFormat("泵类型转换出错，不支持的类型 PumpType ={0}", para.PumpType);
                    return;
                }
                Hashtable alarmMetrix = null;
                uint depletealArmIndex = 0, lowVolArmIndex = 0;//耗尽和低电压索引
                #region //查询耗尽报警索引
                switch (pid)
                {
                    case ProductID.GrasebyC6:
                        alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC6;
                        depletealArmIndex = 0x00000008;
                        lowVolArmIndex = 0x00000100;
                        break;
                    case ProductID.GrasebyC6T:
                        alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC6T;
                        depletealArmIndex = 0x00000008;
                        lowVolArmIndex = 0x00000100;
                        break;
                    case ProductID.GrasebyC8:
                        alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC8;
                        depletealArmIndex = 0x00010000;
                        lowVolArmIndex = 0x00000001;
                        break;
                    case ProductID.GrasebyF6:
                        alarmMetrix = AlarmMetrix.Instance().AlarmMetrixF6;
                        depletealArmIndex = 0x00000008;
                        lowVolArmIndex = 0x00000100;
                        break;
                    case ProductID.GrasebyF8://C8和F8是一样
                        alarmMetrix = AlarmMetrix.Instance().AlarmMetrixC8;
                        depletealArmIndex = 0x00010000;
                        lowVolArmIndex = 0x00000001;
                        break;
                    case ProductID.Graseby1200:
                        alarmMetrix = AlarmMetrix.Instance().AlarmMetrix1200;
                        depletealArmIndex = 0x00000010;
                        lowVolArmIndex = 0x00004000;
                        break;
                    case ProductID.Graseby1200En:
                        alarmMetrix = AlarmMetrix.Instance().AlarmMetrix1200En;
                        depletealArmIndex = 0x00000020;
                        lowVolArmIndex = 0x00008000;
                        break;
                    case ProductID.Graseby2000:
                        alarmMetrix = AlarmMetrix.Instance().AlarmMetrix2000;
                        depletealArmIndex = 0x00000008;
                        lowVolArmIndex = 0x00000100;
                        break;
                    case ProductID.Graseby2100:
                        alarmMetrix = AlarmMetrix.Instance().AlarmMetrix2100;
                        depletealArmIndex = 0x00000008;
                        lowVolArmIndex = 0x00000100;
                        break;
                    default:
                        break;
                }
                #endregion
                if (alarmMetrix == null)
                {
                    Logger.Instance().ErrorFormat("找不到与此泵类型相关的报警映射表 ProductID = {0}", pid);
                    return;
                }

                #region //2017-03-09 检查放电命令遗漏的控制器，这个指令没有被执行时，可以通过报警信息中的电源信息间接得知
                if (count > 0)
                {
                    byte pumpChannel = 0xFF;
                    try
                    {
                        for (int index = 0; index < count; index++)
                        {
                            pumpChannel = (byte)(cmd.PumpPackages[index].Chanel & 0x0F);
                            AgingPump pump = controller.FindPump(dockNo, rowNo, pumpChannel);
                            if (pump != null && pump.AgingStatus == EAgingStatus.DisCharging && cmd.PumpPackages[index].Power.PowerStatus == PumpPowerStatus.AC)
                            {
                                m_CmdManager.SendCmdDischarge(controller.SocketToken, null, null, pumpChannel);
                                if (controller.AgingStatus < EAgingStatus.DisCharging)
                                    controller.AgingStatus = EAgingStatus.DisCharging;
                                pump.AgingStatus = EAgingStatus.DisCharging;
                                //这个控制器中的所有泵的放电时间改成现在
                                pump.BeginDischargeTime = DateTime.Now;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance().ErrorFormat("UpdateAlarmInfo()->检查放电命令遗漏出错，{0}", ex.Message);
                    }
                }
                #endregion

                //循环6个报警包,32个报警，查询太费时间
                for (int i = 0; i < count; i++)
                {
                    info = controller.FindPump(dockNo, rowNo, (byte)(cmd.PumpPackages[i].Chanel & 0x000F));
                    if (info != null)
                    {
                        if (info.AgingStatus == EAgingStatus.AgingComplete)
                        {
                            Logger.Instance().InfoFormat("此泵已经老化结束，上报的报警信息不处理。泵位置信息：货架号={0},行号={1},通道号={2}", info.DockNo, info.RowNo, info.Channel);
                            continue;
                        }
                        info.LostTimes = 0;//清除失联记录
                        info.PumpType = para.PumpType;
                        info.BeginAgingTime = controller.BeginAginTime;
                        if (info.BeginDischargeTime.Year < 2000)
                            info.BeginDischargeTime = controller.BeginDischargeTime;
                        if (info.AgingStatus < controller.AgingStatus)
                            info.AgingStatus = controller.AgingStatus;//老化状态,这些状态都是在发送命令的时候赋值的,只有在补电时状态信息才不同时
                        #region //当收到的报警信息在列表中存在时，需要更新
                        info.Alarm |= cmd.PumpPackages[i].Alarm.Alarm;
                        //如果某个泵一直发电池耗尽报警，那ADAS也要一直发充电命令
                        if ((depletealArmIndex & cmd.PumpPackages[i].Alarm.Alarm) == depletealArmIndex)
                        {
                            //出现了电池耗尽报警,发充电命令
                            byte channel = 1;
                            channel = (byte)(channel << (info.Channel - 1));
                            //此处的通道号是报警信息包中的通道号，并不是按位定义的,出现报警信息后，要加入此队列,add by 20170708
                            m_DepleteManager.UpdateDepleteInfo(cmd.RemoteSocket.IP, info.Channel);
                            OnRechargePump();
                            //m_CmdManager.SendCmdRecharge(channel, cmd.RemoteSocket, null);
                            if (info.BeginBattaryDepleteTime.Year < 2000)
                            {
                                info.BeginBattaryDepleteTime = DateTime.Now;
                                //info.AgingStatus = EAgingStatus.Recharging;//"老化补电中";
                            }
                        }
                        if ((lowVolArmIndex & cmd.PumpPackages[i].Alarm.Alarm) == lowVolArmIndex)
                        {
                            //出现了电池低电报警,记录时间
                            if (info.BeginLowVoltageTime.Year < 2000)
                            {
                                info.BeginLowVoltageTime = DateTime.Now;
                                //info.AgingStatus = EAgingStatus.DisCharging;//"老化放电中";add by 20170708
                            }
                        }
                        //除了低电和耗尽是否有其他报警
                        uint exceptAlarm = lowVolArmIndex | depletealArmIndex;
                        if ((exceptAlarm | cmd.PumpPackages[i].Alarm.Alarm) != exceptAlarm)
                        {
                            info.RedAlarmStatus = EAgingStatus.Alarm;//"有其他报警，显示红色";
                        }
                        else
                        {
                            info.RedAlarmStatus = EAgingStatus.Unknown;//"报警消除，显示正常";
                        }
                        #endregion
                    }
                    else
                    {
                        #region //当收到的报警信息在列表中不存在时，需要新增
                        AgingPump newInfo = new AgingPump();
                        newInfo.DockNo = dockNo;
                        newInfo.RowNo = rowNo;
                        newInfo.Channel = (byte)(cmd.PumpPackages[i].Chanel & 0x0000000F);//此处是通道号的定义
                        newInfo.BeginAgingTime = controller.BeginAginTime;
                        //放电时间一开始保存在控制器对象中，由于报警一时上不来，查看详细看不到放电消息
                        if (newInfo.BeginDischargeTime.Year < 2000)
                            newInfo.BeginDischargeTime = controller.BeginDischargeTime;
                        if (newInfo.AgingStatus < controller.AgingStatus)
                            newInfo.AgingStatus = controller.AgingStatus;//老化状态,这些状态都是在发送命令的时候赋值的,只有在补电时状态信息才不同时
                        newInfo.PumpType = para.PumpType;
                        #region //当收到的报警信息在列表中存在时，需要更新
                        newInfo.Alarm |= cmd.PumpPackages[i].Alarm.Alarm;
                        //如果某个泵一直发电池耗尽报警，那ADAS也要一直发充电命令
                        if ((depletealArmIndex & cmd.PumpPackages[i].Alarm.Alarm) == depletealArmIndex)
                        {
                            //出现了电池耗尽报警,发充电命令
                            byte channel = 1;
                            channel = (byte)(channel << (newInfo.Channel - 1));
                            //此处的通道号是报警信息包中的通道号，并不是按位定义的,出现报警信息后，要加入此队列add by 20170708
                            m_DepleteManager.UpdateDepleteInfo(cmd.RemoteSocket.IP, newInfo.Channel);
                            OnRechargePump();
                            //m_CmdManager.SendCmdRecharge(channel, cmd.RemoteSocket, null);
                            if (newInfo.BeginBattaryDepleteTime.Year < 2000)
                            {
                                newInfo.BeginBattaryDepleteTime = DateTime.Now;
                                //newInfo.AgingStatus = EAgingStatus.Recharging;//"老化补电中";
                            }
                        }
                        if ((lowVolArmIndex & cmd.PumpPackages[i].Alarm.Alarm) == lowVolArmIndex)
                        {
                            //出现了电池低电报警,记录时间
                            if (newInfo.BeginLowVoltageTime.Year < 2000)
                            {
                                newInfo.BeginLowVoltageTime = DateTime.Now;
                                //newInfo.AgingStatus = EAgingStatus.DisCharging;//"老化放电中"; add by 20170708
                            }
                        }
                        //除了低电和耗尽是否有其他报警
                        uint exceptAlarm = lowVolArmIndex | depletealArmIndex;
                        if ((exceptAlarm | cmd.PumpPackages[i].Alarm.Alarm) != exceptAlarm)
                        {
                            newInfo.RedAlarmStatus = EAgingStatus.Alarm;//"有其他报警，显示红色";
                        }
                        else
                        {
                            newInfo.RedAlarmStatus = EAgingStatus.Unknown;//"没有其他报警，显示正常";
                        }
                        #endregion
                        controller.AgingPumpList.Add(newInfo);
                        #endregion
                    }
                }
                if (count < controller.AgingPumpList.Count)
                {
                    Logger.Instance().InfoFormat("有{0}个泵失去连接，请检查网络设备通道是否良好！", controller.AgingPumpList.Count - count);
                    foreach (AgingPump pump in controller.AgingPumpList)
                    {
                        if (cmd.PumpPackages.FindIndex((x) => { return x.Chanel != pump.Channel; }) >= 0)
                        {
                            if (pump.LostTimes <= 25)   //连续25次都失连，则不用再加了,此泵无效了
                                pump.LostTimes = pump.LostTimes + 1;
                        }
                    }
                }
            }
            this.Dispatcher.BeginInvoke(new DeleShowRedAlarm(ShowRedAlarm), new object[] { });
        }

        /// <summary>
        /// 当出现耗尽报警时，此函数被调用
        /// 当第一条补电命令发送后，启动补电定时器
        /// 最后一个泵发送完补电命令后，定时结束，表示此货架老化结束了。
        /// </summary>
        public void OnRechargePump()
        {
            #region 弃用 by 20170710
            //int iCount = 0;
            //int agingPumpCount = GetAgingPumpCount();
            ////统计已经老化补电的泵数量
            //for(int i=0;i<m_Controllers.Count;i++)
            //{
            //    if (m_Controllers[i].SocketToken != null && m_Controllers[i].SocketToken.ConnectSocket !=null && m_Controllers[i].SocketToken.ConnectSocket.Connected)
            //        for(int j=0;j<m_Controllers[i].AgingPumpList.Count;j++)
            //        {
            //            if(m_Controllers[i].AgingPumpList[j].BeginBattaryDepleteTime.Year>2000 &&
            //                m_Controllers[i].AgingPumpList[j].BeginRechargeTime.Year>2000 &&
            //                m_Controllers[i].AgingPumpList[j].LostTimes<=20) //泵不能有失连记录，否则，不统计在内
            //                ++iCount;
            //        }
            //}
            //if (iCount >= agingPumpCount && m_StartRechargeTime.Year<2000)
            //{
            //    //该架子上的泵全部老化补电成功
            //    m_StartRechargeTime = DateTime.Now;
            //}
            #endregion
            if (m_RechargeTimer.Enabled)
                return;
            else
            { 
                this.Dispatcher.BeginInvoke(new DeleStartTimer(StartRechargeTimer), null);
                this.Dispatcher.BeginInvoke(new DeleSetBackGround(SetStatusAndBackground), new object[]{Colors.Yellow, EAgingStatus.Recharging/*"老化补电中"*/});
            }
        }

        /// <summary>
        /// 判断整个架子上的泵是否全部老化完成 add by 20170710
        /// 修改新算法：当架子上的所有泵都进行了补电才算全部完成，否则界面为黄色
        /// </summary>
        /// <returns></returns>
        private bool IsAgingCompleted()
        {
            bool bRet = true;
            for (int i = 0; i < m_Controllers.Count; i++)
            {
                if (m_Controllers[i] != null)
                {
                    for (int j = 0; j < m_Controllers[i].AgingPumpList.Count; j++)
                    {
                        //泵已经进行了补电
                        if (m_Controllers[i].AgingPumpList[j].BeginBattaryDepleteTime.Year > 2000 && m_Controllers[i].AgingPumpList[j].BeginRechargeTime.Year > 2000)
                        {
                            //将最晚的补电时间取出
                            if (m_LastRechargeTime < m_Controllers[i].AgingPumpList[j].BeginRechargeTime)
                                m_LastRechargeTime = m_Controllers[i].AgingPumpList[j].BeginRechargeTime;
                            continue;
                        }
                        else
                        {
                            bRet = false;
                            Logger.Instance().InfoFormat("IsAgingCompleted()-> 货架编号={0}, 通道号={1}的机器还未补电，此货架尚未结束老化", m_Controllers[i].DockNo, m_Controllers[i].AgingPumpList[j].Channel);
                            break;
                        }
                    }
                }
            }
            return bRet;
        }
         
        public void ShowRedAlarm()
        {
            bool bFind = false;
            foreach(var contoller in m_Controllers)
            {
                if(contoller==null)
                    continue;
                //正在老化的泵如果有红色报警，就要显示
                bFind = contoller.AgingPumpList.Exists((x)=> {return x.AgingStatus!=EAgingStatus.AgingComplete && x.RedAlarmStatus==EAgingStatus.Alarm;});
                if(bFind)
                {
                    SetStatusAndBackground(Colors.Red, EAgingStatus.Alarm);
                    //this.Dispatcher.BeginInvoke(new DeleSetBackGround(SetStatusAndBackground), new object[] { Colors.Red, EAgingStatus.Alarm/*"老化补电中"*/});
                    return;
                }
            }
            //如果点击停止，则不要去更新界面报警颜色了
            if (m_bKeepCheckPumpStopStatus==false)
            {
                if (!bFind)
                {
                    SetStatusAndBackground(Colors.Yellow, EAgingStatus.Recharging);
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(new DeleSetTitleAndEnable(SetTitleAndEnable), new object[] { "正在停止泵......", false });
            }
        }

        /// <summary>
        /// 某一老化架的老化结束状态由最后一个泵结束作为标志
        /// 查询目前所有泵的数量，不包括连续20次以上失去连接的泵
        /// </summary>
        /// <returns></returns>
        public int GetAgingPumpCount()
        {
            int iCount = 0;
            for(int i=0;i<m_Controllers.Count;i++)
            {
                //已经连接上的控制器在统计范围内，如果控制器在老化过程中，失去连接，则排除之
                if (m_Controllers[i].SocketToken != null/*&& m_Controllers[i].SocketToken.ConnectSocket !=null*/)
                {
                    iCount += m_Controllers[i].AgingPumpList.Count((x) => { return x.LostTimes <= 20; });//如果控制器连接，但其中某个泵失连超过20次，则排除此泵，不要影响其他泵的老化进程
                }
            }
            return iCount;
        }

        /// <summary>
        /// 根据某个控制器的行号，确定该控制器连接了多少泵，对相关的泵进行发送命令
        /// </summary>
        /// <param name="rowNo"></param>
        /// <returns></returns>
        private void GenChannel()
        {
            List<Tuple<int,int,int>> pumpLocationList = m_HashPumps[m_DockNo] as List<Tuple<int,int,int>>;
            if(pumpLocationList==null)
            {
                Logger.Instance().ErrorFormat("配置信息错误，相应的货架编号没有选择相应的泵,DockNo={0}",m_DockNo);
                return;
            }
            m_HashRowNo.Clear();
            int channel = 0,temp = 0,channelBase = 1;
            foreach(var pump in pumpLocationList)
            {
                channel = channelBase<<pump.Item3-1;
                if(!m_HashRowNo.Contains(pump.Item2))
                {
                    m_HashRowNo.Add(pump.Item2, channel);
                }
                else
                {
                    temp = (int)m_HashRowNo[pump.Item2];
                    m_HashRowNo[pump.Item2] = temp+channel;
                }
            }
        }

        public void ExportExcel(string fileName)
        {
            string saveFileName = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetAssembly(typeof(DockWindow)).Location);
            string dirName = saveFileName + "\\" + DateTime.Now.ToString("yyyy-MM-dd");
            string dockName = dirName + "\\" + this.m_DockNo.ToString()+"号货架";
            if(!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            if (!Directory.Exists(dockName))
            {
                Directory.CreateDirectory(dockName);
            }
            saveFileName = dockName + "\\" + fileName;
            if (String.IsNullOrEmpty(saveFileName.Trim()))
                return; //No name
         
            AgingParameter parameter = m_DockParameter[m_DockNo] as AgingParameter;
            int columns = DockInfoManager.Instance().Get(m_DockNo)/5;
            Excel.Application xlApp = new Excel.Application();
            if (xlApp == null)
            {
                return;
            }
            Excel.Workbooks workbooks = xlApp.Workbooks;
            Excel.Workbook workbook = workbooks.Add(Excel.XlWBATemplate.xlWBATWorksheet);
            Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Worksheets.Add(Missing.Value, Missing.Value, Missing.Value, Missing.Value);
            worksheet.Cells.Font.Size = 10;
            worksheet.Name = m_DockNo+"号货架老化结果";
            
            int index = 0;
            worksheet.Cells[1, ++index] = "序号";
            worksheet.Cells[1, ++index] = "机位";
            worksheet.Cells[1, ++index] = "型号";
            worksheet.Cells[1, ++index] = "机器编号";
            worksheet.Cells[1, ++index] = "速率(mL/h)";
            worksheet.Cells[1, ++index] = "老化开始";
            worksheet.Cells[1, ++index] = "放电开始";
            worksheet.Cells[1, ++index] = "低电压";
            worksheet.Cells[1, ++index] = "电池耗尽";
            worksheet.Cells[1, ++index] = "老化结束";
            worksheet.Cells[1, ++index] = "老化时长(h)";
            worksheet.Cells[1, ++index] = "放电总时间(h)";
            worksheet.Cells[1, ++index] = "耗尽时长(h)";
            worksheet.Cells[1, ++index] = "老化结果";
            worksheet.Cells[1, ++index] = "报警";
            int rowIndex = 2;
            List<Controller> contorllerList = m_Controllers.OrderBy(x=>x.DockNo).ToList<Controller>();
            for(int i=0;i<contorllerList.Count;i++)
            {
                if(contorllerList[i]!=null)
                {
                    List<AgingPump> pumpList = contorllerList[i].SortAgingPumpList();
                    for(int j=0;j<pumpList.Count;j++)
                    {
                        if(pumpList[j]!=null)
                        {
                            index = 0;
                            worksheet.Cells[rowIndex, ++index] = rowIndex-1;
                            worksheet.Cells[rowIndex, ++index] = string.Format("{0}—{1}—{2}", m_DockNo, pumpList[j].RowNo, pumpList[j].Channel);
                            worksheet.Cells[rowIndex, ++index] = pumpList[j].PumpType;
                            worksheet.Cells[rowIndex, ++index] = "";
                            if(parameter!=null)
                                worksheet.Cells[rowIndex, ++index] = parameter.Rate.ToString();
                            if(pumpList[j].BeginAgingTime.Year>2000
                                && pumpList[j].BeginDischargeTime.Year>2000
                                && pumpList[j].BeginLowVoltageTime.Year>2000
                                && pumpList[j].BeginBattaryDepleteTime.Year>2000
                                && pumpList[j].EndAgingTime.Year>2000
                                )
                            {
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].BeginAgingTime.ToString("yyyy-MM-dd HH:mm:ss");
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].BeginDischargeTime.ToString("yyyy-MM-dd HH:mm:ss");
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].BeginLowVoltageTime.ToString("yyyy-MM-dd HH:mm:ss");
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].BeginBattaryDepleteTime.ToString("yyyy-MM-dd HH:mm:ss");
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].EndAgingTime.ToString("yyyy-MM-dd HH:mm:ss");
                                worksheet.Cells[rowIndex, ++index] = (pumpList[j].EndAgingTime-pumpList[j].BeginAgingTime).TotalHours.ToString("F2");
                                worksheet.Cells[rowIndex, ++index] = (pumpList[j].BeginBattaryDepleteTime-pumpList[j].BeginDischargeTime).TotalHours.ToString("F2");//耗尽－开始
                                worksheet.Cells[rowIndex, ++index] = (pumpList[j].BeginBattaryDepleteTime-pumpList[j].BeginLowVoltageTime).TotalHours.ToString("F2");//耗尽－低电
                                bool bPass = pumpList[j].IsPass();
                                worksheet.Cells[rowIndex, ++index] = bPass==true?"通过":"失败";
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].GetAlarmString();
                                Excel.Range titleRange = worksheet.Range[worksheet.Cells[rowIndex, 1], worksheet.Cells[rowIndex, index]];//选取一行   
                                if (bPass)
                                    titleRange.Interior.ColorIndex = 35;//设置绿颜色
                                else
                                    titleRange.Interior.ColorIndex = 3;//设置红颜色
                            }
                            else
                            {
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].BeginAgingTime.Year > 2000 ? pumpList[j].BeginAgingTime.ToString("yyyy-MM-dd HH:mm:ss") : "";
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].BeginDischargeTime.Year > 2000 ? pumpList[j].BeginDischargeTime.ToString("yyyy-MM-dd HH:mm:ss") : "";
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].BeginLowVoltageTime.Year > 2000 ? pumpList[j].BeginLowVoltageTime.ToString("yyyy-MM-dd HH:mm:ss") : "";
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].BeginBattaryDepleteTime.Year > 2000 ? pumpList[j].BeginBattaryDepleteTime.ToString("yyyy-MM-dd HH:mm:ss") : "";
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].EndAgingTime.Year > 2000 ? pumpList[j].EndAgingTime.ToString("yyyy-MM-dd HH:mm:ss") : "";
                                worksheet.Cells[rowIndex, ++index] = "";
                                worksheet.Cells[rowIndex, ++index] = "";//耗尽－开始
                                worksheet.Cells[rowIndex, ++index] = "";;//耗尽－低电
                                worksheet.Cells[rowIndex, ++index] = "失败";
                                worksheet.Cells[rowIndex, ++index] = pumpList[j].GetAlarmString();
                                Excel.Range titleRange = worksheet.Range[worksheet.Cells[rowIndex, 1], worksheet.Cells[rowIndex, index]];//选取一行   
                                titleRange.Interior.ColorIndex = 3;//设置红颜色
                            }

                        }
                        rowIndex++;
                    }
                }
            }
           
            if (saveFileName != "")
            {
                try
                {
                    workbook.Saved = true;
                    System.Reflection.Missing miss = System.Reflection.Missing.Value;
                    workbook.SaveCopyAs(saveFileName);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                return;
            }
            if (workbook != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                workbook = null;
            }
            if (workbooks != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbooks);
                workbooks = null;
            }
            xlApp.Application.Workbooks.Close();
            xlApp.Quit();
            if (xlApp != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
                xlApp = null;
            }
            GC.Collect();
        }

        #region 检查泵启动状态系列函数

        /// <summary>
        /// 创建多个线程，每个线程负责一个控制器，不断去查询下面的泵状态，是否启动。
        /// </summary>
        private void CreateCheckPumpStatusThread()
        {
            if (m_CheckPumpRunningStatusThreads.Count>0)
            {
                foreach(var th in m_CheckPumpRunningStatusThreads)
                {
                    if (th.ThreadState == ThreadState.Running
                       || th.ThreadState == ThreadState.Suspended
                       )
                        th.Abort();
                }
            }
            m_CheckPumpRunningStatusThreads.Clear();
            List<Controller> configControllers = ControllerManager.Instance().Get(m_DockNo);
           //List<Controller> tempController = configControllers.FindAll((x) => { return x.SocketToken!=null; });
            foreach (var controller in configControllers)
            {
                Thread th = new Thread((ParameterizedThreadStart)ProcCheckPumpStatus);
                m_CheckPumpRunningStatusThreads.Add(th);
            }
            for(int i = 0; i<m_CheckPumpRunningStatusThreads.Count;i++)
            {
                m_CheckPumpRunningStatusThreads[i].Start(configControllers[i]);
            }
            StartCheckPumpStatusTimer();
        }

        /// <summary>
        /// 检查泵状态线程执行函数
        /// </summary>
        /// <param name="para">控制器</param>
        private void ProcCheckPumpStatus(object para)
        {
            if (para == null)
                return;
            Controller controller = (Controller)para;
            if (!m_HashRowNo.ContainsKey(controller.RowNo))
                return;
            int iChannel = (int)m_HashRowNo[controller.RowNo];
            byte channel = (byte)(iChannel & 0x000000FF);
            //永远等待下去，直到有信号出现为止,若要关闭，将m_bKeepCheckPumpStatus设置为false
            //如果该控制器已经全部启动了，则终止线程
            while (m_bKeepCheckPumpStatus && !controller.IsRunning)
            {
                if (m_bCanStartCheck)
                {
                    if (controller.SocketToken != null)
                    {
                        //发送泵类型命令后等待它的回应，根据回应结果判断是否要重新发送启动命令
                        //命令的返回及后续操作，由另外一个线程来操作，这里只需要等待即可
                        m_CmdManager.SendPumpType(m_CurrentProductID, m_QueryInterval, controller.SocketToken, CommandResponseForCheckPumpStatus, null, channel);
                        //每个控制器等待10秒,由CommandResponseForCheckPumpStatus函数来更新状态
                        Thread.Sleep(10000);
                    }
                }
                Thread.Sleep(1000);
            }
        }
        
        /// <summary>
        /// 回应命令响应,回应的命令中全带有序列号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandResponseForCheckPumpStatus(object sender, EventArgs e)
        {
            if (e is CmdSendPumpType)
            {
                CmdSendPumpType cmd = e as CmdSendPumpType;
                HandleCmdSendPumpTypeResponseForCheckPumpStatus(cmd);
            }
        }

        private void HandleCmdSendPumpTypeResponseForCheckPumpStatus(CmdSendPumpType cmd)
        {
            if (cmd != null && cmd.RemoteSocket != null)
            {
                byte retChannel = CheckChannelAndPumpStatus(cmd);   //检查哪些通道上的泵没有启动,返回通道号
                long ip = ControllerManager.GetLongIPFromSocket(cmd.RemoteSocket);
                List<Controller> configControllers = ControllerManager.Instance().Get(m_DockNo);
                Controller controller = configControllers.Find((x) => { return x.IP == ip; });
                if (controller==null)
                {
                    Logger.Instance().ErrorFormat("AgingDock::HandleCmdSendPumpTypeResponseForCheckPumpStatus() Error, 找不到IP={0}的控制器", ip);
                    return;
                }

                if (retChannel == 0)
                {
                    if (controller != null)
                    {
                        controller.IsRunning = true;
                    }
                    Logger.Instance().InfoFormat("AgingDock::HandleCmdSendPumpTypeResponseForCheckPumpStatus()->此控制器下的泵已经全部启动,IP={0}", ip);
                    return;
                }
                //重新发送启动命令
                AgingParameter para = m_DockParameter[m_DockNo] as AgingParameter;
                if (para==null)
                {
                    Logger.Instance().ErrorFormat("AgingDock::HandleCmdSendPumpTypeResponseForCheckPumpStatus() Error, DockNo={0}的老化参数为null", m_DockNo);
                    Logger.Instance().Error("AgingDock::HandleCmdSendPumpTypeResponseForCheckPumpStatus() Error, 发送启动命令失败");
                    return;
                }
                m_CmdManager.SendCmdCharge(para.Rate, para.Volume, cmd.RemoteSocket, null, null, retChannel);
                if (controller.AgingStatus == EAgingStatus.Charging && controller.BeginAginTime.Year > 2000)
                {
                    //此控制器已经有部分泵启动了,不用处理
                }
                else
                {
                    //此控制器没有执行过启动命令，
                    controller.AgingStatus = EAgingStatus.Charging;
                    controller.BeginAginTime = DateTime.Now;
                }
            }
            else
            {
                Logger.Instance().ErrorFormat("AgingDock::HandleCmdSendPumpTypeResponseForCheckPumpStatus() Error, 参数为null");
                return;
            }
        }

        /// <summary>
        /// 检查泵状态与控制器的通道号的对应关系，查看哪一个通道上的泵状态是不正常的,返回需要重新发送开始启动命令的通道
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns>通道号</returns>
        private byte CheckChannelAndPumpStatus(CmdSendPumpType cmd)
        {
            #region
            if (cmd == null)
                return 0xFF;
            long ip = ControllerManager.GetLongIPFromSocket(cmd.RemoteSocket);
            List<Controller> configControllers = ControllerManager.Instance().Get(m_DockNo);
            Controller controller = configControllers.Find((x) => { return x.IP == ip; });
            if (controller == null)
            {
                Logger.Instance().ErrorFormat("AgingDock::CheckChannelAndPumpStatus() Error, 找不到IP={0}的控制器", ip);
                return 0xFF;
            }
            else
            {
                int iChannel = (int)m_HashRowNo[controller.RowNo];
                byte controllerChannel = (byte)(iChannel & 0x000000FF);//控制器已经连接的通道
                //如果控制器返回的命令中通道号与原来定义的不同，则一定是某个泵没有响应
                if (cmd.Channel != controllerChannel)
                {
                    //此处暂时不处理，当发生某个泵一直停止不了时再改此处代码
                    Logger.Instance().ErrorFormat("AgingDock::CheckChannelAndPumpStatus() Error, 返回命令中通道号={0},控制器的通道号={1}", cmd.Channel, controllerChannel);
                }
            }
            #endregion
            List<byte> channels = new List<byte>();
            byte bitIndex = 0x01;
            while (bitIndex <= 0x80)
            {
                if ((byte)(cmd.Channel & bitIndex) == bitIndex)
                    channels.Add(bitIndex);
                if (bitIndex == 0x80)
                    break;
                bitIndex = (byte)((bitIndex << 1) & 0x00ff);
            }
            if(channels.Count!=cmd.PumpStatusList.Count)
            {
                Logger.Instance().Error("AgingDock::CheckChannelAndPumpStatus() Error, 通道号与泵状态的个数不相等");
                //如果不相等，那么给所有泵发送启动命令。
                return cmd.Channel;
            }
            else
            {
                byte retChannel = 0;
                for(int iLoop=0;iLoop<channels.Count;iLoop++)
                {
                    if (cmd.PumpStatusList[iLoop] == PumpStatus.Stop
                       || cmd.PumpStatusList[iLoop] == PumpStatus.Pause
                       || cmd.PumpStatusList[iLoop] == PumpStatus.Off)
                        retChannel |= channels[iLoop];
                }
                Logger.Instance().InfoFormat("AgingDock::CheckChannelAndPumpStatus(),计算出没有启动的通道={0}", retChannel);
                return retChannel;
            }
        }

        #endregion

        #region 检查泵停止状态系列函数

        /// <summary>
        /// 创建多个线程，每个线程负责一个控制器，不断去查询下面的泵状态，是否停止。
        /// </summary>
        private void CreateCheckPumpStopStatusThread()
        {
            if (m_CheckPumpStopStatusThreads.Count > 0)
            {
                foreach (var th in m_CheckPumpStopStatusThreads)
                {
                    if (th.ThreadState == ThreadState.Running
                       || th.ThreadState == ThreadState.Suspended
                       )
                        th.Abort();
                }
            }
            m_CheckPumpStopStatusThreads.Clear();
            List<Controller> configControllers = ControllerManager.Instance().Get(m_DockNo);
            foreach (var controller in configControllers)
            {
                Thread th = new Thread((ParameterizedThreadStart)ProcCheckPumpStopStatus);
                m_CheckPumpStopStatusThreads.Add(th);
            }
            for (int i = 0; i < m_CheckPumpStopStatusThreads.Count; i++)
            {
                m_CheckPumpStopStatusThreads[i].Start(configControllers[i]);
            }
            StartCheckPumpStopStatusTimer();
        }

        /// <summary>
        /// 检查泵状态线程执行函数
        /// </summary>
        /// <param name="para">控制器</param>
        private void ProcCheckPumpStopStatus(object para)
        {
            if (para == null)
                return;
            Controller controller = (Controller)para;
            if (!m_HashRowNo.ContainsKey(controller.RowNo))
                return;
            int iChannel = (int)m_HashRowNo[controller.RowNo];
            byte channel = (byte)(iChannel & 0x000000FF);
            //永远等待下去，直到有信号出现为止,若要关闭，将m_bKeepCheckPumpStopStatus设置为false
            //如果该控制器已经全部启动了，则终止线程
            while (m_bKeepCheckPumpStopStatus && controller.IsRunning)
            {
                if (controller.SocketToken != null)
                {
                    //发送泵类型命令后等待它的回应，根据回应结果判断是否要重新发送启动命令
                    //命令的返回及后续操作，由另外一个线程来操作，这里只需要等待即可
                    m_CmdManager.SendPumpType(m_CurrentProductID, m_QueryInterval, controller.SocketToken, CommandResponseForCheckPumpStopStatus, null, channel);
                    //每个控制器等待2秒,由CommandResponseForCheckPumpStopStatus函数来更新状态
                    Thread.Sleep(2000);
                }
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// 回应命令响应,回应的命令中全带有序列号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandResponseForCheckPumpStopStatus(object sender, EventArgs e)
        {
            if (e is CmdSendPumpType)
            {
                CmdSendPumpType cmd = e as CmdSendPumpType;
                HandleCmdSendPumpTypeResponseForCheckPumpStopStatus(cmd);
            }
        }

        private void HandleCmdSendPumpTypeResponseForCheckPumpStopStatus(CmdSendPumpType cmd)
        {
            if (cmd != null && cmd.RemoteSocket != null)
            {
                byte retChannel = CheckChannelAndPumpStopStatus(cmd);   //检查哪些通道上的泵没有停止,返回通道号
                long ip = ControllerManager.GetLongIPFromSocket(cmd.RemoteSocket);
                string szIP = ControllerManager.Long2IP(ip);
                List<Controller> configControllers = ControllerManager.Instance().Get(m_DockNo);
                Controller controller = configControllers.Find((x) => { return x.IP == ip; });
                if (controller == null)
                {
                    Logger.Instance().ErrorFormat("AgingDock::HandleCmdSendPumpTypeResponseForCheckPumpStopStatus() Error, 找不到IP={0}的控制器", szIP);
                    return;
                }

                if (retChannel == 0)
                {
                    if (controller != null)
                    {
                        controller.IsRunning = false;
                    }
                    Logger.Instance().InfoFormat("AgingDock::HandleCmdSendPumpTypeResponseForCheckPumpStopStatus()->此控制器下的泵已经全部停止,IP={0}", szIP);
                    return;
                }
                //重新发送完成停止命令
                m_CmdManager.SendCmdFinishAging(cmd.RemoteSocket, null, null, retChannel);
            }
            else
            {
                Logger.Instance().ErrorFormat("AgingDock::HandleCmdSendPumpTypeResponseForCheckPumpStopStatus() Error, 参数为null");
                return;
            }
        }

        /// <summary>
        /// 检查泵状态与控制器的通道号的对应关系，查看哪一个通道上的泵状态是不正常的,返回需要重新发送重新发送完成停止命令的通道
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns>通道号</returns>
        private byte CheckChannelAndPumpStopStatus(CmdSendPumpType cmd)
        {
            #region
            if (cmd == null)
                return 0xFF;
            long ip = ControllerManager.GetLongIPFromSocket(cmd.RemoteSocket);
            List<Controller> configControllers = ControllerManager.Instance().Get(m_DockNo);
            Controller controller = configControllers.Find((x) => { return x.IP == ip; });
            if (controller == null)
            {
                Logger.Instance().ErrorFormat("AgingDock::CheckChannelAndPumpStopStatus() Error, 找不到IP={0}的控制器", ip);
                return 0xFF;
            }
            else
            {
                int iChannel = (int)m_HashRowNo[controller.RowNo];
                byte controllerChannel = (byte)(iChannel & 0x000000FF);//控制器已经连接的通道
                //如果控制器返回的命令中通道号与原来定义的不同，则一定是某个泵没有响应
                if(cmd.Channel != controllerChannel)
                {
                    //此处暂时不处理，当发生某个泵一直停止不了时再改此处代码
                    Logger.Instance().ErrorFormat("AgingDock::CheckChannelAndPumpStopStatus() Error, 返回命令中通道号={0},控制器的通道号={1},控制器所在货架号={2},控制器所在货架行号={3},", cmd.Channel, controllerChannel, controller.DockNo, controller.RowNo);
                }
            }
            #endregion

            List<byte> channels = new List<byte>();
            byte bitIndex = 0x01;
            while (bitIndex <= 0x80)
            {
                if ((byte)(cmd.Channel & bitIndex) == bitIndex)
                    channels.Add(bitIndex);
                if (bitIndex == 0x80)
                    break;
                bitIndex = (byte)((bitIndex << 1) & 0x00ff);
            }
            if (channels.Count != cmd.PumpStatusList.Count)
            {
                Logger.Instance().Error("AgingDock::CheckChannelAndPumpStopStatus() Error, 通道号与泵状态的个数不相等");
                //如果不相等，那么给所有泵发送启动命令。
                return cmd.Channel;
            }
            else
            {
                byte retChannel = 0;
                for (int iLoop = 0; iLoop < channels.Count; iLoop++)
                {
                    if (cmd.PumpStatusList[iLoop] == PumpStatus.Run
                       || cmd.PumpStatusList[iLoop] == PumpStatus.Bolus
                       || cmd.PumpStatusList[iLoop] == PumpStatus.Purging
                       || cmd.PumpStatusList[iLoop] == PumpStatus.KVO)
                        retChannel |= channels[iLoop];
                }
                Logger.Instance().InfoFormat("AgingDock::CheckChannelAndPumpStopStatus(),计算出没有停止的通道={0}", retChannel);
                return retChannel;
            }
        }

        #endregion

        #region 检查控制器是否放电系列函数

        /// <summary>
        /// 创建一个线程，不断地给未放电的控制器发放电命令。
        /// </summary>
        private void CreateCheckDisChargeThread()
        {
            StartCheckDisChargeTimer();
            Thread th = new Thread(ProcCheckDisCharge);
            th.Start();
        }

        /// <summary>
        /// 检查泵状态线程执行函数
        /// </summary>
        /// <param name="para">控制器</param>
        private void ProcCheckDisCharge()
        {
            int iChannel = 0;
            byte channel = 0;
            //List<Controller> configControllers = ControllerManager.Instance().Get(m_DockNo);
            List<Controller> connected_controllers = m_Controllers.FindAll((x) => { return x.SocketToken != null; });
            if (connected_controllers != null && connected_controllers.Count > 0)
            {
                m_HashDisChargeController.Clear();
                //先将控制器ip放进表中，状态置false
                for (int i = 0; i < connected_controllers.Count; i++)
                {
                    if (!m_HashDisChargeController.ContainsKey(connected_controllers[i].IP))
                        m_HashDisChargeController.Add(connected_controllers[i].IP, false);
                }
                while (m_bKeepDisCharge && m_HashDisChargeController.Count > 0)
                {
                    for (int i = 0; i < connected_controllers.Count; i++)
                    {
                        if (!m_HashRowNo.ContainsKey(connected_controllers[i].RowNo))
                            continue;
                        if (!m_HashDisChargeController.ContainsKey(connected_controllers[i].IP))
                            continue;
                        iChannel = (int)m_HashRowNo[connected_controllers[i].RowNo];
                        channel = (byte)(iChannel & 0x000000FF);
                        //老化放电命令
                        m_CmdManager.SendCmdDischarge(connected_controllers[i].SocketToken, CommandResponseForDischarge, null, channel);
                        Thread.Sleep(3000);
                    }
                    Thread.Sleep(5000);
                }
            }
        }

        /// <summary>
        /// 回应命令响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandResponseForDischarge(object sender, EventArgs e)
        {
            if (e is CmdDischarge)
            {
                CmdDischarge cmd = e as CmdDischarge;
                long ip = ControllerManager.GetLongIPFromSocket(cmd.RemoteSocket);
                m_HashDisChargeController.Remove(ip);
                Controller controller = m_Controllers.Find((x) => { return x.IP == ip; });
                if (controller.SocketToken != null)
                {
                    controller.BeginDischargeTime = DateTime.Now;
                    foreach (var agingPump in controller.AgingPumpList)
                    {
                        if (agingPump != null)
                        {
                            agingPump.AgingStatus = EAgingStatus.DisCharging;//"老化放电中";
                            agingPump.BeginDischargeTime = DateTime.Now;
                        }
                    }
                }

            }
        }

        #endregion

        #region 检查控制器是否补电系列函数

        /// <summary>
        /// 创建一个线程，不断地给未补电的控制器发补电命令。
        /// </summary>
        private void CreateCheckReChargeThread()
        {
            Thread th = new Thread(ProcCheckReCharge);
            th.Start();
        }

        private void StopCheckReChargeThread()
        {
            m_bKeepReCharge = false;
            Thread.Sleep(5000);
            lock (m_DepleteManager)
            {
                m_DepleteManager.Clear();
            }
        }

        /// <summary>
        /// 检查泵状态线程执行函数
        /// </summary>
        /// <param name="para">控制器</param>
        private void ProcCheckReCharge()
        {
            byte channels = 0;
            while (m_bKeepReCharge)
            {
                for (int i = 0; i < m_DepleteManager.DepletePumpQueue.Count; i++)
                {
                    channels = 0;
                    Controller depleteController = m_Controllers.Find((x) => { return x.SocketToken != null && x.SocketToken.IP == m_DepleteManager.DepletePumpQueue[i].ip; });
                    if (depleteController != null)
                    {
                        channels = m_DepleteManager.DepletePumpQueue[i].GenChannel();
                        m_CmdManager.SendCmdRecharge(channels, depleteController.SocketToken, CommandResponseForReCharge);
                        Logger.Instance().InfoFormat("向IP={0}的控制器发送补电命令，通道号={1}", depleteController.IP, channels);
                    }
                }
                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// 回应命令响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandResponseForReCharge(object sender, EventArgs e)
        {
            if (e is CmdRecharge)
            {
                CmdRecharge cmd = e as CmdRecharge;
                Logger.Instance().InfoFormat("收到控制器IP={0}的补电回应", cmd.RemoteSocket.IP);
                Controller depleteController = m_Controllers.Find((x) => { return x.SocketToken!=null && x.SocketToken.IP == cmd.RemoteSocket.IP; });
                DepletePumpList pumpList = null;
                lock (m_DepleteManager)
                {
                    pumpList = m_DepleteManager.GetDepletePumpsByIP(cmd.RemoteSocket.IP);
                }
                if (depleteController == null || pumpList == null)
                    return;
                if (pumpList.channels.Count == 0)
                    return;
                //byte channel = 0;
                foreach (var ch in pumpList.channels)
                {
                    AgingPump pump = null;
                    try
                    {
                        pump = depleteController.AgingPumpList.Find((x) => { return x.Channel == ch; });
                    }
                    catch
                    {
                        Logger.Instance().Info("CommandResponseForReCharge()->depleteController.AgingPumpList.Find() Error");
                    }
                    if (pump != null)
                    {
                        pump.BeginRechargeTime = DateTime.Now;
                        pump.AgingStatus = EAgingStatus.Recharging;
                        Logger.Instance().InfoFormat("货架编号={0},控制器IP={1},通道号={2}的泵已经补电", pump.DockNo, cmd.RemoteSocket.IP, pump.Channel);
                    }
                    else
                    {
                        Logger.Instance().InfoFormat("CommandResponseForReCharge()->泵对象为null 控制器IP={0} 通道号={1}", cmd.RemoteSocket.IP, ch);
                    }
                }
                //收到回应后移除报警泵
                lock (m_DepleteManager)
                {
                    m_DepleteManager.Remove(cmd.RemoteSocket.IP);
                }
            }
        }

        #endregion

    }
}
