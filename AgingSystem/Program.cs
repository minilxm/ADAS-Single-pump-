using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace AgingSystem
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AgingSystem.App app = new AgingSystem.App();
            app.InitializeComponent();
            app.Run();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("错误，CurrentDomain未捕捉到的异常!");
            LogUnhandledException(e.ExceptionObject);

        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("错误，Application未捕捉到的异常!");
            LogUnhandledException(e.Exception);
        }

        static void LogUnhandledException(object exceptionobj)
        {
            //Log the exception here or report it to developer  
            Exception e = ((Exception)exceptionobj);
            MessageBox.Show(e.Message + "\nStackTrace=>" + e.StackTrace);

        }
    }
}
