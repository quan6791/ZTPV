using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for FilesListView.xaml
    /// </summary>
    public partial class FilesListView : UserControl
    {
        public event EventHandler<string> FileSelected;
        public FilesListView()
        {
            InitializeComponent();
            this.Loaded += FilesListView_Loaded;
        }

        private void FilesListView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

            if (!Constants._isSimulation)
            {
                ZeroUser.Instance?.Connect();
                ZeroUser.Instance?.Start();
                if (this.lbx_Files != null)
                {
                    this.lbx_Files.ItemsSource = null;
                }
                this.lbx_Files.Items.Clear();
                this.lbx_Files.ItemsSource = ZeroUser.Instance?.FilesList;
            }
        }

        private void lbx_Files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Equivalent to: getSelected = self.ui.treeWidget.selectedItems()
            if (lbx_Files.SelectedItem != null)
            {
                string selectedItem = lbx_Files.SelectedItem.ToString();

                // Equivalent to: if getSelected:
                if (!string.IsNullOrEmpty(selectedItem))
                {
                    try
                    {
                        // Equivalent to: baseNode = getSelected[0]
                        // getChildNode = baseNode.text(1)
                        // In ListBox, we already have the text directly
                        string getChildNode = selectedItem;

                        // Equivalent to: fname = os.path.splitext(os.path.basename(getChildNode))[0]
                        string fname = Path.GetFileName(Path.GetFileName(getChildNode));

                        // Remove number prefix if exists (e.g., "01. __init__" -> "__init__")
                        if (fname.Contains(". "))
                        {
                            int dotIndex = fname.IndexOf(". ");
                            if (dotIndex >= 0 && dotIndex < fname.Length - 2)
                            {
                                fname = fname.Substring(dotIndex + 2);
                            }
                        }

                        // Equivalent to: tfname = fname+".csv"
                        string tfname = fname + ".csv";

                        // Equivalent to: if os.path.exists(tfname) == False:
                        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, tfname);
                        if (!File.Exists(fullPath))
                        {
                            try
                            {
                                // Equivalent to: shutil.copyfile("./org.csv", tfname) 
                                string orgCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "org.csv");
                                if (File.Exists(orgCsvPath))
                                {
                                    File.Copy(orgCsvPath, fullPath);
                                }
                                else
                                {
                                    // Create a default CSV file if org.csv doesn't exist
                                    CreateDefaultCsvFile(fullPath);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error creating CSV file: {ex.Message}");
                                // Create fallback file
                                CreateDefaultCsvFile(fullPath);
                            }
                        }

                        // Raise event để parent control biết, passing the CSV filename
                        FileSelected?.Invoke(this, tfname);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing file selection: {ex.Message}");
                        // Fallback to original behavior
                        FileSelected?.Invoke(this, selectedItem);
                    }
                }
            }
        }


        private void CreateDefaultCsvFile(string filePath)
        {
            try
            {
                // Create a default CSV file with standard headers
                string defaultCsvContent = @"idx,PositionName,Array,DataType,Replace,DateTime,PosX,PosY,PosZ,PosRz,PosRy,PosRx,PosPosture,PosMulti,Jnt1,Jnt2,Jnt3,Jnt4,Jnt5,Jnt6,Pls1,Pls2,Pls3,Pls4,Pls5,Pls6,VarInt,VarFloat,FrameNo,ToolNo,RobotNo,Description
15,Jnt,4,2,0,,0.0,0.0,0.0,0.0,0.0,0.0,0,0,45.5,30.2,75.8,12.4,88.9,15.6,0,0,0,0,0,0,0,0.0,0,0,0,Sample Joint 4
16,Jnt,5,2,0,,0.0,0.0,0.0,0.0,0.0,0.0,0,0,50.1,35.7,80.3,15.9,92.4,18.2,0,0,0,0,0,0,0,0.0,0,0,0,Sample Joint 5
17,Jnt,6,2,0,,0.0,0.0,0.0,0.0,0.0,0.0,0,0,55.7,40.8,85.1,18.6,95.8,20.9,0,0,0,0,0,0,0,0.0,0,0,0,Sample Joint 6
18,Jnt,7,2,0,,0.0,0.0,0.0,0.0,0.0,0.0,0,0,60.3,45.4,90.7,21.2,99.1,23.5,0,0,0,0,0,0,0,0.0,0,0,0,Sample Joint 7
19,Jnt,8,2,0,,0.0,0.0,0.0,0.0,0.0,0.0,0,0,65.9,50.1,95.4,24.8,102.6,26.1,0,0,0,0,0,0,0,0.0,0,0,0,Sample Joint 8
20,Jnt,9,2,0,,0.0,0.0,0.0,0.0,0.0,0.0,0,0,70.5,55.7,100.2,27.3,106.2,28.8,0,0,0,0,0,0,0,0.0,0,0,0,Sample Joint 9";

                File.WriteAllText(filePath, defaultCsvContent);
                System.Diagnostics.Debug.WriteLine($"Created default CSV file: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating default CSV file: {ex.Message}");
            }
        }

    }
}
