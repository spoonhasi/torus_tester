using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace TorusWPF
{
    /// <summary>
    /// MachineState.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MachineState : UserControl
    {
        public static int totalCount;
        public string memo;
        public string address;
        public string filter;
        public string result;
        public string resultDescribe;
        public int number;
        private int _singleTimeLineCount;
        private int _multiTimeLineCount;
        public MachineState(string _address, string _filter, string _memo)
        {
            InitializeComponent();
            totalCount++;
            _singleTimeLineCount = 0;
            _multiTimeLineCount = 0;
            address = _address;
            filter = _filter;
            result = "";
            resultDescribe = "";
            number = totalCount;
            TextBlockNumber.Text = number.ToString();
            TextBoxAddress.Text = address;
            TextBoxFilter.Text = filter;
            TextBoxResult.Text = result;
            TextBoxResultDescribe.Text = resultDescribe;
            TextBoxInputData.Text = "";
            TextBoxMemo.Text = _memo;
            TextBoxAddress.IsReadOnly = true;
            TextBoxFilter.IsReadOnly = true;
            TextBoxResult.IsReadOnly = true;
            TextBoxResultDescribe.IsReadOnly = true;
        }

        public string AddressLower()
        {
            return address.ToLowerInvariant();
        }

        public void InsertSingleTime(string _contents)
        {
            if(_singleTimeLineCount >= 5)
            {
                string tmpString = TextBoxSingleTime.Text;
                if (tmpString.Substring(tmpString.Length - 1) == "\n")
                {
                    tmpString = tmpString.Substring(0, tmpString.Length - 1);
                }
                tmpString = tmpString.Substring(0, tmpString.LastIndexOf('\n'));
                TextBoxSingleTime.Text = _contents + "\n" + tmpString;
            }
            else
            {
                TextBoxSingleTime.Text = _contents + "\n" + TextBoxSingleTime.Text;
                _singleTimeLineCount++;
            }
        }
        public void InsertMultiTime(string _contents)
        {
            if (_multiTimeLineCount >= 5)
            {
                string tmpString = TextBoxMultiTime.Text;
                if (tmpString.Substring(tmpString.Length - 1) == "\n")
                {
                    tmpString = tmpString.Substring(0, tmpString.Length - 1);
                }
                tmpString = tmpString.Substring(0, tmpString.LastIndexOf('\n'));
                TextBoxMultiTime.Text = _contents + "\n" + tmpString;
            }
            else
            {
                TextBoxMultiTime.Text = _contents + "\n" + TextBoxMultiTime.Text;
                _multiTimeLineCount++;
            }
        }
        public void InsertNumber(int _number)
        {
            number = _number;
            TextBlockNumber.Text = number.ToString();
        }
        public void InsertResult(string _result)
        {
            string tmpResult = _result.Replace("\r\n", "");
            result = tmpResult;
            TextBoxResult.Text = result;
        }
        public void InsertResultDescribe(string _contents)
        {
            resultDescribe = _contents;
            TextBoxResultDescribe.Text = resultDescribe;
        }
        public void DrawColorMan(byte a, byte r, byte g, byte b)
        {
            GridMain.Background = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).UpdateData((Parent as ListBox).Items.IndexOf(this));
        }

        private void ButtonChangeFilter_Click(object sender, RoutedEventArgs e)
        {
            Window tmpChangeFilterWindow = new ChangeFilterWindow(filter, "filter", this);
            tmpChangeFilterWindow.Show();
        }
        public void InsertFilter(string _filter)
        {
            string tmpFilter = _filter.Replace("\r\n", " ");
            if (tmpFilter != filter)
            {
                filter = tmpFilter;
                TextBoxFilter.Text = filter;
            }
        }
        private void ButtonChangeAddress_Click(object sender, RoutedEventArgs e)
        {
            Window tmpChangeFilterWindow = new ChangeFilterWindow(address, "address", this);
            tmpChangeFilterWindow.Show();
        }
        public void InsertAddress(string _address)
        {
            string tmpAddress = _address.Replace("\r\n", " ");
            if (tmpAddress != address)
            {
                address = tmpAddress;
                TextBoxAddress.Text = address;
            }
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).DeleteMachineStateItem((Parent as ListBox).Items.IndexOf(this));
        }

        private void ButtonUp_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).UpMachineStateItem((Parent as ListBox).Items.IndexOf(this));
        }

        private void ButtonDown_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).DownMachineStateItem((Parent as ListBox).Items.IndexOf(this));
        }

        private void ButtonOneGetData_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).OneGetData((Parent as ListBox).Items.IndexOf(this));
        }
    }
}
