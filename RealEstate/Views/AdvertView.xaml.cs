using System.Windows;

namespace RealEstate.Views
{
    /// <summary>
    /// Interaction logic for AdvertView.xaml
    /// </summary>
    public partial class AdvertView : Window
    {
        public AdvertView()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            webControl.Dispose();
        }
    }
}
