
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;

namespace ZeusTPV
{
    public class ZeroUser
    {
        private const string HOST = "192.168.0.23";
        // private const string HOST = "localhost";
        private const string USER = "i611usr";
        private const string PASSWORD = "i611";

        //private MainWindow ow;
        private SshClient sshClient;
        private Thread workerThread;
        private bool isRunning = false;
        private int count = 0;
        public ObservableCollection<string> FilesList { get; set; } = new ObservableCollection<string>();

        private static readonly Lazy<ZeroUser> _instance = new Lazy<ZeroUser>(() => new ZeroUser());
        public static ZeroUser Instance => _instance.Value;



        public ZeroUser()
        {
            //this.ow = _ow;
            //// Equivalent to setDaemon(True) in Python
            //this.ow.DisableBtn();
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

                // Update status on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // update status label
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
            }
        }

        public void Start()
        {
            isRunning = true;
            workerThread = new Thread(Run)
            {
                IsBackground = true // Equivalent to setDaemon(True)
            };
            workerThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            sshClient?.Disconnect();
            sshClient?.Dispose();
        }

        private void Run()
        {
            try
            {
                while (isRunning)
                {
                    if (sshClient != null && sshClient.IsConnected)
                    {
                        var command = sshClient.CreateCommand("python /home/i611usr/read_position.py");
                        var result = command.Execute();

                        if (!string.IsNullOrEmpty(result))
                        {
                            var values = result.Trim().Split(',');

                            if (values.Length >= 15)
                            {
                                // Copy to MainWindow().CurPositionList[]
                                var curPositionList = new List<object>();

                                // Add float values for positions 0-5
                                for (int i = 0; i < 6; i++)
                                {
                                    if (float.TryParse(values[i], out float floatVal))
                                        curPositionList.Add(floatVal);
                                }

                                // Add int values for positions 6-7
                                for (int i = 6; i < 8; i++)
                                {
                                    if (int.TryParse(values[i], out int intVal))
                                        curPositionList.Add(intVal);
                                }

                                // Add float values for remaining positions
                                for (int i = 8; i < values.Length; i++)
                                {
                                    if (float.TryParse(values[i], out float floatVal))
                                        curPositionList.Add(floatVal);
                                }

                                // Update UI on main thread
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    //UpdateUI(values);
                                });

                                // Check singular point
                                if (values.Length > 14 && values[14].Length > 0)
                                {
                                    if (int.TryParse(values[14][0].ToString(), out int singularCheck))
                                    {
                                        CheckSingular(singularCheck);
                                    }
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

        //private void UpdateUI(string[] values)
        //{
        //    try
        //    {
        //        if (values.Length >= 14)
        //        {
        //            // Update position displays
        //            ow.TchCurX.Text = float.Parse(values[0]).ToString("F3");
        //            ow.TchCurY.Text = float.Parse(values[1]).ToString("F3");
        //            ow.TchCurZ.Text = float.Parse(values[2]).ToString("F3");
        //            ow.TchCurRZ.Text = float.Parse(values[3]).ToString("F3");
        //            ow.TchCurRY.Text = float.Parse(values[4]).ToString("F3");
        //            ow.TchCurRX.Text = float.Parse(values[5]).ToString("F3");

        //            ow.TchCurPosture.Text = values[6];
        //            ow.TchCurMt.Text = "0x" + int.Parse(values[7]).ToString("X6");

        //            ow.TchCurJ1.Text = float.Parse(values[8]).ToString("F3");
        //            ow.TchCurJ2.Text = float.Parse(values[9]).ToString("F3");
        //            ow.TchCurJ3.Text = float.Parse(values[10]).ToString("F3");
        //            ow.TchCurJ4.Text = float.Parse(values[11]).ToString("F3");
        //            ow.TchCurJ5.Text = float.Parse(values[12]).ToString("F3");
        //            ow.TchCurJ6.Text = float.Parse(values[13]).ToString("F3");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"UI Update error: {ex.Message}");
        //    }
        //}

        private void UpdateFile(string fileList)
        {
            var lines = fileList.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            FilesList.Clear(); // Clear previous file list

            foreach (var line in lines)
            {
                string fileName = line.Trim();

                // Check for .pyc files (skip them)
                if (fileName.Contains(".pyc"))
                    continue;

                // Check for .py files
                if (fileName.Contains(".py"))
                {
                    count++;
                    FilesList.Add(fileName);

                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    // Create tree widget item equivalent
                    //    // This would depend on your WPF tree structure
                    //    // Example if using TreeView:
                    //    /*
                    //    var item = new TreeViewItem();
                    //    item.Header = $"{count}.";

                    //    // Remove .py extension
                    //    string fileNameWithoutExt = fileName.Substring(0, fileName.Length - 3);
                    //    var subItem = new TreeViewItem();
                    //    subItem.Header = fileNameWithoutExt;
                    //    item.Items.Add(subItem);

                    //    ow.TchTreeView.Items.Add(item);
                    //    */
                    //});
                }
            }
            count = 0;
        }

        private void CheckSingular(int chk)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (chk == 1)
                {
                    //ow.StatusLbl.Content = "Singular Point";
                    // device.Buzzer_Sound();
                    // device.Buzzer_ON();
                    // device.Vibration_ON();
                }
                else
                {
                    // ow.StatusLbl.Content = "Connection";
                }
            });
        }
    }

}