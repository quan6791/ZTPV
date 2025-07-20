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


            //var rb = new Robot("192.168.0.23", 12345);
            //rb.Open();
            //rb.AcqPermission();

            //while (true)
            //{
            //    var result1 = rb.JntMove(45, 0, 0, 0, 0, 0, 5, 1.0, 1.0);
            //    if (result1[0].ToString() != "True")
            //    {
            //        Environment.Exit(1);
            //    }
            //    System.Threading.Thread.Sleep(1000);

            //    var result2 = rb.JntMove(-45, 0, 0, 0, 0, 0, 5, 1.0, 1.0);
            //    if (result2[0].ToString() != "True")
            //    {
            //        Environment.Exit(1);
            //    }
            //    System.Threading.Thread.Sleep(1000);
            //}

            //rb.RelPermission();
            //rb.Close();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //if rb.jntmove(45, 0, 0, 0, 0, 0, 5, 1.0, 1.0)[0] != True:
            //var result = rb.JntMove(5, 0, 0, 0, 0, 0, 5, 1.0, 1.0);
        }
    }
}
