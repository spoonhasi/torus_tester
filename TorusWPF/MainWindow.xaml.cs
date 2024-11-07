using System;
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

namespace TorusWPF
{
    static class Constants
    {
        // App의 고유 ID와 Name를 설정합니다. TorusTester.info 라는 파일명으로 저장되어 있습니다. TorusTester.info을 TORUS/Binary/application에 복사하고 TORUS를 실행시켜야 합니다.
        public const string AppID = "FAFC456B-FA41-40AD-B1EB-C3834076A1DC";
        public const string AppName = "TorusTester";
        public const string ConfigFileName = "config.ini";
    }

    public class NCmachine
    {
        public bool activate { get; set; } = false;
        public string name { get; set; } = "";
        public int id { get; set; } = 0;
        public string vendorCode { get; set; } = "";
        public string address { get; set; } = "";
        public int port { get; set; } = 0;
        public string exDllPath { get; set; } = "";
        public string connectCode { get; set; } = "";
        public string ncVersionCode { get; set; } = "";
        public string toolSystem { get; set; } = "";
        public string username { get; set; } = "";
        public string password { get; set; } = "";
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

        private List<MachineObject> connectedMachineList_ = [];
        private int connectResult_;
        private int cncVendorCode_;
        private Thread threadSingle_;
        private bool threadSignlestopFlag_;
        private Thread threadMulti_;
        private bool threadMultistopFlag_;
        private string commonFilter_;
        private int SignalCopyOrMove_;
        private string addressFilePath_;
        private bool direct_;
        private int timeout_;
        private bool isTimeSeriesCallbackRegistered_;
        private ObservableCollection<NCmachine> ncMachineList_ { get; set; } = [];

        public MainWindow()
        {
            InitializeComponent();

            // 소스코드의 Api폴더에 TORUS/Example_VS19/Api의 내용물을 복사해야 합니다. 본 App은 TORUS v2.3.0의 API로 제작되었습니다.
            ListViewMachineList.ItemsSource = ncMachineList_;
            isTimeSeriesCallbackRegistered_ = false;
            connectResult_ = -1;
            cncVendorCode_ = 0;
            SignalCopyOrMove_ = 0; // 0은 초기화, 1은 복사, 2는 이동
            ButtonSingleStart.IsEnabled = false;
            ButtonSingleStop.IsEnabled = false;
            ButtonMultiStart.IsEnabled = false;
            ButtonMultiStop.IsEnabled = false;
            ButtonMachineMonitoringStart.IsEnabled = false;
            ButtonMachineMonitoringStop.IsEnabled = false;
            TextBlockSon.Text = "0";
            TextBlockMom.Text = "0";
            TextBlockGetDataSon.Text = "0";
            TextBlockGetDataMom.Text = "0";
            TextBlockGetDataPercent.Text = "%";
            TextBlockMissionSon.Text = "0";
            TextBlockMissionMom.Text = "0";
            TextBlockTotalSon.Text = "0";
            TextBlockTotalMom.Text = "0";
            TextBlockTotalPercent.Text = "%";
            TextBlockAddressFilePath.Text = "";
            TextBoxTimeout.Text = ReadConfig(Constants.ConfigFileName, "Main", "Timeout", "100000");
            addressFilePath_ = "";
            TextBlockMemoryTotal.Text = "";
            TextBlockMemoryUsed.Text = "";
            TextBlockMemoryFree.Text = "";
            ComboBoxPeriod.Items.Add("직접통신=True(직접통신)");
            ComboBoxPeriod.Items.Add("직접통신=False(주기통신)");
            ComboBoxPeriod.SelectedIndex = Convert.ToInt32(ReadConfig(Constants.ConfigFileName, "Main", "Periodicity", "0"));
            TextBoxTorusRunPath.Text = ReadConfig(Constants.ConfigFileName, "Main", "TorusRunPath", "");
            TextBoxTorusExitPath.Text = ReadConfig(Constants.ConfigFileName, "Main", "TorusExitPath", "");
            TextBoxTorusMachineListPath.Text = ReadConfig(Constants.ConfigFileName, "Main", "TorusMachineListPath", "");
            if (ComboBoxPeriod.SelectedIndex == 0)
            {
                direct_ = true;
            }
            else
            {
                direct_ = false;
            }
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
            threadSignlestopFlag_ = true;
            threadSingle_?.Join();
            threadMultistopFlag_ = true;
            threadMulti_?.Join();
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
            timeout_ = timeout;
            WriteConfig(Constants.ConfigFileName, "Main", "Timeout", timeout.ToString());
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
            double tmpGetDataSonCount = Convert.ToInt32(TextBlockGetDataSon.Text);
            double tmpGetDataMomCount = Convert.ToInt32(TextBlockGetDataMom.Text);
            double tmpGetDataPercent;
            if (tmpGetDataMomCount == 0)
            {
                tmpGetDataPercent = 0;
            }
            else
            {
                tmpGetDataPercent = tmpGetDataSonCount / tmpGetDataMomCount * 100;
            }
            double tmpMissionSonCount = Convert.ToInt32(TextBlockMissionSon.Text);
            double tmpMissionMomCount = Convert.ToInt32(TextBlockMissionMom.Text);
            double tmpTotalSon = tmpGetDataSonCount + tmpMissionSonCount;
            double tmpTotalMom = tmpGetDataMomCount + tmpMissionMomCount;
            TextBlockTotalSon.Text = tmpTotalSon.ToString();
            TextBlockTotalMom.Text = tmpTotalMom.ToString();
            double tmpTotalPercent;
            if (tmpTotalMom == 0)
            {
                tmpTotalPercent = 0;
            }
            else
            {
                tmpTotalPercent = tmpTotalSon / tmpTotalMom * 100;
            }
            TextBlockGetDataPercent.Text = tmpGetDataPercent.ToString("#.##") + "%";
            TextBlockTotalPercent.Text = tmpTotalPercent.ToString("#.##") + "%";
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
            TextBlockMissionMom.Text = ListBoxMachineMission.Items.Count.ToString();
            TextBlockMissionSon.Text = tmpGoodCount.ToString();
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
            if (connectResult_ != 0)
            {
                // "TorusTester.info"에 기록된 "App ID"와 "App Name"을 사용합니다.
                try
                {
                    if (mapFilePath == "")
                    {
                        connectResult_ = Api.Initialize(new Guid(Constants.AppID), Constants.AppName);
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
                            connectResult_ = Api.Initialize(new Guid(Constants.AppID), Constants.AppName, fileName);
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
            if (connectResult_ == 0)
            {
                connectedMachineList_.Clear();
                ComboBoxMachieID.Items.Clear();
                bool first = true;
                int result = Api.getMachinesInfo(out Item item);
                if (result == 0)
                {
                    MachinesObject machinesObject = JsonSerializer.Deserialize<MachinesObject>(item.ToString());
                    connectedMachineList_ = [.. machinesObject.Value.OrderBy(item => item.ID)];
                    foreach (var machine in connectedMachineList_)
                    {
                        ComboBoxMachieID.Items.Add(machine.ID + ":" + machine.Name);
                        if (first)
                        {
                            first = false;
                            TextBoxCommonFilter.Text = "machine=" + machine.ID;
                        }
                    }
                    ComboBoxMachieID.SelectedIndex = 0;
                    MakeMachineMonitoringList();
                }
                else
                {
                    System.Windows.MessageBox.Show("연결되어 있는 설비가 없습니다. 연결을 종료합니다.", "오류");
                    connectResult_ = -1;
                }
            }
            else if (connectResult_ == 546308133)
            {
                System.Windows.MessageBox.Show("TORUS 구동에 필요한 DLL 파일을 찾을 수 없거나 TORUS가 실행중이 아닙니다.\n오류코드: " + connectResult_, "오류");
            }
            else if (connectResult_ == 548405285)
            {
                System.Windows.MessageBox.Show("오류코드: " + connectResult_, "오류");
            }
            else if (connectResult_ == 548405249)
            {
                System.Windows.MessageBox.Show("오류코드: " + connectResult_, "오류");
            }
            else
            {
                System.Windows.MessageBox.Show("알 수 없는 오류 : " + connectResult_.ToString(), "오류");
            }

            if (connectResult_ == 0)
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
            addressFilePath_ = _filePath;
            TextBlockMissionSon.Text = "0";
            TextBlockMom.Text = ListBoxMachineState.Items.Count.ToString();
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
            addressFilePath_ = "";
            TextBlockMissionSon.Text = "0";
            TextBlockMom.Text = "0";
            AddForTest(MachineState.totalCount);
        }

        private void ButtonSingleStart_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (ListBoxMachineState.Items.Count < 1)
            {
                return;
            }
            TextBoxTimeout.IsReadOnly = true;
            TextBoxGetDataTargetCount.IsReadOnly = true;
            TextBoxCommonFilter.IsReadOnly = true;
            ButtonSingleStart.IsEnabled = false;
            ButtonMultiStart.IsEnabled = false;
            ButtonMultiStop.IsEnabled = false;
            ButtonLoad.IsEnabled = false;
            commonFilter_ = TextBoxCommonFilter.Text.Trim();
            if (commonFilter_ != "")
            {
                if (commonFilter_.Substring(commonFilter_.Length - 1, 1) == "&")
                {
                    commonFilter_ = commonFilter_[..^1]; //Substring(0, commonFilter_.Length - 1)
                }
                if (commonFilter_[..1] == "&")
                {
                    commonFilter_ = commonFilter_.Substring(1, commonFilter_.Length);
                }
            }
            threadSignlestopFlag_ = false;
            threadSingle_ = new Thread(() => ThreadSingle())
            {
                IsBackground = true
            };
            threadSingle_.Start();
            ButtonSingleStop.IsEnabled = true;
        }

        private void ButtonSingleStop_Click(object sender, RoutedEventArgs e)
        {
            threadSignlestopFlag_ = true;
        }

        private void ThreadSingle()
        {
            long tmpTotalTime = 0;
            long tmpOneTurnTime = 0;
            bool tmpCountCheck = true;
            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                TextBoxThreadPeriodResult.Text = "";
                TextBlockGetDataCurrentCount.Text = "0";
            }));
            int tmpGetDataCurrentCount = 0;
            int tmpGetDataTargetCount = 0;
            try
            {
                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    tmpGetDataTargetCount = Convert.ToInt32(TextBoxGetDataTargetCount.Text);
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
                    TextBoxGetDataTargetCount.Text = "0";
                }));
                tmpCountCheck = false;
            }
            int timeLineCount = 0;
            int tmpProcessCount = 0;
            int tmpErrCount = 0;
            int tmpSuccessCount = 0;
            int tmpTotalCount = ListBoxMachineState.Items.Count;
            string tmpAddress = "";
            string tmpFilter = "";
            int tmpResult = -1;
            string tmpErrorMessage = "";
            string tmpErrorCode = "";
            Stopwatch stopwatch = new();
            while (!threadSignlestopFlag_)
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
                        TextBlockSon.Text = tmpProcessCount.ToString();
                    }));
                    tmpAddress = (ListBoxMachineState.Items[i] as MachineState).address;
                    tmpFilter = (ListBoxMachineState.Items[i] as MachineState).filter;
                    if (commonFilter_ != "")
                    {
                        if (tmpFilter != "")
                        {
                            tmpFilter = commonFilter_ + "&" + tmpFilter;
                        }
                        else
                        {
                            tmpFilter = commonFilter_;
                        }
                    }
                    stopwatch.Restart();
                    tmpResult = Api.getData(tmpAddress, tmpFilter, out Item item, direct_, timeout_);
                    stopwatch.Stop();
                    if (tmpResult == 0)
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            (ListBoxMachineState.Items[i] as MachineState).InsertResult(item.ToString());
                            (ListBoxMachineState.Items[i] as MachineState).InsertResultDescribe(MakeSuccessMessage(item));
                            (ListBoxMachineState.Items[i] as MachineState).DrawColorMan(100, 103, 153, 255);
                            (ListBoxMachineState.Items[i] as MachineState).InsertTime("성공: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                        }));
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
                            (ListBoxMachineState.Items[i] as MachineState).InsertTime("실패: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                        }));
                        tmpErrCount++;
                    }
                    tmpOneTurnTime += stopwatch.ElapsedMilliseconds;
                    if (threadSignlestopFlag_)
                    {
                        break;
                    }
                }
                if (tmpTotalCount == tmpSuccessCount + tmpErrCount)
                {
                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        TextBlockGetDataSon.Text = tmpSuccessCount.ToString();
                        TextBlockGetDataMom.Text = tmpTotalCount.ToString();
                        CalTotal();
                    }));
                }
                if (tmpCountCheck)
                {
                    tmpTotalTime += tmpOneTurnTime;
                    tmpGetDataCurrentCount++;
                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        TextBlockGetDataCurrentCount.Text = tmpGetDataCurrentCount.ToString();
                        TextBoxThreadPeriodResult.Text = tmpGetDataCurrentCount.ToString() + "번째 : " + tmpOneTurnTime.ToString() + "ms" + "\n" + TextBoxThreadPeriodResult.Text;
                    }));
                    if (tmpGetDataCurrentCount >= tmpGetDataTargetCount)
                    {
                        threadSignlestopFlag_ = true;
                    }
                }
                else
                {
                    if (timeLineCount >= 10)
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            TextBoxThreadPeriodResult.Text = tmpOneTurnTime.ToString() + "ms\n" + TextBoxThreadPeriodResult.Text[..TextBoxThreadPeriodResult.Text.LastIndexOf('\n')];
                        }));
                    }
                    else
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            TextBoxThreadPeriodResult.Text = tmpOneTurnTime.ToString() + "ms\n" + TextBoxThreadPeriodResult.Text;
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
                        TextBoxThreadPeriodResult.Text = "Total : " + tmpTotalTime.ToString() + "ms" + "\n" + TextBoxThreadPeriodResult.Text;
                    }));
                }
                ButtonSingleStart.IsEnabled = true;
                ButtonSingleStop.IsEnabled = false;
                ButtonMultiStart.IsEnabled = true;
                ButtonMultiStop.IsEnabled = false;
                ButtonLoad.IsEnabled = true;
                TextBoxCommonFilter.IsReadOnly = false;
                TextBoxGetDataTargetCount.IsReadOnly = false;
                TextBoxTimeout.IsReadOnly = false;
            }));
        }

        public void OneGetData(int _index)
        {
            SetTimeout();
            if (connectResult_ != 0)
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
            int tmpResult = Api.getData(tmpAddress, tmpFilter, out Item item, true, timeout_);//하나만 실행
            stopwatch.Stop();
            if (tmpResult == 0)
            {
                (ListBoxMachineState.Items[_index] as MachineState).InsertResult(item.ToString());
                (ListBoxMachineState.Items[_index] as MachineState).InsertResultDescribe(MakeSuccessMessage(item));
                (ListBoxMachineState.Items[_index] as MachineState).DrawColorMan(100, 103, 153, 255);
                (ListBoxMachineState.Items[_index] as MachineState).InsertTime("성공: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
            }
            else
            {
                string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                (ListBoxMachineState.Items[_index] as MachineState).InsertResult(tmpErrorCode);
                (ListBoxMachineState.Items[_index] as MachineState).InsertResultDescribe(tmpErrorMessage);
                (ListBoxMachineState.Items[_index] as MachineState).DrawColorMan(30, 255, 100, 100);
                (ListBoxMachineState.Items[_index] as MachineState).InsertTime("실패: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
            }
            (ListBoxMachineState.Items[_index] as MachineState).ButtonOneGetData.IsEnabled = true;
        }

        public void UpdateData(int _index)
        {
            SetTimeout();
            string inputData = (ListBoxMachineState.Items[_index] as MachineState).TextBoxInputData.Text;
            if (inputData == "")
            {
                return;
            }
            if (connectResult_ != 0)
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
                System.Windows.MessageBox.Show("잘못된 형식입니다.", "오류");
                return;
            }

            int tmpResult = Api.updateData(tmpAddress, tmpFilter, InItem, out _, timeout_);
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
            else if (result == "0x21B00033")
            {
                _errCodeString = "address 혹은 filter가 누락되거나 오타가 있는 경우";
            }
            else if (result == "0x2130001C")
            {
                _errCodeString = "mgrComunication가 종료된 경우";
            }
            else if (result == "0x2130002F")
            {
                _errCodeString = "응답 시간 초과";
            }
            else
            {
                _errCodeString = "알려진 에러코드가 아닙니다.";
            }
            return result + " (" + _errCode + ")";
        }

        private string MakeObjectInfo(string _fullPath, int _machinID, out bool _isDir)
        {
            string totalInfo = "";
            int result;
            _isDir = false;
            result = Api.getAttributeExists(_fullPath, out Item item, _machinID, timeout_);
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
            result = Api.getAttributeType(_fullPath, out item, _machinID, timeout_);
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
            result = Api.getAttributeIsNc(_fullPath, out item, _machinID, timeout_);
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
            result = Api.getAttributeLogicalPath(_fullPath, out item, _machinID, timeout_);
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
            result = Api.getAttributeName(_fullPath, out item, _machinID, timeout_);
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
            result = Api.getAttributePath(_fullPath, out item, _machinID, timeout_);
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
            result = Api.getAttributeSize(_fullPath, out item, _machinID, timeout_);
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
            result = Api.getAttributeEditedTime(_fullPath, out item, _machinID, timeout_);
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
            int result = Api.getData(addressArray, filter_Array, out Item[] itemArray, true, timeout_);
            //Multi의 경우 하나만 오류가 발생해도 함수 실행 결과가 오류로 표시됩니다. itemArray의 항목을 살펴서 "error"가 포함되어 있다면 오류, 아니라면 정상값입니다.
            if (result == 556793903)
            {
                TextBlockMemoryTotal.Text = "오류";
                TextBlockMemoryUsed.Text = "오류";
                TextBlockMemoryFree.Text = "오류";
            }
            else
            {
                for (int i = 0; i < itemArray.Length; i++)
                {
                    int status = itemArray[i].GetValueInt("status");
                    if (status == 0)
                    {
                        string stringValue = itemArray[i].GetValueString("value");
                        if (i == 0)
                        {
                            TextBlockMemoryTotal.Text = stringValue;
                        }
                        else if (i == 1)
                        {
                            TextBlockMemoryUsed.Text = stringValue;
                        }
                        else
                        {
                            TextBlockMemoryFree.Text = stringValue;
                        }
                    }
                    else
                    {
                        if (i == 0)
                        {
                            TextBlockMemoryTotal.Text = "오류";
                        }
                        else if (i == 1)
                        {
                            TextBlockMemoryUsed.Text = "오류";
                        }
                        else
                        {
                            TextBlockMemoryFree.Text = "오류";
                        }
                    }
                }
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
                tmpResult = Api.getFileListEx(tmpCurrentPath, out item, tmpMachineID, timeout_);
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
                tmpResult = Api.getFileList(tmpCurrentPath, out item, tmpMachineID, timeout_);
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
            int result = Api.CreateCNCFile(tmpCurrentPath, tmpNewObjectName, out _, tmpMachineID, timeout_);
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
            int result = Api.CreateCNCFolder(tmpCurrentPath, tmpNewObjectName, out _, tmpMachineID, timeout_);
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
            int result = Api.CNCFileRename(tmpCurrentPath + _oldName, _newName, out item, tmpMachineID, timeout_);
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
            if (SignalCopyOrMove_ == 0) // 0은 초기화, 1은 복사, 2는 이동
            {
                System.Windows.MessageBox.Show("복사 혹은 이동할 파일을 지정하지 않았습니다.");
                return;
            }
            else if (SignalCopyOrMove_ == 1) //(1)복사
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
            if (SignalCopyOrMove_ == 1)
            {
                Item item;
                int result = Api.CNCFileCopy(tmpCopyOrMove, tmpCurrentPath + fileName, out item, tmpMachineID, timeout_);
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
            else if (SignalCopyOrMove_ == 2)
            {
                Item item;
                int result = Api.CNCFileMove(tmpCopyOrMove, tmpCurrentPath + fileName, out item, tmpMachineID, timeout_);
                if (result == 0)
                {
                    CheckForTest("CNCFileMove", true);
                    System.Windows.MessageBox.Show("이동 성공");
                    SignalCopyOrMove_ = 0;
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
            int result = Api.CNCFileDelete(tmpObjectName, out item, tmpMachineID, timeout_);
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
            int result = Api.CNCFileDeleteAll(tmpCurrentPath, out item, tmpMachineID, timeout_);
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
            int result = Api.CNCFileExecute(tmpChannel, tmpObjectName, out item, tmpMachineID, timeout_);
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
            if (connectResult_ != 0)
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
            int tmpResult = Api.CNCFileExecuteExtern(tmpChannel, tmpPath, out tmpItem, tmpMachineID, timeout_);
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
            int result = Api.UploadFile(tmpUploadFile, tmpCurrentPath, tmpMachineID, tmpChannel, timeout_);
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
                int result = Api.DownloadFile(tmpObjectName, localPath, tmpMachineID, tmpChannel, timeout_);
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
            if (connectResult_ != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            if (cncVendorCode_ == 1) //Fanuc
            {
                int tmpDataType = Convert.ToInt32(TextBoxPlcFanucType.Text.ToString());
                string tmpStartAddress = TextBoxPlcFanucStart.Text.ToString();
                string tmpEndAddress = TextBoxPlcFanucEnd.Text.ToString();
                Item tmpItem;
                int tmpResult = Api.getPlcSignal((Api.FANUC_PLC_TYPE)tmpDataType, tmpStartAddress, tmpEndAddress, out tmpItem, tmpMachineID, timeout_);
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
            else if (cncVendorCode_ == 2) //Siemens
            {
                string tmpAddress = TextBoxPlcSiemensAddress.Text.ToString();
                Item tmpItem;
                int tmpResult = Api.getPlcSignal(tmpAddress, out tmpItem, tmpMachineID, timeout_);
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
            else if (cncVendorCode_ == 4 || cncVendorCode_ == 5) //Mitsubishi(타입:16, 어드레스:A), Kcnc
            {
                int tmpDataType = Convert.ToInt32(TextBoxPlcKcncType.Text.ToString());
                string tmpStartAddress = TextBoxPlcKcncStart.Text.ToString();
                int tmpCount = Convert.ToInt32(TextBoxPlcKcncCount.Text.ToString());
                Item tmpItem;
                int tmpResult = Api.getPlcSignal(tmpDataType, tmpCount, tmpStartAddress, out tmpItem, tmpMachineID, timeout_);
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
            if (connectResult_ != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            if (cncVendorCode_ == 1) //Fanuc
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
                int tmpResult = Api.setPlcSignal((Api.FANUC_PLC_TYPE)tmpDataType, tmpStartAddress, tmpEndAddress, inputValue, out tmpItem, tmpMachineID, timeout_);
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
            else if (cncVendorCode_ == 2) //Siemens
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
                int tmpResult = Api.setPlcSignal(tmpAddress, inputValue, out tmpItem, tmpMachineID, timeout_);
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
            else if (cncVendorCode_ == 4 || cncVendorCode_ == 5) //Mitsubishi(타입:16, 어드레스:A), Kcnc
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
                int tmpResult = Api.setPlcSignal(tmpDataType, tmpCount, tmpStartAddress, inputValue, out tmpItem, tmpMachineID, timeout_);
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
            int tmpResult = Api.getData("data://machine/ncmemory/rootpath", "machine=" + tmpMachineID, out item, true, timeout_);
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
            int tmpResult = Api.getData("data://machine/cncvendor", "machine=" + tmpMachineID.ToString(), out item, true, timeout_);
            if (tmpResult == 0)
            {
                cncVendorCode_ = item.GetValueInt("value");
                string tmpStr;
                if (cncVendorCode_ == 1)
                {
                    tmpStr = cncVendorCode_.ToString() + " (Fanuc)";
                }
                else if (cncVendorCode_ == 2)
                {
                    tmpStr = cncVendorCode_.ToString() + " (Siemens)";
                }
                else if (cncVendorCode_ == 3)
                {
                    tmpStr = cncVendorCode_.ToString() + " (CSCAM)";
                }
                else if (cncVendorCode_ == 4)
                {
                    tmpStr = cncVendorCode_.ToString() + " (Mitsubishi)";
                }
                else if (cncVendorCode_ == 5)
                {
                    tmpStr = cncVendorCode_.ToString() + " (KCNC)";
                }
                else if (cncVendorCode_ == 6)
                {
                    tmpStr = cncVendorCode_.ToString() + " (MAZAK)";
                }
                else if (cncVendorCode_ == 7)
                {
                    tmpStr = cncVendorCode_.ToString() + " (Heidenhain)";
                }
                else
                {
                    tmpStr = cncVendorCode_.ToString() + " (UNKNOWN)";
                }
                TextBlockVendorCode.Text = "VendorCode : " + tmpStr;
            }
            else
            {
                cncVendorCode_ = 0;
                TextBlockVendorCode.Text = "VendorCode : 0 (오류)";
            }
            tmpResult = Api.getData("data://machine/ncmemory/rootpath", "machine=" + tmpMachineID, out item, true, timeout_);
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
            SignalCopyOrMove_ = 0;
            TextBoxCopyOrMove.Text = "";
        }

        private void ButtonGetGmodal_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (connectResult_ != 0)
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
            int tmpResult = Api.getGModal(tmpChannel, out tmpItem, tmpMachineID, timeout_);
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
            if (connectResult_ != 0)
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
            int tmpResult = Api.getExModal(tmpChannel, tmpModal, out tmpItem, tmpMachineID, timeout_);
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
            if (connectResult_ != 0)
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
            if (cncVendorCode_ == 1 || cncVendorCode_ == 4 || cncVendorCode_ == 5) //Fanuc, Mitsubishi, KCNC
            {
                string tmpType = TextBoxToolOffsetType.Text.ToString();
                string tmpNumber = TextBoxToolOffsetNumber.Text.ToString();
                Item tmpItem;
                int tmpResult = Api.getToolOffsetData(tmpChannel.ToString(), tmpNumber, tmpType, true, out tmpItem, tmpMachineID, timeout_);
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
            if (connectResult_ != 0)
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
            if (cncVendorCode_ == 1 || cncVendorCode_ == 4 || cncVendorCode_ == 5) //Fanuc, Mitsubishi, KCNC
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
                int tmpResult = Api.setToolOffsetData(tmpChannel.ToString(), tmpNumber, tmpType, inputValue, out tmpItem, tmpMachineID, timeout_);
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
            SignalCopyOrMove_ = 1;
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
            SignalCopyOrMove_ = 2;
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
            if (addressFilePath_ != "")
            {
                SaveAddressFile(addressFilePath_);
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
            addressFilePath_ = _addressFilePath;
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
                MachineState.totalCount = ListBoxMachineState.Items.Count;
                TextBlockMom.Text = ListBoxMachineState.Items.Count.ToString();
                for (int i = 0; i < ListBoxMachineState.Items.Count; i++)
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
            if (connectResult_ == 0 && ButtonMultiStart.IsEnabled == false)
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
            MachineState.totalCount = ListBoxMachineState.Items.Count;
            TextBlockMom.Text = ListBoxMachineState.Items.Count.ToString();
            for (int i = 0; i < ListBoxMachineState.Items.Count; i++)
            {
                (ListBoxMachineState.Items[i] as MachineState).InsertNumber(i + 1);
            }
        }

        private void ComboBoxPeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WriteConfig(Constants.ConfigFileName, "Main", "Periodicity", ComboBoxPeriod.SelectedIndex.ToString());
            if (ComboBoxPeriod.SelectedIndex == 0)
            {
                direct_ = true;
            }
            else
            {
                direct_ = false;
            }
        }

        private void ButtonMultiStart_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (ListBoxMachineState.Items.Count < 1)
            {
                return;
            }
            TextBoxTimeout.IsReadOnly = true;
            TextBoxGetDataTargetCount.IsReadOnly = true;
            TextBoxCommonFilter.IsReadOnly = true;
            ButtonSingleStart.IsEnabled = false;
            ButtonSingleStop.IsEnabled = false;
            ButtonMultiStart.IsEnabled = false;
            ButtonLoad.IsEnabled = false;
            commonFilter_ = TextBoxCommonFilter.Text.Trim();
            if (commonFilter_ != "")
            {
                if (commonFilter_.Substring(commonFilter_.Length - 1, 1) == "&")
                {
                    commonFilter_ = commonFilter_.Substring(0, commonFilter_.Length - 1);
                }
                if (commonFilter_.Substring(0, 1) == "&")
                {
                    commonFilter_ = commonFilter_.Substring(1, commonFilter_.Length);
                }
            }
            threadMultistopFlag_ = false;
            threadMulti_ = new Thread(() => ThreadMulti())
            {
                IsBackground = true
            };
            threadMulti_.Start();
            ButtonMultiStop.IsEnabled = true;
        }

        private void ButtonMultiStop_Click(object sender, RoutedEventArgs e)
        {
            threadMultistopFlag_ = true;
        }

        private void ThreadMulti()
        {
            long tmpTotalTime = 0;
            long tmpOneTurnTime = 0;
            bool tmpCountCheck = true;
            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                TextBoxThreadPeriodResult.Text = "";
                TextBlockGetDataCurrentCount.Text = "0";
                TextBlockSon.Text = "0";
            }));
            int tmpGetDataCurrentCount = 0;
            int tmpGetDataTargetCount = 0;
            try
            {
                _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    tmpGetDataTargetCount = Convert.ToInt32(TextBoxGetDataTargetCount.Text);
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
                    TextBoxGetDataTargetCount.Text = "0";
                }));
                tmpCountCheck = false;
            }
            int timeLineCount = 0;
            while (!threadMultistopFlag_)
            {
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
                    if (commonFilter_ != "")
                    {
                        if (tmpFilter != "")
                        {
                            tmpFilter = commonFilter_ + "&" + tmpFilter;
                        }
                        else
                        {
                            tmpFilter = commonFilter_;
                        }
                    }
                    addressArray[i] = tmpAddress;
                    filter_Array[i] = tmpFilter;
                    if (threadMultistopFlag_)
                    {
                        break;
                    }
                }
                stopwatch.Restart();
                int tmpResult = Api.getData(addressArray, filter_Array, out itemArray, direct_, timeout_);
                stopwatch.Stop();
                //Multi의 경우 하나만 오류가 발생해도 함수 실행 결과가 오류로 표시됩니다. itemArray의 "status"의 값이 0이 아니라면 오류입니다.
                if (tmpResult == 556793903)
                {
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out string tmpErrorMessage);
                    for (int i = 0; i < tmpTotalCount; i++)
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            (ListBoxMachineState.Items[i] as MachineState).InsertResult(tmpErrorCode);
                            (ListBoxMachineState.Items[i] as MachineState).InsertResultDescribe(tmpErrorMessage);
                            (ListBoxMachineState.Items[i] as MachineState).DrawColorMan(30, 255, 100, 100);
                            (ListBoxMachineState.Items[i] as MachineState).InsertTime("실패: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                        }));
                    }
                }
                else
                {
                    for (int i = 0; i < tmpTotalCount; i++)
                    {
                        int status = itemArray[i].GetValueInt("status");
                        if (status == 0)
                        {
                            _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                (ListBoxMachineState.Items[i] as MachineState).InsertResult(itemArray[i].ToString());
                                (ListBoxMachineState.Items[i] as MachineState).InsertResultDescribe(MakeSuccessMessage(itemArray[i]));
                                (ListBoxMachineState.Items[i] as MachineState).DrawColorMan(100, 103, 153, 255);
                                (ListBoxMachineState.Items[i] as MachineState).InsertTime("성공: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
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
                                (ListBoxMachineState.Items[i] as MachineState).InsertTime("실패: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                            }));
                        }
                    }
                }

                tmpOneTurnTime = stopwatch.ElapsedMilliseconds;
                if (tmpCountCheck == true)
                {
                    tmpTotalTime = tmpTotalTime + tmpOneTurnTime;
                    tmpGetDataCurrentCount++;
                    _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        TextBlockGetDataCurrentCount.Text = tmpGetDataCurrentCount.ToString();
                        TextBoxThreadPeriodResult.Text = tmpGetDataCurrentCount.ToString() + "번째 : " + tmpOneTurnTime.ToString() + "ms" + "\n" + TextBoxThreadPeriodResult.Text;
                    }));
                    if (tmpGetDataCurrentCount >= tmpGetDataTargetCount)
                    {
                        threadMultistopFlag_ = true;
                    }
                }
                else
                {
                    if (timeLineCount >= 11)
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            string tmpString = TextBoxThreadPeriodResult.Text;
                            if (tmpString.Substring(tmpString.Length - 1) == "\n")
                            {
                                tmpString = tmpString.Substring(0, tmpString.Length - 1);
                            }
                            tmpString = tmpString.Substring(0, tmpString.LastIndexOf('\n'));
                            TextBoxThreadPeriodResult.Text = tmpOneTurnTime.ToString() + "ms\n" + tmpString;
                        }));
                    }
                    else
                    {
                        _ = System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            if (TextBoxThreadPeriodResult.Text == "")
                            {
                                TextBoxThreadPeriodResult.Text = tmpOneTurnTime.ToString() + "ms";
                            }
                            else
                            {
                                TextBoxThreadPeriodResult.Text = tmpOneTurnTime.ToString() + "ms\n" + TextBoxThreadPeriodResult.Text;
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
                        TextBoxThreadPeriodResult.Text = "Total : " + tmpTotalTime.ToString() + "ms" + "\n" + TextBoxThreadPeriodResult.Text;
                    }));
                }
                ButtonSingleStart.IsEnabled = true;
                ButtonSingleStop.IsEnabled = false;
                ButtonMultiStart.IsEnabled = true;
                ButtonMultiStop.IsEnabled = false;
                ButtonLoad.IsEnabled = true;
                TextBoxCommonFilter.IsReadOnly = false;
                TextBoxGetDataTargetCount.IsReadOnly = false;
                TextBoxTimeout.IsReadOnly = false;
            }));
        }

        private void ButtonGudGet_Click(object sender, RoutedEventArgs e)
        {
            SetTimeout();
            if (connectResult_ != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            if (cncVendorCode_ == 2) //Siemens
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
                    tmpResult = Api.getGudData(GUD_TYPE.STRING, tmpGudNumber, tmpGudCount, tmpGudAdress, false, out tmpItem, tmpMachineID, tmpGudChannel, timeout_);
                }
                else if (tmpGudType == "1")
                {
                    tmpResult = Api.getGudData(GUD_TYPE.DOUBLE, tmpGudNumber, tmpGudCount, tmpGudAdress, false, out tmpItem, tmpMachineID, tmpGudChannel, timeout_);
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
            if (connectResult_ != 0)
            {
                System.Windows.MessageBox.Show("TORUS에 접속되어 있지 않습니다.", "오류");
                return;
            }
            int tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
            if (cncVendorCode_ == 2) //Siemens
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
                    tmpResult = Api.setGudData(tmpGudNumber, tmpGudCount, tmpGudAdress, inputValue, out tmpItem, tmpMachineID, tmpGudChannel, timeout_);
                }
                else if (tmpGudType == "1")
                {
                    double[] inputValue = new double[inputDatasCount];
                    for (int i = 0; i < inputDatasCount; i++)
                    {
                        inputValue[i] = Convert.ToDouble(inputDatas[i]);
                    }
                    tmpResult = Api.setGudData(tmpGudNumber, tmpGudCount, tmpGudAdress, inputValue, out tmpItem, tmpMachineID, tmpGudChannel, timeout_);
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

        private void ButtonEtc_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Test");
        }

        private void ButtonTimeseriesStart_Click(object sender, RoutedEventArgs e)
        {
            int bufferIndex = 1;
            int tmpMachineID = 1;
            if (ComboBoxMachieID.SelectedItem != null)
            {
                tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
                {
                    if (cncVendorCode_ != 1 && cncVendorCode_ != 5) //Fanuc(1) //Kcnc(5)
                    {
                        System.Windows.MessageBox.Show("TORUS는 해당 Vendor에서의 Timeseries를 지원하지 않습니다.", "오류");
                        return;
                    }
                }
            }
            if (isTimeSeriesCallbackRegistered_ == false)
            {
                Api.regist_callback((int)CALLBACK_TYPE.ON_TIMESERIESDATA, OnTimeseriesBufferData); //콜백함수를 등록합니다. 앱을 실핼하는 동안 딱 한번만 등록하면 됩니다.
                isTimeSeriesCallbackRegistered_ = true;
            }
            SetTimeout();
            int tmpResult = Api.startTimeSeries(bufferIndex, tmpMachineID, timeout_);
            if (tmpResult == 0)
            {
                CheckForTest("startTimeSeries", true);
                TextBlockTimeseries.Text = "Api.startTimeSeries 성공";
            }
            else
            {
                CheckForTest("startTimeSeries", false);
                TextBlockTimeseries.Text = "Api.startTimeSeries 실패";
            }
        }

        private void ButtonTimeseriesStop_Click(object sender, RoutedEventArgs e)
        {
            int bufferIndex = 1;
            int tmpMachineID = 1;
            if (ComboBoxMachieID.SelectedItem != null)
            {
                tmpMachineID = GetMachineId(ComboBoxMachieID.SelectedItem.ToString());
                {
                    if (cncVendorCode_ != 1 && cncVendorCode_ != 5) //Fanuc(1) //Kcnc(5)
                    {
                        System.Windows.MessageBox.Show("TORUS는 해당 Vendor에서의 Timeseries를 지원하지 않습니다.", "오류");
                        return;
                    }
                }
            }
            SetTimeout();
            int tmpResult = Api.endTimeSeries(bufferIndex, tmpMachineID, timeout_);
            if (tmpResult == 0)
            {
                CheckForTest("endTimeSeries", true);
                TextBlockTimeseries.Text = "Api.endTimeSeries 성공";
            }
            else
            {
                CheckForTest("endTimeSeries", false);
                TextBlockTimeseries.Text = "Api.endTimeSeries 실패";
            }
        }

        private void ButtonTimeseriesLastBuffer_Click(object sender, RoutedEventArgs e)
        {

        }

        private int OnTimeseriesBufferData(EVENT_CODE evt, int cmd, Item command, ref Item result)
        {
            Item data = command.find("result");
            List<Item> tempData = (List<Item>)data.GetValue();
            Item tmpItem = tempData[0];
            double[] dvals = tmpItem.GetArrayDouble("value");
            double[] dataX = Enumerable.Range(1, dvals.Length).Select(x => (double)x).ToArray();
            double[] dataY = dvals.Take(dvals.Length).Select(x => (double)x).ToArray();
            ScootPlotTimeseries.Plot.Clear();
            ScootPlotTimeseries.Plot.Add.Scatter(dataX, dataY);
            ScootPlotTimeseries.Plot.Axes.AutoScale();
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                TextBlockTimeseries.Text = tmpItem.GetValueString("time");
                ScootPlotTimeseries.Refresh();
            });
            Debug.WriteLine("OnTimeseriesBufferData BufferCount: " + dvals.Length);
            Debug.WriteLine("OnTimeseriesBufferData Start1 Value: " + dvals[0]);
            Debug.WriteLine("OnTimeseriesBufferData Start2 Value: " + dvals[1]);
            Debug.WriteLine("OnTimeseriesBufferData Start3 Value: " + dvals[2]);
            Debug.WriteLine("OnTimeseriesBufferData Start4 Value: " + dvals[3]);
            Debug.WriteLine("OnTimeseriesBufferData Start5 Value: " + dvals[4]);
            Debug.WriteLine("OnTimeseriesBufferData End-5 Value: " + dvals[dvals.Length - 5]);
            Debug.WriteLine("OnTimeseriesBufferData End-4 Value: " + dvals[dvals.Length - 4]);
            Debug.WriteLine("OnTimeseriesBufferData End-3 Value: " + dvals[dvals.Length - 3]);
            Debug.WriteLine("OnTimeseriesBufferData End-2 Value: " + dvals[dvals.Length - 2]);
            Debug.WriteLine("OnTimeseriesBufferData End-1 Value: " + dvals[dvals.Length - 1]);
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

        private void MachineListXmlRead(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                MessageBox.Show("MachineList.xml 파일을 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!filePath.EndsWith("MachineList.xml"))
            {
                MessageBox.Show("파일 이름이 \"MachineList.xml\"이어야 합니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ncMachineList_.Clear();
            XDocument doc = XDocument.Load(filePath);
            if (doc.Root.Name.ToString().Equals("machinelist", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var element in doc.Root.Elements())
                {
                    if (element.Name.ToString().Equals("ncmachine", StringComparison.InvariantCultureIgnoreCase))
                    {
                        bool isPass = false;
                        NCmachine ncMachine = new();
                        foreach (var attribute in element.Attributes())
                        {
                            string attributeName = attribute.Name.ToString().ToLowerInvariant();
                            string attributeValue = attribute.Value.ToString();
                            if (attributeName == "activate")
                            {
                                if (attributeValue.ToLowerInvariant() == "true")
                                {
                                    ncMachine.activate = true;
                                }
                                else
                                {
                                    ncMachine.activate = false;
                                }
                            }
                            else if (attributeName == "name")
                            {
                                ncMachine.name = attributeValue;
                            }
                            else if (attributeName == "id")
                            {
                                if (int.TryParse(attributeValue, out int id))
                                {
                                    ncMachine.id = id;
                                }
                                else
                                {
                                    isPass = true;
                                    break;
                                }
                            }
                            else if (attributeName == "vendorcode")
                            {
                                ncMachine.vendorCode = attributeValue;
                            }
                            else if (attributeName == "address")
                            {
                                ncMachine.address = attributeValue;
                            }
                            else if (attributeName == "port")
                            {
                                if (int.TryParse(attributeValue, out int port))
                                {
                                    ncMachine.port = port;
                                }
                                else
                                {
                                    isPass = true;
                                    break;
                                }
                            }
                            else if (attributeName == "exdllpath")
                            {
                                ncMachine.exDllPath = attributeValue;
                            }
                            else if (attributeName == "connectcode")
                            {
                                ncMachine.connectCode = attributeValue;
                            }
                            else if (attributeName == "ncversioncode")
                            {
                                ncMachine.ncVersionCode = attributeValue;
                            }
                            else if (attributeName == "toolsystem")
                            {
                                ncMachine.toolSystem = attributeValue;
                            }
                            else if (attributeName == "username")
                            {
                                ncMachine.username = attributeValue;
                            }
                            else if (attributeName == "password")
                            {
                                ncMachine.password = attributeValue;
                            }
                        }
                        if (isPass)
                        {
                            continue;
                        }
                        ncMachineList_.Add(ncMachine);
                    }
                }
            }
            ClearMachineListItem();
            ListViewMachineList.Items.Refresh();
        }

        private void MachineListXmlWrite(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                MessageBox.Show("MachineList.xml 파일을 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!filePath.EndsWith("MachineList.xml"))
            {
                MessageBox.Show("파일 이름이 \"MachineList.xml\"이어야 합니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var activeIds = ncMachineList_
                .Where(m => m.activate)
                .Select(m => m.id)
                .ToList();

            var duplicateId = activeIds
                .GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (duplicateId != 0)
            {
                MessageBox.Show($"Activate가 true인 설비 목록 중에 중복된 id({duplicateId})가 발견되었습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            XElement rootElement = new XElement("MachineList");
            foreach (var ncMachine in ncMachineList_)
            {
                XElement machineElement = new XElement("NCmachine",
                    new XAttribute("activate", ncMachine.activate.ToString().ToLowerInvariant()),
                    new XAttribute("name", ncMachine.name),
                    new XAttribute("id", ncMachine.id),
                    new XAttribute("vendorCode", ncMachine.vendorCode),
                    new XAttribute("address", ncMachine.address),
                    new XAttribute("port", ncMachine.port),
                    new XAttribute("exDllPath", ncMachine.exDllPath),
                    new XAttribute("connectCode", ncMachine.connectCode),
                    new XAttribute("ncVersionCode", ncMachine.ncVersionCode),
                    new XAttribute("toolSystem", ncMachine.toolSystem),
                    new XAttribute("username", ncMachine.username),
                    new XAttribute("password", ncMachine.password)
                );
                rootElement.Add(machineElement);
            }
            XDocument doc = new(rootElement);
            doc.Save(filePath);
            MessageBox.Show("MachineList.xml 파일을 변경하였습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (sender == ButtonSearch)
            {
                SearchItem();
            }
            else if (sender == ButtonTorusRunPath)
            {
                if (FileSelector(TextBoxTorusRunPath.Text, out string newPath))
                {
                    WriteConfig(Constants.ConfigFileName, "Main", "TorusRunPath", newPath);
                    TextBoxTorusRunPath.Text = newPath;
                }
            }
            else if (sender == ButtonTorusExitPath)
            {
                if (FileSelector(TextBoxTorusExitPath.Text, out string newPath))
                {
                    WriteConfig(Constants.ConfigFileName, "Main", "TorusExitPath", newPath);
                    TextBoxTorusExitPath.Text = newPath;
                }
            }
            else if (sender == ButtonTorusMachineListPath)
            {
                if (FileSelector(TextBoxTorusMachineListPath.Text, out string newPath))
                {
                    WriteConfig(Constants.ConfigFileName, "Main", "TorusMachineListPath", newPath);
                    TextBoxTorusMachineListPath.Text = newPath;
                }
            }
            else if (sender == ButtonTorusRunExecute)
            {
                RunProcess(TextBoxTorusRunPath.Text);
            }
            else if (sender == ButtonTorusExitExecute)
            {
                RunProcess(TextBoxTorusExitPath.Text);
            }
            else if (sender == ButtonTorusMachineListRead)
            {
                MachineListXmlRead(TextBoxTorusMachineListPath.Text);
            }
            else if (sender == ButtonTorusMachineListWrite)
            {
                MachineListXmlWrite(TextBoxTorusMachineListPath.Text);
            }
            else if (sender == ButtonTorusMachineListItemSave)
            {
                SaveMachineListItem();
            }
            else if (sender == ButtonTorusMachineListItemCancel)
            {
                CancelMachineListItem();
            }
            else if (sender == ButtonTorusMachineListItemAdd)
            {
                AddMachineListItem();
            }
            else if (sender == ButtonTorusMachineListItemRemove)
            {
                RemoveMachineListItem();
            }
            else if (sender == ButtonTorusMachineListArrange)
            {
                ArrangeMachineListItem();
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
                foreach (var item in ListBoxMachineMonitoringList.Items)
                {
                    if (item is MachineMonitoringItem monitoring)
                    {
                        monitoring.OnOff(true);
                    }
                }
                ButtonMachineMonitoringStart.IsEnabled = false;
                ButtonMachineMonitoringStop.IsEnabled = true;
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
            }
        }

        private void ArrangeMachineListItem()
        {
            var selectedItem = ListViewMachineList.SelectedItem as NCmachine;
            var sortedList = new ObservableCollection<NCmachine>(ncMachineList_.OrderBy(x => x.id));
            ncMachineList_ = sortedList;
            ListViewMachineList.ItemsSource = ncMachineList_;
            ListViewMachineList.Items.Refresh();
        }

        private void AddMachineListItem()
        {
            NCmachine machine = new();
            machine.name = "Machine";
            machine.id = 1;
            machine.vendorCode = "Kcnc";
            machine.connectCode = "Default";
            machine.ncVersionCode = "Default";
            machine.toolSystem = "Default";
            ncMachineList_.Add(machine);
            ListViewMachineList.Items.Refresh();
            ListViewMachineList.SelectedItem = machine;
            ListViewMachineList.ScrollIntoView(machine);
        }

        private void RemoveMachineListItem()
        {
            if (ListViewMachineList.SelectedItem is not NCmachine ncMachine)
            {
                return;
            }
            ncMachineList_.Remove(ncMachine);
            ListViewMachineList.Items.Refresh();
            ClearMachineListItem();
        }

        private void SaveMachineListItem()
        {
            if (ListViewMachineList.SelectedItem is not NCmachine ncMachine)
            {
                return;
            }
            if (TextBoxTorusNcMachineName.Text.Trim() == "")
            {
                MessageBox.Show("name은 비어있을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!int.TryParse(TextBoxTorusNcMachineId.Text, out int id) || id < 1)
            {
                MessageBox.Show("id는 1 이상의 정수여야 합니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!int.TryParse(TextBoxTorusNcMachinePort.Text, out int port) || id < 0 || id > 65535)
            {
                MessageBox.Show("port는 0 이상, 65535 이하의 정수여야 합니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string activate = ((ComboBoxItem)ComboBoxTorusNcMachineActivate.SelectedItem)?.Content.ToString().ToLowerInvariant();
            if (activate == "true")
            {
                ncMachine.activate = true;
            }
            else
            {
                ncMachine.activate = false;
            }
            ncMachine.name = TextBoxTorusNcMachineName.Text.Trim();
            ncMachine.id = id;
            ncMachine.vendorCode = ((ComboBoxItem)ComboBoxTorusNcMachineVendorCode.SelectedItem)?.Content.ToString();
            ncMachine.address = TextBoxTorusNcMachineAddress.Text.Trim();
            ncMachine.port = port;
            ncMachine.exDllPath = TextBoxTorusNcMachineExDllPath.Text.Trim();
            ncMachine.connectCode = ((ComboBoxItem)ComboBoxTorusNcMachineConnectCode.SelectedItem)?.Content.ToString();
            ncMachine.ncVersionCode = ((ComboBoxItem)ComboBoxTorusNcMachineNcVersionCode.SelectedItem)?.Content.ToString();
            ncMachine.toolSystem = ((ComboBoxItem)ComboBoxTorusNcMachineToolSystem.SelectedItem)?.Content.ToString();
            ncMachine.username = TextBoxTorusNcMachineUsername.Text.Trim();
            ncMachine.password = TextBoxTorusNcMachinePassword.Text.Trim();
            ListViewMachineList.Items.Refresh();
        }

        private void LoadMachineListItem()
        {
            if (ListViewMachineList.SelectedItem is not NCmachine ncMachine)
            {
                return;
            }
            string targetValue = ncMachine.activate.ToString();
            foreach (ComboBoxItem item in ComboBoxTorusNcMachineActivate.Items)
            {
                if (string.Equals(item.Content.ToString(), targetValue, StringComparison.OrdinalIgnoreCase))
                {
                    ComboBoxTorusNcMachineActivate.SelectedItem = item;
                    break;
                }
            }
            targetValue = ncMachine.name;
            TextBoxTorusNcMachineName.Text = targetValue;
            targetValue = ncMachine.id.ToString();
            TextBoxTorusNcMachineId.Text = targetValue;
            targetValue = ncMachine.vendorCode;
            foreach (ComboBoxItem item in ComboBoxTorusNcMachineVendorCode.Items)
            {
                if (string.Equals(item.Content.ToString(), targetValue, StringComparison.OrdinalIgnoreCase))
                {
                    ComboBoxTorusNcMachineVendorCode.SelectedItem = item;
                    break;
                }
            }
            targetValue = ncMachine.address;
            TextBoxTorusNcMachineAddress.Text = targetValue;
            targetValue = ncMachine.port.ToString();
            TextBoxTorusNcMachinePort.Text = targetValue;
            targetValue = ncMachine.exDllPath;
            TextBoxTorusNcMachineExDllPath.Text = targetValue;
            targetValue = ncMachine.connectCode;
            if (targetValue.Trim() == "")
            {
                targetValue = "Default";
            }
            foreach (ComboBoxItem item in ComboBoxTorusNcMachineConnectCode.Items)
            {
                if (string.Equals(item.Content.ToString(), targetValue, StringComparison.OrdinalIgnoreCase))
                {
                    ComboBoxTorusNcMachineConnectCode.SelectedItem = item;
                    break;
                }
            }
            targetValue = ncMachine.ncVersionCode;
            if (targetValue.Trim() == "")
            {
                targetValue = "Default";
            }
            foreach (ComboBoxItem item in ComboBoxTorusNcMachineNcVersionCode.Items)
            {
                if (string.Equals(item.Content.ToString(), targetValue, StringComparison.OrdinalIgnoreCase))
                {
                    ComboBoxTorusNcMachineNcVersionCode.SelectedItem = item;
                    break;
                }
            }
            targetValue = ncMachine.toolSystem;
            if (targetValue.Trim() == "")
            {
                targetValue = "Default";
            }
            foreach (ComboBoxItem item in ComboBoxTorusNcMachineToolSystem.Items)
            {
                if (string.Equals(item.Content.ToString(), targetValue, StringComparison.OrdinalIgnoreCase))
                {
                    ComboBoxTorusNcMachineToolSystem.SelectedItem = item;
                    break;
                }
            }
            targetValue = ncMachine.username;
            TextBoxTorusNcMachineUsername.Text = targetValue;
            targetValue = ncMachine.password;
            TextBoxTorusNcMachinePassword.Text = targetValue;
        }

        private void CancelMachineListItem()
        {
            if (ListViewMachineList.SelectedItem is not NCmachine ncMachine)
            {
                ClearMachineListItem();
            }
            else
            {
                LoadMachineListItem();
            }
        }

        private void ClearMachineListItem()
        {
            ComboBoxTorusNcMachineActivate.SelectedIndex = 1;
            TextBoxTorusNcMachineName.Text = "";
            TextBoxTorusNcMachineId.Text = "";
            ComboBoxTorusNcMachineVendorCode.SelectedIndex = 4;
            TextBoxTorusNcMachineAddress.Text = "";
            TextBoxTorusNcMachinePort.Text = "";
            TextBoxTorusNcMachineExDllPath.Text = "";
            ComboBoxTorusNcMachineConnectCode.SelectedIndex = 0;
            ComboBoxTorusNcMachineNcVersionCode.SelectedIndex = 0;
            ComboBoxTorusNcMachineToolSystem.SelectedIndex = 0;
            TextBoxTorusNcMachineUsername.Text = "";
            TextBoxTorusNcMachinePassword.Text = "";
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == ListViewMachineList)
            {
                if (ListViewMachineList.SelectedItem is not NCmachine ncMachine)
                {
                    ClearMachineListItem();
                }
                else
                {
                    LoadMachineListItem();
                }
            }
        }

        private void MakeMachineMonitoringList()
        {
            ListBoxMachineMonitoringList.Items.Clear();
            foreach (MachineObject machine in connectedMachineList_)
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
    }
}
