using System.Windows.Controls;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for FilesListView.xaml
    /// </summary>
    public partial class FilesListView : UserControl
    {
        public FilesListView()
        {
            InitializeComponent();
            this.Loaded += FilesListView_Loaded;
        }

        private void FilesListView_Loaded(object sender, System.Windows.RoutedEventArgs e)
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
