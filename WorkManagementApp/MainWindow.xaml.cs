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
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System.Resources;
using System.ComponentModel.Design;

namespace WorkManagementApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // Kinect (MultiFrame)
        private KinectSensor kinect;
        private MultiSourceFrameReader multiFrameReader;

        // Color
        private byte[] colorBuffer;
        private WriteableBitmap colorImage;
        private FrameDescription colorFrameDescription;

        // Body
        private Body[] bodies;

        // Gesture Builder
        private VisualGestureBuilderDatabase gestureDatabase;
        private VisualGestureBuilderFrameSource gestureFrameSource;
        private VisualGestureBuilderFrameReader gestureFrameReader;

        // Gestures
        private Gesture seat;
        private Gesture right_playphone;

        // Gestures : handsign
        private Gesture seki;
        private Gesture drink;
        private Gesture sodeage;
        private Gesture sodesage;
        private Gesture agohige;
        private Gesture agohige_pose;

        //フラグ
        public static bool seat_flag = false;
        public static bool playphoneR_flag = false;

        //Time measurement
        int seat_time = 0;
        int playphoneR_time = 0;

        //タイマー
        DispatcherTimer dispatcherTimer;    // タイマーオブジェクト
        int TimeLimit = 30;                 // 制限時間
        DateTime StartTime;                 // カウント開始時刻
        TimeSpan nowtimespan;               // Startボタンが押されてから現在までの経過時間
        TimeSpan oldtimespan;               // 一時停止ボタンが押されるまでに経過した時間の蓄積
        DispatcherTimer dispatcherTimerState;
        int StateTimeLimit = 60;
        DateTime StateStartTime;
        TimeSpan statenowtimespan;
        TimeSpan stateoldtimespan;

        public MainWindow()
        {
            StateWindow sw = new StateWindow();

            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            // コンポーネントの状態を初期化　
            lblTime.Content = "00:00:00";
            sw.lblTotalTime.Content = "00:00:00";

            // タイマーのインスタンスを生成
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimerState = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimerState.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimerState.Tick += new EventHandler(dispatcherTimer_Tick);
        }

        // タイマー Tick処理
        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            StateWindow sw = new StateWindow();

            nowtimespan = DateTime.Now.Subtract(StartTime);
            statenowtimespan = DateTime.Now.Subtract(StateStartTime);
            lblTime.Content = oldtimespan.Add(nowtimespan).ToString(@"hh\:mm\:ss");

            /* 経過を知らせてくれるけど止まるコード
            if (TimeSpan.Compare(oldtimespan.Add(nowtimespan), new TimeSpan(0, 0, TimeLimit)) >= 0)
            {
                MessageBox.Show(String.Format("{0}秒経過しました。", TimeLimit),
                                "Infomation", MessageBoxButton.OK, MessageBoxImage.Information);
            }*/
        }

        // タイマー操作：開始
        private void TimerStart()
        {
            StartTime = DateTime.Now;
            dispatcherTimer.Start();
        }

        // タイマー操作：停止
        private void TimerStop()
        {
            oldtimespan = oldtimespan.Add(nowtimespan);
            dispatcherTimer.Stop();
        }

        // タイマー操作：リセット
        private void TimerReset()
        {
            StateWindow sw = new StateWindow();

            oldtimespan = new TimeSpan();
            stateoldtimespan = new TimeSpan();
            lblTime.Content = "00:00:00";
            sw.lblTotalTime.Content = "00:00:00";
        }

        public void StateTimerStart()
        {
            StateStartTime = DateTime.Now;
            dispatcherTimerState.Start();
        }

        // タイマー操作：停止
        public void StateTimerStop()
        {
            stateoldtimespan = stateoldtimespan.Add(statenowtimespan);
            dispatcherTimerState.Stop();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Kinectへの接続
            try
            {
                kinect = KinectSensor.GetDefault();
                if (kinect == null)
                {
                    throw new Exception("Cannot open kinect v2 sensor.");
                }

                checkText2.Text = "Connecting Kinect v2 sensor";
                kinect.Open();

                // 初期設定
                Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                checkText2.Text = "Disconnect Kinect v2 sensor";
                Close();
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (multiFrameReader != null)
            {
                multiFrameReader.Dispose();
                multiFrameReader = null;
            }
            if (gestureFrameReader != null)
            {
                gestureFrameReader.Dispose();
                gestureFrameReader = null;
            }
            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }

        /// <summary>
        /// 初期設定
        /// </summary>
        private void Initialize()
        {
            // ColorImageの初期設定
            colorFrameDescription = kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            colorImage = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96, 96, PixelFormats.Bgra32, null);
            ImageColor.Source = colorImage;

            // Bodyの初期設定
            bodies = new Body[kinect.BodyFrameSource.BodyCount];

            // Gesturesの初期設定
            gestureDatabase = new VisualGestureBuilderDatabase(@"../../Gestures/handsign.gbd");
            gestureFrameSource = new VisualGestureBuilderFrameSource(kinect, 0);

            // 使用するジェスチャーをデータベースから取り出す
            foreach (var gesture in gestureDatabase.AvailableGestures)
            {
                if (gesture.Name == "seat")
                {
                    seat = gesture;
                }
                if (gesture.Name == "right_playphone")
                {
                    right_playphone = gesture;
                }
                if (gesture.Name == "drink_pose")
                {
                    drink = gesture;
                }
                if (gesture.Name == "seki")
                {
                    seki = gesture;
                }
                if (gesture.Name == "sodeage")
                {
                    sodeage = gesture;
                }
                if (gesture.Name == "sodesage")
                {
                    sodesage = gesture;
                }
                if (gesture.Name == "agohige")
                {
                    agohige = gesture;
                }
                if (gesture.Name == "agohige_pose")
                {
                    agohige_pose = gesture;
                }
                this.gestureFrameSource.AddGesture(gesture);
            }

            // ジェスチャーリーダーを開く
            gestureFrameReader = gestureFrameSource.OpenReader();
            gestureFrameReader.IsPaused = true;
            gestureFrameReader.FrameArrived += gestureFrameReader_FrameArrived;

            // フレームリーダーを開く (Color / Body)
            multiFrameReader = kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body);
            multiFrameReader.MultiSourceFrameArrived += multiFrameReader_MultiSourceFrameArrived;
        }

        private void multiFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiFrame = e.FrameReference.AcquireFrame();

            // Colorの取得と表示
            using (var colorFrame = multiFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                // RGB画像の表示
                colorBuffer = new byte[colorFrameDescription.Width * colorFrameDescription.Height * colorFrameDescription.BytesPerPixel];
                colorFrame.CopyConvertedFrameDataToArray(colorBuffer, ColorImageFormat.Bgra);

                ImageColor.Source = BitmapSource.Create(colorFrameDescription.Width, colorFrameDescription.Height, 96, 96,
                    PixelFormats.Bgra32, null, colorBuffer, colorFrameDescription.Width * (int)colorFrameDescription.BytesPerPixel);

            }

            // Bodyを１つ探し、ジェスチャー取得の対象として設定
            if (!gestureFrameSource.IsTrackingIdValid)
            {
                using (BodyFrame bodyFrame = multiFrame.BodyFrameReference.AcquireFrame())
                {
                    if (bodyFrame != null)
                    {
                        bodyFrame.GetAndRefreshBodyData(bodies);

                        foreach (var body in bodies)
                        {
                            if (body != null && body.IsTracked)
                            {
                                // ジェスチャー判定対象としてbodyを選択
                                gestureFrameSource.TrackingId = body.TrackingId;
                                // ジェスチャー判定開始
                                gestureFrameReader.IsPaused = false;
                            }
                        }
                    }
                }
            }
        }

        private void gestureFrameReader_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {

            using (var gestureFrame = e.FrameReference.AcquireFrame())
            {

                // ジェスチャーの判定結果がある場合
                if (gestureFrame != null && gestureFrame.DiscreteGestureResults != null)
                {
                    //Discrete
                    var resultSeat = gestureFrame.DiscreteGestureResults[seat];
                    var resultRPP = gestureFrame.DiscreteGestureResults[right_playphone];

                    //Discrete : handsign
                    var result = gestureFrame.DiscreteGestureResults[seki];
                    var result2 = gestureFrame.DiscreteGestureResults[agohige_pose];
                    var result3 = gestureFrame.DiscreteGestureResults[drink];

                    //Continuous
                    var progressResult = gestureFrame.ContinuousGestureResults[agohige];
                    var progressResult2 = gestureFrame.ContinuousGestureResults[sodeage];
                    var progressResult3 = gestureFrame.ContinuousGestureResults[sodesage];

                    //作業してるとき（座っている動作）
                    if (0.9 < resultSeat.Confidence)
                    {
                        Sw_seat(true);
                        checkText.Text = "作業しています";

                        //スマホをいじる動作（集中していない動作）
                        if (0.3 > resultRPP.Confidence)
                        {
                            Sw_playphoneR(true);
                            checkText1.Text = "集中しています";
                        }
                        else
                        {
                            Sw_playphoneR(false);
                            checkText1.Text = "集中していません";
                        }
                    }
                    else
                    {
                        Sw_seat(false);
                        checkText.Text = "作業していません";

                        Sw_playphoneR(false);
                        checkText1.Text = "集中していません";
                    }

                    //咳をする動作
                    TextBlock1.Text = "咳確信度: " + result.Confidence.ToString();
                    if (0.3 < result.Confidence)
                    {
                        TextBlock2.Text = "状況：咳をしています";
                    }

                    //あごひげの動作
                    //TextBlock7.Text = "あごひげ進捗度: " + progressResult.Progress.ToString();   // 進捗を数値化
                    TextBlock8.Text = "あごひげ確信度：" + result2.Confidence.ToString();
                    if (0.3 <= result2.Confidence)
                    {
                        int sw_agohige = Sw_agohige(0);
                        if (sw_agohige == 1)
                        {
                            TextBlock7.Text = "状況：あごひげを触っています";
                            if (0.4 > progressResult.Progress)
                            {
                                TextBlock7.Text = "状況：あごひげの手話をしました";
                            }

                        }
                    }
                    else
                    {
                        Sw_agohige(2);
                        agohige_time = 0;
                        TextBlock7.Text = "状況：";
                    }

                    //飲む動作
                    TextBlock5.Text = "確信度: " + result3.Confidence.ToString();
                    if (result3.Confidence >= 0.8)
                    {
                        Sw_drink(true);
                    }
                    else
                    {
                        drink_time = 0;
                        TextBlock6.Text = "状況：";
                    }
                }
            }
        }

        //各ジェスチャーのチャタリング制御
        private void Sw_seat(bool a)
        {

            if (a)
            {
                seat_time++; //フレームを更新するごとに増加

                if (seat_time >= 20 && !seat_flag)
                {
                    TimerStart();
                    seat_flag = true;
                    seat_time = 0;
                }
            }
            else
            {
                if (seat_flag)
                {
                    TimerStop();
                    seat_flag = false;
                    seat_time = 0;
                }
            }
        }

        private void Sw_playphoneR(bool a)
        {
            if (a)
            {
                playphoneR_time++; //フレームを更新するごとに増加

                if (playphoneR_time >= 20 && !playphoneR_flag)
                {
                    StateTimerStart();
                    playphoneR_flag = true;
                    playphoneR_time = 0;
                }
            }
            else
            {
                if (playphoneR_flag)
                {
                    StateTimerStop();
                    playphoneR_flag = false;
                    playphoneR_time = 0;
                }
            }
        }



        //別ウィンドウの表示
        private void State_open_Click(object sender, RoutedEventArgs e)
        {
            StateWindow sw = new StateWindow();

            sw.lblTotalTime.Content = stateoldtimespan.Add(statenowtimespan).ToString(@"hh\:mm\:ss");
            sw.Show();
        }

        private void Config_open_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow cw = new ConfigWindow();
            cw.Show();
        }
    }
}
