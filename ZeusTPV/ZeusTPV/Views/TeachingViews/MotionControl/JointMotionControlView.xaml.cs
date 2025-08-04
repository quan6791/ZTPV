using System.Windows;
using System.Windows.Controls;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for JointMotionControlView.xaml
    /// </summary>
    public partial class JointMotionControlView : UserControl
    {

        private Jog _jog;
        public JointMotionControlView()
        {
            InitializeComponent();
            this.Loaded += JointMotionControlView_Load;
        }

        private void JointMotionControlView_Load(object sender, RoutedEventArgs e)
        {
            _jog = Jog.Instance;
        }

        private void J6PlusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ6Positive();
        }

        private void J6MinusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ6Negative();
        }

        private void J5MinusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ5Negative();
        }

        private void J5PlusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ5Positive();
        }

        private void J4MinusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ4Negative();
        }

        private void J4PlusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ4Positive();
        }

        private void J3MinusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ3Negative();
        }

        private void J3PlusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ3Positive();
        }

        private void J2MinusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ2Negative();
        }

        private void J2PlusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ2Positive();
        }

        private void J1MinusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ1Negative();
        }

        private void J1PlusBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _jog.JogJ1Positive();
        }
    }
}
