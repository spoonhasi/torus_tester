using IntelligentApiCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace TorusWPF
{
    /// <summary>
    /// MachineMonitoringItem.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MachineMonitoringItem : UserControl
    {
        private readonly MachineObject machineObject;
        private bool _isBlinking = false;
        private volatile bool MonitoringRunFlag = false;
        private volatile bool MonitoringExitFlag = true;
        private int axisCount = 0;
        private List<TextBlock> absoluteAxisNames = [];
        private List<TextBlock> absoluteAxisPositions = [];
        private List<TextBlock> machineAxisNames = [];
        private List<TextBlock> machineAxisPositions = [];
        public MachineMonitoringItem(MachineObject machineObject)
        {
            InitializeComponent();
            this.machineObject = machineObject;
            TextBlockMachineId.Text = machineObject.ID.ToString();
            TextBlockMachineName.Text = machineObject.Name;
            absoluteAxisNames.Add(TextBlockAbsoluteAxis1Name);
            absoluteAxisNames.Add(TextBlockAbsoluteAxis2Name);
            absoluteAxisNames.Add(TextBlockAbsoluteAxis3Name);
            absoluteAxisNames.Add(TextBlockAbsoluteAxis4Name);
            absoluteAxisNames.Add(TextBlockAbsoluteAxis5Name);
            absoluteAxisPositions.Add(TextBlockAbsoluteAxis1Position);
            absoluteAxisPositions.Add(TextBlockAbsoluteAxis2Position);
            absoluteAxisPositions.Add(TextBlockAbsoluteAxis3Position);
            absoluteAxisPositions.Add(TextBlockAbsoluteAxis4Position);
            absoluteAxisPositions.Add(TextBlockAbsoluteAxis5Position);
            machineAxisNames.Add(TextBlockMachineAxis1Name);
            machineAxisNames.Add(TextBlockMachineAxis2Name);
            machineAxisNames.Add(TextBlockMachineAxis3Name);
            machineAxisNames.Add(TextBlockMachineAxis4Name);
            machineAxisNames.Add(TextBlockMachineAxis5Name);
            machineAxisPositions.Add(TextBlockMachineAxis1Position);
            machineAxisPositions.Add(TextBlockMachineAxis2Position);
            machineAxisPositions.Add(TextBlockMachineAxis3Position);
            machineAxisPositions.Add(TextBlockMachineAxis4Position);
            machineAxisPositions.Add(TextBlockMachineAxis5Position);
        }

        public void OnOff(bool onOff)
        {
            if (onOff)
            {
                if (!MonitoringExitFlag)
                {
                    return;
                }
                MonitoringExitFlag = false;
                MonitoringRunFlag = true;
                Task.Run(() => MonitroingStart());
            }
            else
            {
                if (MonitoringExitFlag)
                {
                    return;
                }
                MonitoringRunFlag = false;
            }
        }

        public bool IsExit()
        {
            return MonitoringExitFlag;
        }

        public void MonitroingStart()
        {
            int result = Api.getData("data://machine/channel/numberOfAxes", "machine=" + machineObject.ID + "&channel=1", out Item axisCountItem, true);
            if (result != 0 || axisCountItem == null)
            {
                MonitoringExitFlag = true;
                return;
            }
            ItemObject axisCountItemObject = JsonSerializer.Deserialize<ItemObject>(axisCountItem.ToString());
            if (!int.TryParse(axisCountItemObject.Value[0].ToString(), out axisCount) || axisCount < 2)
            {
                MonitoringExitFlag = true;
                return;
            }
            result = Api.getData("data://machine/channel/axis/axisName", "machine=" + machineObject.ID + "&channel=1&axis=1-" + axisCount, out Item axisNamesItem, true);
            if (result != 0 || axisNamesItem == null)
            {
                MonitoringExitFlag = true;
                return;
            }
            ItemObject axisNamesItemObject = JsonSerializer.Deserialize<ItemObject>(axisNamesItem.ToString());
            if (axisNamesItemObject.Value.Count < axisCount)
            {
                MonitoringExitFlag = true;
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    if (i < axisCount)
                    {
                        absoluteAxisNames[i].Text = axisNamesItemObject.Value[i].ToString();
                        absoluteAxisPositions[i].Text = "0.000";
                        machineAxisNames[i].Text = axisNamesItemObject.Value[i].ToString();
                        machineAxisPositions[i].Text = "0.000";
                    }
                    else
                    {
                        absoluteAxisNames[i].Text = "";
                        absoluteAxisPositions[i].Text = "";
                        machineAxisNames[i].Text = "";
                        machineAxisPositions[i].Text = "";
                    }
                }
                TextBlockMachineMain.Text = "";
                TextBlockMachineSub.Text = "";
                TextBlockMachineFeed.Text = "0";
                TextBlockMachineSpindle.Text = "0";
            });
            bool lastIsEmergency = false;
            string lastMainProgram = "";
            string lastCurrentProgram = "";
            double lastFeed = 0;
            double lastSpindle = 0;
            double lastFeedOverride = 0;
            double lastSpindleOverride = 0;
            int lastActiveToolNumber = 0;
            int lastNcState = -1;
            int lastAlarmState = -1;
            while (MonitoringRunFlag)
            {
                ItemObject axisWorkPostionsItemObject = null;
                ItemObject axisMachinePostionsItemObject = null;
                bool isPerpect = true;
                bool isEmergency = false;
                string mainProgram = "";
                string currentProgram = "";
                double feed = 0;
                double spindle = 0;
                double feedOverride = 0;
                double spindleOverride = 0;
                int activeToolNumber = 0;
                int ncState = 0;
                int alarmState = 0;
                result = Api.getData("data://machine/channel/axis/workPosition", "machine=" + machineObject.ID + "&channel=1&axis=1-" + axisCount, out Item axisWorkPostionsItem, false);
                if (result == 0 && axisWorkPostionsItem != null)
                {
                    axisWorkPostionsItemObject = JsonSerializer.Deserialize<ItemObject>(axisWorkPostionsItem.ToString());
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/axis/machinePosition", "machine=" + machineObject.ID + "&channel=1&axis=1-" + axisCount, out Item axisMachinePostionsItem, false);
                if (result == 0 && axisMachinePostionsItem != null)
                {
                    axisMachinePostionsItemObject = JsonSerializer.Deserialize<ItemObject>(axisMachinePostionsItem.ToString());
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/emergencyStatus", "machine=" + machineObject.ID + "&channel=1", out Item emergencyStatusItem, false);
                if (result == 0 && emergencyStatusItem != null)
                {
                    ItemObject emergencyStatusItemObject = JsonSerializer.Deserialize<ItemObject>(emergencyStatusItem.ToString());
                    if (emergencyStatusItemObject.Value[0].GetInt32() != 0)
                    {
                        isEmergency = true;
                    }
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/currentProgram/mainFile/programName", "machine=" + machineObject.ID + "&channel=1", out Item mainProgramItem, false);
                if (result == 0 && mainProgramItem != null)
                {
                    ItemObject mainProgramItemObject = JsonSerializer.Deserialize<ItemObject>(mainProgramItem.ToString());
                    mainProgram = mainProgramItemObject.Value[0].ToString();
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/currentProgram/currentFile/programName", "machine=" + machineObject.ID + "&channel=1", out Item currentProgramItem, false);
                if (result == 0 && currentProgramItem != null)
                {
                    ItemObject currentProgramItemObject = JsonSerializer.Deserialize<ItemObject>(currentProgramItem.ToString());
                    currentProgram = currentProgramItemObject.Value[0].ToString();
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/spindle/rpm/actualSpeed", "machine=" + machineObject.ID + "&channel=1&spindle=1", out Item actualSpindleItem, false);
                if (result == 0 && actualSpindleItem != null)
                {
                    ItemObject actualSpindleItemObject = JsonSerializer.Deserialize<ItemObject>(actualSpindleItem.ToString());
                    spindle = actualSpindleItemObject.Value[0].GetDouble();
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/feed/feedRate/actualSpeed", "machine=" + machineObject.ID + "&channel=1", out Item actualFeedItem, false);
                if (result == 0 && actualFeedItem != null)
                {
                    ItemObject actualFeedItemObject = JsonSerializer.Deserialize<ItemObject>(actualFeedItem.ToString());
                    feed = actualFeedItemObject.Value[0].GetDouble();
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/spindle/spindleOverride", "machine=" + machineObject.ID + "&channel=1&spindle=1", out Item spindleOverrideItem, false);
                if (result == 0 && spindleOverrideItem != null)
                {
                    ItemObject spindleOverrideItemObject = JsonSerializer.Deserialize<ItemObject>(spindleOverrideItem.ToString());
                    spindleOverride = spindleOverrideItemObject.Value[0].GetDouble();
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/feed/feedOverride", "machine=" + machineObject.ID + "&channel=1", out Item feedOverrideItem, false);
                if (result == 0 && feedOverrideItem != null)
                {
                    ItemObject feedOverrideItemObject = JsonSerializer.Deserialize<ItemObject>(feedOverrideItem.ToString());
                    feedOverride = feedOverrideItemObject.Value[0].GetDouble();
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/activeTool/toolNumber", "machine=" + machineObject.ID + "&channel=1", out Item activeToolNumberItem, false);
                if (result == 0 && activeToolNumberItem != null)
                {
                    ItemObject activeToolNumberItemObject = JsonSerializer.Deserialize<ItemObject>(activeToolNumberItem.ToString());
                    activeToolNumber = activeToolNumberItemObject.Value[0].GetInt32();
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/ncState", "machine=" + machineObject.ID + "&channel=1", out Item ncStateItem, false);
                if (result == 0 && ncStateItem != null)
                {
                    ItemObject ncStateItemObject = JsonSerializer.Deserialize<ItemObject>(ncStateItem.ToString());
                    ncState = ncStateItemObject.Value[0].GetInt32();
                }
                else
                {
                    isPerpect = false;
                }
                result = Api.getData("data://machine/channel/alarmStatus", "machine=" + machineObject.ID + "&channel=1", out Item alarmStateItem, false);
                if (result == 0 && alarmStateItem != null)
                {
                    ItemObject alarmStateItemObject = JsonSerializer.Deserialize<ItemObject>(alarmStateItem.ToString());
                    alarmState = alarmStateItemObject.Value[0].GetInt32();
                }
                else
                {
                    isPerpect = false;
                }
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (axisWorkPostionsItemObject != null)
                    {
                        for (int i = 0; i < axisCount; i++)
                        {
                            absoluteAxisPositions[i].Text = axisWorkPostionsItemObject.Value[i].GetDouble().ToString("0.000");
                        }
                    }
                    if (axisMachinePostionsItemObject != null)
                    {
                        for (int i = 0; i < axisCount; i++)
                        {
                            machineAxisPositions[i].Text = axisMachinePostionsItemObject.Value[i].GetDouble().ToString("0.000");
                        }
                    }
                    if (lastIsEmergency != isEmergency)
                    {
                        if (isEmergency)
                        {
                            BorderEmergencyStatus.Background = Brushes.Red;
                        }
                        else
                        {
                            BorderEmergencyStatus.Background = Brushes.White;
                        }
                        lastIsEmergency = isEmergency;
                    }
                    if (lastMainProgram != mainProgram)
                    {
                        TextBlockMachineMain.Text = mainProgram;
                        lastMainProgram = mainProgram;
                    }
                    if (lastCurrentProgram != currentProgram)
                    {
                        TextBlockMachineSub.Text = currentProgram;
                        lastCurrentProgram = currentProgram;
                    }
                    if (lastSpindle != spindle)
                    {
                        TextBlockMachineSpindle.Text = spindle.ToString("F0");
                        lastSpindle = spindle;
                    }
                    if (lastFeed != feed)
                    {
                        TextBlockMachineFeed.Text = feed.ToString("F0");
                        lastFeed = feed;
                    }
                    if (lastSpindleOverride != spindleOverride)
                    {
                        TextBlockMachineSpindleOverride.Text = spindleOverride.ToString("F0");
                        lastSpindleOverride = spindleOverride;
                    }
                    if (lastFeedOverride != feedOverride)
                    {
                        TextBlockMachineFeedOverride.Text = feedOverride.ToString("F0");
                        lastFeedOverride = feedOverride;
                    }
                    if (lastActiveToolNumber != activeToolNumber)
                    {
                        TextBlockMachineActiveToolNumber.Text = activeToolNumber.ToString();
                        lastActiveToolNumber = activeToolNumber;
                    }
                    if (lastAlarmState != alarmState || lastNcState != ncState)
                    {
                        if (alarmState > 0)
                        {
                            GridNcState.Background = Brushes.Red;
                            TextBlockNcState.Foreground = Brushes.White;
                            TextBlockNcState.Text = "Alarm";
                        }
                        else if (ncState == 0)
                        {
                            GridNcState.Background = Brushes.LightBlue;
                            TextBlockNcState.Foreground = Brushes.Black;
                            TextBlockNcState.Text = "Reset";
                        }
                        else if (ncState == 1)
                        {
                            GridNcState.Background = Brushes.Yellow;
                            TextBlockNcState.Foreground = Brushes.Black;
                            TextBlockNcState.Text = "Stop";
                        }
                        else if (ncState == 2)
                        {
                            GridNcState.Background = Brushes.DarkOrange;
                            TextBlockNcState.Foreground = Brushes.White;
                            TextBlockNcState.Text = "Hold";
                        }
                        else if (ncState == 3)
                        {
                            GridNcState.Background = Brushes.Green;
                            TextBlockNcState.Foreground = Brushes.White;
                            TextBlockNcState.Text = "Run";
                        }
                        else if (ncState == 4)
                        {
                            GridNcState.Background = Brushes.Purple;
                            TextBlockNcState.Foreground = Brushes.White;
                            TextBlockNcState.Text = "MSTR";
                        }
                        else if (ncState == 5)
                        {
                            GridNcState.Background = Brushes.Chocolate;
                            TextBlockNcState.Foreground = Brushes.White;
                            TextBlockNcState.Text = "Interrupted";
                        }
                        else if (ncState == 6)
                        {
                            GridNcState.Background = Brushes.Red;
                            TextBlockNcState.Foreground = Brushes.White;
                            TextBlockNcState.Text = "Pause";
                        }
                        lastAlarmState = alarmState;
                        lastNcState = ncState;
                    }
                });
                _ = BlinkStatus(isPerpect);
                Thread.Sleep(500);
            }
            MonitoringExitFlag = true;
        }

        public async Task BlinkStatus(bool isPerfect)
        {
            if (_isBlinking)
            {
                return; // 이미 실행 중이면 종료
            }
            _isBlinking = true; // 깜빡임 시작
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (isPerfect)
                {
                    BorderMonitoringState.Background = Brushes.LawnGreen;
                    await Task.Delay(300);
                }
                else
                {
                    BorderMonitoringState.Background = Brushes.Orange;
                    await Task.Delay(300);
                }
                BorderMonitoringState.Background = Brushes.Gray;
                await Task.Delay(isPerfect ? 300 : 300);
                _isBlinking = false;
            });
        }
    }
}
