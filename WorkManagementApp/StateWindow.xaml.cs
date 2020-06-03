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
    /// StateWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class StateWindow : Window
    {
        //タイマー
        DispatcherTimer dispatcherTimer;    // タイマーオブジェクト
        int TimeLimit = 30;                 // 制限時間
        DateTime StartTime;                 // カウント開始時刻
        TimeSpan nowtimespan;               // Startボタンが押されてから現在までの経過時間
        TimeSpan oldtimespan;               // 一時停止ボタンが押されるまでに経過した時間の蓄積

        MainWindow mw;

        public StateWindow()
        {
            InitializeComponent();

            // コンポーネントの状態を初期化　
            lblTotalTime.Content = "00:00:000";

            // タイマーのインスタンスを生成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            mw = new MainWindow();
        }

        // タイマー Tick処理
        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            nowtimespan = DateTime.Now.Subtract(StartTime);
            lblTotalTime.Content = oldtimespan.Add(nowtimespan).ToString(@"mm\:ss\:fff");

            if (TimeSpan.Compare(oldtimespan.Add(nowtimespan), new TimeSpan(0, 0, TimeLimit)) >= 0)
            {
                MessageBox.Show(String.Format("{0}秒経過しました。", TimeLimit),
                                "Infomation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // タイマー操作：開始
        public void TimerStart()
        {
            StartTime = DateTime.Now;
            dispatcherTimer.Start();
        }

        // タイマー操作：停止
        public void TimerStop()
        {
            oldtimespan = oldtimespan.Add(nowtimespan);
            dispatcherTimer.Stop();
        }

        // タイマー操作：リセット
        public void TimerReset()
        {
            oldtimespan = new TimeSpan();
            lblTotalTime.Content = "00:00:000";
        }

        private void State_close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}