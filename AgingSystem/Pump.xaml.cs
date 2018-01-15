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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using log4net;

namespace  AgingSystem
{
    /// <summary>
    /// Interaction logic for Pump.xaml
    /// </summary>
    public partial class Pump : UserControl
    {
        public Pump()
        {
            InitializeComponent();
            InitPumpImage();
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

        private void InitPumpImage()
        {
            pumpImg.Source = BitmapToBitmapImage(Properties.Resources.pump);
            selectedImg.Source = BitmapToBitmapImage(Properties.Resources.selected);
        }

        public void EnableControls(bool bEnabled = true)
        {
            btnConfig.IsEnabled = bEnabled;
            btnStart.IsEnabled = bEnabled;
            btnStop.IsEnabled = bEnabled;
        }

        private void pumpImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedImg.Visibility == System.Windows.Visibility.Visible)
                selectedImg.Visibility = System.Windows.Visibility.Hidden;
            else if(selectedImg.Visibility == System.Windows.Visibility.Hidden)
                selectedImg.Visibility = System.Windows.Visibility.Visible;
            if(selectedImg.Visibility == System.Windows.Visibility.Visible)
                btnConfig.IsEnabled = true;
            else
                btnConfig.IsEnabled = false;
        }

        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            Configuration cfg = new Configuration();
            cfg.ShowDialog();
        }
        
    }
}
