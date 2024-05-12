using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;

using IntelligentApiCS;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using static IntelligentApiCS.Api;
using System.ComponentModel.DataAnnotations;
using System.Reflection.PortableExecutable;
using System.Windows.Markup;
using Microsoft.Win32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TorusWPF
{
    static class Constants
    {
        // App의 고유 ID와 Name를 설정합니다. TorusTester.info 라는 파일명으로 저장되어 있습니다. TorusTester.info을 TORUS/Binary/application에 복사하고 TORUS를 실행시켜야 합니다.
        public const string AppID = "FAFC456B-FA41-40AD-B1EB-C3834076A1DC";
        public const string AppName = "TorusTester";
        public const string ConfigFileName = "config.ini";
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        //설정 불러오기용 함수
        public static string ReadConfig(string _settingFileName, string _section, string _key, string _defaultValue = "")
        {
            StringBuilder temp = new StringBuilder(255);
            GetPrivateProfileString(_section, _key, null, temp, 255, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settingFileName));
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

        public MainWindow()
        {
            InitializeComponent();

            // 소스코드의 Api폴더에 TORUS/Example_VS19/Api의 내용물을 복사해야 합니다. 본 App은 TORUS v2.2.0의 API로 제작되었습니다.

            connectResult_ = -1;
            cncVendorCode_ = 0;
            SignalCopyOrMove_ = 0; // 0은 초기화, 1은 복사, 2는 이동
            ButtonSingleStart.IsEnabled = false;
            ButtonSingleStop.IsEnabled = false;
            ButtonMultiStart.IsEnabled = false;
            ButtonMultiStop.IsEnabled = false;
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
            TextBoxTimeout.Text = ReadConfig(Constants.ConfigFileName, "Main", "Timeout", "10000");
            addressFilePath_ = "";
            TextBlockMemoryTotal.Text = "";
            TextBlockMemoryUsed.Text = "";
            TextBlockMemoryFree.Text = "";
            ComboBoxPeriod.Items.Add("직접통신=True(직접통신)");
            ComboBoxPeriod.Items.Add("직접통신=False(주기통신)");
            ComboBoxPeriod.SelectedIndex = Convert.ToInt32(ReadConfig(Constants.ConfigFileName, "Main", "periodicity", "0"));
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
            string lastMachineStateModelFilePath = ReadConfig(Constants.ConfigFileName, "Main", "lastMachineStateModelFilePath");
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
        public int GetMachineId(string _id_name)
        {
            int index = _id_name.IndexOf(':');
            if (index == -1)
            {
                return 0;
            }
            else
            {
                string id = _id_name.Substring(0, index);
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
        //Json 형식으로 변환 하는 함수입니다.
        JObject GetJObject(Item _item)
        {
            string itemString = _item.ToString().Replace("\r\n", "");
            JObject json = JObject.Parse(itemString);
            return json;
        }
        // TORUS에 연결 하는 함수입니다.
        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (connectResult_ != 0)
            {
                // "TorusTester.info"에 기록된 "App ID"와 "App Name"을 사용합니다.
                connectResult_ = Api.Initialize(new Guid(Constants.AppID), Constants.AppName);
            }
            if (connectResult_ == 0)
            {
                ComboBoxMachieID.Items.Clear();
                bool first = true;
                Item item;
                int result = Api.getMachinesInfo(out item);
                if (result == 0 && item != null)
                {
                    JObject json = GetJObject(item);
                    JArray values = (JArray)json["value"];
                    for (int i = 0; i < values.Count; i++)
                    {
                        ComboBoxMachieID.Items.Add(values[i]["id"] + ":" + values[i]["name"]);
                        if (first)
                        {
                            first = false;
                            TextBoxCommonFilter.Text = "machine=" + values[i]["id"].ToString();
                        }
                    }
                    ButtonConnect.IsEnabled = false;
                    ButtonSingleStart.IsEnabled = true;
                    ButtonSingleStop.IsEnabled = false;
                    ButtonMultiStart.IsEnabled = true;
                    ButtonMultiStop.IsEnabled = false;
                    ComboBoxMachieID.SelectedIndex = 0;
                }
                else
                {
                    System.Windows.MessageBox.Show("연결되어 있는 설비가 없습니다. 연결을 종료합니다.", "오류");
                    connectResult_ = -1;
                }
            }
            else if (connectResult_ == 546308133)
            {
                System.Windows.MessageBox.Show("TORUS 구동에 필요한 DLL 파일을 찾을 수 없거나 TORUS가 실행중이 아닙니다.", "오류");
            }
            else
            {
                System.Windows.MessageBox.Show("알 수 없는 오류 : " + connectResult_.ToString(), "오류");
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
                            if (items[1].Substring(items[1].Length - 1) == "&")
                            {
                                items[1] = items[1].Substring(0, items[1].Length - 1);
                            }
                            if (items[1].Substring(0, 1) == "&")
                            {
                                items[1] = items[1].Substring(1, items[1].Length - 1);
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
            WriteConfig(Constants.ConfigFileName, "Main", "lastMachineStateModelFilePath", _filePath);
        }


        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new();
            openFileDialog.InitialDirectory = ReadConfig(Constants.ConfigFileName, "Main", "LoadListFileDefaultDirPath");
            openFileDialog.Filter = "txt files (*.txt)|*.txt";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadMachineStateModel(openFileDialog.FileName);
            }
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
                    commonFilter_ = commonFilter_.Substring(0, commonFilter_.Length - 1);
                }
                if (commonFilter_.Substring(0, 1) == "&")
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
                    tmpOneTurnTime = tmpOneTurnTime + stopwatch.ElapsedMilliseconds;
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
                    tmpTotalTime = tmpTotalTime + tmpOneTurnTime;
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
                            TextBoxThreadPeriodResult.Text = tmpOneTurnTime.ToString() + "ms\n" + TextBoxThreadPeriodResult.Text.Substring(0, TextBoxThreadPeriodResult.Text.LastIndexOf('\n'));
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
            stopwatch.Restart();
            int tmpResult = Api.getData(tmpAddress, tmpFilter, out Item item, true, timeout_);
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
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
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
            Item InItem;
            Item OutItem;
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
            int tmpResult = Api.updateData(tmpAddress, tmpFilter, InItem, out OutItem, timeout_);
            if (tmpResult == 0)
            {
                System.Windows.MessageBox.Show("변경에 성공했습니다.", "성공");
            }
            else
            {
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
                System.Windows.MessageBox.Show("변경 실패\n" + tmpErrorMessage, tmpErrorCode);
            }
        }
        string MakeErrorMessage(int _errCode, out string _errCodeString, string _errCode16Str = "")
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
            return result;
        }

        private string MakeObjectInfo(string _fullPath, int _machinID, out bool _isDir)
        {
            string totalInfo = "";
            Item item;
            int result;
            _isDir = false;
            result = Api.getAttributeExists(_fullPath, out item, _machinID, timeout_);
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
                    if (_fullPath.Substring(_fullPath .Length - 1) == "/")
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
                totalInfo = totalInfo + "time:ERROR|";
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
                        //string[] fileInfos = item.GetArrayString("value");//TODO
                        JObject json = GetJObject(item);
                        JArray values = (JArray)json["value"];
                        for (int i = 0; i < values.Count; i++)
                        {
                            string name = values[i]["name"].ToString();
                            bool isDir = false;
                            if (values[i]["isDir"].ToString().ToLower() == "true")
                            {
                                isDir = true;
                            }
                            string size = values[i]["size"].ToString();
                            string datetime = values[i]["datetime"].ToString();
                            ListBoxFileList.Items.Add(new FileItem(name, "size:" + size + ", time:" + datetime, isDir));
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
                                bool tmpIsDir;
                                string tmpTotalInfo = MakeObjectInfo(tmpCurrentPath + tmpOneItem, tmpMachineID, out tmpIsDir);
                                ListBoxFileList.Items.Add(new FileItem(tmpOneItem, tmpTotalInfo, tmpIsDir));
                            }
                        }
                    }
                }
                else
                {
                    CheckForTest("getFileList", false);
                    string tmpErrorMessage;
                    string tmpErrorCode = MakeErrorMessage(tmpResult, out tmpErrorMessage);
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
            Item item;
            int result = Api.CreateCNCFile(tmpCurrentPath, tmpNewObjectName, out item, tmpMachineID, timeout_);
            if (result == 0)
            {
                CheckForTest("CreateCNCFile", true);
                System.Windows.MessageBox.Show("파일 생성 성공");
                ShowFileList(TextBoxCurrentPath.Text);
            }
            else
            {
                CheckForTest("CreateCNCFile", false);
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
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
            Item item;
            int result = Api.CreateCNCFolder(tmpCurrentPath, tmpNewObjectName, out item, tmpMachineID, timeout_);
            if (result == 0)
            {
                CheckForTest("CreateCNCFolder", true);
                System.Windows.MessageBox.Show("폴더 생성 성공");
                ShowFileList(TextBoxCurrentPath.Text);
            }
            else
            {
                CheckForTest("CreateCNCFolder", false);
                string tmpErrorMessage;
                string tmpErrorCode = MakeErrorMessage(result, out tmpErrorMessage);
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
            int result = Api.UploadFile(tmpUploadFile, tmpCurrentPath , tmpMachineID, tmpChannel, timeout_);
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
                if (localPath.Substring(localPath .Length - 1) != "/" && localPath.Substring(localPath.Length - 1) != "\\")
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
            string type = _item.GetValueString("datatype");
            string timestatmp = _item.GetValueString("time");
            string address = _item.GetValueString("address");
            string filter = _item.GetValueString("filter");
            string[] values;
            if (type == "boolean")
            {
                values = _item.GetArrayBoolean("value").Select(b => b.ToString()).ToArray();
            }
            else
            {
                values = _item.GetArrayString("value");
            }
            int status = _item.GetValueInt("status");
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
            WriteConfig(Constants.ConfigFileName, "Main", "periodicity", ComboBoxPeriod.SelectedIndex.ToString());
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
    }
}
