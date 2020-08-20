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
using System.Net;

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
        private Gesture left_playphone;

        // Gestures : handsign
        private Gesture seki;
        private Gesture drink;
        private Gesture agohige;
        private Gesture atumeru;
        private Gesture konnitiwa;
        private Gesture netu;
        private Gesture ohayo_pose;
        private Gesture sayonara_ges;
        private Gesture urayamasii;
        private Gesture urusai;
        private Gesture wakaranai;
        private Gesture wakarimasita_ges;

        //フラグ
        public static bool seat_flag = false;
        public static bool playphoneR_flag = false;
        public static bool playphoneL_flag = false;

        //Time measurement
        int seat_time = 0;
        int playphoneR_time = 0;
        int drink_time = 0;
        int agohige_time = 0;
        int seki_time = 0;

        //タイマー
        DispatcherTimer dispatcherTimer;    // タイマーオブジェクト

        // 目標時間
        public int timeLimitMM = 999;
        public int timeLimitHH = 999;
        DateTime StartTime;                 // カウント開始時刻
        TimeSpan nowtimespan;               // Startボタンが押されてから現在までの経過時間
        TimeSpan oldtimespan;               // 一時停止ボタンが押されるまでに経過した時間の蓄積
        DispatcherTimer dispatcherTimerState;
        DateTime StateStartTime;
        TimeSpan statenowtimespan;
        TimeSpan stateoldtimespan;

        DispatcherTimer glafTimer;
        TimeSpan nowglaftimespan;
        TimeSpan oldglaftimespan;
        TimeSpan subtracttimespan;
        string glafTimeSpan;

        public int TimeLimitHH { get; set; }
        public int TimeLimitMM { get; set; }
        public string SetText { set; get; }

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

            //グラフでつかうタイマーインスタンス
            glafTimer = new DispatcherTimer(DispatcherPriority.Normal);
            glafTimer.Tick += new EventHandler(glafTimer_Tick);
            glafTimer.Interval += new TimeSpan(1, 0, 0);

            glafTimer.Start();
            oldglaftimespan = oldtimespan.Add(nowtimespan);
        }

        //１時間内で作業した時間を表示する
        private void glafTimer_Tick(object sender, EventArgs e)
        {
            nowglaftimespan = oldtimespan.Add(nowtimespan);
            subtracttimespan = nowglaftimespan.Subtract(oldtimespan);
            glafTimeSpan = subtracttimespan.ToString(@"mm");

            oldglaftimespan = oldtimespan.Add(nowtimespan);

            /*現在時刻ごとに１２個glafTimeSpanを用意しておいて、
             設定画面を開くときに受け渡せるようにしておく*/
        }

        // タイマー Tick処理
        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            StateWindow sw = new StateWindow();

            nowtimespan = DateTime.Now.Subtract(StartTime);
            statenowtimespan = DateTime.Now.Subtract(StateStartTime);
            lblTime.Content = oldtimespan.Add(nowtimespan).ToString(@"hh\:mm\:ss");

            //経過を知らせてくれるけど止まるコード
            /*if (TimeSpan.Compare(oldtimespan.Add(nowtimespan), new TimeSpan(0, 0, TimeLimit)) >= 0)
            {
                MessageBox.Show(String.Format("{0}秒経過しました。", TimeLimit),
                                "Infomation", MessageBoxButton.OK, MessageBoxImage.Information);
            }*/

            if (TimeSpan.Compare(oldtimespan.Add(nowtimespan), new TimeSpan(timeLimitHH, timeLimitMM, 0)) >= 0)
            {
                MessageBox.Show(String.Format("目標作業時間に到達しました、おめでとうございます！"),
                                "Infomation", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            //メッセージボックスを出すとMainWindowが停止する、目標時間達成の知らせが出続ける
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
                //集中力
                if (gesture.Name == "seat")
                {
                    seat = gesture;
                }
                if (gesture.Name == "right_playphone")
                {
                    right_playphone = gesture;
                }
                if (gesture.Name == "left_playphone")
                {
                    left_playphone = gesture;
                }

                //手話
                if (gesture.Name == "drink_pose") //飲む
                { drink = gesture; }
                if (gesture.Name == "seki") //風邪
                { seki = gesture; }
                if (gesture.Name == "agohige") //好き
                { agohige = gesture; }
                if (gesture.Name == "atumeru") //集める（胸付近を両手で仰ぐ）
                { atumeru = gesture; }
                if (gesture.Name == "konnitiwa") //こんにちは（額でチョキをする）
                { konnitiwa = gesture; }
                if (gesture.Name == "netu") //熱（額でパーをする）
                { netu = gesture; }
                if (gesture.Name == "ohayo_pose") //おはよう（頭の横でグーをする）
                { ohayo_pose = gesture; }
                if (gesture.Name == "sayonara_ges") //さようならジェスチャー（頭の真横が１、遠ざけると０）
                { sayonara_ges = gesture; }
                if (gesture.Name == "urayamasii") //うらやましい（右胸付近で自分を人差し指で指す）
                { urayamasii = gesture; }
                if (gesture.Name == "urusai") //うるさい（右耳に人差し指をいれる）
                { urusai = gesture; }
                if (gesture.Name == "wakaranai") //わからない（口元でパーをする）
                { wakaranai = gesture; }
                if (gesture.Name == "wakarimasita_ges") //わかりましたジェスチャー（お腹を上下にさする）
                { wakarimasita_ges = gesture; }
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
                    var resultLPP = gestureFrame.DiscreteGestureResults[left_playphone];

                    //Discrete : handsign
                    var result = gestureFrame.DiscreteGestureResults[seki];
                    var result2 = gestureFrame.DiscreteGestureResults[agohige];
                    var result3 = gestureFrame.DiscreteGestureResults[drink];
                    var result4 = gestureFrame.DiscreteGestureResults[atumeru];
                    var result5 = gestureFrame.DiscreteGestureResults[konnitiwa];
                    var result6 = gestureFrame.DiscreteGestureResults[netu];
                    var result7 = gestureFrame.DiscreteGestureResults[ohayo_pose];
                    var result8 = gestureFrame.DiscreteGestureResults[urayamasii];
                    var result9 = gestureFrame.DiscreteGestureResults[urusai];
                    var result10 = gestureFrame.DiscreteGestureResults[wakaranai];

                    //Continuous : handsign
                    var progressResult = gestureFrame.ContinuousGestureResults[sayonara_ges];
                    var progressResult2 = gestureFrame.ContinuousGestureResults[wakarimasita_ges];

                    textBlock.Text = "座ってる動作：" + resultSeat.Confidence.ToString();
                    textBlock1.Text = "咳：" + result.Confidence.ToString();
                    textBlock2.Text = "あごひげ：" + result2.Confidence.ToString();
                    textBlock3.Text = "飲む：" + result3.Confidence.ToString();
                    textBlock4.Text = "集める：" + result4.Confidence.ToString();
                    textBlock5.Text = "こんにちは：" + result5.Confidence.ToString();
                    textBlock6.Text = "熱：" + result6.Confidence.ToString();
                    textBlock7.Text = "おはよう：" + result7.Confidence.ToString();
                    textBlock8.Text = "羨ましい：" + result8.Confidence.ToString();
                    textBlock9.Text = "うるさい：" + result9.Confidence.ToString();
                    textBlock10.Text = "わからない：" + result10.Confidence.ToString();
                    textBlock11.Text = "さようならges：" + progressResult.Progress.ToString();
                    textBlock12.Text = "わかりましたges：" + progressResult2.Progress.ToString();

                    //作業してるとき（座っている動作）
                    if (0.8 < resultSeat.Confidence)
                    {
                        Sw_seat(true);
                        checkText.Text = "作業しています";

                        
                    }
                    else if(resultSeat.Confidence < 0.5)
                    {
                        Sw_seat(false);
                        checkText.Text = "作業していません";

                        Sw_playphoneR(false);
                        checkText1.Text = "集中していません";
                    }

                    //スマホをいじる動作（集中していない動作）
                    if ((0.2 > resultRPP.Confidence && 0.8 < resultSeat.Confidence) || (0.2 > resultLPP.Confidence && 0.8 < resultSeat.Confidence))
                    {
                        Sw_playphoneR(true);
                        checkText1.Text = "集中しています";
                    }
                    else if(0.3 < resultRPP.Confidence || 0.3 < resultLPP.Confidence)
                    {
                        Sw_playphoneR(false);
                        checkText1.Text = "集中していません";
                    }

                    //咳をする動作
                    if ((0.4 < result.Confidence && 0.3 > result2.Confidence && 0.5 > result3.Confidence) || 
                        (0.8 < result.Confidence && 0.8 < result3.Confidence)) 
                    {
                        Sw_seki(true);  
                    }
                    else
                    {
                        seki_time = 0;
                    }

                    //あごひげの動作
                    if ((0.3 <= result2.Confidence && 0.4 > result.Confidence && 0.5 > result3.Confidence) ||
                        (0.9 < result2.Confidence))
                    {
                        Sw_agohige(true);
                    }
                    else
                    {
                        agohige_time = 0;
                    }

                    //飲む動作
                    if (result3.Confidence >= 0.5 && 0.4 > result.Confidence && 0.3 > result2.Confidence)
                    {
                        Sw_drink(true);
                    }
                    else
                    {
                        drink_time = 0;
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

                if (seat_time >= 30 && !seat_flag)
                {
                    TimerStart();
                    seat_flag = true;
                    seat_time = 0;
                }
            }
            else
            {
                seat_time++;

                if (seat_time >= 30 && seat_flag)
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

        private void Sw_drink(bool drink_flag)
        {
            if (drink_flag)
            {
                drink_time++;

                if (drink_time == 20)
                {
                    Console.WriteLine("飲む動作");
                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/drink.wav");
                    player.Play();

                    drink_time = 0;
                }

            }
        }

        private void Sw_seki(bool seki_flag)
        {
            if (seki_flag)
            {
                seki_time++;

                if (seki_time == 20)
                {
                    Console.WriteLine("咳の手話");
                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/seki.wav");
                    player.Play();

                    seki_time = 0;
                }

            }
        }

        private void Sw_agohige(bool agohige_flag)
        {
            if (agohige_flag)
            {
                agohige_time++;

                if (agohige_time == 20)
                {
                    Console.WriteLine("あごひげの手話");
                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/agohige.wav");
                    player.Play();

                    agohige_time = 0;
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

            cw.SetParent(this);
            cw.ShowDialog();
        }

        public void StrTimeLimit()
        {
            timeLimitHH = TimeLimitHH;
            timeLimitMM = TimeLimitMM;
        }
    }
}

//作業している時間と集中している時間でズレが生じる原因はジェスチャー判別にある