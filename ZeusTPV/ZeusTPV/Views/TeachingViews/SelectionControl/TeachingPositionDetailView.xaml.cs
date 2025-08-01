using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for TeachingPositionDetailView.xaml
    /// </summary>
    public partial class TeachingPositionDetailView : UserControl
    {
        public TeachingPositionDetailView()
        {
            InitializeComponent();
            this.DataContext = new PositionDetailViewModel();

        }

        public void LoadPositionData(TeachDataRecord selectedRecord)
        {
            if (this.DataContext is PositionDetailViewModel viewModel)
            {
                viewModel.LoadFromTeachDataRecord(selectedRecord);
            }
        }
    }

    public class PositionDetailViewModel : INotifyPropertyChanged
    {
        private PositionData _jntPosition;
        private PositionData _currentPosition;

        public PositionDetailViewModel()
        {
            // Initialize with sample data
            JntPosition = new PositionData
            {
                X = double.NaN,
                Y = double.NaN,
                Z = double.NaN,
                Rz = double.NaN,
                Ry = double.NaN,
                Rx = double.NaN,
                Posture = 0,
                CC = "0x000000"
            };

            CurrentPosition = new PositionData
            {
                X = 180.552,
                Y = 180.552,
                Z = 180.552,
                Rz = 180.552,
                Ry = 180.552,
                Rx = 180.552,
                Posture = 7,
                CC = "0x000000"
            };
        }

        public PositionData JntPosition
        {
            get => _jntPosition;
            set
            {
                _jntPosition = value;
                OnPropertyChanged();
            }
        }

        public PositionData CurrentPosition
        {
            get => _currentPosition;
            set
            {
                _currentPosition = value;
                OnPropertyChanged();
            }
        }

        public void LoadFromTeachDataRecord(TeachDataRecord record)
        {
            if (record != null)
            {
                JntPosition = new PositionData
                {
                    X = (double)record.PosX,
                    Y = (double)record.PosY,
                    Z = (double)record.PosZ,
                    Rz = (double)record.PosRz,
                    Ry = (double)record.PosRy,
                    Rx = (double)record.PosRx,
                    Posture = (int)record.PosPosture,
                    CC = "0x000000"
                };
            }
        }

        public void UpdateCurrentPosition(double[] position)
        {
            if (position != null && position.Length >= 6)
            {
                CurrentPosition = new PositionData
                {
                    X = position[0],
                    Y = position[1],
                    Z = position[2],
                    Rz = position[3],
                    Ry = position[4],
                    Rx = position[5],
                    Posture = 7,
                    CC = "0x000000"
                };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PositionData : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private double _z;
        private double _rz;
        private double _ry;
        private double _rx;
        private int _posture;
        private string _cc;

        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
            }
        }

        public double Z
        {
            get => _z;
            set
            {
                _z = value;
                OnPropertyChanged();
            }
        }

        public double Rz
        {
            get => _rz;
            set
            {
                _rz = value;
                OnPropertyChanged();
            }
        }

        public double Ry
        {
            get => _ry;
            set
            {
                _ry = value;
                OnPropertyChanged();
            }
        }

        public double Rx
        {
            get => _rx;
            set
            {
                _rx = value;
                OnPropertyChanged();
            }
        }

        public int Posture
        {
            get => _posture;
            set
            {
                _posture = value;
                OnPropertyChanged();
            }
        }

        public string CC
        {
            get => _cc;
            set
            {
                _cc = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
