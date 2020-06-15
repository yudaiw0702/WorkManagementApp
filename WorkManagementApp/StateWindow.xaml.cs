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
using LiveCharts;
using LiveCharts.Wpf;

namespace WorkManagementApp
{
    /// <summary>
    /// StateWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class StateWindow : Window
    {
        // Form1クラスのプロパティとして以下を追加
        public ChartValues<double> DataValues { get; set; }
        public List<string> Labels { get; set; }
        public SeriesCollection SeriesCollection { get; set; }  // 追加するプロパティ

        public StateWindow()
        {
            InitializeComponent();

            // コンストラクタのInitializeComponent()の後かForm1_Load内にて
            Labels = new List<string>();

            List<string> texts = new List<string> { "時" };
            Random r = new System.Random();

            DateTime dt = DateTime.Now;
            Console.WriteLine(dt);

            Console.WriteLine(dt.Year + "年");
            Console.WriteLine(dt.Month + "月");
            Console.WriteLine(dt.Day + "日");
            Console.WriteLine(dt.Hour + "時");
            Console.WriteLine(dt.Minute + "分");
            Console.WriteLine(dt.Second + "秒");
            Console.WriteLine(dt.Millisecond + "ミリ秒");

           

            var valarray = new double[12];
            for (var i = 0; i < valarray.Length; i++)
            {
                valarray[i] = r.Next(60) / 1.0;
                Labels.Add(String.Format("{0} {1}", i, texts[i % texts.Count]));
            }
            DataValues = new ChartValues<double>(valarray);
            // (*2)
            SeriesCollection = new SeriesCollection
    {
        new ColumnSeries
        {
            Values = DataValues,  // DataValuesプロパティと紐づける
            Fill = Brushes.DarkBlue
        }
    };

            DataContext = this; // Binding用
        }

        private void CartesianChart_DataClick(object sender, ChartPoint chartpoint)
        {
            Console.WriteLine("clicked!");
            Random r = new Random();
            var n = DataValues.Count;
            DataValues.Clear();
            DataValues.AddRange(new double[n].Select(_ => r.Next(60) / 1.0));
        }

        private void State_close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

/*時間ごとに作業時間を分けるグラフメモ
 *現在時刻以下の時間グラフは動作していなければならない
 *常に時間取得をする必要がある？＝常に開いているメインウィンドウで現在時刻を動かす
 *１時間ごとにグラフを表示していく
 *１時間経過後の作業している時間と１時間前の時間で差を計算する、それをグラフで表示
*/