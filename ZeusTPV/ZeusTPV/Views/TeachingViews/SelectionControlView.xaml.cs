using System.ComponentModel;
using System.Windows.Controls;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for SelectionControlView.xaml
    /// </summary>
    public partial class SelectionControlView : UserControl
    {
        private FilesListView _filesListView;
        public SelectionControlView()
        {
            InitializeComponent();

            // Chỉ load content khi không phải design time
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                LoadContent();
            }
        }

        private void LoadContent()
        {
            this.SelectionControlContent.Content = _filesListView ?? new FilesListView();
        }
    }
}
