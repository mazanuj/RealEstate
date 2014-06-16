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

namespace RealEstate.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Loader.IsFormLoaded = true;

            App.NotifyIcon = MyNotifyIcon;
        }

        Point m_start;
        Vector m_startOffset;

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            m_start = e.GetPosition(mainWindow);
            m_startOffset = new Vector(dragTransform.X, dragTransform.Y);
            ConsoleWindow.CaptureMouse();
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (ConsoleWindow.IsMouseCaptured)
            {
                Vector offset = Point.Subtract(e.GetPosition(mainWindow), m_start);

                dragTransform.X = m_startOffset.X + offset.X;
                dragTransform.Y = m_startOffset.Y + offset.Y;
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ConsoleWindow.ReleaseMouseCapture();
        }
    }

    public static class Loader
    {
        public static bool IsFormLoaded = false;
    }
}
