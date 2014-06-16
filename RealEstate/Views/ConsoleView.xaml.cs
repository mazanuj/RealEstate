using System.Windows.Controls;

namespace RealEstate.Views
{
    /// <summary>
    /// Interaction logic for ConsoleView.xaml
    /// </summary>
    public partial class ConsoleView : UserControl
    {
        public ConsoleView()
        {
            InitializeComponent();
        }

        private void ConsoleText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConsoleText.ScrollToEnd();
        }
    }
}
