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
using System.Reflection;
using System.IO;
using Cmd;
using Analyse;

namespace  AgingSystem
{
    /// <summary>
    /// Interaction logic for Parameter.xaml
    /// </summary>
    public partial class ParameterList : Window
    {
        private ParameterArgs m_Args = null;
        /// <summary>
        /// 由列表传入的参数
        /// </summary>
        public ParameterArgs Args
        {
            get { return m_Args;}
        }


        public ParameterList()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
        }

        public void Init()
        {
            string executePath = Assembly.GetEntryAssembly().Location;
            int index = executePath.LastIndexOf('\\');
            string path = executePath.Substring(0, index + 1) + "Config\\Parameter.txt";
            LoadParameter(path);
            FillParameterList();
        }

        private bool LoadParameter(string path)
        {
            ParameterManager.Instance().Clear();
            if (!File.Exists(path))
            {
                Logger.Instance().ErrorFormat("老化参数配置文件不存在 path={0}", path);
                return false;
            }
            try
            {
                decimal[] outValue = new decimal[5];
                string factorString = string.Empty;
                char[] separatorLine = new char[2] { (char)0x0D, (char)0x0A };
                char[] separator = new char[2] { '\t', ' ' };
                StreamReader reader = new StreamReader(path);
                if (reader != null)
                {
                    factorString = reader.ReadToEnd();
                    reader.Close();
                    string[] factors = factorString.Split(separatorLine, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in factors)
                    {
                        #region
                        string[] factor = s.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        OcclusionLevel level = OcclusionLevel.H;
                        if (factor.Length != 7)
                            continue;
                        if (decimal.TryParse(factor[1],    out outValue[0])
                            && decimal.TryParse(factor[2], out outValue[1])
                            && decimal.TryParse(factor[3], out outValue[2])
                            && decimal.TryParse(factor[4], out outValue[3])
                            && decimal.TryParse(factor[5], out outValue[4])
                            )
                        {
                            if(Enum.IsDefined(typeof(OcclusionLevel), factor[6]))
                                level = (OcclusionLevel)Enum.Parse(typeof(OcclusionLevel), factor[6]);
                            else
                                break;
                            //如果转换成功，就新建一个DefaultParameter对象
                            ParameterManager.Instance().Add(new AgingParameter(factor[0], outValue[0], outValue[1], outValue[2], outValue[3], outValue[4], level));
                            continue;
                        }
                        #endregion
                    }
                }
                else
                {
                    Logger.Instance().Error("LoadParameter()读文件错误");
                }
            }
            catch (Exception e)
            {
                Logger.Instance().ErrorFormat("LoadParameter()读文件错误,Message={0}", e.Message);
            }
            return true;
        }

        private void FillParameterList()
        {
            List<AgingParameter> paraList = ParameterManager.Instance().ParaList;
            for(int iLoop=0; iLoop < paraList.Count; iLoop++)
            {
                Parameter para = new Parameter();
                para.Name = paraList[iLoop].PumpType;
                para.SetParameter((iLoop+1).ToString(), 
                    paraList[iLoop].PumpType, 
                    paraList[iLoop].Rate.ToString(), 
                    paraList[iLoop].Volume.ToString(),
                    paraList[iLoop].ChargeTime.ToString(),
                    //paraList[iLoop].DischargeTime.ToString(),
                    paraList[iLoop].OclusionLevel.ToString(),
                    paraList[iLoop].RechargeTime.ToString());
                para.Margin = new Thickness(1);
                para.Cursor = Cursors.Hand;
                para.OnSelected += OnSelected;
                if(iLoop%2==0)
                    para.Background = new SolidColorBrush(Color.FromArgb(0xFF,0x00,0xA2,0xE8));
                parameterListGrid.Children.Add(para);
                Grid.SetRow(para, iLoop+1);
                Grid.SetColumn(para, 0);
            }
        }

        private void OnSelected(object sender, ParameterArgs e)
        {
            m_Args = e;
            try 
            {
                Parameter para = null;
                for (int iLoop = 0; iLoop < parameterListGrid.Children.Count; iLoop++)
                {
                    if (parameterListGrid.Children[iLoop] is Parameter)
                    {
                        para = parameterListGrid.Children[iLoop] as Parameter;
                        if (para.IsChecked && para.PumpType != e.m_PumpType)
                        {
                            para.SetCheckedStatus(false);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
               MessageBox.Show(ex.Message);
            }
            
        }

        private void OnSelect(object sender, RoutedEventArgs e)
        {
            if(m_Args!=null)
                this.DialogResult = true;
            else
                MessageBox.Show("请选择一项老化配置参数！");
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

      
    }
}
