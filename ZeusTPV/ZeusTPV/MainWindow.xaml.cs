using System.Windows;

namespace ZeusTPV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Robot rb = new Robot("192.168.0.23", 12345);
        public MainWindow()
        {
            InitializeComponent();
            rb.Open();
            rb.AcqPermission();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //if rb.jntmove(45, 0, 0, 0, 0, 0, 5, 1.0, 1.0)[0] != True:
            var result = rb.JntMove(5, 0, 0, 0, 0, 0, 5, 1.0, 1.0);
        }
    }
}
