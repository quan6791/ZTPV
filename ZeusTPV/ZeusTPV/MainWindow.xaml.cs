using System.Windows;
using ZeusTPV.Views;

namespace ZeusTPV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TeachingView _teachingView;
        private HomeView _homeView;
        public MainWindow()
        {
            InitializeComponent();
            _teachingView = new TeachingView();
            MainContent.Content = _teachingView;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_homeView == null)
            {
                _homeView = new HomeView();
            }
            MainContent.Content = _homeView;
        }

        private void TeachingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_teachingView == null)
            {
                _teachingView = new TeachingView();
            }
            MainContent.Content = _teachingView;

        }
    }
}