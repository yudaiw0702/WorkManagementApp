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
using LiveCharts.Wpf;  // .Wpf は必要 / .WinForms は必要に応じて

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

            List<string> texts = new List<string> { "hogehoge", "fuga", "foo bar" };
            Random r = new System.Random();

            var valarray = new double[50];
            for (var i = 0; i < valarray.Length; i++)
            {
                valarray[i] = r.Next(80) / 100.0;
                Labels.Add(String.Format("{0} - {1}", texts[i % texts.Count], i));
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
            // イベントハンドラ cartesianChart1_DataClick にて
            Console.WriteLine("clicked!");
            Random r = new System.Random();
            var n = DataValues.Count;
            DataValues.Clear();
            DataValues.AddRange(new double[n].Select(_ => r.Next(80) / 100.0));
        }

        private void State_close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}