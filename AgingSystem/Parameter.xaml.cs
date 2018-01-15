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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Analyse;

namespace  AgingSystem
{
    /// <summary>
    /// Interaction logic for Parameter.xaml
    /// </summary>
    public partial class Parameter : UserControl
    {
        public event EventHandler<ParameterArgs> OnSelected;

        public string PumpType
        {
            get { return lbPumpType.Content.ToString();}
        }

        public bool IsChecked
        {
            get { 
                if(chNo.IsChecked.HasValue)
                    return (bool)chNo.IsChecked; 
                else
                    return false;
            }
        }

        public Parameter()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 填充参数
        /// </summary>
        /// <param name="strNo"></param>
        /// <param name="strPumpType"></param>
        /// <param name="strRate"></param>
        /// <param name="strVolume"></param>
        /// <param name="strCharge"></param>
        /// <param name="strDischarge"></param>
        /// <param name="strRecharge"></param>
        public void SetParameter(string strNo,
                                 string strPumpType,
                                 string strRate,
                                 string strVolume,
                                 string strCharge,
                                 string strLevel,
                                 string strRecharge)
        {
            chNo.Content        = strNo;
            lbPumpType.Content  = strPumpType;
            lbRate.Content      = strRate;    
            lbVolume.Content    = strVolume; 
            lbCharge.Content    = strCharge; 
            lbOcclusionLevel.Content = strLevel;
            lbRecharge.Content  = strRecharge;
        }

        public void SetCheckedStatus(bool bChecked = true)
        {
            chNo.IsChecked = bChecked;
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            if(e.OriginalSource is CheckBox)
            {
                CheckBox ch = e.OriginalSource as CheckBox;
                if(ch.IsChecked==true)
                {
                    if(OnSelected!=null)
                    {
                        OnSelected(this, new ParameterArgs(lbPumpType.Content.ToString(),
                                                            lbRate.Content.ToString(),
                                                            lbVolume.Content.ToString(),
                                                            lbCharge.Content.ToString(),
                                                            lbRecharge.Content.ToString(),
                                                            lbOcclusionLevel.Content.ToString()
                                                            )
                                   );
                    }
                }
            }
        }

         
    }
}
