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
    /// FileItem.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileItem : UserControl
    {
        string objectName;
        string objectInfo;
        bool isDir;
        public FileItem(string _objectName, string _objectInfo, bool _isDir)
        {
            InitializeComponent();
            objectName = _objectName;
            objectInfo = _objectInfo;
            TextBoxObjectName.Text = objectName;
            TextBoxObjectInfo.Text = objectInfo;
            if(_isDir)
            {
                TextBlockObjectType.Text = "폴더";
                ButtonInto.Content = "들어가기";
                isDir = true;
                ButtonExecute.Visibility = Visibility.Hidden;
            }
            else if (!_isDir)
            {
                TextBlockObjectType.Text = "파일";
                ButtonInto.Content = "다운로드";
                isDir = false;
            }
            else
            {
                TextBlockObjectType.Text = "";
                ButtonInto.Visibility = Visibility.Hidden;
                ButtonExecute.Visibility = Visibility.Hidden;
            }
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).DeleteOneItem(objectName);
        }

        private void ButtonInto_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).IntoDir(objectName, isDir);
        }

        private void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).ReadyCopy(objectName);
        }

        private void ButtonMove_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).ReadyMove(objectName);
        }

        private void TextBoxObjectName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).ObjectRename(objectName);
        }

        private void ButtonExecute_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MainWindow).ExcuteOneItem(objectName);
        }
    }
}
