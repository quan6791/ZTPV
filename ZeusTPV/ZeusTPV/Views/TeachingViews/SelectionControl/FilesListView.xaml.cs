using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for FilesListView.xaml
    /// </summary>
    public partial class FilesListView : UserControl
    {
        public event EventHandler<string> FileSelected;
        public FilesListView()
        {
            InitializeComponent();
            this.Loaded += FilesListView_Loaded;
        }

        private void FilesListView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

            if (!Constants._isSimulation)
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

        private void lbx_Files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lbx_Files.SelectedItem is string selectedItem)
            {
                string selectedFile = selectedItem.ToString();

                // Raise event để parent control biết
                FileSelected?.Invoke(this, selectedFile);
            }
        }
    }
}
