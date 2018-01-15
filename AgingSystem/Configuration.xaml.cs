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
using System.Collections;
using System.Configuration;
using Cmd;
using Analyse;

namespace  AgingSystem
{
    /// <summary>
    /// Configuration.xaml 的交互逻辑
    /// </summary>
    public partial class Configuration : Window
    {
        public event EventHandler<ConfigrationArgs> OnSaveConfigration;
        public event EventHandler<SelectedPumpsArgs> OnSelectedPumps;
        
        private int m_DockCount = 0;
        private int m_DockNo = 0;       //货架编号从1开始增加
        private List<Color> m_PumpBackgroundColor = new List<Color>();
        
        public Configuration()
        {
            InitializeComponent();
            m_PumpBackgroundColor.Add(Colors.Blue);
            m_PumpBackgroundColor.Add(Colors.Yellow);
            m_PumpBackgroundColor.Add(Colors.Red);
            m_PumpBackgroundColor.Add(Colors.Gray);
            m_PumpBackgroundColor.Add(Colors.Green);
        }

        public Configuration(int dockNo)
        {
            InitializeComponent();
            m_DockNo = dockNo;
            m_PumpBackgroundColor.Add(Color.FromRgb(0x00,0xA2,0xE8));
            m_PumpBackgroundColor.Add(Color.FromRgb(0x99,0xD9,0xEA));
            m_PumpBackgroundColor.Add(Color.FromRgb(0x70,0x92,0xBE));
            m_PumpBackgroundColor.Add(Color.FromRgb(0xC8,0xBF,0xE7));
            m_PumpBackgroundColor.Add(Color.FromRgb(0x2E,0x66,0xBA));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = string.Format("配置第{0}号货架", m_DockNo);
            System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (!(int.TryParse(config.AppSettings.Settings["DockCount"].Value, out m_DockCount)))
                m_DockCount = 12;
            int pumpCount = DockInfoManager.Instance().Get(m_DockNo);
            if (pumpCount == 30)
                this.Height = 600;
            LoadDockList();
            LoadPumpList();
            InitParameter();
            InitSelectedPumps();
        }

        private void InitParameter()
        {
            if(DockWindow.m_DockParameter.ContainsKey(m_DockNo))
            {
                AgingParameter para = DockWindow.m_DockParameter[m_DockNo] as AgingParameter;
                int index = -1;
                ComboBoxItem item = null;
                for (int i = 0; i < cmPumpType.Items.Count; i++)
                {
                    item = cmPumpType.Items[i] as ComboBoxItem;
                    //cmPumpType.Items[9]
                    if (string.Compare(item.Content.ToString(), para.PumpType, true) == 0)
                    {
                        index = i;
                        break;
                    }
                }
                if (index >= 0)
                {
                    cmPumpType.SelectedIndex = index;
                }
                for (int i = 0; i < cmOcclusionLevel.Items.Count; i++)
                {
                    item = cmOcclusionLevel.Items[i] as ComboBoxItem;
                    //cmPumpType.Items[9]
                    if (string.Compare(item.Content.ToString(), para.OclusionLevel.ToString(), true) == 0)
                    {
                        index = i;
                        break;
                    }
                }
                if (index >= 0)
                {
                    cmOcclusionLevel.SelectedIndex = index;
                }
                tbRate.Text = para.Rate.ToString();
                tbVolume.Text = para.Volume.ToString();
                tbCharge.Text = para.ChargeTime.ToString();
                //tbDischarge.Text = para.DischargeTime.ToString();
                tbRecharge.Text = para.RechargeTime.ToString();
            }
        }

        private void InitSelectedPumps()
        {
            if (DockWindow.m_DockPumpList.ContainsKey(m_DockNo))
            {
                List<Tuple<int,int,int>> pumpLocation = DockWindow.m_DockPumpList[m_DockNo] as List<Tuple<int,int,int>>;
               
                SinglePump pump = null;
                for (int i = 0; i < pumplistGrid.Children.Count; i++)
                {
                    if (pumplistGrid.Children[i] is SinglePump)
                    {
                        pump = pumplistGrid.Children[i] as SinglePump;
                        if (pump.Tag!=null 
                            && pumpLocation.FindIndex((x)=>{return x.Item1==(int)pump.Tag;})>=0)
                        {
                            pump.chNo.IsChecked = true;
                        }
                    }
                }
             
            }
        }

        private void LoadDockList()
        {
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
                otherDockCheckBoxGrid.RowDefinitions.Add(row);
            }
            for (int i = 0; i < 5; i++)
            {
                ColumnDefinition col = new ColumnDefinition();
                col.Width = new GridLength(this.Width / 5-5);//GridLength.Auto;
                otherDockCheckBoxGrid.ColumnDefinitions.Add(col);
            }

            for (int i = 0; i < m_DockCount; i++)
            {
                CheckBox box = new  CheckBox();
                box.Name = "box" + (i + 1).ToString();
                box.Tag = i + 1;
                box.Margin = new Thickness(3, 3, 3, 3);
                box.Padding = new Thickness(5,0,0,0);
                box.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                box.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                box.FontSize = 12;
                box.Content = (i + 1).ToString() + "号货架";
                if(m_DockNo==i + 1)
                {
                    box.IsChecked = true;
                    box.IsEnabled = false;
                }
                otherDockCheckBoxGrid.Children.Add(box);
                Grid.SetRow(box, i / 5);
                Grid.SetColumn(box, i % 5);
            }
        }

        private void LoadPumpList()
        {
            int pumpCount =  DockInfoManager.Instance().Get(m_DockNo);
            int pumpCountPerRow = pumpCount / 5;    //每行有几个机位
            if (pumpCount <= 0)
            {
                Logger.Instance().Info("泵数量小于等于0，请重新设置。");
                return;
            }
            int rowCount = pumpCount / 2;
            if (pumpCount % 2 > 0)
                rowCount += 1;
            for (int i = 0; i < rowCount; i++)
            {
                RowDefinition row = new RowDefinition();
                row.Height = GridLength.Auto;
                pumplistGrid.RowDefinitions.Add(row);
            }
            int iRow = 0,iCol = 0;
            for (int i = 0, rowIndex = 1; i < pumpCount; i++)
            {
                //AgingSystem:SinglePump  Grid.Row="1"  Grid.Column="1" Margin="1" Cursor="Hand" Background="Blue"/>
                iRow = i/pumpCountPerRow+1;
                iCol = (i+1)%pumpCountPerRow;
                if(iCol==0)
                    iCol = pumpCountPerRow;
                SinglePump pump = new SinglePump(m_DockNo, i+1, iRow, iCol);
                pump.Name = "pump" + (i + 1).ToString();
                pump.Tag = i + 1;
                pump.Margin = new Thickness(1, 1, 1, 1);
                pump.Cursor = Cursors.Hand;
                pump.SetPump(i + 1, "", "");

                pumplistGrid.Children.Add(pump);
                Grid.SetRow(pump, rowIndex);
                if (i % 2 == 1)
                    ++rowIndex;
                Grid.SetColumn(pump, i % 2);
                pump.Background = new SolidColorBrush(m_PumpBackgroundColor[i / pumpCountPerRow]);
            }

        }

        private void OnSelect(object sender, RoutedEventArgs e)
        {
            ParameterList parameterList = new ParameterList();
            bool? bRet = parameterList.ShowDialog();
            if(bRet.HasValue)
            {
                if(bRet==true)
                {
                    int index = -1;
                    ComboBoxItem item = null;
                    for(int i = 0;i<cmPumpType.Items.Count;i++)
                    {
                        item = cmPumpType.Items[i] as ComboBoxItem;
                        //cmPumpType.Items[9]
                        if( string.Compare(item.Content.ToString(),parameterList.Args.m_PumpType, true)==0)
                        {
                            index = i;
                            break;
                        }
                    }
                    if(index>=0)
                    {
                        cmPumpType.SelectedIndex = index;
                    }
                    for(int i = 0;i<cmOcclusionLevel.Items.Count;i++)
                    {
                        item = cmOcclusionLevel.Items[i] as ComboBoxItem;
                        //cmPumpType.Items[9]
                        if( string.Compare(item.Content.ToString(),parameterList.Args.m_OccLevel, true)==0)
                        {
                            index = i;
                            break;
                        }
                    }
                    if(index>=0)
                    {
                        cmOcclusionLevel.SelectedIndex = index;
                    }
                    tbRate.Text = parameterList.Args.m_Rate;
                    tbVolume.Text = parameterList.Args.m_Volume;
                    tbCharge.Text = parameterList.Args.m_ChargeTime;
                    //tbDischarge.Text = parameterList.Args.m_DischargeTime;
                    tbRecharge.Text = parameterList.Args.m_RechargeTime;
                }
                else
                {

                }
            }
        }

        private void OnSave(object sender, RoutedEventArgs e)
        { 
            if( string.IsNullOrEmpty(tbRate.Text.Trim())
                ||string.IsNullOrEmpty(tbVolume.Text.Trim())
                ||string.IsNullOrEmpty(tbCharge.Text.Trim())
                ||string.IsNullOrEmpty(tbRecharge.Text.Trim())
                ||cmPumpType.SelectedIndex<0
                ||cmOcclusionLevel.SelectedIndex<0
                )
            {
                MessageBox.Show("请将参数填写完整");
                return;
            }
            ComboBoxItem item      = cmPumpType.SelectedItem as ComboBoxItem;
            ComboBoxItem itemLevel = cmOcclusionLevel.SelectedItem as ComboBoxItem;
            decimal rate           = Convert.ToDecimal(float.Parse(tbRate.Text).ToString("F1"));
            decimal volume         = Convert.ToDecimal(float.Parse(tbVolume.Text).ToString("F1"));
            decimal charge         = Convert.ToDecimal(tbCharge.Text);
           
            OcclusionLevel level   = OcclusionLevel.H;
            if(Enum.IsDefined(typeof(OcclusionLevel), itemLevel.Content.ToString()))
                level = (OcclusionLevel)Enum.Parse(typeof(OcclusionLevel), itemLevel.Content.ToString());
            else
                Logger.Instance().ErrorFormat("Configuration::OnSave()->压力转换出错,OcclusionLevel={0}", itemLevel.Content.ToString());
            decimal discharge = 0;
            decimal recharge  = Convert.ToDecimal(tbRecharge.Text);

            Hashtable dockParameter = new Hashtable();
            AgingParameter para = new AgingParameter(item.Content.ToString(),
                                                     rate,
                                                     volume,
                                                     charge,
                                                     discharge,
                                                     recharge,
                                                     level);
            for(int i=0;i<otherDockCheckBoxGrid.Children.Count;i++)
            {
                if(otherDockCheckBoxGrid.Children[i] is CheckBox)
                {
                    CheckBox box = otherDockCheckBoxGrid.Children[i] as CheckBox;
                    if(box.IsChecked.HasValue)
                    {
                        if(box.IsChecked==true)
                        {
                           dockParameter.Add((int)box.Tag, new AgingParameter(para));    //这里需要拷贝一份新的参数
                        }
                    }
                }
            }
            if(OnSaveConfigration!=null)
                OnSaveConfigration(this, new ConfigrationArgs(dockParameter));
            this.DialogResult = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SinglePump pump = null;
            List<Tuple<int,int,int>> pumpLocationList = new List<Tuple<int,int,int>>();
            for (int i = 0; i < pumplistGrid.Children.Count; i++)
            {
                if (pumplistGrid.Children[i] is SinglePump)
                {
                    pump = pumplistGrid.Children[i] as SinglePump;
                    if (pump.chNo.IsChecked.HasValue && pump.chNo.IsChecked==true)
                        pumpLocationList.Add(new Tuple<int,int,int>((int)pump.Tag, pump.RowNo, pump.ColNo));
                }
            }
            Hashtable hashPumps = new Hashtable();
            hashPumps.Add(m_DockNo, pumpLocationList);
            if(OnSelectedPumps!=null)
            {
                OnSelectedPumps(this, new SelectedPumpsArgs(hashPumps));
            }
            GC.Collect();
        }

        /// <summary>
        /// 单击选择全部或反选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectAllPump(object sender, RoutedEventArgs e)
        {
            SinglePump pump = null;
            for(int i=0;i<pumplistGrid.Children.Count; i++)
            {
                if(pumplistGrid.Children[i] is SinglePump)
                {
                    pump = pumplistGrid.Children[i] as SinglePump;
                    pump.chNo.IsChecked = this.chNo.IsChecked;
                }
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SinglePump pump = null;
            for (int i = 0; i < pumplistGrid.Children.Count; i++)
            {
                if (pumplistGrid.Children[i] is SinglePump)
                {
                    pump = pumplistGrid.Children[i] as SinglePump;
                    pump.lbPumpType.Content = ((ComboBoxItem)(cmPumpType.SelectedItem)).Content.ToString();
                }
            }
        }

        private void OnOcclusionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

       
        private void TextBoxChanged(object sender, TextChangedEventArgs e, float max)
        {
            TextBox obj = sender as TextBox;
            InputValidation.ValidateTextBox(obj, max, 1);
            try
            {
                if (Convert.ToSingle(obj.Text.Trim('.')) > max)
                {
                    obj.Text = string.Empty;
                }
            }
            catch
            {
                obj.Text = string.Empty;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            Key key = Key.D0;
            while (key <= Key.D9)
            {
                if (e.KeyboardDevice.IsKeyDown(key))
                {
                    e.Handled = false;
                    return;
                }
                key += 1;
            }
            key = Key.NumPad0;
            while (key <= Key.NumPad9)
            {
                if (e.KeyboardDevice.IsKeyDown(key))
                {
                    e.Handled = false;
                    return;
                }
                key += 1;
            }
            if (e.KeyboardDevice.IsKeyDown(Key.OemPeriod) || e.KeyboardDevice.IsKeyDown(Key.Decimal))
            {
                e.Handled = false;
                return;
            }
            e.Handled = true;
        }

        private void tbRate_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(sender, e);
        }

        private void tbVolume_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(sender, e);
        }

        private void tbVolume_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxChanged(sender, e, 9999f);
        }

        private void tbCharge_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxChanged(sender, e, 24);
        }

        private void tbCharge_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(sender, e);
        }

        private void tbRecharge_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(sender, e);
        }

        private void tbRecharge_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxChanged(sender, e, 20);
        }

        private void tbRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxChanged(sender, e, 1200.0f);
        }
        
      
    }
}
