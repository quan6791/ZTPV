using System;
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
        private TeachingDataView _teachingDataView;
        private TeachingPositionDetailView _teachingPositionDetailView;
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
            //this.SelectionControlContent.Content = _filesListView ?? new FilesListView();
            // this.SelectionControlContent.Content = _teachingDataView ?? new TeachingDataView();
            try
            {
                _filesListView = new FilesListView();
                _filesListView.FileSelected += OnFileSelected;
                this.SelectionControlContent.Content = _filesListView;
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during initialization
                System.Diagnostics.Debug.WriteLine($"Error initializing FilesListView: {ex.Message}");
            }
        }

        private void OnFileSelected(object sender, string selectedFile)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"File selected: {selectedFile}");

                // Create TeachingDataView with selected file
                // _teachingDataView = new TeachingDataView(selectedFile);
                _teachingDataView = new TeachingDataView();
                _teachingDataView.PositionSelected += OnPositionSelected;

                // Switch content to TeachingDataView
                this.SelectionControlContent.Content = _teachingDataView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error switching to TeachingDataView: {ex.Message}");
            }
        }

        private void OnPositionSelected(object sender, int e)
        {
            this.SelectionControlContent.Content = _teachingPositionDetailView ?? new TeachingPositionDetailView();
        }

        private void FileSelectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.SelectionControlContent.Content = _filesListView ?? new FilesListView();

        }

        private void PosSelectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.SelectionControlContent.Content = _teachingDataView ?? new TeachingDataView();

        }
    }
}
