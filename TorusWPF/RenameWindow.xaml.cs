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
    /// RenameWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RenameWindow : Window
    {
        string originalName;
        bool isDir;
        public RenameWindow(string _objectName)
        {
            InitializeComponent();
            if (_objectName.Substring(_objectName.Length - 1) == "/")
            {
                isDir = true;
                Title = "폴더 이름 바꾸기";
                _objectName = _objectName.Substring(0, _objectName.Length - 1);
            }
            else
            {
                isDir = false;
                Title = "파일 이름 바꾸기";
            }
            originalName = _objectName;
            TextBoxNewName.Text = _objectName;
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            if (originalName != TextBoxNewName.Text && TextBoxNewName.Text != "")
            {
                if (isDir)
                {
                    (App.Current.MainWindow as MainWindow).ActualObjectRename(originalName + "/", TextBoxNewName.Text);
                }
                else
                {
                    (App.Current.MainWindow as MainWindow).ActualObjectRename(originalName, TextBoxNewName.Text);
                }
            }
            Window.GetWindow(this).Close();
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }
    }
}
