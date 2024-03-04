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
using System.Windows.Shapes;

namespace TorusWPF
{
    /// <summary>
    /// ChangeFilterWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChangeFilterWindow : Window
    {
        MachineState tmpMachineState;
        string originalValue;
        string type;
        public ChangeFilterWindow(string _originalValue, string _type, MachineState _tmpMachineState)
        {
            InitializeComponent();
            tmpMachineState = _tmpMachineState;
            originalValue = _originalValue;
            type = _type;
            TextBoxValue.Text = _originalValue;
            TextBoxValue.Focus();
            TextBoxValue.Select(TextBoxValue.Text.Length, 0);
            Title = "Change " + type;
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            if (originalValue != TextBoxValue.Text)
            {
                if (type == "filter")
                {
                    tmpMachineState.InsertFilter(TextBoxValue.Text);
                }
                else if (type == "address")
                {
                    tmpMachineState.InsertAddress(TextBoxValue.Text);
                }
            }
            Window.GetWindow(this).Close();
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }

        private void TextBoxValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (type == "filter")
                {
                    tmpMachineState.InsertFilter(TextBoxValue.Text);
                }
                else if (type == "address")
                {
                    tmpMachineState.InsertAddress(TextBoxValue.Text);
                }
                Window.GetWindow(this).Close();
            }
        }
    }
}
