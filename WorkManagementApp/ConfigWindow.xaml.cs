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
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Data.SqlTypes;
using System.Security.Cryptography.X509Certificates;

namespace WorkManagementApp
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public int TimeLimitHH { get; set; }
        public int TimeLimitMM { get; set; }
        public string strAddress { set; get; }

        public ConfigWindow()
        {
            InitializeComponent();
        }

        private void Config_close_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {

        }
        MainWindow mw;
        public void SetParent(MainWindow parent)
        {
            mw = parent;
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {

            string timePickMM;
            string timePickHH;

            timePickHH = string.Format("{0:hh}", timePicker.Value);
            timePickMM = string.Format("{0:mm}", timePicker.Value);

            //int型に変換
            int h = int.Parse(timePickHH);
            int m = int.Parse(timePickMM);

            if (h == 12)
                h = 0;

            //MainWindowクラスのTimeLimitに設定した時間を代入
            mw.SetText = strAddress;
            mw.TimeLimitHH = h;
            mw.TimeLimitMM = m;

            mw.StrTimeLimit();

            Close();
        }
    }
}
