using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for XYMotionControlView.xaml
    /// </summary>
    public partial class XYMotionControlView : UserControl
    {
        private Jog _jog;

        public XYMotionControlView()
        {
            InitializeComponent();
            this.Loaded += XYMotionControlView_Load;

        }

        private void XYMotionControlView_Load(object sender, RoutedEventArgs e)
        {
            _jog = Jog.Instance;
        }

        // Rz
        private async void RzPositive_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogRzPositive();
            await Task.Delay(2000);
            _jog.StopJog();

        }

        private async void RzNegative_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogRzNegative();
            await Task.Delay(2000);
            _jog.StopJog();

        }

        // Ry
        private async void RyPositive_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogRyPositive();
            await Task.Delay(2000);
            _jog.StopJog();

        }

        private async void RyNegative_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogRyNegative();
            await Task.Delay(2000);
            _jog.StopJog();
        }

        // Rx
        private async void RxPositive_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogRxPositive();
            await Task.Delay(2000);
            _jog.StopJog();
        }
        private async void RxNegative_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogRxNegative();
            await Task.Delay(2000);
            _jog.StopJog();
        }

        // Z
        private async void ZPositive_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogZPositive();
            await Task.Delay(2000);
            _jog.StopJog();
        }

        private async void ZNegative_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogZNegative();
            await Task.Delay(2000);
            _jog.StopJog();
        }

        // Y
        private async void YPositive_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogYPositive();
            await Task.Delay(2000);
            _jog.StopJog();
        }

        private async void YNegative_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogYNegative();
            await Task.Delay(2000);
            _jog.StopJog();
        }

        // X
        private async void XPositive_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogXPositive();
            await Task.Delay(2000);
            _jog.StopJog();
        }
        private async void XNegative_Click(object sender, RoutedEventArgs e)
        {
            _jog.JogXNegative();
            await Task.Delay(2000);
            _jog.StopJog();
        }

    }
}
