using System.Threading.Tasks;
using System.Windows;

namespace ZeusTPV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //private const string IP_ADDR = "192.168.0.23";
        //private const int PORT = 12345;
        //private Robot rb;
        private Jog jog;
        public MainWindow()
        {
            InitializeComponent();

            //rb = new Robot(IP_ADDR, PORT);
            //Console.WriteLine(rb.Open());

            //var ret = rb.AcqPermission();
            //while (!(bool)ret[0]) // Cast the object to bool
            //{
            //    Console.WriteLine("fail to acquire permission!");
            //    ret = rb.AcqPermission();
            //    Thread.Sleep(1000);
            //}
            //Console.WriteLine("Success Acquire Permission");


            //var curpos = rb.MarkMT();
            //Console.WriteLine($"curpos : {curpos}");
            jog = new Jog();
            this.Closed += (s, e) =>
            {
                // Dispose of the jog object when the window is closed
                jog.Dispose();
            };
        }

        //private async void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    //var curpos = rb.MarkMT();
        //    //Console.WriteLine($"curpos0 : {curpos[0]}");
        //    //Console.WriteLine($"curpos0 : {curpos[1]}");

        //    //// rb.JntMove(9, 0, 0, 0, 0, 0, 10, 0.4, 0.4);
        //    //// rb.CpMove(0, 0, -10, 0, 0, 0, 0, 0, 10, 0.4, 0.4);
        //    //rb.CpMove((double)curpos[1], (double)curpos[2], (double)curpos[3] + 2, (double)curpos[4],
        //    //         (double)curpos[5], (double)curpos[6], 6, 1, 10, 0.4, 0.4);

        //    jog.JogZNegative();
        //    await Task.Delay(2000);
        //    jog.StopJog();
        //    jog.GetPosition();
        //}

        // Rz
        private async void RzPositive_Click(object sender, RoutedEventArgs e)
        {
            jog.JogRzPositive();
            await Task.Delay(2000);
            jog.StopJog();

        }

        private async void RzNegative_Click(object sender, RoutedEventArgs e)
        {
            jog.JogRzNegative();
            await Task.Delay(2000);
            jog.StopJog();

        }

        // Ry
        private async void RyPositive_Click(object sender, RoutedEventArgs e)
        {
            jog.JogRyPositive();
            await Task.Delay(2000);
            jog.StopJog();

        }

        private async void RyNegative_Click(object sender, RoutedEventArgs e)
        {
            jog.JogRyNegative();
            await Task.Delay(2000);
            jog.StopJog();
        }

        // Rx
        private async void RxPositive_Click(object sender, RoutedEventArgs e)
        {
            jog.JogRxPositive();
            await Task.Delay(2000);
            jog.StopJog();
        }
        private async void RxNegative_Click(object sender, RoutedEventArgs e)
        {
            jog.JogRxNegative();
            await Task.Delay(2000);
            jog.StopJog();
        }

        // Z
        private async void ZPositive_Click(object sender, RoutedEventArgs e)
        {
            jog.JogZPositive();
            await Task.Delay(2000);
            jog.StopJog();
        }

        private async void ZNegative_Click(object sender, RoutedEventArgs e)
        {
            jog.JogZNegative();
            await Task.Delay(2000);
            jog.StopJog();
        }

        // Y
        private async void YPositive_Click(object sender, RoutedEventArgs e)
        {
            jog.JogYPositive();
            await Task.Delay(2000);
            jog.StopJog();
        }

        private async void YNegative_Click(object sender, RoutedEventArgs e)
        {
            jog.JogYNegative();
            await Task.Delay(2000);
            jog.StopJog();
        }

        // X
        private async void XPositive_Click(object sender, RoutedEventArgs e)
        {
            jog.JogXPositive();
            await Task.Delay(2000);
            jog.StopJog();
        }
        private async void XNegative_Click(object sender, RoutedEventArgs e)
        {
            jog.JogXNegative();
            await Task.Delay(2000);
            jog.StopJog();
        }

    }
}