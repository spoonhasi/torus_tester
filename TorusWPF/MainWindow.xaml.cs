﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;
using IntelligentApiCS;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using static IntelligentApiCS.Api;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Win32;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Reflection.PortableExecutable;
using Microsoft.VisualBasic;
using System.Windows.Input;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ScottPlot;

namespace TorusWPF
{
    static class Constants
    {
        // App의 고유 ID와 Name를 설정합니다. TorusTester.info 라는 파일명으로 저장되어 있습니다. TorusTester.info을 TORUS/Binary/application에 복사하고 TORUS를 실행시켜야 합니다.
        public const string AppID = "FAFC456B-FA41-40AD-B1EB-C3834076A1DC";
        public const string AppName = "TorusTester";
        public const string ConfigFileName = "config.ini";
    }
    public class TimeSeriesItem
    {
        public List<List<double>> value { get; set; }
        public List<long> starttimestamp { get; set; }
        public List<long> endtimestamp { get; set; }
        public List<long> streamdatasizes { get; set; }
        public long count { get; set; }
        public long status { get; set; }
        public string datatype { get; set; }
        public string time { get; set; }
        public string address { get; set; }
        public string filter { get; set; }
    }
    public class MachineObject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("vendorCode")]
        public string VendorCode { get; set; }

        [JsonPropertyName("connectCode")]
        public string ConnectCode { get; set; }

        [JsonPropertyName("ip_address")]
        public string Address { get; set; }

        //[JsonPropertyName("toolSystem")]
        //public int ToolSystem { get; set; }
    }
    public class MachinesObject
    {
        [JsonPropertyName("value")]
        public List<MachineObject> Value { get; set; }
    }
    public class FileObject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("datetime")]
        public string Datetime { get; set; }

        [JsonPropertyName("isDir")]
        public bool IsDir { get; set; }
    }
    public class FilesObject
    {
        [JsonPropertyName("value")]
        public List<FileObject> Value { get; set; }
    }

    public class ItemObject
    {
        [JsonPropertyName("value")]
        public List<JsonElement> Value { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("datatype")]
        public string DataType { get; set; }

        [JsonPropertyName("time")]
        public string Timestamp { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("filter")]
        public string Filter { get; set; }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
#pragma warning disable SYSLIB1054 // 컴파일 타임에 P/Invoke 마샬링 코드를 생성하려면 'DllImportAttribute' 대신 'LibraryImportAttribute'를 사용하세요.
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
#pragma warning restore SYSLIB1054 // 컴파일 타임에 P/Invoke 마샬링 코드를 생성하려면 'DllImportAttribute' 대신 'LibraryImportAttribute'를 사용하세요.

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        //설정 불러오기용 함수
        public static string ReadConfig(string _settingFileName, string _section, string _key, string _defaultValue = "")
        {
            StringBuilder temp = new(255);
            _ = GetPrivateProfileString(_section, _key, null, temp, 255, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settingFileName));
            if (temp.ToString() == "")
            {
                WriteConfig(_settingFileName, _section, _key, _defaultValue);
                return _defaultValue;
            }
            return temp.ToString();
        }
        //설정 저장용 함수
        public static void WriteConfig(string _settingFileName, string _section, string _key, string _contents)
        {
            WritePrivateProfileString(_section, _key, _contents, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settingFileName));
        }

        private List<MachineObject> _connectedMachineList = [];
        private int _connectResult;
        private int _cncVendorCode;
        private int _timeseriesVendorCode;
        private Thread _threadSingle;
        private bool _threadSignlestopFlag;
        private Thread _threadMulti;
        private bool _threadMultistopFlag;
        private string _commonFilter;
        private int _signalCopyOrMove;
        private string _addressFilePath;
        private bool _singleDirect;
        private bool _multiDirect;
        private int _timeout;
        private int _userApiTimeout;
        private bool _isTimeSeriesCallbackRegistered;
        private bool _OffChanged;
        private bool _isSingleRunning;
        private bool _isMultiRunning;
        private readonly object _lock = new();

        public MainWindow()
        {
            InitializeComponent();

            // 소스코드의 Api폴더에 TORUS/Example_VS19/Api의 내용물을 복사해야 합니다. 본 App은 TORUS v2.3.2의 API로 제작되었습니다.
            ButtonTimeseriesStart.IsEnabled = false;
            ButtonTimeseriesStop.IsEnabled = false;

            ComboBoxTimeSeriesMachieID.IsEnabled = false;
            ComboBoxTimeSeriesStatus.IsEnabled = false;
            ComboBoxTimeSeriesMode.IsEnabled = false;
            TextBoxTimeSeriesPeriod.IsEnabled = false;
            ButtonTimeSeriesPeriod.IsEnabled = false;
            ButtonTimeSeriesFrequency.IsEnabled = false;
            TextBoxTimeSeriesFrequency.IsEnabled = false;
            ComboBoxTimeSeriesCategory.IsEnabled = false;
            TextBoxTimeSeriesSubCategory.IsEnabled = false;
            ButtonTimeSeriesSubCategory.IsEnabled = false;

            _isSingleRunning = false;
            _isMultiRunning = false;
            _OffChanged = true;
            _isTimeSeriesCallbackRegistered = false;
            _connectResult = -1;
            _cncVendorCode = 0;
            _timeseriesVendorCode = 0;
            _signalCopyOrMove = 0; // 0은 초기화, 1은 복사, 2는 이동
            ButtonSingleStart.IsEnabled = false;
            ButtonSingleStop.IsEnabled = false;
            ButtonMultiStart.IsEnabled = false;
            ButtonMultiStop.IsEnabled = false;
            ButtonMachineMonitoringStart.IsEnabled = false;
            ButtonMachineMonitoringStop.IsEnabled = false;
            TextBlockSinglePassed.Text = "0";
            TextBlockSinglePassedAll.Text = "0";
            TextBlockMultiSuccess.Text = "0";
            TextBlockMultiAll.Text = "0";
            TextBlockMultiPercent.Text = "%";
            TextBlockSingleSuccess.Text = "0";
            TextBlockSingleAll.Text = "0";
            TextBlockSinglePercent.Text = "%";
            TextBlockMissionSuccess.Text = "0";
            TextBlockMissionAll.Text = "0";
            TextBlockTotalSuccess.Text = "0";
            TextBlockTotalAll.Text = "0";
            TextBlockTotalPercent.Text = "%";
            TextBlockAddressFilePath.Text = "";
            TextBoxTimeout.Text = ReadConfig(Constants.ConfigFileName, "Main", "Timeout", "100000");
            TextBoxUserApiTimeout.Text = ReadConfig(Constants.ConfigFileName, "Main", "UserApiTimeout", "100000");
            _addressFilePath = "";
            TextBlockMemoryTotal.Text = "";
            TextBlockMemoryUsed.Text = "";
            TextBlockMemoryFree.Text = "";
            ComboBoxSingleDirect.Items.Add("True(비주기통신)");
            ComboBoxSingleDirect.Items.Add("False(주기통신)");
            ComboBoxSingleDirect.SelectedIndex = Convert.ToInt32(ReadConfig(Constants.ConfigFileName, "Main", "SingleDirect", "0"));
            if (ComboBoxSingleDirect.SelectedIndex == 0)
            {
                _singleDirect = true;
            }
            else
            {
                _singleDirect = false;
            }
            ComboBoxMultiDirect.Items.Add("True(비주기통신)");
            ComboBoxMultiDirect.Items.Add("False(주기통신)");
            ComboBoxMultiDirect.SelectedIndex = Convert.ToInt32(ReadConfig(Constants.ConfigFileName, "Main", "MultiDirect", "0"));
            if (ComboBoxMultiDirect.SelectedIndex == 0)
            {
                _multiDirect = true;
            }
            else
            {
                _multiDirect = false;
            }
            TextBoxMonitoringInterval.Text = ReadConfig(Constants.ConfigFileName, "Main", "MonitoringInterval", "10");
            SetTimeout();
            //MachineStateModelSample.txt에 모든 MachineStateModel이 예시로 기록되어 있습니다. filter만 바꿔서 사용하면 됩니다.
            string lastMachineStateModelFilePath = ReadConfig(Constants.ConfigFileName, "Main", "LastMachineStateModelFilePath");
            if (lastMachineStateModelFilePath == "" || File.Exists(lastMachineStateModelFilePath) == false)
            {
                string defaultMachineStateModelFilePath = "../../../MachineStateModelSample.txt";
                if (File.Exists(defaultMachineStateModelFilePath))
                {
                    lastMachineStateModelFilePath = Path.GetFullPath(defaultMachineStateModelFilePath);
                }
            }
            LoadMachineStateModel(lastMachineStateModelFilePath);
        }

        ~MainWindow()
        {
            _threadSignlestopFlag = true;
            _threadSingle?.Join();
            _threadMultistopFlag = true;
            _threadMulti?.Join();
        }
        public static int GetMachineId(string _id_name)
        {
            int index = _id_name.IndexOf(':');
            if (index == -1)
            {
                return 0;
            }
            else
            {
                string id = _id_name[..index];
                return Convert.ToInt32(id);
            }
        }

        // 타임아웃 적용 함수입니다.
        public void SetTimeout()
        {
            bool result = int.TryParse(TextBoxTimeout.Text, out int timeout);
            if (result == false)
            {
                timeout = 10000;
            }
            TextBoxTimeout.Text = timeout.ToString();
            _timeout = timeout;
            WriteConfig(Constants.ConfigFileName, "Main", "Timeout", timeout.ToString());
            result = int.TryParse(TextBoxUserApiTimeout.Text, out int userApiTimeout);
            if (result == false)
            {
                userApiTimeout = 10000;
            }
            TextBoxUserApiTimeout.Text = userApiTimeout.ToString();
            _userApiTimeout = userApiTimeout;
            WriteConfig(Constants.ConfigFileName, "Main", "UserApiTimeout", userApiTimeout.ToString());
        }
        // 사용자 함수의 사용 결과가 저장됩니다. MachineStateModel을 Load할 때마다 초기화 됩니다.
        private void AddForTest(int _startNumber)
        {
            ListBoxMachineMission.Items.Clear();
            MissionItem.totalCount = _startNumber;
            ListBoxMachineMission.Items.Add(new MissionItem("getAttributeExists"));
            ListBoxMachineMission.Items.Add(new MissionItem("getAttributeType"));
            ListBoxMachineMission.Items.Add(new MissionItem("getAttributeIsNc"));
            ListBoxMachineMission.Items.Add(new MissionItem("getAttributeLogicalPath"));
            ListBoxMachineMission.Items.Add(new MissionItem("getAttributeName"));
            ListBoxMachineMission.Items.Add(new MissionItem("getAttributePath"));
            ListBoxMachineMission.Items.Add(new MissionItem("getAttributeSize"));
            ListBoxMachineMission.Items.Add(new MissionItem("getAttributeEditedTime"));
            ListBoxMachineMission.Items.Add(new MissionItem("getFileList"));
            ListBoxMachineMission.Items.Add(new MissionItem("CreateCNCFile"));
            ListBoxMachineMission.Items.Add(new MissionItem("CreateCNCFolder"));
            ListBoxMachineMission.Items.Add(new MissionItem("CNCFileRename"));
            ListBoxMachineMission.Items.Add(new MissionItem("CNCFileCopy"));
            ListBoxMachineMission.Items.Add(new MissionItem("CNCFileMove"));
            ListBoxMachineMission.Items.Add(new MissionItem("CNCFileDelete"));
            ListBoxMachineMission.Items.Add(new MissionItem("CNCFileDeleteAll"));
            ListBoxMachineMission.Items.Add(new MissionItem("CNCFileExecute"));
            ListBoxMachineMission.Items.Add(new MissionItem("CNCFileExecuteExtern"));
            ListBoxMachineMission.Items.Add(new MissionItem("uploadFile"));
            ListBoxMachineMission.Items.Add(new MissionItem("downloadFile"));
            ListBoxMachineMission.Items.Add(new MissionItem("getFileListEx"));
            ListBoxMachineMission.Items.Add(new MissionItem("getPLCSignal"));
            ListBoxMachineMission.Items.Add(new MissionItem("setPLCSignal"));
            ListBoxMachineMission.Items.Add(new MissionItem("getToolOffsetData"));
            ListBoxMachineMission.Items.Add(new MissionItem("setToolOffsetData"));
            ListBoxMachineMission.Items.Add(new MissionItem("getGModal"));
            ListBoxMachineMission.Items.Add(new MissionItem("getExModal"));
            ListBoxMachineMission.Items.Add(new MissionItem("getGUD"));
            ListBoxMachineMission.Items.Add(new MissionItem("setGUD"));
            ListBoxMachineMission.Items.Add(new MissionItem("startTimeSeries"));
            ListBoxMachineMission.Items.Add(new MissionItem("endTimeSeries"));
        }

        private void CalTotal()
        {
            double tmpSingleSuccessCount = Convert.ToDouble(TextBlockSingleSuccess.Text);
            double tmpMultiSuccessCount = Convert.ToDouble(TextBlockMultiSuccess.Text);
            double tmpGetDataSuccessCount = Math.Max(tmpSingleSuccessCount, tmpMultiSuccessCount);
            double tmpSingleAllCount = Convert.ToDouble(TextBlockSingleAll.Text);
            double tmpMultiAllCount = Convert.ToDouble(TextBlockMultiAll.Text);
            double tmpGetDataAllCount;
            if (tmpSingleSuccessCount > tmpMultiSuccessCount)
            {
                tmpGetDataAllCount = tmpSingleAllCount;
            }
            else
            {
                tmpGetDataAllCount = tmpMultiAllCount;
            }
            double tmpGetDataPercent;
            if (tmpGetDataAllCount == 0)
            {
                tmpGetDataPercent = 0;
            }
            else
            {
                tmpGetDataPercent = tmpGetDataSuccessCount / tmpGetDataAllCount * 100;
            }
            double tmpMissionSuccessCount = Convert.ToInt32(TextBlockMissionSuccess.Text);
            double tmpMissionAllCount = Convert.ToInt32(TextBlockMissionAll.Text);
            double tmpTotalSuccess = tmpGetDataSuccessCount + tmpMissionSuccessCount;
            double tmpTotalAll = tmpGetDataAllCount + tmpMissionAllCount;
            TextBlockTotalSuccess.Text = tmpTotalSuccess.ToString();
            TextBlockTotalAll.Text = tmpTotalAll.ToString();
            double tmpTotalPercent;
            if (tmpTotalAll == 0)
            {
                tmpTotalPercent = 0;
            }
            else
            {
                tmpTotalPercent = tmpTotalSuccess / tmpTotalAll * 100;
            }
            TextBlockTotalPercent.Text = tmpTotalPercent.ToString("0.##") + "%";
        }

        private void CheckForTest(string _functionName, bool _successOrFail)
        {
            int tmpIndex = -1;
            for (int i = 0; i < ListBoxMachineMission.Items.Count; i++)
            {
                if (_functionName == (ListBoxMachineMission.Items[i] as MissionItem).functionName)
                {
                    tmpIndex = i;
                    break;
                }
            }
            if (tmpIndex < 0)
            {
                return;
            }
            if ((ListBoxMachineMission.Items[tmpIndex] as MissionItem).isGood == true)
            {
                return;
            }
            (ListBoxMachineMission.Items[tmpIndex] as MissionItem).SuccessOrFail(_successOrFail);
            int tmpGoodCount = 0;
            for (int i = 0; i < ListBoxMachineMission.Items.Count; i++)
            {
                if ((ListBoxMachineMission.Items[i] as MissionItem).isGood == true)
                {
                    tmpGoodCount++;
                }
            }
            TextBlockMissionAll.Text = ListBoxMachineMission.Items.Count.ToString();
            TextBlockMissionSuccess.Text = tmpGoodCount.ToString();
            CalTotal();
        }

        // TORUS에 연결 하는 함수입니다.
        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }
        private void ButtonConnectWith_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = ReadConfig(Constants.ConfigFileName, "Main", "LoadMapFileDirPath"),
                Filter = "xml files (*.xml)|*.xml"
            };
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string directoryPath = Path.GetDirectoryName(openFileDialog.FileName);
                WriteConfig(Constants.ConfigFileName, "Main", "LoadMapFileDirPath", directoryPath);
                Connect(openFileDialog.FileName);
            }
        }

        private void Connect(string mapFilePath = "")
        {
            if (_connectResult != 0)
            {
                // "TorusTester.info"에 기록된 "App ID"와 "App Name"을 사용합니다.
                try
                {
                    if (mapFilePath == "")
                    {
                        _connectResult = Api.Initialize(new Guid(Constants.AppID), Constants.AppName);
                    }
                    else
                    {
                        //connectResult_ = Api.Initialize(new Guid(Constants.AppID), Constants.AppName, mapFilePath);

                        string fileName = Path.GetFileName(mapFilePath);
                        string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        string destFilePath = Path.Combine(appDirectory, fileName);// 대상 파일 경로를 설정 (복사할 파일 이름을 유지)
                        try
                        {
                            // 소스 파일이 이미 애플리케이션 폴더에 있는지 확인
                            if (Path.GetFullPath(mapFilePath).Equals(Path.GetFullPath(destFilePath), StringComparison.OrdinalIgnoreCase))
                            {
                                Debug.WriteLine("소스 파일이 이미 애플리케이션 폴더에 있습니다. 복사를 건너뜁니다.");
                            }
                            else
                            {
                                File.Copy(mapFilePath, destFilePath, true); // 덮어쓰기 옵션 true
                                Debug.WriteLine("파일이 성공적으로 복사되었습니다.");
                            }
                            _connectResult = Api.Initialize(new Guid(Constants.AppID), Constants.AppName, fileName);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"오류 발생: {ex.Message}");
                        }
                    }
                }
                catch (System.DllNotFoundException ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "오류");
                    return;
                }
            }
            if (_connectResult == 0)
            {
                _connectedMachineList.Clear();
                ComboBoxMachieID.Items.Clear();
                ComboBoxTimeSeriesMachieID.Items.Clear();
                bool first = true;
                int result = Api.getMachinesInfo(out Item item);
                if (result == 0)
                {
                    MachinesObject machinesObject = JsonSerializer.Deserialize<MachinesObject>(item.ToString());
                    _connectedMachineList = [.. machinesObject.Value.OrderBy(item => item.ID)];
                    foreach (var machine in _connectedMachineList)
                    {
                        ComboBoxMachieID.Items.Add(machine.ID + ":" + machine.Name);
                        ComboBoxTimeSeriesMachieID.Items.Add(machine.ID + ":" + machine.Name);
                        if (first)
                        {
                            first = false;
                            TextBoxCommonFilter.Text = "machine=" + machine.ID;
                        }
                    }
                    ComboBoxMachieID.SelectedIndex = 0;
                    ComboBoxTimeSeriesMachieID.SelectedIndex = 0;
                    MakeMachineMonitoringList();
                }
                else
                {
                    System.Windows.MessageBox.Show("연결되어 있는 설비가 없습니다. 연결을 종료합니다.", "오류");
                    _connectResult = -1;
                }
            }
            else if (_connectResult == 546308133)
            {
                System.Windows.MessageBox.Show("TORUS 구동에 필요한 DLL 파일을 찾을 수 없거나 TORUS가 실행중이 아닙니다.\n오류코드: " + _connectResult, "오류");
            }
            else if (_connectResult == 548405285)
            {
                System.Windows.MessageBox.Show("오류코드: " + _connectResult, "오류");
            }
            else if (_connectResult == 548405249)
            {
                System.Windows.MessageBox.Show("오류코드: " + _connectResult, "오류");
            }
            else
            {
                System.Windows.MessageBox.Show("알 수 없는 오류 : " + _connectResult.ToString(), "오류");
            }

            if (_connectResult == 0)
            {
                ButtonConnect.IsEnabled = false;
                ButtonConnectWith.IsEnabled = false;
                ButtonSingleStart.IsEnabled = true;
                ButtonSingleStop.IsEnabled = false;
                ButtonMultiStart.IsEnabled = true;
                ButtonMultiStop.IsEnabled = false;
                ButtonMachineMonitoringStart.IsEnabled = true;
                ButtonMachineMonitoringStop.IsEnabled = false;
            }
        }

        // MachineStateModel을 불러오는 함수입니다.
        private void LoadMachineStateModel(string _filePath)
        {
            bool fileExists = File.Exists(_filePath);
            if (fileExists == false)
            {
                return;
            }
            string directoryPath = Path.GetDirectoryName(_filePath);
            WriteConfig(Constants.ConfigFileName, "Main", "LoadListFileDefaultDirPath", directoryPath);
            ListBoxMachineState.Items.Clear();
            MachineState.totalCount = 0;
            string[] contents = System.IO.File.ReadAllLines(_filePath);
            if (contents.Length > 0)
            {
                for (int i = 0; i < contents.Length; i++)
                {
                    string[] items = contents[i].Split('\t');
                    if (items.Length > 1)
                    {
                        if (items[1] != "")
                        {
                            if (items[1][^1..] == "&")//Substring(items[1].Length - 1)
                            {
                                items[1] = items[1][..^1];
                            }
                            if (items[1][..1] == "&")//Substring(0, 1)
                            {
                                items[1] = items[1][1..];
                            }
                        }
                    }
                    if (items.Length > 2)
                    {
                        ListBoxMachineState.Items.Add(new MachineState(items[0].Trim(), items[1].Trim(), items[2].Trim()));
                    }
                    else if (items.Length == 2)
                    {
                        ListBoxMachineState.Items.Add(new MachineState(items[0].Trim(), items[1].Trim(), ""));
                    }
                    else if (items.Length == 1)
                    {
                        ListBoxMachineState.Items.Add(new MachineState(items[0].Trim(), "", ""));
                    }
                    else
                    {
                        ListBoxMachineState.Items.Add(new MachineState("", "", ""));
                    }
                }
            }
            TextBlockAddressFilePath.Text = _filePath;
            _addressFilePath = _filePath;
            TextBlockMissionSuccess.Text = "0";
            TextBlockSingleAll.Text = ListBoxMachineState.Items.Count.ToString();
            TextBlockSinglePassedAll.Text = ListBoxMachineState.Items.Count.ToString();
            TextBlockMultiAll.Text = ListBoxMachineState.Items.Count.ToString();
            AddForTest(MachineState.totalCount);
            WriteConfig(Constants.ConfigFileName, "Main", "LastMachineStateModelFilePath", _filePath);
        }


        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = ReadConfig(Constants.ConfigFileName, "Main", "LoadListFileDefaultDirPath"),
                Filter = "txt files (*.txt)|*.txt"
            };
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadMachineStateModel(openFileDialog.FileName);
            }
        }

        private void ButtonNew_Click(object sender, RoutedEventArgs e)
        {
            ListBoxMachineState.Items.Clear();
            MachineState.totalCount = 0;
            TextBlockAddressFilePath.Text = "";
            _addressFilePath = "";
            TextBlockMissionSuccess.Text = "0";
            TextBlockSingleAll.Text = "0";
            TextBlockSinglePassedAll.Text = "0";
            TextBlockMultiAll.Text = "0";
            AddForTest(MachineState.totalCount);
        }

        void SingleStart(bool check)
        {
            SetTimeout();
            if (ListBoxMachineState.Items.Count < 1)
            {
                return;
            }
            _isSingleRunning = true;
            ComboBoxSingleDirect.IsEnabled = false;
            TextBoxTimeout.IsReadOnly = true;
            TextBoxSingleGetDataTargetCount.IsReadOnly = true;
            TextBoxCommonFilter.IsReadOnly = true;
            ButtonSingleStart.IsEnabled = false;
            ButtonLoad.IsEnabled = false;
            _commonFilter = TextBoxCommonFilter.Text.Trim();
            if (_commonFilter != "")
            {
                if (_commonFilter.Substring(_commonFilter.Length - 1, 1) == "&")
                {
                    _commonFilter = _commonFilter[..^1]; //Substring(0, commonFilter_.Length - 1)
                }
                if (_commonFilter[..1] == "&")
                {
                    _commonFilter = _commonFilter.Substring(1, _commonFilter.Length);
                }
            }
            _threadSignlestopFlag = false;
            _threadSingle = new Thread(() => ThreadSingle(check))
            {
                IsBackground = true
            };
            _threadSingle.Start();
            ButtonSingleStop.IsEnabled = true;
        }

        private void ThreadSingle(bool check)
        {
            long tmpTotalTime = 0;
            long tmpOneTurnTime = 0;
            bool tmpCountCheck = true;
            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                TextBlockSinglePercent.Text = "%";
                TextBlockSingleSuccess.Text = "0";
                TextBlockSinglePassed.Text = "0";
                TextBoxSingleThreadResult.Text = "";
                TextBlockSingleGetDataCurrentCount.Text = "0";
            }));
            int tmpGetDataCurrentCount = 0;
            int tmpGetDataTargetCount = 0;
            try
            {
                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    tmpGetDataTargetCount = Convert.ToInt32(TextBoxSingleGetDataTargetCount.Text);
                }));
            }
            catch
            {
                tmpGetDataTargetCount = 0;
            }
            if (tmpGetDataTargetCount <= 0)
            {
                tmpGetDataTargetCount = 0;
                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    TextBoxSingleGetDataTargetCount.Text = "0";
                }));
                tmpCountCheck = false;
            }
            int timeLineCount = 0;
            int tmpProcessCount = 0;
            int tmpErrCount = 0;
            double tmpSuccessCount = 0;
            int tmpTotalCount = ListBoxMachineState.Items.Count;
            string tmpAddress = "";
            string tmpFilter = "";
            int tmpResult = -1;
            string tmpErrorMessage = "";
            string tmpErrorCode = "";
            double tmpAllCount = 0;
            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                tmpAllCount = Convert.ToInt32(TextBlockSingleAll.Text);
            }));
            Stopwatch stopwatch = new();
            while (!_threadSignlestopFlag)
            {
                tmpOneTurnTime = 0;
                tmpProcessCount = 0;
                tmpErrCount = 0;
                tmpSuccessCount = 0;
                tmpTotalCount = ListBoxMachineState.Items.Count;
                for (int i = 0; i < tmpTotalCount; i++)
                {
                    tmpProcessCount++;
                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        (ListBoxMachineState.Items[i] as MachineState).DrawColorMan(80, 166, 255, 50);
                        TextBlockSinglePassed.Text = tmpProcessCount.ToString();
                    }));
                    tmpAddress = (ListBoxMachineState.Items[i] as MachineState).address;
                    tmpFilter = (ListBoxMachineState.Items[i] as MachineState).filter;
                    if (_commonFilter != "")
                    {
                        if (tmpFilter != "")
                        {
                            tmpFilter = _commonFilter + "&" + tmpFilter;
                        }
                        else
                        {
                            tmpFilter = _commonFilter;
                        }
                    }
                    stopwatch.Restart();
                    tmpResult = Api.getData(tmpAddress, tmpFilter, out Item item, _singleDirect, _timeout);
                    stopwatch.Stop();
                    lock (_lock)
                    {
                        if (tmpResult == 0)
                        {
                            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                (ListBoxMachineState.Items[i] as MachineState).InsertResult(item.ToString());
                                (ListBoxMachineState.Items[i] as MachineState).InsertResultDescribe(MakeSuccessMessage(item));
                                (ListBoxMachineState.Items[i] as MachineState).DrawColorMan(100, 103, 153, 255);
                                (ListBoxMachineState.Items[i] as MachineState).InsertSingleTime("Single 성공: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                            }));
                            if (true)
                            {
                                if (item == null)
                                {
                                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                    {
                                        MessageBox.Show("null 반환 발생", "Single 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }));
                                    Debug.WriteLine("null 반환 발생");
                                }
                                string returnAddress = item.GetValueString("address");
                                string returnFilter = item.GetValueString("filter");
                                if (!string.Equals(tmpAddress, returnAddress, StringComparison.OrdinalIgnoreCase) || !string.Equals(tmpFilter, returnFilter, StringComparison.OrdinalIgnoreCase))
                                {
                                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                    {
                                        MessageBox.Show("불일치 발생\n" + tmpAddress + "\n" +
                                            returnAddress + "\n" + tmpFilter + "\n" + returnFilter, "Single 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }));
                                    Debug.WriteLine("불일치 발생\n" + tmpAddress + "\n" + returnAddress + "\n" + tmpFilter + "\n" + returnFilter);
                                }
                            }
                            tmpSuccessCount++;
                        }
                        else
                        {
                            tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                (ListBoxMachineState.Items[i] as MachineState).InsertResult(tmpErrorCode);
                                (ListBoxMachineState.Items[i] as MachineState).InsertResultDescribe(tmpErrorMessage);
                                (ListBoxMachineState.Items[i] as MachineState).DrawColorMan(30, 255, 100, 100);
                                (ListBoxMachineState.Items[i] as MachineState).InsertSingleTime("Single 실패: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                            }));
                            tmpErrCount++;
                        }
                    }
                    Thread.Sleep(1);
                    tmpOneTurnTime += stopwatch.ElapsedMilliseconds;
                    if (_threadSignlestopFlag)
                    {
                        break;
                    }
                }
                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    TextBlockSingleSuccess.Text = tmpSuccessCount.ToString();
                    TextBlockSinglePercent.Text = (tmpSuccessCount / tmpAllCount * 100).ToString("0.##") + "%";
                }));
                if (tmpCountCheck)
                {
                    tmpTotalTime += tmpOneTurnTime;
                    tmpGetDataCurrentCount++;
                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        TextBlockSingleGetDataCurrentCount.Text = tmpGetDataCurrentCount.ToString();
                        TextBoxSingleThreadResult.Text = tmpGetDataCurrentCount.ToString() + "번째 : " + tmpOneTurnTime.ToString() + "ms" + "\n" + TextBoxSingleThreadResult.Text;
                    }));
                    if (tmpGetDataCurrentCount >= tmpGetDataTargetCount)
                    {
                        _threadSignlestopFlag = true;
                    }
                }
                else
                {
                    if (timeLineCount >= 13)
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            TextBoxSingleThreadResult.Text = tmpOneTurnTime.ToString() + "ms\n" + TextBoxSingleThreadResult.Text[..TextBoxSingleThreadResult.Text.LastIndexOf('\n')];
                        }));
                    }
                    else
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            if (TextBoxSingleThreadResult.Text == "")
                            {
                                TextBoxSingleThreadResult.Text = tmpOneTurnTime.ToString() + "ms";
                            }
                            else
                            {
                                TextBoxSingleThreadResult.Text = tmpOneTurnTime.ToString() + "ms\n" + TextBoxSingleThreadResult.Text;
                            }
                        }));
                        timeLineCount++;
                    }
                }
            }
            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (tmpCountCheck == true)
                {
                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        TextBoxSingleThreadResult.Text = "Total : " + tmpTotalTime.ToString() + "ms" + "\n" + TextBoxSingleThreadResult.Text;
                    }));
                }
                ButtonSingleStart.IsEnabled = true;
                ButtonSingleStop.IsEnabled = false;
                TextBoxSingleGetDataTargetCount.IsReadOnly = false;
                ComboBoxSingleDirect.IsEnabled = true;
                _isSingleRunning = false;
                if (_isSingleRunning == false && _isMultiRunning == false)
                {
                    ButtonLoad.IsEnabled = true;
                    TextBoxCommonFilter.IsReadOnly = false;
                    TextBoxTimeout.IsReadOnly = false;
                }
            }));
        }

        public void OneGetData(int _index)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            (ListBoxMachineState.Items[_index] as MachineState).ButtonOneGetData.IsEnabled = false;
            Stopwatch stopwatch = new();
            (ListBoxMachineState.Items[_index] as MachineState).DrawColorMan(80, 166, 255, 50);
            string tmpcommonFilter = TextBoxCommonFilter.Text.Trim();
            string tmpAddress = (ListBoxMachineState.Items[_index] as MachineState).address;
            string tmpFilter = (ListBoxMachineState.Items[_index] as MachineState).filter;
            if (tmpAddress.StartsWith("data://machine/"))
            {
                if (tmpcommonFilter != "")
                {
                    if (tmpFilter != "")
                    {
                        tmpFilter = tmpcommonFilter + "&" + tmpFilter;
                    }
                    else
                    {
                        tmpFilter = tmpcommonFilter;
                    }
                }
            }
            stopwatch.Restart();
            int tmpResult = Api.getData(tmpAddress, tmpFilter, out Item item, true, _timeout);//하나만 실행
            stopwatch.Stop();
            lock (_lock)
            {
                if (tmpResult == 0)
                {
                    (ListBoxMachineState.Items[_index] as MachineState).InsertResult(item.ToString());
                    (ListBoxMachineState.Items[_index] as MachineState).InsertResultDescribe(MakeSuccessMessage(item));
                    (ListBoxMachineState.Items[_index] as MachineState).DrawColorMan(100, 103, 153, 255);
                    (ListBoxMachineState.Items[_index] as MachineState).InsertSingleTime("성공: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                }
                else
                {
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    (ListBoxMachineState.Items[_index] as MachineState).InsertResult(tmpErrorCode);
                    (ListBoxMachineState.Items[_index] as MachineState).InsertResultDescribe(tmpErrorMessage);
                    (ListBoxMachineState.Items[_index] as MachineState).DrawColorMan(30, 255, 100, 100);
                    (ListBoxMachineState.Items[_index] as MachineState).InsertSingleTime("실패: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                }
            }
            (ListBoxMachineState.Items[_index] as MachineState).ButtonOneGetData.IsEnabled = true;
        }

        public void UpdateData(int _index)
        {
            SetTimeout();
            string inputOriginalData = (ListBoxMachineState.Items[_index] as MachineState).TextBoxInputData.Text;
            string inputData = inputOriginalData;
            if (inputData == "")
            {
                return;
            }
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            if (!inputData.Contains('[') && !inputData.Contains(']'))
            {
                inputData = "{\"value\":[" + inputData + "]}";
            }
            Item InItem;
            string tmpAddress = (ListBoxMachineState.Items[_index] as MachineState).address;
            string tmpFilter = (ListBoxMachineState.Items[_index] as MachineState).filter;
            string tmpcommonFilter = TextBoxCommonFilter.Text.Trim();
            if (tmpcommonFilter != "")
            {
                if (tmpFilter != "")
                {
                    tmpFilter = tmpcommonFilter + "&" + tmpFilter;
                }
                else
                {
                    tmpFilter = tmpcommonFilter;
                }
            }
            InItem = Item.Parse(inputData);
            if (InItem == null)
            {
                string[] tokens = inputOriginalData.Split(',');

                List<double> values = new List<double>();
                foreach (string token in tokens)
                {
                    if (double.TryParse(token.Trim(), out double val))
                    {
                        values.Add(val);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("잘못된 형식입니다.", "오류");
                        return;
                    }
                }
                if (values.Count > 0)
                {
                    InItem = Item.MakeItem("value", values.ToArray());
                }
                else
                {
                    System.Windows.MessageBox.Show("잘못된 형식입니다.", "오류");
                    return;
                }
                if (InItem == null)
                {
                    System.Windows.MessageBox.Show("잘못된 형식입니다.", "오류");
                    return;
                }
            }
            int tmpResult = Api.updateData(tmpAddress, tmpFilter, InItem, out _, _timeout);
            if (tmpResult == 0)
            {
                System.Windows.MessageBox.Show("변경에 성공했습니다.", "성공");
            }
            else
            {
                string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                System.Windows.MessageBox.Show("변경 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        static string MakeErrorMessage(int _errCode, out string _errCodeString, string _errCode16Str = "")
        {
            string result;
            if (_errCode16Str == "")
            {
                result = Convert.ToString(_errCode, 16);
                result = result.ToUpper();
                result = "0x" + result;
            }
            else
            {
                result = _errCode16Str;
                result = result.Replace("error code", "");
                result = result.Replace("[", "");
                result = result.Replace("]", "");
                result = result.Replace(" ", "");
            }
            if (result == "0x21B00000")
            {
                _errCodeString = "NC_ERR_EXCEPTION";
            }
            else if (result == "0x21B10000")
            {
                _errCodeString = "NC_ERR_INIT";
            }
            else if (result == "0x21B20000")
            {
                _errCodeString = "NC_ERR_NO_INIT_VALUE";
            }
            else if (result == "0x21B30000")
            {
                _errCodeString = "NC_ERR_NO_DEFINE_NUMBER_IN_CODE";
            }
            else if (result == "0x21B40000")
            {
                _errCodeString = "NC_ERR_NO_DEFINE_NUMBER_IN_FUNCTION";
            }
            else if (result == "0x21B50000")
            {
                _errCodeString = "NC_ERR_DUPLICATED_WRITER";
            }
            else if (result == "0x21B60000")
            {
                _errCodeString = "NC_ERR_CONNET_FAIL";
            }
            else if (result == "0x21B61000")
            {
                _errCodeString = "NC_ERR_NO_MACHINE";
            }
            else if (result == "0x21B62000")
            {
                _errCodeString = "NC_ERR_NO_CONNET_TRY";
            }
            else if (result == "0x21B70000")
            {
                _errCodeString = "NC_ERR_NO_FUNCTION";
            }
            else if (result == "0x21B80000")
            {
                _errCodeString = "NC_ERR_NO_OPTION";
            }
            else if (result == "0x21B81000")
            {
                _errCodeString = "NC_ERR_NO_DLL";
            }
            else if (result == "0x21B82000")
            {
                _errCodeString = "NC_ERR_NO_HANDLE";
            }
            else if (result == "0x21B83000")
            {
                _errCodeString = "NC_ERR_NO_ACTIVE_TOOL_OR_TOOL_GROUP";
            }
            else if (result == "0x21B84000")
            {
                _errCodeString = "NC_ERR_WRONG_TOOL_MODE";
            }
            else if (result == "0x21B85000")
            {
                _errCodeString = "NC_ERR_NO_TOOL_GROUP";
            }
            else if (result == "0x21B86000")
            {
                _errCodeString = "NC_ERR_NO_SELECETD_FILE";
            }
            else if (result == "0x21B90000")
            {
                _errCodeString = "NC_ERR_INVALID_WRITE_VALUE";
            }
            else if (result == "0x21B91000")
            {
                _errCodeString = "NC_ERR_WRONG_WRITE_VALUE_LIST_COUNT";
            }
            else if (result == "0x21B92000")
            {
                _errCodeString = "NC_ERR_INAPPROPRIATE_STATUS";
            }
            else if (result == "0x21BA0000")
            {
                _errCodeString = "NC_ERR_WRONG_FILTER_VALUE";
            }
            else if (result == "0x21BB0000")
            {
                _errCodeString = "NC_ERR_UNKNOWN";
            }
            else if (result == "0x21BC1000")
            {
                _errCodeString = "NC_ERR_NO_OBJECT_IN_NC";
            }
            else if (result == "0x21BC1100")
            {
                _errCodeString = "NC_ERR_NO_OBJECT_IN_LOCAL";
            }
            else if (result == "0x21BC2000")
            {
                _errCodeString = "NC_ERR_OBJECT_AREADY_EXIST_IN_NC";
            }
            else if (result == "0x21BC2100")
            {
                _errCodeString = "NC_ERR_OBJECT_AREADY_EXIST_IN_LOCAL";
            }
            else if (result == "0x21BC3000")
            {
                _errCodeString = "NC_ERR_FAIL_CREATE_OBJECT_IN_NC";
            }
            else if (result == "0x21BC3100")
            {
                _errCodeString = "NC_ERR_FAIL_CREATE_OBJECT_IN_LOCAL";
            }
            else if (result == "0x21BC4000")
            {
                _errCodeString = "NC_ERR_OBJECT_IS_USED_IN_NC";
            }
            else if (result == "0x21BC4100")
            {
                _errCodeString = "NC_ERR_OBJECT_IS_USED_IN_LOCAL";
            }
            else if (result == "0x21BC5000")
            {
                _errCodeString = "NC_ERR_STORAGE_SHORTAGE_IN_NC";
            }
            else if (result == "0x21BC6000")
            {
                _errCodeString = "NC_ERR_WRONG_NC_FILE";
            }
            else if (result == "0x21BC6100")
            {
                _errCodeString = "NC_ERR_WRONG_NAME_OF_NC_FILE";
            }
            else if (result == "0x21BC7000")
            {
                _errCodeString = "NC_ERR_FAIL_OPEN_OBJECT_IN_NC";
            }
            else if (result == "0x21BC7100")
            {
                _errCodeString = "NC_ERR_FAIL_OPEN_OBJECT_IN_LOCAL";
            }
            else if (result == "0x21BD0000")
            {
                _errCodeString = "NC_ERR_WRONG_RETURN_DATA_TYPE";
            }
            else if (result == "0x21BE0000")
            {
                _errCodeString = "NC_ERR_NULL_VALUE";
            }
            else if (result == "0X20206028")
            {
                _errCodeString = "MgrCommunication Address 또는 Filter 오류";
            }
            else if (result == "0X2150001C")
            {
                _errCodeString = "LibRpcClient Connect 오류";
            }
            else if (result == "0x2150002F")
            {
                _errCodeString = "LibRpcClient TimeOut 오류";
            }
            else
            {
                _errCodeString = "매뉴얼의 에러코드를 참조하십시오.";
            }
            return result + " (" + _errCode + ")";
        }

        private string MakeObjectInfo(string _fullPath, int _machinID, out bool _isDir)
        {
            string totalInfo = "";
            int result;
            _isDir = false;
            result = Api.getAttributeExists(_fullPath, out Item item, _machinID, _userApiTimeout);
            if (result == 0)
            {
                bool value = item.GetValueBoolean("value");
                if (value)
                {
                    totalInfo += "exists|";
                }
                else
                {
                    totalInfo += "notExists|";
                }
                CheckForTest("getAttributeExists", true);
            }
            else
            {
                totalInfo += "exists:ERROR|";
                CheckForTest("getAttributeExists", false);
            }
            result = Api.getAttributeType(_fullPath, out item, _machinID, _userApiTimeout);
            if (result == 0)
            {
                if (item.GetValueInt("value") == 0)
                {
                    totalInfo += "DIR|";
                    _isDir = true;
                }
                else if (item.GetValueInt("value") == 1)
                {
                    totalInfo += "FILE|";
                    _isDir = false;
                }
                else if (item.GetValueInt("value") == 2)
                {
                    // 해당 값이 2이면 같은 이름의 폴더와 파일이 같은 폴더내에 있다는 의미입니다. 그러나 경로명으로 구분이 가능합니다.
                    // Fanuc의 경우 동일한 이름의 폴더와 파일이 같은 경로에 존재할 수 있습니다. 
                    // getFileList와 getFileListEx는 항목이 폴더인 경우에는 경로 끝에 '/'를 표시합니다. 이것을 이용해 폴더와 파일을 구분할 수 있습니다.
                    if (_fullPath[^1..] == "/")// == _fullPath.Substring(_fullPath.Length - 1)
                    {
                        totalInfo += "DIR|";
                        _isDir = true;
                    }
                    else
                    {
                        totalInfo += "FILE|";
                        _isDir = false;
                    }
                }
                CheckForTest("getAttributeType", true);
            }
            else
            {
                totalInfo += "type:ERROR|";
                CheckForTest("getAttributeType", false);
            }
            result = Api.getAttributeIsNc(_fullPath, out item, _machinID, _userApiTimeout);
            if (result == 0)
            {
                bool value = item.GetValueBoolean("value");
                if (value)
                {
                    totalInfo += "NCfile|";
                }
                else
                {
                    totalInfo += "notNCfile|";
                }
                CheckForTest("getAttributeIsNc", true);
            }
            else
            {
                totalInfo += "isNC:ERROR|";
                CheckForTest("getAttributeIsNc", false);
            }
            result = Api.getAttributeLogicalPath(_fullPath, out item, _machinID, _userApiTimeout);
            if (result == 0)
            {
                totalInfo = totalInfo + item.GetValueString("value") + "|";
                CheckForTest("getAttributeLogicalPath", true);
            }
            else
            {
                totalInfo += "logicalPath:ERROR|";
                CheckForTest("getAttributeLogicalPath", false);
            }
            result = Api.getAttributeName(_fullPath, out item, _machinID, _userApiTimeout);
            if (result == 0)
            {
                totalInfo = totalInfo + item.GetValueString("value") + "|";
                CheckForTest("getAttributeName", true);
            }
            else
            {
                totalInfo += "name:ERROR|";
                CheckForTest("getAttributeName", false);
            }
            result = Api.getAttributePath(_fullPath, out item, _machinID, _userApiTimeout);
            if (result == 0)
            {
                totalInfo = totalInfo + item.GetValueString("value") + "|";
                CheckForTest("getAttributePath", true);
            }
            else
            {
                totalInfo += "path:ERROR|";
                CheckForTest("getAttributePath", false);
            }
            result = Api.getAttributeSize(_fullPath, out item, _machinID, _userApiTimeout);
            if (result == 0)
            {
                totalInfo = totalInfo + item.GetValueInt("value").ToString() + "|";
                CheckForTest("getAttributeSize", true);
            }
            else
            {
                totalInfo += "size:ERROR|";
                CheckForTest("getAttributeSize", false);
            }
            result = Api.getAttributeEditedTime(_fullPath, out item, _machinID, _userApiTimeout);
            if (result == 0)
            {
                totalInfo = totalInfo + item.GetValueString("value") + "|";
                CheckForTest("getAttributeEditedTime", true);
            }
            else
            {
                totalInfo += "time:ERROR|";
                CheckForTest("getAttributeEditedTime", false);
            }
            return totalInfo;
        }

        private void ShowFileList(string _path)
        {
            SetTimeout();
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            string[] addressArray = new string[3];
            string[] filter_Array = new string[3];
            addressArray[0] = "data://machine/ncmemory/totalcapacity";
            filter_Array[0] = "machine=" + tmpMachineID.ToString();
            addressArray[1] = "data://machine/ncmemory/usedcapacity";
            filter_Array[1] = "machine=" + tmpMachineID.ToString();
            addressArray[2] = "data://machine/ncmemory/freecapacity";
            filter_Array[2] = "machine=" + tmpMachineID.ToString();
            int result = Api.getData(addressArray[0], filter_Array[0], out Item itemTotalcapacity, true, _userApiTimeout);
            if (result == 558891055)
            {
                TextBlockMemoryTotal.Text = "Timeout";
            }
            else if (result != 0 || itemTotalcapacity == null)
            {
                TextBlockMemoryTotal.Text = "오류";
            }
            else
            {
                TextBlockMemoryTotal.Text = itemTotalcapacity.GetValueString("value");
            }
            result = Api.getData(addressArray[1], filter_Array[1], out Item itemUsedcapacity, true, _userApiTimeout);
            if (result == 558891055)
            {
                TextBlockMemoryUsed.Text = "Timeout";
            }
            else if (result != 0 || itemUsedcapacity == null)
            {
                TextBlockMemoryUsed.Text = "오류";
            }
            else
            {
                TextBlockMemoryUsed.Text = itemUsedcapacity.GetValueString("value");
            }
            result = Api.getData(addressArray[2], filter_Array[2], out Item itemFreecapacity, true, _userApiTimeout);
            if (result == 558891055)
            {
                TextBlockMemoryFree.Text = "Timeout";
            }
            else if (result != 0 || itemFreecapacity == null)
            {
                TextBlockMemoryFree.Text = "오류";
            }
            else
            {
                TextBlockMemoryFree.Text = itemFreecapacity.GetValueString("value");
            }
            Item item;
            int tmpResult;
            ListBoxFileList.Items.Clear();
            string tmpCurrentPath = _path;
            if (tmpCurrentPath == "")
            {
                return;
            }
            TextBoxCurrentPath.Text = tmpCurrentPath;
            if (CheckBoxShowDetail.IsChecked == false)
            {
                tmpResult = Api.getFileListEx(tmpCurrentPath, out item, tmpMachineID, _userApiTimeout);
                if (tmpResult == 0)
                {
                    CheckForTest("getFileListEx", true);
                    if (item != null)
                    {
                        FilesObject filesObject = JsonSerializer.Deserialize<FilesObject>(item.ToString());
                        List<FileObject> FileList = [.. filesObject.Value.OrderByDescending(file => file.IsDir).ThenBy(file => file.Name)];
                        foreach (FileObject file in FileList)
                        {
                            if (DateTime.TryParse(file.Datetime, out DateTime datetime))
                            {
                                ListBoxFileList.Items.Add(new FileItem(file.Name, "size:" + file.Size + ", time:" + datetime.ToString("yyyy-MM-ddTHH:mm:ss"), file.IsDir));
                            }
                            else
                            {
                                ListBoxFileList.Items.Add(new FileItem(file.Name, "size:" + file.Size + ", time:" + file.Datetime, file.IsDir));
                            }
                        }
                    }
                }
                else
                {
                    CheckForTest("getFileListEx", false);
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    System.Windows.MessageBox.Show("파일 리스트 조회 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else
            {
                tmpResult = Api.getFileList(tmpCurrentPath, out item, tmpMachineID, _userApiTimeout);
                if (tmpResult == 0)
                {
                    CheckForTest("getFileList", true);
                    if (item != null)
                    {
                        string[] tmpFileList = item.GetArrayString("value");
                        for (int i = 0; i < tmpFileList.Length; i++)
                        {
                            string tmpOneItem = tmpFileList[i];
                            if (tmpOneItem != "")
                            {
                                string tmpTotalInfo = MakeObjectInfo(tmpCurrentPath + tmpOneItem, tmpMachineID, out bool tmpIsDir);
                                ListBoxFileList.Items.Add(new FileItem(tmpOneItem, tmpTotalInfo, tmpIsDir));
                            }
                        }
                    }
                }
                else
                {
                    CheckForTest("getFileList", false);
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    System.Windows.MessageBox.Show("파일 리스트 조회 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
        }

        private void ButtonMakeNewFile_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            string tmpNewObjectName = TextBoxNewObjectName.Text;
            if (tmpCurrentPath == "" || tmpNewObjectName == "")
            {
                System.Windows.MessageBox.Show("새로운 파일의 이름을 입력해주세요.");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            int result = Api.CreateCNCFile(tmpCurrentPath, tmpNewObjectName, out _, tmpMachineID, _userApiTimeout);
            if (result == 0)
            {
                CheckForTest("CreateCNCFile", true);
                System.Windows.MessageBox.Show("파일 생성 성공");
                ShowFileList(TextBoxCurrentPath.Text);
            }
            else
            {
                CheckForTest("CreateCNCFile", false);
                string tmpErrorCode = MakeErrorMessage(result, out string tmpErrorMessage);
                System.Windows.MessageBox.Show("파일 생성 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        private void ButtonMakeNewDir_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            string tmpNewObjectName = TextBoxNewObjectName.Text;
            if (tmpCurrentPath == "" || tmpNewObjectName == "")
            {
                System.Windows.MessageBox.Show("새로운 폴더의 이름을 입력해주세요.");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            int result = Api.CreateCNCFolder(tmpCurrentPath, tmpNewObjectName, out _, tmpMachineID, _userApiTimeout);
            if (result == 0)
            {
                CheckForTest("CreateCNCFolder", true);
                System.Windows.MessageBox.Show("폴더 생성 성공");
                ShowFileList(TextBoxCurrentPath.Text);
            }
            else
            {
                CheckForTest("CreateCNCFolder", false);
                string tmpErrorCode = MakeErrorMessage(result, out string tmpErrorMessage);
                System.Windows.MessageBox.Show("폴더 생성 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        public void ActualObjectRename(string _oldName, string _newName)
        {
            SetTimeout();
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            if (tmpCurrentPath == "")
            {
                System.Windows.MessageBox.Show("경로를 확인하시기 바랍니다.");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            Item item;
            int result = Api.CNCFileRename(tmpCurrentPath + _oldName, _newName, out item, tmpMachineID, _userApiTimeout);
            if (result == 0)
            {
                CheckForTest("CNCFileRename", true);
                System.Windows.MessageBox.Show("이름 변경 성공");
                ShowFileList(TextBoxCurrentPath.Text);
            }
            else
            {
                CheckForTest("CNCFileRename", false);
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
                System.Windows.MessageBox.Show("이름 변경 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        private void ButtonCopyOrMove_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_signalCopyOrMove == 0) // 0은 초기화, 1은 복사, 2는 이동
            {
                System.Windows.MessageBox.Show("복사 혹은 이동할 파일을 지정하지 않았습니다.");
                return;
            }
            else if (_signalCopyOrMove == 1) //(1)복사
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("현재 폴더로 파일을 복사 하시겠습니까?", "복사", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            else //(2)이동
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("현재 폴더로 파일을 이동 하시겠습니까?", "이동", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            string tmpCopyOrMove = TextBoxCopyOrMove.Text;
            if (tmpCurrentPath == "" || tmpCopyOrMove == "")
            {
                System.Windows.MessageBox.Show("경로를 확인하시기 바랍니다.");
                return;
            }
            string fileName = tmpCopyOrMove.Substring(tmpCopyOrMove.LastIndexOf('/') + 1);
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            if (_signalCopyOrMove == 1)
            {
                Item item;
                int result = Api.CNCFileCopy(tmpCopyOrMove, tmpCurrentPath + fileName, out item, tmpMachineID, _userApiTimeout);
                if (result == 0)
                {
                    CheckForTest("CNCFileCopy", true);
                    System.Windows.MessageBox.Show("복사 성공");
                }
                else
                {
                    CheckForTest("CNCFileCopy", false);
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
                    System.Windows.MessageBox.Show("복사 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else if (_signalCopyOrMove == 2)
            {
                Item item;
                int result = Api.CNCFileMove(tmpCopyOrMove, tmpCurrentPath + fileName, out item, tmpMachineID, _userApiTimeout);
                if (result == 0)
                {
                    CheckForTest("CNCFileMove", true);
                    System.Windows.MessageBox.Show("이동 성공");
                    _signalCopyOrMove = 0;
                    TextBoxCopyOrMove.Text = "";
                }
                else
                {
                    CheckForTest("CNCFileMove", false);
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
                    System.Windows.MessageBox.Show("이동 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            ShowFileList(tmpCurrentPath);
        }

        public void DeleteOneItem(string _objectName, bool _isDir)
        {
            SetTimeout();
            if (System.Windows.MessageBox.Show("삭제하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            string tmpObjectName = _objectName;
            if (tmpCurrentPath == "" || tmpObjectName == "")
            {
                System.Windows.MessageBox.Show("경로를 확인하시기 바랍니다.");
                return;
            }
            tmpObjectName = tmpCurrentPath + tmpObjectName;
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            Item item;
            int result = Api.CNCFileDelete(tmpObjectName, out item, tmpMachineID, _userApiTimeout);
            if (result == 0)
            {
                CheckForTest("CNCFileDelete", true);
                System.Windows.MessageBox.Show("삭제 성공");
                ShowFileList(TextBoxCurrentPath.Text);
            }
            else
            {
                CheckForTest("CNCFileDelete", false);
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
                System.Windows.MessageBox.Show("삭제 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        private void ButtonDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            if (tmpCurrentPath == "")
            {
                System.Windows.MessageBox.Show("경로를 확인하시기 바랍니다.");
                return;
            }
            if (System.Windows.MessageBox.Show("현재 경로의 모든 항목을 삭제하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            Item item;
            int result = Api.CNCFileDeleteAll(tmpCurrentPath, out item, tmpMachineID, _userApiTimeout);
            if (result == 0)
            {
                CheckForTest("CNCFileDeleteAll", true);
                System.Windows.MessageBox.Show("하위 항목 삭제 성공");
            }
            else
            {
                CheckForTest("CNCFileDeleteAll", false);
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
                System.Windows.MessageBox.Show("하위 항목 삭제 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
            ShowFileList(TextBoxCurrentPath.Text);
        }

        public void ExcuteOneItem(string _objectName)
        {
            SetTimeout();
            if (System.Windows.MessageBox.Show("해당 파일을 Excute하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            string tmpObjectName = _objectName;
            if (tmpCurrentPath == "" || tmpObjectName == "")
            {
                System.Windows.MessageBox.Show("경로를 확인하시기 바랍니다.");
                return;
            }
            tmpObjectName = tmpCurrentPath + tmpObjectName;
            int tmpChannel;
            try
            {
                tmpChannel = Convert.ToInt32(TextBoxChannel.Text.ToString());
            }
            catch
            {
                System.Windows.MessageBox.Show("채널을 확인하시기 바랍니다.");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            Item item;
            int result = Api.CNCFileExecute(tmpChannel, tmpObjectName, out item, tmpMachineID, _userApiTimeout);
            if (result == 0)
            {
                CheckForTest("CNCFileExecute", true);
                System.Windows.MessageBox.Show("Excute 지정 성공");
            }
            else
            {
                CheckForTest("CNCFileExecute", false);
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
                System.Windows.MessageBox.Show("Excute 지정 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        private void ButtonGetExecuteExtern_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            int tmpChannel;
            try
            {
                tmpChannel = Convert.ToInt32(TextBoxChannel.Text.ToString());
            }
            catch
            {
                System.Windows.MessageBox.Show("채널을 확인하시기 바랍니다.");
                return;
            }
            string tmpPath = TextBoxExecuteExternPath.Text.ToString();
            Item tmpItem;
            int tmpResult = Api.CNCFileExecuteExtern(tmpChannel, tmpPath, out tmpItem, tmpMachineID, _userApiTimeout);
            if (tmpResult == 0 && tmpItem != null)
            {
                string tmpString = tmpItem.ToString();
                TextBoxExecuteExternResult.Text = tmpString.Replace("\r\n", "");
                CheckForTest("CNCFileExecuteExtern", true);
                System.Windows.MessageBox.Show("ExecuteExtern 성공");
            }
            else
            {
                CheckForTest("CNCFileExecuteExtern", false);
                TextBoxExecuteExternResult.Text = "";
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                System.Windows.MessageBox.Show("ExecuteExtern 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        private void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            if (tmpCurrentPath.Trim() == "")
            {
                System.Windows.MessageBox.Show("경로를 확인하시기 바랍니다.");
                return;
            }
            if (tmpCurrentPath.Substring(tmpCurrentPath.Length - 1) != "/" && tmpCurrentPath.Substring(tmpCurrentPath.Length - 1) != "\\")
            {
                tmpCurrentPath += "/";
                TextBoxCurrentPath.Text = tmpCurrentPath;
            }
            string tmpUploadFile = TextBoxUpload.Text;
            if (tmpUploadFile.Trim() == "")
            {
                System.Windows.MessageBox.Show("업로드 파일을 확인하시기 바랍니다.");
                return;
            }
            int tmpChannel;
            try
            {
                tmpChannel = Convert.ToInt32(TextBoxChannel.Text.ToString());
            }
            catch
            {
                System.Windows.MessageBox.Show("채널을 확인하시기 바랍니다.");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            int result = Api.UploadFile(tmpUploadFile, tmpCurrentPath, tmpMachineID, tmpChannel, _userApiTimeout);
            if (result == 0)
            {
                CheckForTest("uploadFile", true);
                System.Windows.MessageBox.Show("업로드 성공");
            }
            else
            {
                CheckForTest("uploadFile", false);
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
                System.Windows.MessageBox.Show("업로드 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
            ShowFileList(TextBoxCurrentPath.Text);
        }

        public void IntoDir(string _objectName, bool _isDir)
        {
            SetTimeout();
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            string tmpObjectName = _objectName;
            if (tmpCurrentPath == "" || tmpObjectName == "")
            {
                System.Windows.MessageBox.Show("경로를 확인하시기 바랍니다.");
                return;
            }
            tmpObjectName = tmpCurrentPath + tmpObjectName;
            if (_isDir)
            {
                ShowFileList(tmpObjectName);
            }
            else
            {
                int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());

                int tmpChannel;
                try
                {
                    tmpChannel = Convert.ToInt32(TextBoxChannel.Text.ToString());
                }
                catch
                {
                    System.Windows.MessageBox.Show("채널을 확인하시기 바랍니다.");
                    return;
                }
                string localPath = TextBoxDownload.Text;
                if (localPath.Trim() == "")
                {
                    System.Windows.MessageBox.Show("다운로드 폴더 경로를 확인하시기 바랍니다.");
                    return;
                }
                if (localPath.Substring(localPath.Length - 1) != "/" && localPath.Substring(localPath.Length - 1) != "\\")
                {
                    if (localPath.Contains('\\'))
                    {
                        localPath += "\\";
                    }
                    else
                    {
                        localPath += "/";
                    }
                    TextBoxDownload.Text = localPath;
                }
                int result = Api.DownloadFile(tmpObjectName, localPath, tmpMachineID, tmpChannel, _userApiTimeout);
                if (result == 0)
                {
                    CheckForTest("downloadFile", true);
                    System.Windows.MessageBox.Show("다운로드 성공");
                }
                else
                {
                    CheckForTest("downloadFile", false);
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
                    System.Windows.MessageBox.Show("다운로드 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
        }

        private void ButtonGetPlc_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            if (_cncVendorCode == 1) //Fanuc
            {
                int tmpDataType = Convert.ToInt32(TextBoxPlcFanucType.Text.ToString());
                string tmpStartAddress = TextBoxPlcFanucStart.Text.ToString();
                string tmpEndAddress = TextBoxPlcFanucEnd.Text.ToString();
                Item tmpItem;
                int tmpResult = Api.getPlcSignal((Api.FANUC_PLC_TYPE)tmpDataType, tmpStartAddress, tmpEndAddress, out tmpItem, tmpMachineID, _userApiTimeout);
                if (tmpResult == 0)
                {
                    string tmpString = tmpItem.ToString();
                    TextBoxPlcResult.Text = tmpString.Replace("\r\n", "");
                    CheckForTest("getPLCSignal", true);
                    System.Windows.MessageBox.Show("getPLCSignal 성공");
                }
                else
                {
                    TextBoxPlcResult.Text = "";
                    CheckForTest("getPLCSignal", false);
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    System.Windows.MessageBox.Show("getPLCSignal 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else if (_cncVendorCode == 2) //Siemens
            {
                string tmpAddress = TextBoxPlcSiemensAddress.Text.ToString();
                Item tmpItem;
                int tmpResult = Api.getPlcSignal(tmpAddress, out tmpItem, tmpMachineID, _userApiTimeout);
                if (tmpResult == 0)
                {
                    string tmpString = tmpItem.ToString();
                    TextBoxPlcResult.Text = tmpString.Replace("\r\n", "");
                    CheckForTest("getPLCSignal", true);
                    System.Windows.MessageBox.Show("getPLCSignal 성공");
                }
                else
                {
                    TextBoxPlcResult.Text = "";
                    CheckForTest("getPLCSignal", false);
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    System.Windows.MessageBox.Show("getPLCSignal 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else if (_cncVendorCode == 4 || _cncVendorCode == 5) //Mitsubishi(타입:16, 어드레스:A), Kcnc
            {
                int tmpDataType = Convert.ToInt32(TextBoxPlcKcncType.Text.ToString());
                string tmpStartAddress = TextBoxPlcKcncStart.Text.ToString();
                int tmpCount = Convert.ToInt32(TextBoxPlcKcncCount.Text.ToString());
                Item tmpItem;
                int tmpResult = Api.getPlcSignal(tmpDataType, tmpCount, tmpStartAddress, out tmpItem, tmpMachineID, _userApiTimeout);
                if (tmpResult == 0)
                {
                    string tmpString = tmpItem.ToString();
                    TextBoxPlcResult.Text = tmpString.Replace("\r\n", "");
                    CheckForTest("getPLCSignal", true);
                    System.Windows.MessageBox.Show("getPLCSignal 성공");
                }
                else
                {
                    TextBoxPlcResult.Text = "";
                    CheckForTest("getPLCSignal", false);
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    System.Windows.MessageBox.Show("getPLCSignal 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else
            {
                TextBoxPlcResult.Text = "";
                System.Windows.MessageBox.Show("해당기능을 지원하지 않는 모듈입니다.", "오류");
            }
        }

        private void ButtonSetPlc_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            if (_cncVendorCode == 1) //Fanuc
            {
                int tmpDataType = Convert.ToInt32(TextBoxPlcFanucType.Text.ToString());
                string tmpStartAddress = TextBoxPlcFanucStart.Text.ToString();
                string tmpEndAddress = TextBoxPlcFanucEnd.Text.ToString();
                string inputData = TextBoxPlcSetData.Text;
                string[] inputDatas = inputData.Split(',');
                int inputDatasCount = inputDatas.Length;
                double[] inputValue = new double[inputDatasCount];
                for (int i = 0; i < inputDatasCount; i++)
                {
                    inputValue[i] = Convert.ToDouble(inputDatas[i]);
                }
                Item tmpItem;
                int tmpResult = Api.setPlcSignal((Api.FANUC_PLC_TYPE)tmpDataType, tmpStartAddress, tmpEndAddress, inputValue, out tmpItem, tmpMachineID, _userApiTimeout);
                TextBoxPlcResult.Text = "";
                if (tmpResult == 0)
                {
                    CheckForTest("setPLCSignal", true);
                    System.Windows.MessageBox.Show("setPLCSignal 성공");
                }
                else
                {
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    CheckForTest("setPLCSignal", false);
                    System.Windows.MessageBox.Show("setPLCSignal 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else if (_cncVendorCode == 2) //Siemens
            {
                string tmpAddress = TextBoxPlcSiemensAddress.Text.ToString();
                string inputData = TextBoxPlcSetData.Text;
                string[] inputDatas = inputData.Split(',');
                int inputDatasCount = inputDatas.Length;
                double[] inputValue = new double[inputDatasCount];
                for (int i = 0; i < inputDatasCount; i++)
                {
                    inputValue[i] = Convert.ToDouble(inputDatas[i]);
                }
                Item tmpItem;
                int tmpResult = Api.setPlcSignal(tmpAddress, inputValue, out tmpItem, tmpMachineID, _userApiTimeout);
                TextBoxPlcResult.Text = "";
                if (tmpResult == 0)
                {
                    CheckForTest("setPLCSignal", true);
                    System.Windows.MessageBox.Show("setPLCSignal 성공");
                }
                else
                {
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    CheckForTest("setPLCSignal", false);
                    System.Windows.MessageBox.Show("setPLCSignal 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else if (_cncVendorCode == 4 || _cncVendorCode == 5) //Mitsubishi(타입:16, 어드레스:A), Kcnc
            {
                int tmpDataType = Convert.ToInt32(TextBoxPlcKcncType.Text.ToString());
                string tmpStartAddress = TextBoxPlcKcncStart.Text.ToString();
                int tmpCount = Convert.ToInt32(TextBoxPlcKcncCount.Text.ToString());
                string inputData = TextBoxPlcSetData.Text;
                string[] inputDatas = inputData.Split(',');
                int inputDatasCount = inputDatas.Length;
                double[] inputValue = new double[inputDatasCount];
                for (int i = 0; i < inputDatasCount; i++)
                {
                    inputValue[i] = Convert.ToDouble(inputDatas[i]);
                }
                Item tmpItem;
                int tmpResult = Api.setPlcSignal(tmpDataType, tmpCount, tmpStartAddress, inputValue, out tmpItem, tmpMachineID, _userApiTimeout);
                TextBoxPlcResult.Text = "";
                if (tmpResult == 0)
                {
                    CheckForTest("setPLCSignal", true);
                    System.Windows.MessageBox.Show("setPLCSignal 성공");
                }
                else
                {
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    CheckForTest("setPLCSignal", false);
                    System.Windows.MessageBox.Show("setPLCSignal 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else
            {
                TextBoxPlcResult.Text = "";
                System.Windows.MessageBox.Show("해당기능을 지원하지 않는 모듈입니다.", "오류");
            }
        }

        private void ButtonToUpperDir_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            if (tmpCurrentPath == "")
            {
                System.Windows.MessageBox.Show("현재 경로를 확인하시기 바랍니다.");
                return;
            }
            string rootPath;
            string tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString()).ToString();
            Item item;
            int tmpResult = Api.getData("data://machine/ncmemory/rootpath", "machine=" + tmpMachineID, out item, true, _userApiTimeout);
            if (tmpResult == 0)
            {
                rootPath = item.GetValueString("value");
                if (rootPath == tmpCurrentPath)
                {
                    System.Windows.MessageBox.Show("현재 루트 폴더입니다.");
                    return;
                }
                tmpCurrentPath = tmpCurrentPath.Substring(0, tmpCurrentPath.LastIndexOf('/'));
                ShowFileList(tmpCurrentPath.Substring(0, tmpCurrentPath.LastIndexOf('/') + 1));
            }
            else
            {
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                System.Windows.MessageBox.Show("상위 폴더 조회 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        private void ComboBoxMachieID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetTimeout();
            string tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString()).ToString();
            Item item;
            int tmpResult = Api.getData("data://machine/cncvendor", "machine=" + tmpMachineID.ToString(), out item, true, _userApiTimeout);
            if (tmpResult == 0)
            {
                _cncVendorCode = item.GetValueInt("value");
                string tmpStr;
                if (_cncVendorCode == 1)
                {
                    tmpStr = _cncVendorCode.ToString() + " (Fanuc)";
                }
                else if (_cncVendorCode == 2)
                {
                    tmpStr = _cncVendorCode.ToString() + " (Siemens)";
                }
                else if (_cncVendorCode == 3)
                {
                    tmpStr = _cncVendorCode.ToString() + " (CSCAM)";
                }
                else if (_cncVendorCode == 4)
                {
                    tmpStr = _cncVendorCode.ToString() + " (Mitsubishi)";
                }
                else if (_cncVendorCode == 5)
                {
                    tmpStr = _cncVendorCode.ToString() + " (KCNC)";
                }
                else if (_cncVendorCode == 6)
                {
                    tmpStr = _cncVendorCode.ToString() + " (MAZAK)";
                }
                else if (_cncVendorCode == 7)
                {
                    tmpStr = _cncVendorCode.ToString() + " (Heidenhain)";
                }
                else
                {
                    tmpStr = _cncVendorCode.ToString() + " (UNKNOWN)";
                }
                TextBlockVendorCode.Text = "VendorCode : " + tmpStr;
            }
            else
            {
                _cncVendorCode = 0;
                TextBlockVendorCode.Text = "VendorCode : 0 (오류)";
            }
            tmpResult = Api.getData("data://machine/ncmemory/rootpath", "machine=" + tmpMachineID, out item, true, _userApiTimeout);
            if (tmpResult == 0)
            {
                TextBoxCurrentPath.Text = item.GetValueString("value");
                ShowFileList(TextBoxCurrentPath.Text);
            }
            else
            {
                TextBoxCurrentPath.Text = "";
                ListBoxFileList.Items.Clear();
            }
            _signalCopyOrMove = 0;
            TextBoxCopyOrMove.Text = "";
        }

        private void ComboBoxTimeSeriesMachieID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetTimeout();
            string tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString()).ToString();
            Item item;
            int tmpResult = Api.getData("data://machine/cncvendor", "machine=" + tmpMachineID.ToString(), out item, true, _userApiTimeout);
            if (tmpResult == 0)
            {
                _timeseriesVendorCode = item.GetValueInt("value");
                string tmpStr;
                if (_timeseriesVendorCode == 1)
                {
                    tmpStr = _timeseriesVendorCode.ToString() + " (Fanuc)";
                }
                else if (_timeseriesVendorCode == 2)
                {
                    tmpStr = _timeseriesVendorCode.ToString() + " (Siemens)";
                }
                else if (_timeseriesVendorCode == 3)
                {
                    tmpStr = _timeseriesVendorCode.ToString() + " (CSCAM)";
                }
                else if (_timeseriesVendorCode == 4)
                {
                    tmpStr = _timeseriesVendorCode.ToString() + " (Mitsubishi)";
                }
                else if (_timeseriesVendorCode == 5)
                {
                    tmpStr = _timeseriesVendorCode.ToString() + " (KCNC)";
                }
                else if (_timeseriesVendorCode == 6)
                {
                    tmpStr = _timeseriesVendorCode.ToString() + " (MAZAK)";
                }
                else if (_timeseriesVendorCode == 7)
                {
                    tmpStr = _timeseriesVendorCode.ToString() + " (Heidenhain)";
                }
                else
                {
                    tmpStr = _timeseriesVendorCode.ToString() + " (UNKNOWN)";
                }
                TextBlockTimeSeriesVendorCode.Text = "VendorCode : " + tmpStr;
            }
            else
            {
                _timeseriesVendorCode = 0;
                TextBlockTimeSeriesVendorCode.Text = "VendorCode : 0 (오류)";
            }
            InitTimeseries();
        }

        private void ButtonGetGmodal_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            int tmpChannel;
            try
            {
                tmpChannel = Convert.ToInt32(TextBoxChannel.Text.ToString());
            }
            catch
            {
                System.Windows.MessageBox.Show("채널을 확인하시기 바랍니다.");
                return;
            }
            Item tmpItem;
            int tmpResult = Api.getGModal(tmpChannel, out tmpItem, tmpMachineID, _userApiTimeout);
            if (tmpResult == 0)
            {
                string tmpString = tmpItem.ToString();
                TextBoxGmodalResult.Text = tmpString.Replace("\r\n", "");
                CheckForTest("getGModal", true);
                System.Windows.MessageBox.Show("getGmodal 성공");
            }
            else
            {
                CheckForTest("getGModal", false);
                TextBoxGmodalResult.Text = "";
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                System.Windows.MessageBox.Show("getGmodal 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        private void ButtonGetExModal_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            int tmpChannel;
            try
            {
                tmpChannel = Convert.ToInt32(TextBoxChannel.Text.ToString());
            }
            catch
            {
                System.Windows.MessageBox.Show("채널을 확인하시기 바랍니다.");
                return;
            }
            string tmpModal = TextBoxExModalModal.Text.ToString();
            Item tmpItem;
            int tmpResult = Api.getExModal(tmpChannel, tmpModal, out tmpItem, tmpMachineID, _userApiTimeout);
            if (tmpResult == 0)
            {
                string tmpString = tmpItem.ToString();
                TextBoxExModalResult.Text = tmpString.Replace("\r\n", "");
                CheckForTest("getExModal", true);
                System.Windows.MessageBox.Show("getExModal 성공");
            }
            else
            {
                CheckForTest("getExModal", false);
                TextBoxExModalResult.Text = "";
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                System.Windows.MessageBox.Show("getExModal 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }

        private void ButtonGetToolOffset_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            int tmpChannel;
            try
            {
                tmpChannel = Convert.ToInt32(TextBoxChannel.Text.ToString());
            }
            catch
            {
                System.Windows.MessageBox.Show("채널을 확인하시기 바랍니다.");
                return;
            }
            if (_cncVendorCode == 1 || _cncVendorCode == 4 || _cncVendorCode == 5) //Fanuc, Mitsubishi, KCNC
            {
                string tmpType = TextBoxToolOffsetType.Text.ToString();
                string tmpNumber = TextBoxToolOffsetNumber.Text.ToString();
                Item tmpItem;
                int tmpResult = Api.getToolOffsetData(tmpChannel.ToString(), tmpNumber, tmpType, true, out tmpItem, tmpMachineID, _userApiTimeout);
                if (tmpResult == 0)
                {
                    string tmpString = tmpItem.ToString();
                    TextBoxToolOffsetResult.Text = tmpString.Replace("\r\n", "");
                    CheckForTest("getToolOffsetData", true);
                    System.Windows.MessageBox.Show("getToolOffsetData 성공");
                }
                else
                {
                    CheckForTest("getToolOffsetData", false);
                    TextBoxToolOffsetResult.Text = "";
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    System.Windows.MessageBox.Show("getToolOffsetData 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else
            {
                TextBoxToolOffsetResult.Text = "";
                CheckForTest("getToolOffsetData", false);
                System.Windows.MessageBox.Show("해당기능을 지원하지 않는 모듈입니다.", "오류");
            }
        }

        private void ButtonSetToolOffset_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            int tmpChannel;
            try
            {
                tmpChannel = Convert.ToInt32(TextBoxChannel.Text.ToString());
            }
            catch
            {
                System.Windows.MessageBox.Show("채널을 확인하시기 바랍니다.");
                return;
            }
            if (_cncVendorCode == 1 || _cncVendorCode == 4 || _cncVendorCode == 5) //Fanuc, Mitsubishi, KCNC
            {
                string tmpType = TextBoxToolOffsetType.Text;
                string tmpNumber = TextBoxToolOffsetNumber.Text;
                string inputData = TextBoxToolOffsetSetData.Text;
                string[] inputDatas = inputData.Split(',');
                int inputDatasCount = inputDatas.Length;
                double[] inputValue = new double[inputDatasCount];
                for (int i = 0; i < inputDatasCount; i++)
                {
                    inputValue[i] = Convert.ToDouble(inputDatas[i]);
                }
                Item tmpItem;
                int tmpResult = Api.setToolOffsetData(tmpChannel.ToString(), tmpNumber, tmpType, inputValue, out tmpItem, tmpMachineID, _userApiTimeout);
                TextBoxToolOffsetResult.Text = "";
                if (tmpResult == 0)
                {
                    CheckForTest("setToolOffsetData", true);
                    System.Windows.MessageBox.Show("setToolOffsetData 성공");
                }
                else
                {
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    CheckForTest("setToolOffsetData", false);
                    System.Windows.MessageBox.Show("setToolOffsetData 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else
            {
                TextBoxToolOffsetResult.Text = "";
                CheckForTest("getToolOffsetData", false);
                System.Windows.MessageBox.Show("해당기능을 지원하지 않는 모듈입니다.", "실패");
            }
        }

        public void ReadyCopy(string _objectName)
        {
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            if (tmpCurrentPath == "")
            {
                System.Windows.MessageBox.Show("경로를 확인하시기 바랍니다.");
                return;
            }
            _signalCopyOrMove = 1;
            TextBoxCopyOrMove.Text = tmpCurrentPath + _objectName;
        }

        public void ReadyMove(string _objectName)
        {
            string tmpCurrentPath = TextBoxCurrentPath.Text;
            if (tmpCurrentPath == "")
            {
                System.Windows.MessageBox.Show("경로를 확인하시기 바랍니다.");
                return;
            }
            _signalCopyOrMove = 2;
            TextBoxCopyOrMove.Text = tmpCurrentPath + _objectName;
        }

        private void ButtonUploadFileSelect_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new();
            openFileDialog.InitialDirectory = ReadConfig(Constants.ConfigFileName, "Main", "UploadFileDefaultDirPath");
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string tmpDirPath = openFileDialog.FileName.Replace(openFileDialog.SafeFileName, "");
                WriteConfig(Constants.ConfigFileName, "Main", "UploadFileDefaultDirPath", tmpDirPath);
                string tmpPath = openFileDialog.FileName;
                TextBoxUpload.Text = tmpPath;
            }
        }

        public void ObjectRename(string _objectName)
        {
            System.Windows.Window tmpRenameWindow = new RenameWindow(_objectName);
            tmpRenameWindow.Show();
        }

        private void ButtonRenew_Click(object sender, RoutedEventArgs e)
        {
            ShowFileList(TextBoxCurrentPath.Text);
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (_addressFilePath != "")
            {
                SaveAddressFile(_addressFilePath);
            }
            else
            {
                System.Windows.Forms.SaveFileDialog saveFileDialog = new();
                saveFileDialog.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (saveFileDialog.FileName != "")
                    {
                        SaveAddressFile(saveFileDialog.FileName);
                    }
                }
            }
        }

        private void ButtonSaveAs_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filter = "txt files (*.txt)|*.txt";
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (saveFileDialog.FileName != "")
                {
                    SaveAddressFile(saveFileDialog.FileName);
                }
            }
        }

        private int SaveAddressFile(string _addressFilePath)
        {
            FileInfo fi = new FileInfo(_addressFilePath);
            if (fi.Exists)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("기존 파일에 덮어 씌우시겠습니까?", "저장", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    System.Windows.MessageBox.Show("기존 파일이 있어 저장을 취소하였습니다.", "취소");
                    return -1;
                }
            }
            string tmpsaveContents = "";
            for (int i = 0; i < ListBoxMachineState.Items.Count; i++)
            {
                tmpsaveContents = tmpsaveContents + (ListBoxMachineState.Items[i] as MachineState).address + "\t" + (ListBoxMachineState.Items[i] as MachineState).filter + "\t" + (ListBoxMachineState.Items[i] as MachineState).TextBoxMemo.Text + "\n";
            }
            File.WriteAllText(_addressFilePath, tmpsaveContents);
            this._addressFilePath = _addressFilePath;
            TextBlockAddressFilePath.Text = _addressFilePath;
            System.Windows.MessageBox.Show("저장완료", "성공");
            return 0;
        }

        public void DeleteMachineStateItem(int _index)
        {
            if (ButtonMultiStart.IsEnabled == false)
            {
                System.Windows.MessageBox.Show("작업중에는 불가능합니다.", "오류");
                return;
            }
            MessageBoxResult result = System.Windows.MessageBox.Show("항목을 삭제하시겠습니까?", "삭제", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                ListBoxMachineState.Items.RemoveAt(_index);
                int count = ListBoxMachineState.Items.Count;
                MachineState.totalCount = count;
                TextBlockSingleAll.Text = count.ToString();
                TextBlockSinglePassedAll.Text = count.ToString();
                TextBlockMultiAll.Text = count.ToString();
                for (int i = 0; i < count; i++)
                {
                    (ListBoxMachineState.Items[i] as MachineState).InsertNumber(i + 1);
                }
            }
        }

        public void UpMachineStateItem(int _index)
        {
            if (_index == 0)
            {
                return;
            }
            if (ButtonMultiStart.IsEnabled == false)
            {
                System.Windows.MessageBox.Show("작업중에는 불가능합니다.", "오류");
                return;
            }
            MachineState tmpMachineState = (ListBoxMachineState.Items[_index] as MachineState);
            ListBoxMachineState.Items.RemoveAt(_index);
            ListBoxMachineState.Items.Insert(_index - 1, tmpMachineState);
            for (int i = 0; i < ListBoxMachineState.Items.Count; i++)
            {
                (ListBoxMachineState.Items[i] as MachineState).InsertNumber(i + 1);
            }
        }

        public void DownMachineStateItem(int _index)
        {
            if (_index == ListBoxMachineState.Items.Count - 1)
            {
                return;
            }
            if (ButtonMultiStart.IsEnabled == false)
            {
                System.Windows.MessageBox.Show("작업중에는 불가능합니다.", "오류");
                return;
            }
            MachineState tmpMachineState = (ListBoxMachineState.Items[_index] as MachineState);
            ListBoxMachineState.Items.RemoveAt(_index);
            ListBoxMachineState.Items.Insert(_index + 1, tmpMachineState);
            for (int i = 0; i < ListBoxMachineState.Items.Count; i++)
            {
                (ListBoxMachineState.Items[i] as MachineState).InsertNumber(i + 1);
            }
        }

        public string MakeSuccessMessage(Item _item)
        {
            string address = _item.GetValueString("address");
            string filter = _item.GetValueString("filter");
            string timestatmp = "";
            string type = "";
            int status = 0;
            if (address.StartsWith("data://device"))
            {

            }
            else if (address.StartsWith("data://machine"))
            {
                timestatmp = _item.GetValueString("time");
                type = _item.GetValueString("datatype");
                status = _item.GetValueInt("status");
            }
            string[] values;
            if (type == "boolean")
            {
                values = _item.GetArrayBoolean("value").Select(b => b.ToString()).ToArray();
            }
            else
            {
                values = _item.GetArrayString("value");
            }
            if (address == "data://machine/machinepowerontime")//분단위
            {
                for (int i = 0; i < values.Length; i++)
                {
                    int tmpTime = Convert.ToInt32(values[i]);
                    int tmpHour = tmpTime / 60;
                    int tmpMinute = tmpTime % 60;
                    values[i] = tmpHour.ToString() + "H " + tmpMinute.ToString() + "M";
                }
            }
            else if (address == "data://machine/channel/workstatus/machiningtime/processingmachiningtime"
                  || address == "data://machine/channel/workstatus/machiningtime/estimatedmachiningtime"
                  || address == "data://machine/channel/workstatus/machiningtime/machineoperationtime"
                  || address == "data://machine/channel/workstatus/machiningtime/actualcuttingtime")//초단위
            {
                for (int i = 0; i < values.Length; i++)
                {
                    double tmpTimeTypeDouble = Convert.ToDouble(values[i]);
                    int tmpTime = (int)tmpTimeTypeDouble;
                    int tmpHour = tmpTime / 3600;
                    int tmpMS = tmpTime % 3600;
                    int tmpMinute = tmpMS / 60;
                    int tmpSecond = tmpMS % 60;
                    values[i] = tmpHour.ToString() + "H " + tmpMinute.ToString() + "M " + tmpSecond.ToString() + "S";
                }
            }
            if (values.Length == 0)
            {

            }
            string returnValue = "status: " + status + "  \ttype: " + type + "\ttime: " + timestatmp + "\nvalue: ";
            for (int i = 0; i < values.Length; i++)
            {
                if (i == 0)
                {
                    returnValue = returnValue + values[i];
                }
                else
                {
                    returnValue = returnValue + ", " + values[i];
                }
            }
            return returnValue;
        }

        private void ButtonInsertNewMachineState_Click(object sender, RoutedEventArgs e)
        {
            if (_connectResult == 0 && ButtonMultiStart.IsEnabled == false)
            {
                System.Windows.MessageBox.Show("작업중에는 불가능합니다.", "오류");
                return;
            }
            int tmpIndex = ListBoxMachineState.Items.IndexOf(ListBoxMachineState.SelectedItem);
            if (tmpIndex < 0)
            {
                tmpIndex = 0;
            }
            ListBoxMachineState.Items.Insert(tmpIndex, new MachineState("", "", ""));
            int count = ListBoxMachineState.Items.Count;
            MachineState.totalCount = count;
            TextBlockSingleAll.Text = count.ToString();
            TextBlockSinglePassedAll.Text = count.ToString();
            TextBlockMultiAll.Text = count.ToString();
            for (int i = 0; i < count; i++)
            {
                (ListBoxMachineState.Items[i] as MachineState).InsertNumber(i + 1);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == ComboBoxSingleDirect)
            {
                WriteConfig(Constants.ConfigFileName, "Main", "SingleDirect", ComboBoxSingleDirect.SelectedIndex.ToString());
                if (ComboBoxSingleDirect.SelectedIndex == 0)
                {
                    _singleDirect = true;
                }
                else
                {
                    _singleDirect = false;
                }
            }
            else if (sender == ComboBoxMultiDirect)
            {
                WriteConfig(Constants.ConfigFileName, "Main", "MultiDirect", ComboBoxMultiDirect.SelectedIndex.ToString());
                if (ComboBoxMultiDirect.SelectedIndex == 0)
                {
                    _multiDirect = true;
                }
                else
                {
                    _multiDirect = false;
                }
            }
        }

        void MultiStart(bool check)
        {
            SetTimeout();
            if (ListBoxMachineState.Items.Count < 1)
            {
                return;
            }
            _isMultiRunning = true;
            ComboBoxMultiDirect.IsEnabled = false;
            TextBoxTimeout.IsReadOnly = true;
            TextBoxMultiGetDataTargetCount.IsReadOnly = true;
            TextBoxCommonFilter.IsReadOnly = true;
            ButtonMultiStart.IsEnabled = false;
            ButtonLoad.IsEnabled = false;
            _commonFilter = TextBoxCommonFilter.Text.Trim();
            if (_commonFilter != "")
            {
                if (_commonFilter.Substring(_commonFilter.Length - 1, 1) == "&")
                {
                    _commonFilter = _commonFilter.Substring(0, _commonFilter.Length - 1);
                }
                if (_commonFilter.Substring(0, 1) == "&")
                {
                    _commonFilter = _commonFilter.Substring(1, _commonFilter.Length);
                }
            }
            _threadMultistopFlag = false;
            _threadMulti = new Thread(() => ThreadMulti(check))
            {
                IsBackground = true
            };
            _threadMulti.Start();
            ButtonMultiStop.IsEnabled = true;
        }

        private void ThreadMulti(bool check)
        {
            long tmpTotalTime = 0;
            long tmpOneTurnTime = 0;
            bool tmpCountCheck = true;
            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                TextBlockMultiPercent.Text = "%";
                TextBlockMultiSuccess.Text = "0";
                TextBoxMultiThreadResult.Text = "";
                TextBlockMultiGetDataCurrentCount.Text = "0";
            }));
            int tmpGetDataCurrentCount = 0;
            int tmpGetDataTargetCount = 0;
            try
            {
                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    tmpGetDataTargetCount = Convert.ToInt32(TextBoxMultiGetDataTargetCount.Text);
                }));
            }
            catch
            {
                tmpGetDataTargetCount = 0;
            }
            if (tmpGetDataTargetCount <= 0)
            {
                tmpGetDataTargetCount = 0;
                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    TextBoxMultiGetDataTargetCount.Text = "0";
                }));
                tmpCountCheck = false;
            }
            int timeLineCount = 0;
            double successCount = 0;
            double tmpAllCount = 0; ;
            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                tmpAllCount = Convert.ToInt32(TextBlockMultiAll.Text);
            }));
            while (!_threadMultistopFlag)
            {
                successCount = 0;
                tmpOneTurnTime = 0;
                int tmpTotalCount = ListBoxMachineState.Items.Count;
                Stopwatch stopwatch = new();
                string[] addressArray = new string[tmpTotalCount];
                string[] filter_Array = new string[tmpTotalCount];
                Item[] itemArray = new Item[tmpTotalCount];
                for (int i = 0; i < tmpTotalCount; i++)
                {
                    string tmpAddress = (ListBoxMachineState.Items[i] as MachineState).address;
                    string tmpFilter = (ListBoxMachineState.Items[i] as MachineState).filter;
                    if (_commonFilter != "")
                    {
                        if (tmpFilter != "")
                        {
                            tmpFilter = _commonFilter + "&" + tmpFilter;
                        }
                        else
                        {
                            tmpFilter = _commonFilter;
                        }
                    }
                    addressArray[i] = tmpAddress;
                    filter_Array[i] = tmpFilter;
                    if (_threadMultistopFlag)
                    {
                        break;
                    }
                }
                stopwatch.Restart();
                int tmpResult = Api.getData(addressArray, filter_Array, out itemArray, _multiDirect, _timeout);
                stopwatch.Stop();
                //Multi의 경우 하나만 오류가 발생해도 함수 실행 결과가 오류로 표시됩니다. itemArray의 "status"의 값이 0이 아니라면 오류입니다.
                lock (_lock)
                {
                    if (tmpResult == 558891055)
                    {
                        string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                        for (int i = 0; i < tmpTotalCount; i++)
                        {
                            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                (ListBoxMachineState.Items[i] as MachineState).InsertResult(tmpErrorCode);
                                (ListBoxMachineState.Items[i] as MachineState).InsertResultDescribe(tmpErrorMessage);
                                (ListBoxMachineState.Items[i] as MachineState).DrawColorMan(30, 255, 100, 100);
                                (ListBoxMachineState.Items[i] as MachineState).InsertMultiTime("Multi 실패: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                            }));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < tmpTotalCount; i++)
                        {
                            if (true)//테스트 코드
                            {
                                if (itemArray[i] == null)
                                {
                                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                    {
                                        MessageBox.Show("null 반환 발생", "Multi 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }));
                                    Debug.WriteLine("null 반환 발생");
                                }
                                string returnAddress = itemArray[i].GetValueString("address");
                                string returnFilter = itemArray[i].GetValueString("filter");
                                if (!string.Equals(addressArray[i], returnAddress, StringComparison.OrdinalIgnoreCase) || !string.Equals(filter_Array[i], returnFilter, StringComparison.OrdinalIgnoreCase))
                                {
                                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                    {
                                        MessageBox.Show("불일치 발생\n" + addressArray[i] + "\n" +
                                            returnAddress + "\n" + filter_Array[i] + "\n" + returnFilter, "Multi 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }));
                                    Debug.WriteLine("불일치 발생\n" + addressArray[i] + "\n" + returnAddress + "\n" + filter_Array[i] + "\n" + returnFilter);
                                }
                            }
                            int status = itemArray[i].GetValueInt("status");
                            if (status == 0)
                            {
                                successCount++;
                                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    (ListBoxMachineState.Items[i] as MachineState).InsertResult(itemArray[i].ToString());
                                    (ListBoxMachineState.Items[i] as MachineState).InsertResultDescribe(MakeSuccessMessage(itemArray[i]));
                                    (ListBoxMachineState.Items[i] as MachineState).DrawColorMan(100, 103, 153, 255);
                                    (ListBoxMachineState.Items[i] as MachineState).InsertMultiTime("Multi 성공: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                                }));
                            }
                            else
                            {
                                string tmpErrorCode = MakeErrorMessage(status, out string tmpErrorMessage);
                                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    (ListBoxMachineState.Items[i] as MachineState).InsertResult(tmpErrorCode);
                                    (ListBoxMachineState.Items[i] as MachineState).InsertResultDescribe(tmpErrorMessage);
                                    (ListBoxMachineState.Items[i] as MachineState).DrawColorMan(30, 255, 100, 100);
                                    (ListBoxMachineState.Items[i] as MachineState).InsertMultiTime("Multi 실패: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                                }));
                            }
                        }
                    }
                }
                Thread.Sleep(10);
                tmpOneTurnTime = stopwatch.ElapsedMilliseconds;
                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    TextBlockMultiSuccess.Text = successCount.ToString();
                    TextBlockMultiPercent.Text = (successCount / tmpAllCount * 100).ToString("0.##") + "%";
                }));
                if (tmpCountCheck == true)
                {
                    tmpTotalTime = tmpTotalTime + tmpOneTurnTime;
                    tmpGetDataCurrentCount++;
                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        TextBlockMultiGetDataCurrentCount.Text = tmpGetDataCurrentCount.ToString();
                        TextBoxMultiThreadResult.Text = tmpGetDataCurrentCount.ToString() + "번째 : " + tmpOneTurnTime.ToString() + "ms" + "\n" + TextBoxMultiThreadResult.Text;
                    }));
                    if (tmpGetDataCurrentCount >= tmpGetDataTargetCount)
                    {
                        _threadMultistopFlag = true;
                    }
                }
                else
                {
                    if (timeLineCount >= 13)
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            string tmpString = TextBoxMultiThreadResult.Text;
                            if (tmpString.Substring(tmpString.Length - 1) == "\n")
                            {
                                tmpString = tmpString.Substring(0, tmpString.Length - 1);
                            }
                            tmpString = tmpString.Substring(0, tmpString.LastIndexOf('\n'));
                            TextBoxMultiThreadResult.Text = tmpOneTurnTime.ToString() + "ms\n" + tmpString;
                        }));
                    }
                    else
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            if (TextBoxMultiThreadResult.Text == "")
                            {
                                TextBoxMultiThreadResult.Text = tmpOneTurnTime.ToString() + "ms";
                            }
                            else
                            {
                                TextBoxMultiThreadResult.Text = tmpOneTurnTime.ToString() + "ms\n" + TextBoxMultiThreadResult.Text;
                            }
                        }));
                        timeLineCount++;
                    }
                }
            }
            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (tmpCountCheck == true)
                {
                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        TextBoxMultiThreadResult.Text = "Total : " + tmpTotalTime.ToString() + "ms" + "\n" + TextBoxMultiThreadResult.Text;
                    }));
                }
                ButtonMultiStart.IsEnabled = true;
                ButtonMultiStop.IsEnabled = false;
                TextBoxMultiGetDataTargetCount.IsReadOnly = false;
                ComboBoxMultiDirect.IsEnabled = true;
                _isMultiRunning = false;
                if (_isSingleRunning == false && _isMultiRunning == false)
                {
                    ButtonLoad.IsEnabled = true;
                    TextBoxCommonFilter.IsReadOnly = false;
                    TextBoxTimeout.IsReadOnly = false;
                }
            }));
        }

        private void ButtonGudGet_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            if (_cncVendorCode == 2) //Siemens
            {
                int tmpResult = -1;
                Item tmpItem;
                string tmpGudType = TextBoxGudType.Text;
                int tmpGudNumber = Convert.ToInt32(TextBoxGudNumber.Text.ToString());
                int tmpGudCount = Convert.ToInt32(TextBoxGudCount.Text.ToString());
                int tmpGudChannel = Convert.ToInt32(TextBoxGudChannel.Text.ToString());
                string tmpGudAdress = TextBoxGudAddress.Text;
                if (tmpGudType == "0")
                {
                    tmpResult = Api.getGudData(GUD_TYPE.STRING, tmpGudNumber, tmpGudCount, tmpGudAdress, false, out tmpItem, tmpMachineID, tmpGudChannel, _userApiTimeout);
                }
                else if (tmpGudType == "1")
                {
                    tmpResult = Api.getGudData(GUD_TYPE.DOUBLE, tmpGudNumber, tmpGudCount, tmpGudAdress, false, out tmpItem, tmpMachineID, tmpGudChannel, _userApiTimeout);
                }
                else
                {
                    System.Windows.MessageBox.Show("Type은 0(STRING) 또는 1(DOUBLE) 이어야 합니다.", "오류");
                    return;
                }

                if (tmpResult == 0)
                {
                    string tmpString = tmpItem.ToString();
                    TextBoxGudGet.Text = tmpString.Replace("\r\n", "");
                    CheckForTest("getGUD", true);
                    System.Windows.MessageBox.Show("getGUD 성공");
                }
                else
                {
                    TextBoxGudGet.Text = "";
                    CheckForTest("getGUD", false);
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    System.Windows.MessageBox.Show("getGUD 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else
            {
                TextBoxGudGet.Text = "";
                System.Windows.MessageBox.Show("해당기능을 지원하지 않는 모듈입니다.", "오류");
            }
        }

        private void ButtonGudSet_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            if (_cncVendorCode == 2) //Siemens
            {
                int tmpResult = -1;
                Item tmpItem;
                string tmpGudType = TextBoxGudType.Text;
                int tmpGudNumber = Convert.ToInt32(TextBoxGudNumber.Text.ToString());
                int tmpGudCount = Convert.ToInt32(TextBoxGudCount.Text.ToString());
                int tmpGudChannel = Convert.ToInt32(TextBoxGudChannel.Text.ToString());
                string tmpGudAdress = TextBoxGudAddress.Text;

                string inputData = TextBoxGudSet.Text;
                string[] inputDatas = inputData.Split(',');
                int inputDatasCount = inputDatas.Length;
                if (inputDatasCount != tmpGudCount)
                {
                    System.Windows.MessageBox.Show("입력데이터의 양과 Count가 일치하지 않습니다.", "오류");
                    return;
                }
                if (tmpGudType == "0")
                {
                    string[] inputValue = new string[inputDatasCount];
                    for (int i = 0; i < inputDatasCount; i++)
                    {
                        inputValue[i] = inputDatas[i];
                    }
                    tmpResult = Api.setGudData(tmpGudNumber, tmpGudCount, tmpGudAdress, inputValue, out tmpItem, tmpMachineID, tmpGudChannel, _userApiTimeout);
                }
                else if (tmpGudType == "1")
                {
                    double[] inputValue = new double[inputDatasCount];
                    for (int i = 0; i < inputDatasCount; i++)
                    {
                        inputValue[i] = Convert.ToDouble(inputDatas[i]);
                    }
                    tmpResult = Api.setGudData(tmpGudNumber, tmpGudCount, tmpGudAdress, inputValue, out tmpItem, tmpMachineID, tmpGudChannel, _userApiTimeout);
                }
                else
                {
                    System.Windows.MessageBox.Show("Type은 0(STRING) 또는 1(DOUBLE) 이어야 합니다.", "오류");
                    return;
                }
                TextBoxGudGet.Text = "";
                if (tmpResult == 0)
                {
                    CheckForTest("setGUD", true);
                    System.Windows.MessageBox.Show("setGUD 성공");
                }
                else
                {
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                    CheckForTest("setGUD", false);
                    System.Windows.MessageBox.Show("setGUD 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
            }
            else
            {
                TextBoxPlcResult.Text = "";
                System.Windows.MessageBox.Show("해당기능을 지원하지 않는 모듈입니다.", "오류");
            }
        }

        readonly Color[] colors =
        [
            Color.FromColor(System.Drawing.Color.Red),
            Color.FromColor(System.Drawing.Color.Blue),
            Color.FromColor(System.Drawing.Color.Green),
            Color.FromColor(System.Drawing.Color.Orange),
            Color.FromColor(System.Drawing.Color.Purple),
            Color.FromColor(System.Drawing.Color.Teal),
            Color.FromColor(System.Drawing.Color.Gray),
            Color.FromColor(System.Drawing.Color.Yellow)
        ];

        private int OnTimeseriesBufferData(EVENT_CODE evt, int cmd, Item command, ref Item result)
        {
            Item data = command.find("result");
            string json = "{" + data.ToString() + "}";
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            JsonElement firstResult = root.GetProperty("result")[0];
            TimeSeriesItem item = JsonSerializer.Deserialize<TimeSeriesItem>(firstResult.GetRawText());

            //List<Item> tempData = (List<Item>)data.GetValue();
            //Item tmpItem = tempData[0];
            //double[] dvals = tmpItem.GetArrayDouble("value");
            ScootPlotTimeseries.Plot.Clear();
            for (int i = 0; i < item.count; i++)
            {
                double[] dvals = item.value[i].ToArray();
                double[] dataX = Enumerable.Range(1, dvals.Length).Select(x => (double)x).ToArray();
                double[] dataY = dvals.Take(dvals.Length).Select(x => (double)x).ToArray();
                ScootPlotTimeseries.Plot.Add.ScatterPoints(dataX, dataY, color: colors[i]);
                ScootPlotTimeseries.Plot.Axes.AutoScale();
                if (dvals.Length >= 10)
                {
                    Debug.WriteLine("StartTimestamp: " + item.starttimestamp[i]);
                    Debug.WriteLine("EndTimestamp: " + item.endtimestamp[i]);
                    Debug.WriteLine("Size: " + item.streamdatasizes[i]);
                    Debug.WriteLine("OnTimeseriesBufferData BufferCount: " + dvals.Length);
                    Debug.WriteLine("OnTimeseriesBufferData Start1 Value: " + dvals[0]);
                    Debug.WriteLine("OnTimeseriesBufferData Start2 Value: " + dvals[1]);
                    Debug.WriteLine("OnTimeseriesBufferData Start3 Value: " + dvals[2]);
                    Debug.WriteLine("OnTimeseriesBufferData End-3 Value: " + dvals[dvals.Length - 3]);
                    Debug.WriteLine("OnTimeseriesBufferData End-2 Value: " + dvals[dvals.Length - 2]);
                    Debug.WriteLine("OnTimeseriesBufferData End-1 Value: " + dvals[dvals.Length - 1]);
                }
            }

            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                //TextBlockTimeseries.Text = tmpItem.GetValueString("time");
                ScootPlotTimeseries.Refresh();
            });
            return 0;
        }

        public static void RunProcess(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                // ProcessStartInfo 설정
                ProcessStartInfo processStartInfo = new()
                {
                    FileName = filePath, // 실행할 배치 파일
                    WorkingDirectory = System.IO.Path.GetDirectoryName(filePath), // 배치 파일이 있는 폴더
                    UseShellExecute = true // Windows 셸을 사용하여 실행
                };
                // Process 실행
                using Process process = new();
                process.StartInfo = processStartInfo;
                process.Start();
                process.WaitForExit(); // 프로세스가 종료될 때까지 기다림
            }
            else
            {
                MessageBox.Show("실행 파일을 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool FileSelector(string oldPath, out string newPath)
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "파일 열기",
                Filter = "모든 파일 (*.*)|*.*",
            };
            if (oldPath != "")
            {
                string absolutePath = System.IO.Path.GetFullPath(oldPath);
                string dirPath = System.IO.Path.GetDirectoryName(absolutePath);
                if (dirPath != null)
                {
                    openFileDialog.InitialDirectory = dirPath;
                }
            }
            if (openFileDialog.ShowDialog() == true)
            {
                newPath = openFileDialog.FileName;
                return true;
            }
            newPath = "";
            return false;
        }

        private void SearchItem()
        {
            string targetText = TextBoxSearch.Text.ToLowerInvariant();
            if (targetText.Trim() == "")
            {
                TextBlockSearchResult.Text = "검색 결과:";
                return;
            }
            List<int> targetIndexList = [];
            for (int i = 0; i < ListBoxMachineState.Items.Count; i++)
            {
                if (ListBoxMachineState.Items[i] is MachineState item)
                {
                    if (item.AddressLower().Contains(targetText))
                    {
                        targetIndexList.Add(i + 1);
                    }
                }
            }
            if (targetIndexList.Count > 0)
            {
                TextBlockSearchResult.Text = "검색 결과: " + string.Join(", ", targetIndexList) + "번 입니다.";
                int targetIndex = targetIndexList[0] - 1;
                ListBoxMachineState.SelectedIndex = targetIndex;
                ListBoxMachineState.ScrollIntoView(ListBoxMachineState.Items[targetIndex]);
            }
            else
            {
                TextBlockSearchResult.Text = "검색 결과: 해당 Address를 찾을 수 없습니다.";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonSingleStart)
            {
                SingleStart(false);
            }
            else if (sender == ButtonSingleStop)
            {
                _threadSignlestopFlag = true;
            }
            else if (sender == ButtonMultiStart)
            {
                MultiStart(false);
            }
            else if (sender == ButtonMultiStop)
            {
                _threadMultistopFlag = true;
            }
            else if (sender == ButtonSearch)
            {
                SearchItem();
            }
            else if (sender == ButtonMachineMonitoringStart)
            {
                bool isAllExit = true;
                foreach (var item in ListBoxMachineMonitoringList.Items)
                {
                    if (item is MachineMonitoringItem monitoring)
                    {
                        if (!monitoring.IsExit())
                        {
                            isAllExit = false;
                        }
                    }
                }
                if (!isAllExit)
                {
                    return;
                }
                if (!int.TryParse(TextBoxMonitoringInterval.Text, out int interval))
                {
                    System.Windows.MessageBox.Show("갱신 주기가 유효한 값이 아닙니다.", "오류");
                    return;
                }
                WriteConfig(Constants.ConfigFileName, "Main", "MonitoringInterval", interval.ToString());
                foreach (var item in ListBoxMachineMonitoringList.Items)
                {
                    if (item is MachineMonitoringItem monitoring)
                    {
                        monitoring.OnOff(true, interval);
                    }
                }
                ButtonMachineMonitoringStart.IsEnabled = false;
                ButtonMachineMonitoringStop.IsEnabled = true;
                TextBoxMonitoringInterval.IsEnabled = false;
            }
            else if (sender == ButtonMachineMonitoringStop)
            {
                ButtonMachineMonitoringStop.IsEnabled = false;
                foreach (var item in ListBoxMachineMonitoringList.Items)
                {
                    if (item is MachineMonitoringItem monitoring)
                    {
                        monitoring.OnOff(false);
                    }
                }
                while (true)
                {
                    bool isAllExit = true;
                    foreach (var item in ListBoxMachineMonitoringList.Items)
                    {
                        if (item is MachineMonitoringItem monitoring)
                        {
                            if (!monitoring.IsExit())
                            {
                                isAllExit = false;
                            }
                        }
                    }
                    if (isAllExit)
                    {
                        break;
                    }
                }
                ButtonMachineMonitoringStart.IsEnabled = true;
                TextBoxMonitoringInterval.IsEnabled = true;
            }
        }

        private void MakeMachineMonitoringList()
        {
            ListBoxMachineMonitoringList.Items.Clear();
            foreach (MachineObject machine in _connectedMachineList)
            {
                ListBoxMachineMonitoringList.Items.Add(new MachineMonitoringItem(machine));
            }
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender == TextBoxSearch)
            {
                if (e.Key == Key.Enter)
                {
                    // 엔터 키가 눌렸을 때 실행할 동작
                    SearchItem();
                    // 기본 엔터 동작을 방지하려면 아래 코드로 이벤트 처리 완료
                    e.Handled = true;
                }
            }
        }

        private void ComboBoxTimeSeries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_OffChanged)
            {
                return;
            }
            SetTimeout();
            if (sender == ComboBoxTimeSeriesStatus)
            {
                int targetValue = GetMachineId(ComboBoxTimeSeriesStatus.SelectedItem.ToString());
                int tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString());
                string inputData = "{\"value\":[" + targetValue.ToString() + "]}";
                Item InItem = Item.Parse(inputData);
                int tmpResult = Api.updateData("data://machine/buffer/statusOfStream", "machine=" + tmpMachineID + "&buffer=1", InItem, out _, _userApiTimeout);
                if (tmpResult == 0)
                {
                    System.Windows.MessageBox.Show("변경에 성공했습니다.", "성공");
                }
                else
                {
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    System.Windows.MessageBox.Show("변경 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
                GetTimeseriesSettingvalue();
            }
            else if (sender == ComboBoxTimeSeriesMode)
            {
                int targetValue = GetMachineId(ComboBoxTimeSeriesMode.SelectedItem.ToString());
                int tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString());
                string inputData = "{\"value\":[" + targetValue.ToString() + "]}";
                Item InItem = Item.Parse(inputData);
                int tmpResult = Api.updateData("data://machine/buffer/modOfStream", "machine=" + tmpMachineID + "&buffer=1", InItem, out _, _userApiTimeout);
                if (tmpResult == 0)
                {
                    System.Windows.MessageBox.Show("변경에 성공했습니다.", "성공");
                }
                else
                {
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    System.Windows.MessageBox.Show("변경 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
                GetTimeseriesSettingvalue();
            }
            else if (sender == ComboBoxTimeSeriesCategory)
            {
                int targetValue = GetMachineId(ComboBoxTimeSeriesCategory.SelectedItem.ToString());
                int tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString());
                string inputData = "{\"value\":[" + targetValue.ToString() + "]}";
                Item InItem = Item.Parse(inputData);
                int tmpResult = Api.updateData("data://machine/buffer/stream/streamCategory", "machine=" + tmpMachineID + "&buffer=1&stream=1", InItem, out _, _userApiTimeout);
                if (tmpResult == 0)
                {
                    System.Windows.MessageBox.Show("변경에 성공했습니다.", "성공");
                }
                else
                {
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    System.Windows.MessageBox.Show("변경 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
                GetTimeseriesSettingvalue();
            }
        }

        private void InitTimeseries()
        {
            _OffChanged = true;
            ComboBoxTimeSeriesStatus.Items.Clear();
            ComboBoxTimeSeriesMode.Items.Clear();
            ComboBoxTimeSeriesCategory.Items.Clear();
            TextBoxTimeSeriesPeriod.Text = "";
            TextBoxTimeSeriesFrequency.Text = "";
            TextBoxTimeSeriesSubCategory.Text = "";
            if (_timeseriesVendorCode != 1 && _timeseriesVendorCode != 5)//1=Fanuc, 2=KCNC
            {
                ComboBoxTimeSeriesMachieID.IsEnabled = false;
                ComboBoxTimeSeriesStatus.IsEnabled = false;
                ComboBoxTimeSeriesMode.IsEnabled = false;
                TextBoxTimeSeriesPeriod.IsEnabled = false;
                TextBoxTimeSeriesFrequency.IsEnabled = false;
                ButtonTimeSeriesPeriod.IsEnabled = false;
                ButtonTimeSeriesFrequency.IsEnabled = false;
                ComboBoxTimeSeriesCategory.IsEnabled = false;
                TextBoxTimeSeriesSubCategory.IsEnabled = false;
                ButtonTimeSeriesSubCategory.IsEnabled = false;
                ButtonTimeseriesStart.IsEnabled = false;
                ButtonTimeseriesStop.IsEnabled = false;
                return;
            }
            ComboBoxTimeSeriesStatus.Items.Add("0:설정 가능");
            ComboBoxTimeSeriesStatus.Items.Add("1:샘플링 준비");
            ComboBoxTimeSeriesStatus.Items.Add("2:수집대기");
            ComboBoxTimeSeriesStatus.Items.Add("3:수집중");
            ComboBoxTimeSeriesStatus.Items.Add("4:수집대기 혹은 수집중");
            ComboBoxTimeSeriesStatus.Items.Add("5:수집 종료");
            ComboBoxTimeSeriesStatus.Items.Add("-1:연결 실패");
            ComboBoxTimeSeriesStatus.Items.Add("-2:설정값 적용 실패");
            ComboBoxTimeSeriesMode.Items.Add("0:연속수집");
            ComboBoxTimeSeriesMode.Items.Add("1:1회수집");
            if (_timeseriesVendorCode == 1)
            {
                ComboBoxTimeSeriesMode.Items.Add("2:연속수집(저밀도)");
            }
            if (_timeseriesVendorCode == 1)
            {
                ComboBoxTimeSeriesCategory.Items.Add("0:Axis-POSF,Spindle-CSPOS");
                ComboBoxTimeSeriesCategory.Items.Add("1:Axis-ERR,Spindle-SPEED");
                ComboBoxTimeSeriesCategory.Items.Add("2:Axis-VCMD,Spindle-TCMD");
                ComboBoxTimeSeriesCategory.Items.Add("3:Axis-TCMD,Spindle-VCMD");
                ComboBoxTimeSeriesCategory.Items.Add("4:Axis-SPEED,Spindle-ERR");
                ComboBoxTimeSeriesCategory.Items.Add("5:Axis-DTRQ,Spindle-LMDAT");
                ComboBoxTimeSeriesCategory.Items.Add("6:Axis-SYNC,Spindle-DTRQ");
                ComboBoxTimeSeriesCategory.Items.Add("7:Axis-OVCLV,Spindle-INORM");
                ComboBoxTimeSeriesCategory.Items.Add("8:Axis-IEFF,Spindle-MTTMP");
                ComboBoxTimeSeriesCategory.Items.Add("9:Axis-VDC,Spindle-EPMTR");
                ComboBoxTimeSeriesCategory.Items.Add("10:Axis-SFERR,Spindle-FREQ");
                ComboBoxTimeSeriesCategory.Items.Add("11:Axis-ALGCMD,Spindle-GAIN");
                ComboBoxTimeSeriesCategory.Items.Add("12:Axis-ALGFB,Spindle-TCMD2");
                ComboBoxTimeSeriesCategory.Items.Add("13:Axis-IR,Spindle-PA1");
                ComboBoxTimeSeriesCategory.Items.Add("14:Axis-IR1,Spindle-PB1");
                ComboBoxTimeSeriesCategory.Items.Add("15:Axis-IR2,Spindle-PA2");
                ComboBoxTimeSeriesCategory.Items.Add("16:Axis-IR3,Spindle-PB2");
                ComboBoxTimeSeriesCategory.Items.Add("17:Axis-IR4,Spindle-VERR");
                ComboBoxTimeSeriesCategory.Items.Add("18:Axis-IS,Spindle-SPSPD");
                ComboBoxTimeSeriesCategory.Items.Add("19:Axis-IS1,Spindle-LMDTC");
                ComboBoxTimeSeriesCategory.Items.Add("20:Axis-IS2,Spindle-LMMAX");
                ComboBoxTimeSeriesCategory.Items.Add("21:Axis-IS3,Spindle-DURTM");
                ComboBoxTimeSeriesCategory.Items.Add("22:Axis-IS4");
                ComboBoxTimeSeriesCategory.Items.Add("23:Axis-ID");
                ComboBoxTimeSeriesCategory.Items.Add("24:Axis-IQ");
                ComboBoxTimeSeriesCategory.Items.Add("25:Axis-MTTMP");
                ComboBoxTimeSeriesCategory.Items.Add("26:Axis-PCTMP");
                ComboBoxTimeSeriesCategory.Items.Add("27:Axis-VCOUT");
                ComboBoxTimeSeriesCategory.Items.Add("28:Axis-VCC0");
                ComboBoxTimeSeriesCategory.Items.Add("29:Axis-BLACL");
                ComboBoxTimeSeriesCategory.Items.Add("30:Axis-ABS");
                ComboBoxTimeSeriesCategory.Items.Add("31:Axis-FREQ");
                ComboBoxTimeSeriesCategory.Items.Add("32:Axis-FRTCM");
                ComboBoxTimeSeriesCategory.Items.Add("33:Axis-VDC");
                ComboBoxTimeSeriesCategory.Items.Add("34:Axis-ACC");
                ComboBoxTimeSeriesCategory.Items.Add("35:Axis-ACC1");
                ComboBoxTimeSeriesCategory.Items.Add("36:Axis-ACC2");
                ComboBoxTimeSeriesCategory.Items.Add("37:Axis-ACC3");
                ComboBoxTimeSeriesCategory.Items.Add("38:Axis-FRVCM");
                ComboBoxTimeSeriesCategory.Items.Add("39:Axis-BLCMP");
                ComboBoxTimeSeriesCategory.Items.Add("40:Axis-BLAC1");
                ComboBoxTimeSeriesCategory.Items.Add("41:Axis-BLAC2");
                ComboBoxTimeSeriesCategory.Items.Add("42:Axis-SMTBKL");
                ComboBoxTimeSeriesCategory.Items.Add("43:Axis-SMTTRQ");
                ComboBoxTimeSeriesCategory.Items.Add("44:Axis-SMTBAC");
                ComboBoxTimeSeriesCategory.Items.Add("45:Axis-ALGOFS");
                ComboBoxTimeSeriesCategory.Items.Add("46:Axis-ALGOF2");
            }
            else if (_timeseriesVendorCode == 5)
            {
                ComboBoxTimeSeriesCategory.Items.Add("1:상태정보 X");
                ComboBoxTimeSeriesCategory.Items.Add("2:상태정보 Y");
                ComboBoxTimeSeriesCategory.Items.Add("3:상태정보 G");
                ComboBoxTimeSeriesCategory.Items.Add("4:상태정보 F");
                ComboBoxTimeSeriesCategory.Items.Add("5:상태정보 R");
                ComboBoxTimeSeriesCategory.Items.Add("6:상태정보 T");
                ComboBoxTimeSeriesCategory.Items.Add("7:상태정보 C");
                ComboBoxTimeSeriesCategory.Items.Add("8:상태정보 D");
                ComboBoxTimeSeriesCategory.Items.Add("9:상태정보 SN");
                ComboBoxTimeSeriesCategory.Items.Add("10:상태정보 SV");
                ComboBoxTimeSeriesCategory.Items.Add("11:상태정보 SSV");
                ComboBoxTimeSeriesCategory.Items.Add("12:상태정보 MS");
            }
            GetTimeseriesSettingvalue();
            _OffChanged = false;
        }

        private void GetTimeseriesSettingvalue()
        {
            _OffChanged = true;
            int tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString());
            int tmpTotalCount = 15;
            string[] addressArray = new string[tmpTotalCount];
            string[] filter_Array = new string[tmpTotalCount];
            addressArray[0] = "data://machine/buffer/statusOfStream";
            filter_Array[0] = "machine=" + tmpMachineID + "&buffer=1";
            addressArray[1] = "data://machine/buffer/bufferEnabled";
            filter_Array[1] = "machine=" + tmpMachineID + "&buffer=1";
            addressArray[2] = "data://machine/buffer/numberOfStream";
            filter_Array[2] = "machine=" + tmpMachineID + "&buffer=1";
            addressArray[3] = "data://machine/buffer/modOfStream";
            filter_Array[3] = "machine=" + tmpMachineID + "&buffer=1";
            addressArray[4] = "data://machine/buffer/machineChannelOfStream";
            filter_Array[4] = "machine=" + tmpMachineID + "&buffer=1";
            addressArray[5] = "data://machine/buffer/periodOfStream";
            filter_Array[5] = "machine=" + tmpMachineID + "&buffer=1";
            addressArray[6] = "data://machine/buffer/triggerOfStream";
            filter_Array[6] = "machine=" + tmpMachineID + "&buffer=1";
            addressArray[7] = "data://machine/buffer/stream/streamEnabled";
            filter_Array[7] = "machine=" + tmpMachineID + "&buffer=1&stream=1";
            addressArray[8] = "data://machine/buffer/stream/streamFrequency";
            filter_Array[8] = "machine=" + tmpMachineID + "&buffer=1&stream=1";
            addressArray[9] = "data://machine/buffer/stream/streamEnabled";
            filter_Array[9] = "machine=" + tmpMachineID + "&buffer=1&stream=1";
            addressArray[10] = "data://machine/buffer/stream/streamCategory";
            filter_Array[10] = "machine=" + tmpMachineID + "&buffer=1&stream=1";
            addressArray[11] = "data://machine/buffer/stream/streamSubcategory";
            filter_Array[11] = "machine=" + tmpMachineID + "&buffer=1&stream=1";
            addressArray[12] = "data://machine/buffer/stream/streamType";
            filter_Array[12] = "machine=" + tmpMachineID + "&buffer=1&stream=1";
            addressArray[13] = "data://machine/buffer/stream/streamStartBit";
            filter_Array[13] = "machine=" + tmpMachineID + "&buffer=1&stream=1";
            addressArray[14] = "data://machine/buffer/stream/streamEndBit";
            filter_Array[14] = "machine=" + tmpMachineID + "&buffer=1&stream=1";
            Item[] itemArray = new Item[tmpTotalCount];

            int tmpResult = Api.getData(addressArray, filter_Array, out itemArray, true, _userApiTimeout);
            if (tmpResult == 558891055)
            {
                return;
            }
            else
            {
                for (int i = 0; i < tmpTotalCount; i++)
                {
                    int status = itemArray[i].GetValueInt("status");
                    if (status == 0)
                    {
                        if (i == 0)//data://machine/buffer/statusOfStream
                        {
                            int value = itemArray[i].GetValueInt("value");
                            if (value >= 0 && value <= 5)
                            {
                                ComboBoxTimeSeriesStatus.SelectedIndex = value;
                            }
                            else if (value == -1)
                            {
                                ComboBoxTimeSeriesStatus.SelectedIndex = 6;
                            }
                            else if (value == -2)
                            {
                                ComboBoxTimeSeriesStatus.SelectedIndex = 7;
                            }
                            if (value == 2 || value == 3 || value == 4)
                            {
                                ComboBoxTimeSeriesMachieID.IsEnabled = false;
                                ComboBoxTimeSeriesStatus.IsEnabled = false;
                                ButtonTimeseriesStart.IsEnabled = false;
                                ButtonTimeseriesStop.IsEnabled = true;
                            }
                            else
                            {
                                ComboBoxTimeSeriesMachieID.IsEnabled = true;
                                ComboBoxTimeSeriesStatus.IsEnabled = true;
                                ButtonTimeseriesStart.IsEnabled = true;
                                ButtonTimeseriesStop.IsEnabled = false;
                            }
                            if (value == 0)
                            {
                                ComboBoxTimeSeriesMode.IsEnabled = true;
                                TextBoxTimeSeriesPeriod.IsEnabled = true;
                                ButtonTimeSeriesPeriod.IsEnabled = true;
                                ButtonTimeSeriesFrequency.IsEnabled = true;
                                TextBoxTimeSeriesFrequency.IsEnabled = true;
                                ComboBoxTimeSeriesCategory.IsEnabled = true;
                                TextBoxTimeSeriesSubCategory.IsEnabled = true;
                                ButtonTimeSeriesSubCategory.IsEnabled = true;
                            }
                            else
                            {
                                ComboBoxTimeSeriesMode.IsEnabled = false;
                                TextBoxTimeSeriesPeriod.IsEnabled = false;
                                ButtonTimeSeriesPeriod.IsEnabled = false;
                                ButtonTimeSeriesFrequency.IsEnabled = false;
                                TextBoxTimeSeriesFrequency.IsEnabled = false;
                                ComboBoxTimeSeriesCategory.IsEnabled = false;
                                TextBoxTimeSeriesSubCategory.IsEnabled = false;
                                ButtonTimeSeriesSubCategory.IsEnabled = false;
                            }
                        }
                        else if (i == 3)//data://machine/buffer/modOfStream
                        {
                            int value = itemArray[i].GetValueInt("value");
                            if (value >= 0 && value <= 3)
                            {
                                ComboBoxTimeSeriesMode.SelectedIndex = value;
                            }
                        }
                        else if (i == 5)//data://machine/buffer/periodOfStream
                        {
                            int value = itemArray[i].GetValueInt("value");
                            TextBoxTimeSeriesPeriod.Text = value.ToString();
                        }
                        else if (i == 8)//data://machine/buffer/stream/streamFrequency
                        {
                            int value = itemArray[i].GetValueInt("value");
                            TextBoxTimeSeriesFrequency.Text = value.ToString();
                        }
                        else if (i == 10)//data://machine/buffer/stream/streamCategory
                        {

                            int value = itemArray[i].GetValueInt("value");
                            if (_timeseriesVendorCode == 1)
                            {
                                ComboBoxTimeSeriesCategory.SelectedIndex = value;
                            }
                            else if (_timeseriesVendorCode == 5)
                            {
                                ComboBoxTimeSeriesCategory.SelectedIndex = value - 1;
                            }
                        }
                        else if (i == 11)//data://machine/buffer/stream/streamSubcategory
                        {
                            int value = itemArray[i].GetValueInt("value");
                            TextBoxTimeSeriesSubCategory.Text = value.ToString();
                        }
                    }
                }
            }
            _OffChanged = false;
        }

        private void ButtonTimeSeries_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonTimeSeriesPeriod)
            {
                if (!int.TryParse(TextBoxTimeSeriesPeriod.Text, out int targetValue))
                {
                    return;
                }
                int tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString());
                string inputData = "{\"value\":[" + targetValue.ToString() + "]}";
                Item InItem = Item.Parse(inputData);
                int tmpResult = Api.updateData("data://machine/buffer/periodOfStream", "machine=" + tmpMachineID + "&buffer=1", InItem, out _, _userApiTimeout);
                if (tmpResult == 0)
                {
                    System.Windows.MessageBox.Show("변경에 성공했습니다.", "성공");
                }
                else
                {
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    System.Windows.MessageBox.Show("변경 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
                GetTimeseriesSettingvalue();
            }
            else if (sender == ButtonTimeSeriesFrequency)
            {
                if (!int.TryParse(TextBoxTimeSeriesFrequency.Text, out int targetValue))
                {
                    return;
                }
                int tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString());
                string inputData = "{\"value\":[" + targetValue.ToString() + "]}";
                Item InItem = Item.Parse(inputData);
                int tmpResult = Api.updateData("data://machine/buffer/stream/streamFrequency", "machine=" + tmpMachineID + "&buffer=1&stream=1", InItem, out _, _userApiTimeout);
                if (tmpResult == 0)
                {
                    System.Windows.MessageBox.Show("변경에 성공했습니다.", "성공");
                }
                else
                {
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    System.Windows.MessageBox.Show("변경 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
                GetTimeseriesSettingvalue();
            }
            else if (sender == ButtonTimeSeriesSubCategory)
            {
                if (!int.TryParse(TextBoxTimeSeriesSubCategory.Text, out int targetValue))
                {
                    return;
                }
                int tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString());
                string inputData = "{\"value\":[" + targetValue.ToString() + "]}";
                Item InItem = Item.Parse(inputData);
                int tmpResult = Api.updateData("data://machine/buffer/stream/streamSubcategory", "machine=" + tmpMachineID + "&buffer=1&stream=1", InItem, out _, _userApiTimeout);
                if (tmpResult == 0)
                {
                    System.Windows.MessageBox.Show("변경에 성공했습니다.", "성공");
                }
                else
                {
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    System.Windows.MessageBox.Show("변경 실패\n" + tmpErrorMessage, tmpErrorCode);
                }
                GetTimeseriesSettingvalue();
            }
        }

        private void ButtonTimeseriesStart_Click(object sender, RoutedEventArgs e)
        {
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int statusValue = GetMachineId(ComboBoxTimeSeriesStatus.SelectedItem.ToString());
            if (statusValue == 0)
            {
                System.Windows.MessageBox.Show("[0:설정 가능] 상태에서는 수집할 수 없습니다.", "오류");
                return;
            }
            int bufferIndex = 1;
            int tmpMachineID = 0;
            if (ComboBoxTimeSeriesMachieID.SelectedItem != null)
            {
                tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString());
                {
                    if (_timeseriesVendorCode != 1 && _timeseriesVendorCode != 5) //Fanuc(1) //Kcnc(5)
                    {
                        System.Windows.MessageBox.Show("TORUS는 해당 Vendor에서의 Timeseries를 지원하지 않습니다.", "오류");
                        return;
                    }
                }
            }
            if (tmpMachineID == 0)
            {
                System.Windows.MessageBox.Show("MachineID를 확인하십시오", "오류");
                return;
            }
            if (_isTimeSeriesCallbackRegistered == false)
            {
                Api.regist_callback((int)CALLBACK_TYPE.ON_TIMESERIESDATA, OnTimeseriesBufferData); //콜백함수를 등록합니다. 앱을 실행하는 동안 딱 한번만 등록하면 됩니다.
                _isTimeSeriesCallbackRegistered = true;
            }
            SetTimeout();
            int tmpResult = Api.startTimeSeries(bufferIndex, tmpMachineID, _userApiTimeout);
            if (tmpResult == 0)
            {
                CheckForTest("startTimeSeries", true);
                TextBlockTimeseries.Text = "Api.startTimeSeries 성공";
                ButtonTimeseriesStart.IsEnabled = false;

                ComboBoxTimeSeriesMachieID.IsEnabled = false;
                ComboBoxTimeSeriesStatus.IsEnabled = false;
                ComboBoxTimeSeriesMode.IsEnabled = false;
                TextBoxTimeSeriesPeriod.IsEnabled = false;
                ButtonTimeSeriesPeriod.IsEnabled = false;
                ButtonTimeSeriesFrequency.IsEnabled = false;
                TextBoxTimeSeriesFrequency.IsEnabled = false;
                ComboBoxTimeSeriesCategory.IsEnabled = false;
                TextBoxTimeSeriesSubCategory.IsEnabled = false;
                ButtonTimeSeriesSubCategory.IsEnabled = false;
                ButtonTimeseriesStop.IsEnabled = true;

                int currentStatusValue = statusValue;
                Task.Run(() =>
                {
                    _OffChanged = true;
                    while (true)
                    {
                        Thread.Sleep(1000);
                        int statusResult = Api.getData("data://machine/buffer/statusOfStream", "machine=" + tmpMachineID + "&buffer=1", out Item item, true, _userApiTimeout);
                        if (statusResult == 0)
                        {
                            int value = item.GetValueInt("value");

                            if (currentStatusValue != value)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (value >= 0 && value <= 5)
                                    {
                                        ComboBoxTimeSeriesStatus.SelectedIndex = value;
                                    }
                                    else if (value == -1)
                                    {
                                        ComboBoxTimeSeriesStatus.SelectedIndex = 6;
                                    }
                                    else if (value == -2)
                                    {
                                        ComboBoxTimeSeriesStatus.SelectedIndex = 7;
                                    }
                                });
                                currentStatusValue = value;
                            }
                            if (value != 2 && value != 3 && value != 4)
                            {
                                break;
                            }
                        }
                    }
                    _OffChanged = false;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        GetTimeseriesSettingvalue();
                    });
                });
            }
            else
            {
                CheckForTest("startTimeSeries", false);
                TextBlockTimeseries.Text = "Api.startTimeSeries 실패";
            }
        }

        private void ButtonTimeseriesStop_Click(object sender, RoutedEventArgs e)
        {
            if (_connectResult != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int bufferIndex = 1;
            int tmpMachineID = 1;
            if (ComboBoxTimeSeriesMachieID.SelectedItem != null)
            {
                tmpMachineID = GetMachineId(ComboBoxTimeSeriesMachieID.SelectedItem.ToString());
                {
                    if (_timeseriesVendorCode != 1 && _timeseriesVendorCode != 5) //Fanuc(1) //Kcnc(5)
                    {
                        System.Windows.MessageBox.Show("TORUS는 해당 Vendor에서의 Timeseries를 지원하지 않습니다.", "오류");
                        return;
                    }
                }
            }
            SetTimeout();
            int statusValue = GetMachineId(ComboBoxTimeSeriesStatus.SelectedItem.ToString());
            int tmpResult = Api.endTimeSeries(bufferIndex, tmpMachineID, _userApiTimeout);
            if (tmpResult == 0)
            {
                CheckForTest("endTimeSeries", true);
                TextBlockTimeseries.Text = "Api.endTimeSeries 성공";
                if (_isTimeSeriesCallbackRegistered == false)
                {
                    Task.Run(() =>
                    {
                        int currentStatusValue = statusValue;
                        _OffChanged = true;
                        while (true)
                        {
                            Thread.Sleep(1000);
                            int statusResult = Api.getData("data://machine/buffer/statusOfStream", "machine=" + tmpMachineID + "&buffer=1", out Item item, true, _userApiTimeout);
                            if (statusResult == 0)
                            {
                                int value = item.GetValueInt("value");
                                if (currentStatusValue != value)
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        if (value >= 0 && value <= 5)
                                        {
                                            ComboBoxTimeSeriesStatus.SelectedIndex = value;
                                        }
                                        else if (value == -1)
                                        {
                                            ComboBoxTimeSeriesStatus.SelectedIndex = 6;
                                        }
                                        else if (value == -2)
                                        {
                                            ComboBoxTimeSeriesStatus.SelectedIndex = 7;
                                        }
                                    });
                                    currentStatusValue = value;
                                }
                                if (value != 2 && value != 3 && value != 4)
                                {
                                    break;
                                }
                            }
                        }
                        _OffChanged = false;
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            GetTimeseriesSettingvalue();
                        });
                    });
                }
            }
            else
            {
                CheckForTest("endTimeSeries", false);
                TextBlockTimeseries.Text = "Api.endTimeSeries 실패";
            }
        }

        private void ButtonEtc_Click(object sender, RoutedEventArgs e)
        {
            //if (isDoubleRunning_)
            //{
            //    threadSignlestopFlag_ = true;
            //    threadMultistopFlag_ = true;
            //    ButtonSingleStop.IsEnabled = false;
            //    ButtonEtc.Content = "Double Start";
            //    isDoubleRunning_ = false;
            //}
            //else
            //{
            //    SingleStart(false);
            //    MultiStart(false);
            //    ButtonMultiStop.IsEnabled = false;
            //    ButtonEtc.Content = "Double Stop";
            //    isDoubleRunning_ = true;
            //}
        }
    }
}
