using System.Windows.Threading;
using RealEstate.Modes;
using System;
using System.Windows;
using System.IO;
using Hardcodet.Wpf.TaskbarNotification;

namespace RealEstate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ModeManager.SetMode(e.Args);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText("CurrentDomain log.txt", e.ExceptionObject.ToString());
        }

        void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            File.WriteAllText("Dispatcher log.txt", e.Exception.ToString());
        }

        public static TaskbarIcon NotifyIcon;
    }
}
