using System.Windows.Controls;
using System.Windows.Media;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for MotionControlView.xaml
    /// </summary>
    /// 

    public partial class MotionControlView : UserControl
    {

        private XYMotionControlView _xYMotionControlView;
        private JointMotionControlView _jointMotionControlView;
        private MoveToMotionControlView _moveToMotionControlView;
        private PathCheckMotionControlView _pathCheckMotionControlView;


        // Define colors
        private readonly SolidColorBrush ActiveColor = new SolidColorBrush(Color.FromRgb(66, 133, 244)); // #4285F4
        private readonly SolidColorBrush InactiveColor = new SolidColorBrush(Color.FromRgb(230, 230, 230)); // #E6E6E6
        private readonly SolidColorBrush ActiveTextColor = Brushes.White;
        private readonly SolidColorBrush InactiveTextColor = new SolidColorBrush(Color.FromRgb(136, 136, 136)); // #888888


        public MotionControlView()
        {
            InitializeComponent();

            this.MotionControlContent.Content = _xYMotionControlView ?? new XYMotionControlView();
            SetActiveButton(btnXY);

        }

        private void XYButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.MotionControlContent.Content = _xYMotionControlView ?? new XYMotionControlView();
            SetActiveButton(btnXY);

        }

        private void JointButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.MotionControlContent.Content = _jointMotionControlView ?? new JointMotionControlView();
            SetActiveButton(btnJoint);

        }

        private void MoveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.MotionControlContent.Content = _moveToMotionControlView ?? new MoveToMotionControlView();
            SetActiveButton(btnMoveTo);

        }

        private void PathCheck_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.MotionControlContent.Content = _pathCheckMotionControlView ?? new PathCheckMotionControlView();
            SetActiveButton(btnPathCheck);

        }

        private void SetActiveButton(Button activeButton)
        {
            // Reset all buttons to inactive state
            ResetAllButtons();

            // Set active button style
            activeButton.Background = ActiveColor;
            activeButton.Foreground = ActiveTextColor;
        }

        private void ResetAllButtons()
        {
            // Set all buttons to inactive state
            btnXY.Background = InactiveColor;
            btnXY.Foreground = InactiveTextColor;

            btnJoint.Background = InactiveColor;
            btnJoint.Foreground = InactiveTextColor;

            btnMoveTo.Background = InactiveColor;
            btnMoveTo.Foreground = InactiveTextColor;

            btnPathCheck.Background = InactiveColor;
            btnPathCheck.Foreground = InactiveTextColor;
        }
    }
}
