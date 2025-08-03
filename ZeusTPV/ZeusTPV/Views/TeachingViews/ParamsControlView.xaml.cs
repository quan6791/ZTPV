using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for ParamsControlView.xaml
    /// </summary>
    public partial class ParamsControlView : UserControl
    {
        private bool isSpeedMode = false;
        private double speedValue = 50;
        private double pitchValue = 40;
        public ParamsControlView()
        {
            InitializeComponent();
            InitializeDefaultValues();

        }

        private void InitializeDefaultValues()
        {
            // Set default to Pitch mode
            SetPitchMode();
        }

        private void btnSpeed_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Speed button clicked");
            SetSpeedMode();
        }

        private void btnPitch_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pitch button clicked");
            SetPitchMode();
        }

        private void SetSpeedMode()
        {
            isSpeedMode = true;

            // Update button styles
            btnSpeed.Background = new SolidColorBrush(Color.FromRgb(66, 133, 244)); // #4285F4
            btnSpeed.Foreground = Brushes.White;
            btnPitch.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)); // #E6E6E6
            btnPitch.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)); // #888888

            // Save current pitch value and load speed value
            pitchValue = sliderValue.Value;
            sliderValue.Value = speedValue;
            txtValue.Text = speedValue.ToString("F0");

            Debug.WriteLine($"Speed mode activated - Value: {speedValue}");
        }

        private void SetPitchMode()
        {
            isSpeedMode = false;

            // Update button styles
            btnPitch.Background = new SolidColorBrush(Color.FromRgb(66, 133, 244)); // #4285F4
            btnPitch.Foreground = Brushes.White;
            btnSpeed.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)); // #E6E6E6
            btnSpeed.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)); // #888888

            // Save current speed value and load pitch value
            speedValue = sliderValue.Value;
            sliderValue.Value = pitchValue;
            txtValue.Text = pitchValue.ToString("F0");

            Debug.WriteLine($"Pitch mode activated - Value: {pitchValue}");
        }

        private void sliderValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtValue != null)
            {
                double value = Math.Round(e.NewValue);
                txtValue.Text = value.ToString("F0");

                // Save value to appropriate variable
                if (isSpeedMode)
                {
                    speedValue = value;
                    Debug.WriteLine($"Speed value changed: {speedValue}");

                    // Send speed command to server
                    //_ = SendParameterToServer("SPEED", speedValue);
                }
                else
                {
                    pitchValue = value;
                    Debug.WriteLine($"Pitch value changed: {pitchValue}");

                    // Send pitch command to server
                    //_ = SendParameterToServer("PITCH", pitchValue);
                }
            }
        }

        //private async System.Threading.Tasks.Task SendParameterToServer(string paramType, double value)
        //{
        //    try
        //    {
        //        // Send parameter change to robot server
        //        bool success = await ParameterControl.Instance.SetParameter(paramType, value);

        //        if (success)
        //        {
        //            Debug.WriteLine($"{paramType} parameter set to {value} successfully");
        //        }
        //        else
        //        {
        //            Debug.WriteLine($"Failed to set {paramType} parameter to {value}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error sending {paramType} parameter: {ex.Message}");
        //    }
        //}

        // Public properties to get current values
        public double CurrentSpeedValue => speedValue;
        public double CurrentPitchValue => pitchValue;
        public bool IsSpeedMode => isSpeedMode;
    }



    //public class ParameterControl
    //{
    //    private static ParameterControl _instance;
    //    private static readonly object _lock = new object();
    //    private TcpClient tcpClient;
    //    private NetworkStream stream;

    //    public static ParameterControl Instance
    //    {
    //        get
    //        {
    //            if (_instance == null)
    //            {
    //                lock (_lock)
    //                {
    //                    if (_instance == null)
    //                        _instance = new ParameterControl();
    //                }
    //            }
    //            return _instance;
    //        }
    //    }

    //    private ParameterControl()
    //    {
    //        _ = ConnectToServer();
    //    }

    //    private async Task ConnectToServer()
    //    {
    //        try
    //        {
    //            tcpClient = new TcpClient();
    //            await tcpClient.ConnectAsync("localhost", 12345);
    //            stream = tcpClient.GetStream();
    //            Debug.WriteLine("Parameter Control service connected to server");
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.WriteLine($"Parameter Control server connection failed: {ex.Message}");
    //        }
    //    }

    //    public async Task<bool> SetParameter(string paramType, double value)
    //    {
    //        try
    //        {
    //            if (stream == null || !tcpClient.Connected)
    //            {
    //                await ConnectToServer();
    //            }

    //            int cmd = paramType == "SPEED" ? 6001 : 6002; // SET_SPEED or SET_PITCH

    //            var command = new
    //            {
    //                cmd = cmd,
    //                @params = new object[] { value }
    //            };

    //            string jsonCommand = JsonConvert.SerializeObject(command);
    //            byte[] data = Encoding.UTF8.GetBytes(jsonCommand);

    //            await stream.WriteAsync(data, 0, data.Length);

    //            // Read response
    //            byte[] buffer = new byte[1024];
    //            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
    //            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

    //            Debug.WriteLine($"Parameter Control response: {response}");

    //            var result = JsonConvert.DeserializeObject<dynamic>(response);
    //            return result.results != null && result.results[0] == true;
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.WriteLine($"Parameter Control command error: {ex.Message}");
    //            return false;
    //        }
    //    }
    //}
}

