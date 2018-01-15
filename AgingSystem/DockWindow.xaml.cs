using System;
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
using System.Windows.Shapes;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.Windows.Threading;
using Analyse;
using AsyncSocket;
using Cmd;

namespace  AgingSystem
{
    /// <summary>
    /// Interaction logic for DockWindow.xaml
    /// </summary>
    public partial class DockWindow : Window
    {
        private delegate void SocketConnectOrCloseDelegate(SocketConnectArgs e);
        private delegate void OnUploadAlarmDelegate(BaseCommand e);
        
        private int                 m_DockCount        = 0;
        public static int           m_QueryInterval    = 0;
        public static Hashtable     m_DockParameter    = new Hashtable();         //存放每个货架的配置信息（int 货架号，DefaultParameter）
        public static Hashtable     m_DockPumpList     = new Hashtable();         //存放每个货架泵信息（int 货架号，List<Tuple<int,int,int>>分别是位置，行号，列号）
        private List<AgingDock>     m_DockList         = new List<AgingDock>();   //泵架列表
        private List<Color> ColorSet                   = new List<Color>();
        private DispatcherTimer     m_ShowCurrentTimer = new DispatcherTimer();   //显示当前时间 
        private int                 m_TimeOut          = 60000;                    //默认所有超时时间为60秒，包括命令响应时间
        private int                 m_HeartBeatTimeOut = 240000;                   //默认所有超时时间为4分钟，心跳
        public static int           m_CheckPumpStatusMaxMunites = 30 * 60;                   //分，设置检查泵状态持续时间，超时将关闭线程
        public static int           m_CheckPumpStopStatusMaxMunites = 300;              //分，设置检查泵停止状态持续时间，超时将关闭线程
        public static int           m_CheckDisChargeMaxMunites = 180;              //分，设置检查放电持续时间，超时将关闭线程

        public DockWindow()
        {
            InitializeComponent();
            ColorSet.Add(Color.FromRgb(34, 200, 230));
            ColorSet.Add(Color.FromRgb(20, 149, 171));
            ColorSet.Add(Color.FromRgb(12, 87, 101));
            ColorSet.Add(Color.FromRgb(6, 52, 60));
            //ColorSet.Add(Color.FromRgb(0x99, 0xD9, 0xEA));
            //ColorSet.Add(Color.FromRgb(0x70, 0x92, 0xBE));
            //ColorSet.Add(Color.FromRgb(0xC8, 0xBF, 0xE7));
            //ColorSet.Add(Color.FromRgb(0x2E, 0x66, 0xBA));
            m_ShowCurrentTimer.Interval = new TimeSpan(0,0,1); //一秒钟更新一次
            m_ShowCurrentTimer.Tick += new EventHandler(OnShowCurrentTime);
        }

        /// <summary>
        /// 显示当前时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnShowCurrentTime(object sender, EventArgs e)
        {
            lbCurrentTime.Content = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StartCurrentTimer();
            Logger.Instance().Info("");
            Logger.Instance().Info("");
            Logger.Instance().Info("");
            Logger.Instance().Info("");
            Logger.Instance().Info("");
            Logger.Instance().Info("=====================应用程序启动===========================");
            System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (!(int.TryParse(config.AppSettings.Settings["DockCount"].Value, out m_DockCount)))
                m_DockCount = 10;
            if (!(int.TryParse(config.AppSettings.Settings["QueryInterval"].Value, out m_QueryInterval)))
                m_QueryInterval = 60;
            if (!(int.TryParse(config.AppSettings.Settings["TimeOut"].Value, out m_TimeOut)))
                m_TimeOut = 60000;
            if (!(int.TryParse(config.AppSettings.Settings["HeartBeat"].Value, out m_HeartBeatTimeOut)))
                m_HeartBeatTimeOut = 240000;
            if (!(int.TryParse(config.AppSettings.Settings["CheckPumpStatusMaxMunites"].Value, out m_CheckPumpStatusMaxMunites)))
                m_CheckPumpStatusMaxMunites = 30*60;
            if (!(int.TryParse(config.AppSettings.Settings["CheckPumpStopStatusMaxMunites"].Value, out m_CheckPumpStopStatusMaxMunites)))
                m_CheckPumpStopStatusMaxMunites = 5 * 60;//5分钟
            if (!(int.TryParse(config.AppSettings.Settings["CheckDisChargeMaxMunites"].Value, out m_CheckDisChargeMaxMunites)))
                m_CheckDisChargeMaxMunites = 30 * 60;//30分钟
            DockInfoManager.Instance().Init();
            ControllerManager.Instance().Init();
            LoadDockList();
            ProtocolEngine.Instance().SetTimeOut(m_TimeOut);            //设置命令解析超时时间
            AsyncServer.Instance().SocketTimeOut = m_HeartBeatTimeOut;  //心跳超时时间
            ProtocolEngine.Instance().InitTcp();
            ProtocolEngine.Instance().SocketConnectOrCloseResponse += OnSocketConnectOrClose;
            ProtocolEngine.Instance().SendPumpType2Wifi += OnSendPumpType2Wifi;
            ProtocolEngine.Instance().UploadAlarm += OnUploadAlarm;
            ProtocolEngine.Instance().Start();
        }

        /// <summary>
        /// 窗口退出时关闭TCP服务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(MessageBox.Show("确定要关闭？", "窗口关闭提示", MessageBoxButton.YesNo)==MessageBoxResult.Yes)
            { 
                ProtocolEngine.Instance().Stop();
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 开启时钟
        /// </summary>
        private void StartCurrentTimer()
        {
            StopCurrentTimer();
            m_ShowCurrentTimer.Start();
        }

        /// <summary>
        /// 停止显示当前时间
        /// </summary>
        private void StopCurrentTimer()
        {
            m_ShowCurrentTimer.Stop();
        }

        private void LoadDockList()
        {
            m_DockList.Clear();
            if (m_DockCount <= 0)
            {
                Logger.Instance().Info("机架数量小于等于0，请重新设置。");
                return;
            }
            int rowCount = m_DockCount / 5;
            if (m_DockCount % 5 > 0)
                rowCount += 1;

            for (int i = 0; i < rowCount; i++)
            {
                RowDefinition row = new RowDefinition();
                row.Height = GridLength.Auto;
                dockGrid.RowDefinitions.Add(row);
            }
            for (int i = 0; i < 5; i++)
            {
                ColumnDefinition col = new ColumnDefinition();
                col.Width = GridLength.Auto;
                dockGrid.ColumnDefinitions.Add(col);
            }

            for (int i = 0; i < m_DockCount; i++)
            {
                AgingDock dock = new AgingDock();
                dock.OnConfigrationCompleted += OnConfigrationCompleted;
                dock.OnPumpSelected += OnPumpSelected;
                dock.SetTimeOut(m_TimeOut);//设置信号量的等待时间,和命令超时时间一样
                dock.Name = "dock" + (i + 1).ToString();
                dock.Tag = i + 1;
                dock.DockNo = i + 1;
                dock.Margin = new Thickness(10, 10, 10, 10);
                dock.SetTitle((i + 1).ToString() + "号架");
                dockGrid.Children.Add(dock);
                m_DockList.Add(dock);
                Grid.SetRow(dock, i / 5);
                dock.dockGrid.Background = new SolidColorBrush(ColorSet[i / 5]);
                dock.DefaultColor =ColorSet[i / 5];
                Grid.SetColumn(dock, i % 5);
            }
        }

        private void OnConfigrationCompleted(object sender, ConfigrationArgs e)
        {
            foreach(DictionaryEntry entry in e.DockParameter)
            {
                if(m_DockParameter.ContainsKey(entry.Key))
                {
                    m_DockParameter[entry.Key] = new AgingParameter((AgingParameter)entry.Value);
                }
                else
                {
                    m_DockParameter.Add(entry.Key, new AgingParameter((AgingParameter)entry.Value));
                }
            }
            AgingDock dock = null;
            for(int i=0;i<m_DockList.Count;i++)
            {
                
                dock = m_DockList[i];
                if(m_DockParameter.ContainsKey(dock.DockNo))
                {
                    dock.lbPumpType.Content = "泵型号:" + ((AgingParameter)m_DockParameter[dock.DockNo]).PumpType;
                }
              
            }

        }

        private void OnPumpSelected(object sender, SelectedPumpsArgs e)
        {
            foreach(DictionaryEntry entry in e.SelectedPumps)
            {
                if (m_DockPumpList.ContainsKey(entry.Key))
                {
                    m_DockPumpList[entry.Key] = entry.Value;
                }
                else
                {
                    m_DockPumpList.Add(entry.Key, entry.Value);
                }
            }
        }

        private void OnSocketConnectOrClose(object sender, SocketConnectArgs e)
        {
            this.Dispatcher.Invoke(new SocketConnectOrCloseDelegate(SocketConnectOrClose), new object[] { e });
        }

        /// <summary>
        ///重新连接的时间，延时10秒发送泵类型命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSendPumpType2Wifi(object sender, SocketConnectArgs e)
        {
            this.Dispatcher.Invoke(new SocketConnectOrCloseDelegate(SendPumpType2Wifi), new object[] { e });
            Thread th = new Thread(new ParameterizedThreadStart(ProcSendPumpType2Wifi));
            th.Start(e);
        }

        /// <summary>
        /// 发送线程
        /// </summary>
        /// <param name="obj"></param>
        private void ProcSendPumpType2Wifi(object obj)
        {
            if(obj is SocketConnectArgs)
            {
                Thread.Sleep(10*1000);
                SendPumpType2Wifi(obj as SocketConnectArgs);
                Thread.Sleep(1000);
            }
        }

        private void OnUploadAlarm(object sender, BaseCommand e)
        {
            this.Dispatcher.Invoke(new OnUploadAlarmDelegate(UploadAlarm), new object[] { e });
        }

        /// <summary>
        /// 当客户端l连入或断开时要更新主界面
        /// </summary>
        /// <param name="e"></param>
        private void SocketConnectOrClose(SocketConnectArgs e)
        {
            Controller controller = ControllerManager.Instance().Get(e.ConnectedSocket);
            if(controller==null)
            {
                return;
            }
            int dockNo = controller.DockNo;
            AgingDock dock = null;
            for(int i=0;i<m_DockList.Count;i++)
            {
                dock = m_DockList[i];
                if (dock.DockNo == dockNo)
                {
                    if (e.Connected == true)
                    {
                        #region
                        //如果已经开始老化了，新的控制器不准连入，但是如果是偶然断开重连的除外
                        if(dock.IsStartAging)
                        { 
                            //这里主要是通过连接时间戳判断的
                            if(controller.SocketConnectTimestamp.Year>2000)
                                controller.SocketConnectTimestamp = DateTime.Now;
                            else
                            {
                                if (controller.SocketToken != null && controller.SocketToken.ConnectSocket!=null)
                                    AsyncServer.Instance().CloseClientSocketEx(controller.SocketToken);
                                    //controller.SocketToken.ConnectSocket.Shutdown(SocketShutdown.Both);
                                else
                                {
                                    Logger.Instance().ErrorFormat("SocketConnectOrClose() Error,编号为{0}的控制器SocketToken为null",dockNo);
                                }
                                break;
                            }
                        }
                        else
                        {
                            controller.SocketConnectTimestamp = DateTime.Now;
                        }
                        SolidColorBrush brush = dock.dockGrid.Background as SolidColorBrush;
                        switch(dock.AgingStatus)
                        {
                            case EAgingStatus.Unknown:
                            case EAgingStatus.Waiting:
                                dock.dockGrid.Background = new SolidColorBrush(Colors.Blue);
                                dock.AgingStatus = EAgingStatus.PowerOn;
                                break;
                            case EAgingStatus.PowerOn:
                                dock.dockGrid.Background = new SolidColorBrush(Colors.Blue);
                                    break;
                            case EAgingStatus.Charging:
                            case EAgingStatus.DisCharging:
                            case EAgingStatus.Recharging:
                                dock.dockGrid.Background = new SolidColorBrush(Colors.Yellow);
                                break;
                            case EAgingStatus.AgingComplete:
                                dock.dockGrid.Background = new SolidColorBrush(Colors.Green);
                                break;
                            case EAgingStatus.Alarm:    //出现除低电和耗尽以外的报警时提示红色
                                dock.dockGrid.Background = new SolidColorBrush(Colors.Red);
                                break;
                            default:
                                break;
                        }
                        dock.lbStatus.Content = AgingStatusMetrix.Instance().GetAgingStatus(dock.AgingStatus);
                        List<Controller> controllers = ControllerManager.Instance().Get(dockNo);
                        int clientCount = controllers.Count((x) => { return x.SocketToken != null && x.SocketToken.ConnectSocket!=null && x.SocketToken.ConnectSocket.Connected == true; });
                        dock.lbWifiCount.Content = string.Format("控制器数量:{0}/5", clientCount);
                    #endregion
                    }
                    else
                    {
                        #region
                        List<Controller> controllers = ControllerManager.Instance().Get(dockNo);
                        int clientCount = controllers.Count((x) => { return x.SocketToken != null && x.SocketToken != e.ConnectedSocket && x.SocketToken.ConnectSocket !=null && x.SocketToken.ConnectSocket.Connected == true; });
                        dock.lbWifiCount.Content = string.Format("控制器数量:{0}/5", clientCount);
                        //int index = controllers.FindIndex((x) => { return x.SocketToken != null && x.SocketToken != e.ConnectedSocket;});
                        if (clientCount <= 0)
                        {
                            //dock.AgingStatus = EAgingStatus.Waiting;
                            dock.dockGrid.Background = new SolidColorBrush(dock.DefaultColor);
                            dock.lbStatus.Content = "等待接入";
                            break;
                        }
                        #endregion
                    }
                    break;
                   
                }
            }
        }

        /// <summary>
        /// 当客户端连入时要发送泵类型信息，
        /// 如果用户尚未配置，则不用发，留到点开始的时候发
        /// </summary>
        /// <param name="e"></param>
        private void SendPumpType2Wifi(SocketConnectArgs e)
        {
            Controller controller = ControllerManager.Instance().Get(e.ConnectedSocket);
            if (controller == null)
                return;
            int dockNo = controller.DockNo;
            AgingDock dock = m_DockList.Find((x)=>{return x.DockNo==dockNo;});
            if(dock==null || dock.ChannelHashRowNo==null || !dock.ChannelHashRowNo.ContainsKey(controller.RowNo))
                return;
            AgingParameter para = m_DockParameter[dockNo] as AgingParameter;
            if (para!=null)
            {
                if(Enum.IsDefined(typeof(ProductID), para.PumpType))
                {
                    ProductID pid = (ProductID)Enum.Parse(typeof(ProductID), para.PumpType);
                    CommandManage cmdManager = new CommandManage();
                    int iChannel = (int)dock.ChannelHashRowNo[controller.RowNo];
                    byte channel = (byte)(iChannel & 0x000000FF);
                    //一旦收到连接，立即发送泵信息,必须要延迟10秒
                    cmdManager.SendPumpType(pid, (ushort)m_QueryInterval, e.ConnectedSocket, null, channel);
                }
                else
                {
                    Logger.Instance().ErrorFormat("SendPumpType2Wifi()->para.PumpType is not defined! para.PumpType={0}", para.PumpType);
                }
            }
            else
            {
                Logger.Instance().Debug("SendPumpType2Wifi()->DefaultParameter is null");
            }
        }

        private void UploadAlarm(BaseCommand e)
        {
            if (e != null && e.RemoteSocket != null)
            {
                Controller controller = ControllerManager.Instance().Get(e.RemoteSocket);
                if (controller != null)
                {
                    int dockNo = controller.DockNo;
                    AgingDock dock = m_DockList.Find((x) => { return (int)x.Tag == dockNo; });//在每个货架内部保存报警信息
                    if (dock != null)
                        dock.UpdateAlarmInfo(e as GetAlarm);
                    else
                        Logger.Instance().ErrorFormat("UploadAlarm()->货架没找到 DockNo={0}", dockNo);
                }
                else
                {
                    IPEndPoint point = e.RemoteSocket.ConnectSocket.RemoteEndPoint as IPEndPoint;
                    byte[] ipByte = point.Address.GetAddressBytes();
                    long ip = ControllerManager.Bytes2Long(ipByte);
                    Logger.Instance().ErrorFormat("UploadAlarm()->控制器没找到 ip={0}", ip);
                }
            }
            else
            {
                Logger.Instance().Error("UploadAlarm()->传入的参数为空");
            }
        }
        /// <summary>
        /// 命令响应处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        
        public void CommandResponse(object sender, EventArgs e)
        {
            if (e is CmdSendPumpType)
            {
                CmdSendPumpType cmd = e as CmdSendPumpType;
            }
        }

        /// <summary>
        /// 菜单命令，全部开始，即配置好所有货架后，点击全部开始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStartAll(object sender, RoutedEventArgs e)
        {
            foreach(var dock in m_DockList)
            {
                if(dock!=null)
                {
                    if(dock.CanStart())
                    {
                        dock.StartAging();
                    }
                }
            }
        }

        /// <summary>
        /// 老化全部完成后，点击全部停止，停止老化操作。关闭所有连接，并清空所有状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStopAll(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要全部停止吗？", "提示", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                foreach(var dock in m_DockList)
                {
                    if(dock!=null)
                    {
                        dock.StopAging();
                    }
                }
            }
            else
            {
                return;
            }
        }
         
    }
}
