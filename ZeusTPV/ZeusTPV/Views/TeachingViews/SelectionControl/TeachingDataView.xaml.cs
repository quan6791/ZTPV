using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ZeusTPV.Views
{
    /// <summary>
    /// Interaction logic for TeachingDataView.xaml
    /// </summary>
    public partial class TeachingDataView : UserControl
    {

        private TempTeachData _teachData;
        public event EventHandler<int> PositionSelected;


        public TeachingDataView(string selectedFile = null)
        {
            InitializeComponent();
            // Load sample data
            if (selectedFile != null)
                _teachData = new TempTeachData(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, selectedFile + ".csv"));
            else
                _teachData = new TempTeachData(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleTeachFile1.csv"));

            DataContext = _teachData;

            // Set initial selection
            if (_teachData.DisplayRecords.Count > 3)
            {
                TeachDataGrid.SelectedIndex = 3; // Select "Jnt [7]" row
            }

        }

        private void FileSelect_Click(object sender, RoutedEventArgs e)
        {
            // Handle file selection
            MessageBox.Show("File Select functionality would be implemented here",
                          "File Select", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PositionSelect_Click(object sender, RoutedEventArgs e)
        {
            // Handle position selection
            MessageBox.Show("Position Select functionality would be implemented here",
                          "Position Select", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TeachDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TeachDataGrid.SelectedIndex >= 0)
            {
                _teachData.SetCurIndex(TeachDataGrid.SelectedIndex);
            }
        }

        private void ShowPositionDetail_Click(object sender, RoutedEventArgs e)
        {
            //if (TeachDataGrid.SelectedItem is DisplayTeachDataRecord selectedRecord)
            //{
            //    var positionDetailView = new TeachingPositionDetailView();
            //    //positionDetailView.LoadPositionData(selectedRecord);

            //    // Replace content or show in popup
            //    this.Content = positionDetailView;
            //}
            PositionSelected?.Invoke(this, _teachData.GetCurIndex());
        }
    }

    //// Additional methods for handling ComboBox, etc.
    //private void DescriptionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //{
    //    if (DescriptionComboBox.SelectedIndex >= 0)
    //    {
    //        // Handle sorting based on selection
    //        SortDataGrid(DescriptionComboBox.SelectedIndex);
    //    }
    //}

    //    private void SortDataGrid(int sortOption)
    //    {
    //        // Implementation for different sorting options
    //        switch (sortOption)
    //        {
    //            case 0: // Source description order
    //                // Sort by index
    //                break;
    //            case 1: // Alphabetical order
    //                // Sort by position name
    //                break;
    //            case 2: // Type order
    //                // Sort by data type
    //                break;
    //            case 3: // Replace date order
    //                // Sort by replace date
    //                break;
    //        }
    //    }
    //}
}


public class TeachDataRecord
{
    public int idx { get; set; }
    public string PositionName { get; set; }
    public int Array { get; set; }
    public int DataType { get; set; }
    public int Replace { get; set; }
    public DateTime? DateTime { get; set; }
    public double? PosX { get; set; }
    public double? PosY { get; set; }
    public double? PosZ { get; set; }
    public double? PosRz { get; set; }
    public double? PosRy { get; set; }
    public double? PosRx { get; set; }
    public double? PosPosture { get; set; }
    public double? PosMulti { get; set; }
    public double? Jnt1 { get; set; }
    public double? Jnt2 { get; set; }
    public double? Jnt3 { get; set; }
    public double? Jnt4 { get; set; }
    public double? Jnt5 { get; set; }
    public double? Jnt6 { get; set; }
    public long? Pls1 { get; set; }
    public long? Pls2 { get; set; }
    public long? Pls3 { get; set; }
    public long? Pls4 { get; set; }
    public long? Pls5 { get; set; }
    public long? Pls6 { get; set; }
    public long? VarInt { get; set; }
    public double? VarFloat { get; set; }
    public int? FrameNo { get; set; }
    public int? ToolNo { get; set; }
    public int? RobotNo { get; set; }
    public string Description { get; set; }
}


public class DisplayTeachDataRecord : INotifyPropertyChanged
{
    private int _index;
    private string _positionName;
    private string _dataType;
    private string _replaceDateTime;
    private bool _isSelected;

    public int Index
    {
        get => _index;
        set { _index = value; OnPropertyChanged(nameof(Index)); }
    }

    public string PositionName
    {
        get => _positionName;
        set { _positionName = value; OnPropertyChanged(nameof(PositionName)); }
    }

    public string DataType
    {
        get => _dataType;
        set { _dataType = value; OnPropertyChanged(nameof(DataType)); }
    }

    public string ReplaceDateTime
    {
        get => _replaceDateTime;
        set { _replaceDateTime = value; OnPropertyChanged(nameof(ReplaceDateTime)); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class TempTeachData : INotifyPropertyChanged
{
    // Constants (same as before)
    public const int ARYINDX_NotAry = -1;
    public const int DATATYPE_UDf = 0;
    public const int DATATYPE_Pos = 1;
    public const int DATATYPE_Jnt = 2;
    public const int DATATYPE_Pls = 3;
    public const int DATATYPE_Int = 4;
    public const int DATATYPE_Flt = 5;
    public const int DATATYPE_Frm = 6;
    public const int DATATYPE_Tol = 7;
    public const int REPLACE_INI = 0;
    public const int REPLACE_FIN = 1;

    // Properties
    public List<double> CurPositionList { get; set; } = new List<double>
        {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0, 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0
        };

    private List<TeachDataRecord> _dataRecords;
    private List<DisplayTeachDataRecord> _displayRecords;
    private string _teachDataCSVFile;
    private int _curIdx;
    private TeachDataRecord _curTD;
    private string _selectedFile = "program1";
    private string _selectedPosition = "Jnt [7]";

    public TempTeachData(string teachFilename)
    {
        _teachDataCSVFile = teachFilename;
        LoadDataFromCSV();
        ProcessData();
        UpdateDisplayData();

        _curIdx = 0;
        if (_dataRecords.Count > 0)
            _curTD = _dataRecords[_curIdx];
    }
    private void LoadDataFromCSV()
    {
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null // Ignore missing fields
            };

            var reader = new StringReader(File.ReadAllText(_teachDataCSVFile));
            var csv = new CsvReader(reader, config);
            _dataRecords = csv.GetRecords<TeachDataRecord>().ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading CCSV: {ex.Message}");
            _dataRecords = CreateSampleData();
        }
    }

    private List<TeachDataRecord> CreateSampleData()
    {
        // Create sample data for testing
        return new List<TeachDataRecord>
            {
              new TeachDataRecord { idx = 15, PositionName = "Jnt", Array = 4, DataType = DATATYPE_Jnt, Replace = REPLACE_INI },
                new TeachDataRecord { idx = 16, PositionName = "Jnt", Array = 5, DataType = DATATYPE_Jnt, Replace = REPLACE_INI },
                new TeachDataRecord { idx = 17, PositionName = "Jnt", Array = 6, DataType = DATATYPE_Jnt, Replace = REPLACE_INI },
                new TeachDataRecord { idx = 18, PositionName = "Jnt", Array = 7, DataType = DATATYPE_Jnt, Replace = REPLACE_INI },
                new TeachDataRecord { idx = 19, PositionName = "Jnt", Array = 8, DataType = DATATYPE_Jnt, Replace = REPLACE_INI },
                new TeachDataRecord { idx = 20, PositionName = "Jnt", Array = 9, DataType = DATATYPE_Jnt, Replace = REPLACE_INI },
            };
    }

    private void ProcessData()
    {
        foreach (var record in _dataRecords)
        {
            if (record.Array == 0) record.Array = ARYINDX_NotAry;
            if (record.DataType == 0) record.DataType = DATATYPE_UDf;
            if (record.Replace == 0) record.Replace = REPLACE_INI;
            if (string.IsNullOrEmpty(record.Description)) record.Description = "";

            if (record.Replace != REPLACE_INI && !record.DateTime.HasValue)
            {
                record.DateTime = DateTime.Now;
            }
        }
    }

    private void UpdateDisplayData()
    {
        _displayRecords = new List<DisplayTeachDataRecord>();
        foreach (var record in _dataRecords)
        {
            var displayRecord = new DisplayTeachDataRecord
            {
                Index = record.idx,
                PositionName = FormatPositionName(record.PositionName, record.Array),
                DataType = GetDataTypeString(record.DataType),
                ReplaceDateTime = record.DateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                IsSelected = false
            };


            _displayRecords.Add(displayRecord);
        }

        if (_displayRecords.Count > 3)
        {
            _displayRecords[3].IsSelected = true;
            _curIdx = 3;
            _curTD = _dataRecords[_curIdx];
        }
    }

    private string FormatPositionName(string name, int index)
    {
        if (index == ARYINDX_NotAry)
            return name;
        else
            return $"{name} [{index}]";
    }

    private string GetDataTypeString(int dataType)
    {
        switch (dataType)
        {
            case DATATYPE_UDf:
                return "-";
            case DATATYPE_Pos:
                return "Position";
            case DATATYPE_Jnt:
                return "Joint";
            case DATATYPE_Pls:
                return "Pulse";
            case DATATYPE_Int:
                return "Integer";
            case DATATYPE_Flt:
                return "Float";
            case DATATYPE_Frm:
                return "Frame";
            case DATATYPE_Tol:
                return "Tool";
            default:
                return "?";
        }
    }

    // Public methods (same as before but updated)
    public void SetCurIndex(int idx)
    {
        if (idx >= 0 && idx < _dataRecords.Count)
        {
            // Clear previous selection
            for (int i = 0; i < _displayRecords.Count; i++)
            {
                _displayRecords[i].IsSelected = (i == idx);
            }

            _curIdx = idx;
            _curTD = _dataRecords[_curIdx];
            _selectedFile = _displayRecords[idx].PositionName;

            OnPropertyChanged(nameof(CurrentIndex));
            OnPropertyChanged(nameof(CurrentTeachData));
            OnPropertyChanged(nameof(SelectedPosition));
            OnPropertyChanged(nameof(DisplayRecords));
        }
    }

    // Properties for UI Binding
    public string SelectedFile
    {
        get => _selectedFile;
        set { _selectedFile = value; OnPropertyChanged(); }
    }

    public string SelectedPosition
    {
        get => _selectedPosition;
        set { _selectedPosition = value; OnPropertyChanged(); }
    }

    public List<DisplayTeachDataRecord> DisplayRecords => _displayRecords;
    public string TeachDataCSVFile => _teachDataCSVFile;
    public int CurrentIndex => _curIdx;
    public TeachDataRecord CurrentTeachData => _curTD;

    // All other methods remain the same...
    public int GetCurIndex() => _curIdx;
    public TeachDataRecord GetCurTeachData() => _curTD;
    public int GetCurTD_SourceIndex() => _curTD?.idx ?? 0;
    public string GetCurTD_PositionName() => _curTD?.PositionName ?? "";
    public int GetCurTD_ArrayIndex() => _curTD?.Array ?? ARYINDX_NotAry;
    public int GetCurTD_DataType() => _curTD?.DataType ?? DATATYPE_UDf;

    public List<double> GetCurTD_PositionList()
    {
        if (_curTD == null) return new List<double>();
        return new List<double>
            {
                _curTD.PosX ?? 0.0,
                _curTD.PosY ?? 0.0,
                _curTD.PosZ ?? 0.0,
                _curTD.PosRz ?? 0.0,
                _curTD.PosRy ?? 0.0,
                _curTD.PosRx ?? 0.0,
                _curTD.PosPosture ?? 0.0,
                _curTD.PosMulti ?? 0.0
            };
    }

    public List<double> GetCurTD_JointList()
    {
        if (_curTD == null) return new List<double>();
        return new List<double>
            {
                _curTD.Jnt1 ?? 0.0,
                _curTD.Jnt2 ?? 0.0,
                _curTD.Jnt3 ?? 0.0,
                _curTD.Jnt4 ?? 0.0,
                _curTD.Jnt5 ?? 0.0,
                _curTD.Jnt6 ?? 0.0
            };
    }

    public void Replace()
    {
        if (_curTD == null) return;

        var curPos = CurPositionList.Take(8).ToList();
        var curJnt = CurPositionList.Skip(8).Take(6).ToList();

        if (curPos.Count >= 8)
        {
            _curTD.PosX = curPos[0];
            _curTD.PosY = curPos[1];
            _curTD.PosZ = curPos[2];
            _curTD.PosRz = curPos[3];
            _curTD.PosRy = curPos[4];
            _curTD.PosRx = curPos[5];
            _curTD.PosPosture = (int)curPos[6];
            _curTD.PosMulti = (int)curPos[7];
        }

        if (curJnt.Count >= 6)
        {
            _curTD.Jnt1 = curJnt[0];
            _curTD.Jnt2 = curJnt[1];
            _curTD.Jnt3 = curJnt[2];
            _curTD.Jnt4 = curJnt[3];
            _curTD.Jnt5 = curJnt[4];
            _curTD.Jnt6 = curJnt[5];
        }

        _curTD.DateTime = DateTime.Now;
        _curTD.Replace = REPLACE_FIN;

        UpdateDisplayData();
        SaveToCSV();
        OnPropertyChanged(nameof(DisplayRecords));
    }

    private void SaveToCSV()
    {
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            var writer = new StringWriter();
            var csv = new CsvWriter(writer, config);

            csv.WriteRecords(_dataRecords);
            File.WriteAllText(_teachDataCSVFile, writer.ToString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving CSV: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

