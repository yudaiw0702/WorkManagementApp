using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WorkManagementApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer dispatcherTimer;    // タイマーオブジェクト
        int TimeLimit = 30;                 // 制限時間
        DateTime StartTime;                 // カウント開始時刻
        TimeSpan nowtimespan;               // Startボタンが押されてから現在までの経過時間
        TimeSpan oldtimespan;               // 一時停止ボタンが押されるまでに経過した時間の蓄積

        public MainWindow()
        {
            InitializeComponent();

            // コンポーネントの状態を初期化　
            lblTime.Content = "00:00:000";
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnReset.IsEnabled = true;

            // タイマーのインスタンスを生成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            // タイマー開始
            TimerStart();

        }

        // タイマー Tick処理
        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            nowtimespan = DateTime.Now.Subtract(StartTime);
            lblTime.Content = oldtimespan.Add(nowtimespan).ToString(@"mm\:ss\:fff");

            if (TimeSpan.Compare(oldtimespan.Add(nowtimespan), new TimeSpan(0, 0, TimeLimit)) >= 0)
            {
                TimerStop();
                TimerReset();
                MessageBox.Show(String.Format("{0}秒経過しました。", TimeLimit),
                                "Infomation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ボタンクリック時の処理分岐
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Control ctrl = (Control)sender;
            switch (ctrl.Name)
            {
                case "btnStart":
                    TimerStart();
                    break;

                case "btnStop":
                    TimerStop();
                    break;

                case "btnReset":
                    TimerReset();
                    break;

                default:
                    break;
            }
        }

        // タイマー操作：開始
        private void TimerStart()
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnReset.IsEnabled = false;
            StartTime = DateTime.Now;
            dispatcherTimer.Start();
        }

        // タイマー操作：停止
        private void TimerStop()
        {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnReset.IsEnabled = true;
            oldtimespan = oldtimespan.Add(nowtimespan);
            dispatcherTimer.Stop();
        }

        // タイマー操作：リセット
        private void TimerReset()
        {
            oldtimespan = new TimeSpan();
            lblTime.Content = "00:00:000";
        }

    }
}
