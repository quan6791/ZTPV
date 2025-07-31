using System.Windows;
using System.Windows.Controls;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for SelectionControlView.xaml
    /// </summary>
    public partial class SelectionControlView : UserControl
    {
        public SelectionControlView()
        {
            InitializeComponent();
            this.Loaded += SelectionControlView_Loaded;
        }

        private void SelectionControlView_Loaded(object sender, RoutedEventArgs e)
        {
            ZeroUser.Instance?.Connect();
            if (this.lbx_Files != null)
            {
                this.lbx_Files.ItemsSource = null;
            }
            this.lbx_Files.Items.Clear();
            this.lbx_Files.ItemsSource = ZeroUser.Instance?.FilesList;
        }
    }
}
