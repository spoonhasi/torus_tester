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
    /// MissionItem.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MissionItem : UserControl
    {
        public static int totalCount;
        public int number;
        public string functionName;
        public string result;
        public bool isGood;
        public MissionItem(string _functionName, int _number = -1)
        {
            InitializeComponent();
            if (_number > -1)
            {
                totalCount = _number;
            }
            totalCount++;
            number = totalCount;
            TextBlockNumber.Text = number.ToString();
            functionName = _functionName;
            TextBoxFunctionName.Text = functionName;
            result = "";
            TextBoxResult.Text = result;
            isGood = false;
        }

        public void SuccessOrFail(bool _successOrFail)
        {
            if (_successOrFail == true)
            {
                isGood = true;
                result = "Success";
                TextBoxResult.Text = result;
                GridMain.Background = new SolidColorBrush(Color.FromArgb(100, 103, 153, 255));
            }
            else
            {
                isGood = false;
                result = "Fail";
                TextBoxResult.Text = result;
                GridMain.Background = new SolidColorBrush(Color.FromArgb(90, 255, 100, 100));
            }
        }
    }
}
