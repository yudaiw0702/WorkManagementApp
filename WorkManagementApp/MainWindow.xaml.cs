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
using System.Threading;
using Microsoft.DirectX.AudioVideoPlayback;
using Microsoft.Win32;

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

        /* Gestures
        private Gesture seat;
        private Gesture right_playphone;
        private Gesture left_playphone;
        */

        // Gestures : handsign
        private Gesture kaze;
        private Gesture nomu;
        private Gesture siawase;
        private Gesture atumeru;
        private Gesture konnitiwa;
        private Gesture netu;
        private Gesture ohayos;
        private Gesture sayonara_ges;
        private Gesture urayamasii;
        private Gesture urusais;
        private Gesture wakaranai;
        //private Gesture wakarimasita_ges;

        //フラグ
        public static bool seat_flag = false;
        public static bool playphoneR_flag = false;
        public static bool playphoneL_flag = false;
        public static bool sayonara_flag = false;

        //ジェスチャー用フラグ
        public static bool sayonaraflag_ges = false;
        public static bool wakarimasitaflag_ges = false;

        private System.Media.SoundPlayer player = null;

        //Time measurement
        int seat_time = 0;
        int playphoneR_time = 0;
        int nomu_time = 0;
        int siawase_time = 0;
        int kaze_time = 0;
        int sayonara_time = 0;
        int atumeru_time = 0;
        int konnitiha_time = 0;
        int netu_time = 0;
        int ohayo_time = 0;
        int urayamasii_time = 0;
        int urusais_time = 0;
        int wakaranai_time = 0;

        
        public int nomu_total = 0;
        public int siawase_total = 0;
        public int kaze_total = 0;
        public int sayonara_total = 0;
        public int atumeru_total = 0;
        public int konnitiha_total = 0;
        public int netu_total = 0;
        public int ohayo_total = 0;
        public int urayamasii_total = 0;
        public int urusais_total = 0;
        public int wakaranai_total = 0;
        
        //int wakarimasita_time = 0;

        //タイマー
        DispatcherTimer dispatcherTimer;    // タイマーオブジェクト

        //メモ用
        private string saveFileName = @"memo.txt";

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
                /* 集中力
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
                */

                //手話
                if (gesture.Name == "nomu") //飲む
                { nomu = gesture; }
                if (gesture.Name == "kaze") //風邪
                { kaze = gesture; }
                if (gesture.Name == "siawase") //幸せ
                { siawase = gesture; }
                if (gesture.Name == "atumeru") //集める（胸付近を両手で仰ぐ）
                { atumeru = gesture; }
                if (gesture.Name == "konnitiwa") //こんにちは（額でチョキをする）
                { konnitiwa = gesture; }
                if (gesture.Name == "netu") //熱（額でパーをする）
                { netu = gesture; }
                if (gesture.Name == "ohayos") //おはよう（頭の横でグーをする）
                { ohayos = gesture; }
                if (gesture.Name == "sayonara_ges") //さようならジェスチャー（頭の真横が１、遠ざけると０）
                { sayonara_ges = gesture; }
                if (gesture.Name == "urayamasii") //うらやましい（右胸付近で自分を人差し指で指す）
                { urayamasii = gesture; }
                if (gesture.Name == "urusais") //うるさい（右耳に人差し指をいれる）
                { urusais = gesture; }
                if (gesture.Name == "wakaranai") //わからない（口元でパーをする）
                { wakaranai = gesture; }
                //if (gesture.Name == "wakarimasita_ges") //わかりましたジェスチャー（お腹を上下にさする）
                //{ wakarimasita_ges = gesture; }
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
                    /* Discrete
                    var resultSeat = gestureFrame.DiscreteGestureResults[seat];
                    var resultRPP = gestureFrame.DiscreteGestureResults[right_playphone];
                    var resultLPP = gestureFrame.DiscreteGestureResults[left_playphone];
                    */

                    //Discrete : handsign
                    var result = gestureFrame.DiscreteGestureResults[kaze];
                    var result2 = gestureFrame.DiscreteGestureResults[siawase];
                    var result3 = gestureFrame.DiscreteGestureResults[nomu];
                    var result4 = gestureFrame.DiscreteGestureResults[atumeru];
                    var result5 = gestureFrame.DiscreteGestureResults[konnitiwa];
                    var result6 = gestureFrame.DiscreteGestureResults[netu];
                    var result7 = gestureFrame.DiscreteGestureResults[ohayos];
                    var result8 = gestureFrame.DiscreteGestureResults[urayamasii];
                    var result9 = gestureFrame.DiscreteGestureResults[urusais];
                    var result10 = gestureFrame.DiscreteGestureResults[wakaranai];

                    //Continuous : handsign
                    var progressResult = gestureFrame.ContinuousGestureResults[sayonara_ges];
                    //var progressResult2 = gestureFrame.ContinuousGestureResults[wakarimasita_ges];

                    //ジェスチャー判定のしきい値
                    var kazeLimit = 0.3;
                    var siawaseLimit = 0.23;
                    var nomuLimit = 0.5;
                    var atumeruLimit = 0.4;
                    var konnitihaLimit = 0.7;
                    var netuLimit = 0.5;
                    var ohayoLimit = 0.6;
                    var urayamasiiLimit = 0.55;
                    var urusaiLimit = 0.15;
                    var wakaranaiLimit = 0.2;
                    var sayonaraLimit = 0.2;

                    //textBlock.Text = "座ってる動作：" + resultSeat.Confidence.ToString();
                    textBlock1.Text = "風邪：" + kazeLimit + " / " + result.Confidence.ToString();
                    textBlock2.Text = "幸せ：" + siawaseLimit + " / " + result2.Confidence.ToString();
                    textBlock3.Text = "飲む：" + nomuLimit + " / " + result3.Confidence.ToString();
                    textBlock4.Text = "集める：" + atumeruLimit + " / " + result4.Confidence.ToString();
                    textBlock5.Text = "こんにちは：" + konnitihaLimit + " / " + result5.Confidence.ToString();
                    textBlock6.Text = "熱：" + netuLimit + " / " + result6.Confidence.ToString();
                    textBlock7.Text = "おはよう：" + ohayoLimit + " / " + result7.Confidence.ToString();
                    textBlock8.Text = "羨ましい：" + urayamasiiLimit + " / " + result8.Confidence.ToString();
                    textBlock9.Text = "うるさい：" + urusaiLimit + " / " + result9.Confidence.ToString();
                    textBlock10.Text = "わからない：" + wakaranaiLimit + " / " + result10.Confidence.ToString();
                    textBlock11.Text = "さようならges：" + sayonaraLimit + " / " + progressResult.Progress.ToString();
                    //textBlock12.Text = "わかりましたges：" + progressResult2.Progress.ToString();

                    /*作業してるとき（座っている動作）
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
                    */

                    //咳をする動作 口まわり以外でも動作する
                    if (result.Confidence >= kazeLimit && result3.Confidence <= 0.2 && result9.Confidence <= 0.1)
                    {
                        Sw_kaze(true);
                    }
                    else
                    {
                        kaze_time = 0;
                    }

                    //幸せの動作
                    if (result2.Confidence >= siawaseLimit && result3.Confidence <= 0.2 && result8.Confidence <= 0.45)
                    {
                        Sw_siawase(true);
                    }
                    else
                    {
                        siawase_time = 0;
                    }

                    //飲む動作 幸せ風邪がよく絡む
                    if (result3.Confidence >= nomuLimit)
                    {
                        Sw_nomu(true);
                    }
                    else
                    {
                        nomu_time = 0;
                    }

                    //集める動作
                    if (result4.Confidence >= atumeruLimit && result8.Confidence <= 0.47)
                    {
                        Sw_atumeru(true);
                    }
                    else
                    {
                        atumeru_time = 0;
                    }

                    //こんにちは
                    if (result5.Confidence >= konnitihaLimit && result9.Confidence <= 0.1)
                    {
                        Sw_konnitiha(true);
                    }
                    else
                    {
                        konnitiha_time = 0;
                    }

                    //熱
                    if (result6.Confidence >= netuLimit)
                    {
                        Sw_netu(true);
                    }
                    else
                    {
                        netu_time = 0;
                    }

                    //おはよう 
                    if (result7.Confidence >= ohayoLimit && result.Confidence <= 0.3)
                    {
                        Sw_ohayo(true);
                    }
                    else
                    {
                        ohayo_time = 0;
                    }

                    //羨ましい
                    if (result8.Confidence >= urayamasiiLimit && result4.Confidence <= 0.4　&& result2.Confidence <= 0.6 && result9.Confidence <= 0.1)
                    {
                        Sw_urayamasii(true);
                    }
                    else
                    {
                        urayamasii_time = 0;
                    }

                    //うるさい
                    if (result9.Confidence >= urusaiLimit)
                    {
                        Sw_urusais(true);
                    }
                    else
                    {
                        urusais_time = 0;
                    }

                    //わからない
                    if (result10.Confidence >= wakaranaiLimit)
                    {
                        Sw_wakaranai(true);
                    }
                    else
                    {
                        wakaranai_time = 0;
                    }

                    //さようならのジェスチャー
                    if (progressResult.Progress < sayonaraLimit)
                    {
                        Sw_sayonara(true);
                    }
                    else if (sayonaraflag_ges && progressResult.Progress > 0.8)
                    {
                        Sw_sayonara(false);
                    }

                    /*わかりましたジェスチャー
                    if (progressResult2.Progress > 0.5)
                    {
                        Sw_wakarimasita(true);
                    }
                    else if (sayonaraflag_ges && progressResult2.Progress < 0.1)
                    {
                        Sw_wakarimasita(false);
                    }*/
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
                    seat_flag = true;
                    seat_time = 0;
                }
            }
            else
            {
                seat_time++;

                if (seat_time >= 30 && seat_flag)
                {
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
                    playphoneR_flag = true;
                    playphoneR_time = 0;
                }
            }
            else
            {
                if (playphoneR_flag)
                {
                    playphoneR_flag = false;
                    playphoneR_time = 0;
                }
            }
        }

        private void Sw_nomu(bool nomu_flag)
        {
            if (nomu_flag)
            {
                nomu_time++;
                nomu_total++;

                if (nomu_time == 20)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "飲む動作");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/drink.wav");
                    audio.Play();
                }

                if (nomu_time >= 5000)
                    nomu_time = 0;
            }
        }

        private void Sw_kaze(bool kaze_flag)
        {
            if (kaze_flag)
            {
                kaze_time++;
                kaze_total++;

                if (kaze_time == 10)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "風邪の手話");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/kaze.wav");
                    audio.Play();
                }

                if (kaze_time >= 5000)
                    kaze_time = 0;

            }
        }

        private void Sw_siawase(bool siawase_flag)
        {
            if (siawase_flag)
            {
                siawase_time++;
                siawase_total++;

                if (siawase_time == 20)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "幸せの手話");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/siawase.wav");
                    audio.Play();

                }

                if (siawase_time >= 5000)
                    siawase_time = 0;

            }

        }

        private void Sw_atumeru(bool atumeru_flag)
        {
            if (atumeru_flag)
            {
                atumeru_time++;
                atumeru_total++;

                if (atumeru_time == 30)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "集めるの手話");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/atumeru.wav");
                    audio.Play();
                }

                if (atumeru_time >= 5000)
                atumeru_time = 0;
            }

        }

        private void Sw_konnitiha(bool konnitiha_flag)
        {
            if (konnitiha_flag)
            {
                konnitiha_time++;
                konnitiha_total++;

                if (konnitiha_time == 10)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "こんにちはの手話");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/konnitiha.wav");
                    audio.Play();
                }

                if (konnitiha_time >= 5000)
                    konnitiha_time = 0;
            }

        }

        private void Sw_netu(bool netu_flag)
        {
            if (netu_flag)
            {
                netu_time++;
                netu_total++;

                if (netu_time == 10)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "熱の手話");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/netu.wav");
                    audio.Play();
                }

                if (netu_time >= 5000)
                    netu_time = 0;
            }

        }

        private void Sw_ohayo(bool ohayo_flag)
        {
            if (ohayo_flag)
            {
                ohayo_time++;
                ohayo_total++;

                if (ohayo_time == 10)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "おはようの手話");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/ohayo.wav");
                    audio.Play();
                }

                if (ohayo_time >= 5000)
                    ohayo_time = 0;
            }

        }

        private void Sw_urayamasii(bool urayamasii_flag)
        {
            if (urayamasii_flag)
            {
                urayamasii_time++;
                urayamasii_total++;

                if (urayamasii_time == 20)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "羨ましいの手話");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/urayamasii.wav");
                    audio.Play();
                }

                if (urayamasii_time >= 5000)
                    urayamasii_time = 0;
            }

        }

        private void Sw_urusais(bool urusais_flag)
        {
            if (urusais_flag)
            {
                urusais_time++;
                urusais_total++;

                if (urusais_time == 10)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "うるさいの手話");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/urusai.wav");
                    audio.Play();
                }

                if (urusais_time >= 5000)
                    urusais_time = 0;
            }

        }

        private void Sw_wakaranai(bool wakaranai_flag)
        {
            if (wakaranai_flag)
            {
                wakaranai_time++;
                wakaranai_total++;

                if (wakaranai_time == 20)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "分からないの手話");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/wakaranai.wav");
                    audio.Play();

                }

                if (wakaranai_time >= 5000)
                    wakaranai_time = 0;
            }

        }
        private void Sw_sayonara(bool sayonara_flag)
        {
            if (sayonara_flag)
            {
                sayonara_time++;
                sayonara_total++;

                if (sayonara_time < 100)
                {
                    sayonaraflag_ges = true;
                }
                else
                {
                    sayonaraflag_ges = false;
                    sayonara_time = 0;
                }

            }
            else
            {
                sayonara_time++;
                if (sayonara_time == 30)
                {
                    Console.WriteLine("[" + System.DateTime.Now.ToString() + "]" + "さよならのジェスチャー");

                    var audio = new Audio(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/sayonara.wav");
                    audio.Play();

                    sayonaraflag_ges = false;

                }
                if (sayonara_time >= 5000)
                    sayonara_time = 0;
            }
        }

        /*
        private void Sw_wakarimasita(bool wakarimasita_flag)
        {
            if (wakarimasita_flag)
            {
                wakarimasita_time++;

                if (wakarimasita_time < 100)
                {
                    wakarimasitaflag_ges = true;
                }
                else
                {
                    wakarimasitaflag_ges = false;
                    Console.WriteLine("false");
                    wakarimasita_time = 0;
                }

            }
            else
            {
                wakarimasita_time++;
                if (wakarimasita_time > 20)
                {
                    Console.WriteLine("わかりましたのジェスチャー");
                    wakarimasitaflag_ges = false;
                    wakarimasita_time = 0;
                }
            }
        }*/

        private void PlaySound(string waveFile)
        {
            //再生されているときは止める
            if (player != null)
                StopSound();

            //読み込む
            player = new System.Media.SoundPlayer(@"C:\Users\yudai\source\repos\WorkManagementApp\WorkManagementApp/Sound/hanabi.wav");
            //非同期再生する
            player.Play();

            player.PlayLooping();

            //次のようにすると、最後まで再生し終えるまで待機する
            //player.PlaySync();
        }

        //再生されている音を止める
        private void StopSound()
        {
            if (player != null)
            {
                player.Stop();
                player.Dispose();
                player = null;
            }
        }

        private void MusicStart(object sender, EventArgs e)
        {
            PlaySound(@"C: \Users\yudai\source\repos\WorkManagementApp\WorkManagementApp / Sound / hanabi.wav");
        }

        private void MusicStop(object sender, EventArgs e)
        {
            StopSound();
        }

        private void BtnTimeStart(object sender, RoutedEventArgs e)
        {
            TimerStart();
        }

        private void BtnTimeStop(object sender, RoutedEventArgs e)
        {
            TimerStop();
        }

        //メモ
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            // 保存用ダイヤログを開く
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.InitialDirectory = Environment.CurrentDirectory;
            saveFileDialog1.FileName = saveFileName;
            if (saveFileDialog1.ShowDialog() == true)
            {
                System.IO.Stream stream;
                stream = saveFileDialog1.OpenFile();
                if (stream != null)
                {
                    // ファイルに書き込む
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(stream);
                    sw.Write(textBoxMemo.Text);
                    sw.Close();
                    stream.Close();
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

        public void BtnTotalCount(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("nomu：" + nomu_total + "ms");
            Console.WriteLine("siawase：" + siawase_total + "ms");
            Console.WriteLine("kaze：" + kaze_total + "ms");
            Console.WriteLine("sayonara：" + sayonara_total + "ms");
            Console.WriteLine("atumeru：" + atumeru_total + "ms");
            Console.WriteLine("konnitiha：" + konnitiha_total + "ms");
            Console.WriteLine("netu：" + netu_total + "ms");
            Console.WriteLine("ohayo：" + ohayo_total + "ms");
            Console.WriteLine("urayamasii：" + urayamasii_total + "ms");
            Console.WriteLine("urusais：" + urusais_total + "ms");
            Console.WriteLine("wakaranai：" + wakaranai_total + "ms");
        }
    }
}

//作業している時間と集中している時間でズレが生じる原因はジェスチャー判別にある