using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace ZeusTPV
{
    public class ZeroUser
    {
        private const string HOST = "192.168.0.23";
        private const string USER = "i611usr";
        private const string PASSWORD = "i611";

        private SshClient sshClient;
        private Thread workerThread;
        private bool isRunning = false;
        private int count = 0;

        // Properties
        public ObservableCollection<string> FilesList { get; set; } = new ObservableCollection<string>();
        public List<double> CurPositionList { get; set; } = new List<double>(new double[14]);

        // Events for position updates
        public event Action<PositionData> PositionUpdated;
        public event Action<JogStatus> JogStatusUpdated;

        // Status flags
        public bool RetSingular { get; private set; }
        public bool RetSpdLimit { get; private set; }
        public bool RetAngleLimit { get; private set; }

        // Singleton pattern
        private static readonly Lazy<ZeroUser> _instance = new Lazy<ZeroUser>(() => new ZeroUser());
        public static ZeroUser Instance => _instance.Value;

        public ZeroUser()
        {
            // Initialize CurPositionList with default values
            CurPositionList = Enumerable.Repeat(0.0, 14).ToList();
        }

        public void Connect()
        {
            try
            {
                var connectionInfo = new ConnectionInfo(HOST, USER, new PasswordAuthenticationMethod(USER, PASSWORD));
                sshClient = new SshClient(connectionInfo);
                sshClient.Connect();

                count = 0;
                // Read i611usr/home file list
                var command = sshClient.CreateCommand("ls");
                var result = command.Execute();
                UpdateFile(result);

                Console.WriteLine("SSH connection established successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
            }
        }

        private void Run()
        {
            try
            {
                while (isRunning)
                {
                    if (sshClient != null && sshClient.IsConnected)
                    {
                        // Execute Python script - equivalent to stdin, stdout, stderr = self.ssh.exec_command(...)
                        var command = sshClient.CreateCommand("python /opt/i611/tools/read_position.py");
                        var result = command.Execute();

                        if (!string.IsNullOrEmpty(result))
                        {
                            // Split result - equivalent to list = stdout.readline().split(',')
                            var list = result.Trim().Split(',');

                            if (list.Length >= 17) // Ensure we have enough elements
                            {
                                try
                                {
                                    // Copy to CurPositionList - equivalent to self.ow.td.CurPositionList = [float(s) for s in list[:6]]
                                    CurPositionList.Clear();

                                    // Add float values for positions 0-5
                                    for (int i = 0; i < 6; i++)
                                    {
                                        CurPositionList.Add(double.Parse(list[i]));
                                    }

                                    // Add int values for positions 6-7, then convert to double
                                    // equivalent to self.ow.td.CurPositionList.extend([int(s) for s in list[6:8]])
                                    for (int i = 6; i < 8; i++)
                                    {
                                        CurPositionList.Add(double.Parse(list[i]));
                                    }

                                    // Add float values for positions 8-13
                                    // equivalent to self.ow.td.CurPositionList.extend([float(s) for s in list[8:14]])
                                    for (int i = 8; i < 14; i++)
                                    {
                                        CurPositionList.Add(double.Parse(list[i]));
                                    }

                                    // Check status flags
                                    RetSingular = CheckSingular(int.Parse(list[14][0].ToString()));
                                    RetSpdLimit = CheckSpdLimit(int.Parse(list[15][0].ToString()));
                                    RetAngleLimit = CheckJntAngleLimit(int.Parse(list[16][0].ToString()));

                                    // Create position data object
                                    var positionData = new PositionData
                                    {
                                        X = double.Parse(list[0]),
                                        Y = double.Parse(list[1]),
                                        Z = double.Parse(list[2]),
                                        RZ = double.Parse(list[3]),
                                        RY = double.Parse(list[4]),
                                        RX = double.Parse(list[5]),
                                        Posture = list[6],
                                        Mt = $"0x{int.Parse(list[7]):X6}",
                                        J1 = double.Parse(list[8]),
                                        J2 = double.Parse(list[9]),
                                        J3 = double.Parse(list[10]),
                                        J4 = double.Parse(list[11]),
                                        J5 = double.Parse(list[12]),
                                        J6 = double.Parse(list[13])
                                    };

                                    var jogStatus = new JogStatus
                                    {
                                        RetSingular = RetSingular,
                                        RetSpdLimit = RetSpdLimit,
                                        RetAngleLimit = RetAngleLimit
                                    };

                                    // Update UI on main thread
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        // Raise events to notify subscribers
                                        PositionUpdated?.Invoke(positionData);
                                        JogStatusUpdated?.Invoke(jogStatus);
                                    });

                                    Console.WriteLine($"Singular: {RetSingular}, SpdLimit: {RetSpdLimit}, AngleLimit: {RetAngleLimit}");
                                }
                                catch (Exception parseEx)
                                {
                                    Console.WriteLine($"Parse error: {parseEx.Message}");
                                }
                            }
                        }
                    }

                    Thread.Sleep(10); // 0.01 seconds
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Run error: {e.Message}");
            }
        }

        // Status check methods
        private bool CheckSingular(int chk)
        {
            bool isSingular = chk == 1;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isSingular)
                {
                    Console.WriteLine("Singular Point detected");
                    // Add buzzer/vibration logic here
                }
            });

            return isSingular;
        }

        private bool CheckSpdLimit(int chk)
        {
            return chk == 1;
        }

        private bool CheckJntAngleLimit(int chk)
        {
            return chk == 1;
        }

        public void Start()
        {
            isRunning = true;
            workerThread = new Thread(Run)
            {
                IsBackground = true
            };
            workerThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            sshClient?.Disconnect();
            sshClient?.Dispose();
        }

        private void UpdateFile(string fileList)
        {
            var lines = fileList.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            Application.Current.Dispatcher.Invoke(() =>
            {
                FilesList.Clear();
            });

            foreach (var line in lines)
            {
                string fileName = line.Trim();

                if (fileName.Contains(".pyc"))
                    continue;

                if (fileName.Contains(".py"))
                {
                    count++;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        FilesList.Add($"{count:D2}. {fileName.Replace(".py", "")}");
                    });
                }
            }
            count = 0;
        }
    }

    // Data classes for position and status
    public class PositionData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double RZ { get; set; }
        public double RY { get; set; }
        public double RX { get; set; }
        public string Posture { get; set; } = "";
        public string Mt { get; set; } = "";
        public double J1 { get; set; }
        public double J2 { get; set; }
        public double J3 { get; set; }
        public double J4 { get; set; }
        public double J5 { get; set; }
        public double J6 { get; set; }
    }

    public class JogStatus
    {
        public bool RetSingular { get; set; }
        public bool RetSpdLimit { get; set; }
        public bool RetAngleLimit { get; set; }
    }
}