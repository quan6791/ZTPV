using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for PathCheckMotionControlView.xaml
    /// </summary>
    public partial class PathCheckMotionControlView : UserControl
    {
        public ObservableCollection<PathDataItem> PathData { get; set; }

        public PathCheckMotionControlView()
        {
            InitializeComponent();
            InitializeData();
            dgPathData.ItemsSource = PathData;

        }


        private void InitializeData()
        {
            PathData = new ObservableCollection<PathDataItem>();

            // Add sample data
            PathData.Add(new PathDataItem { Name = "P1", Type = "PTP", Path = "Joint", XJ1 = "0.0", YJ2 = "0.0", ZJ3 = "0.0" });
            PathData.Add(new PathDataItem { Name = "P2", Type = "Linear", Path = "XYZ", XJ1 = "100.0", YJ2 = "50.0", ZJ3 = "200.0" });
            PathData.Add(new PathDataItem { Name = "P3", Type = "PTP", Path = "Joint", XJ1 = "45.0", YJ2 = "-30.0", ZJ3 = "90.0" });
        }
    }

    // Data model for path items
    public class PathDataItem : INotifyPropertyChanged
    {
        private string _name;
        private string _type;
        private string _path;
        private string _xj1;
        private string _yj2;
        private string _zj3;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(nameof(Path)); }
        }

        public string XJ1
        {
            get => _xj1;
            set { _xj1 = value; OnPropertyChanged(nameof(XJ1)); }
        }

        public string YJ2
        {
            get => _yj2;
            set { _yj2 = value; OnPropertyChanged(nameof(YJ2)); }
        }

        public string ZJ3
        {
            get => _zj3;
            set { _zj3 = value; OnPropertyChanged(nameof(ZJ3)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
