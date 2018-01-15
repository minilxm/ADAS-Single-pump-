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
using Analyse;

namespace  AgingSystem
{
    /// <summary>
    /// Interaction logic for PumpList.xaml
    /// </summary>
    public partial class PumpList : Window
    {
        private int m_DockNo = 0;       //货架编号从1开始增加
      
        public PumpList()
        {
            InitializeComponent();
        }

        public PumpList(int dockNo)
        {
            InitializeComponent();
            m_DockNo = dockNo;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(m_DockNo<=0)
            {
                MessageBox.Show("货架信息错误！");
                return;
            }
            int count = DockInfoManager.Instance().Get(m_DockNo);
        }




    }
}
