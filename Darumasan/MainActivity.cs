using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;

using Android.Views;
using Android.Widget;

using Com.Google.VRToolkit.CardBoard;

namespace Darumasan
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class MainActivity : CardboardActivity
    {
        private const int MP = ViewGroup.LayoutParams.MatchParent;
        private const int WC = ViewGroup.LayoutParams.WrapContent;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // layoutは利用せず、プログラムコードで画面を作成。
            var frame = new FrameLayout(this);
            SetContentView(frame);

            // create CardboardView and set.
            // CardboarViewの作成。
            var glview = new CardboardView(this);
            glview.SetAlignedToNorth(false);
            frame.AddView(glview, MP, MP);
            SetCardboardView(glview);

            // dont work on my AQUOS sense4 lite without below.
            // これがないと私のAQUOS sense4 liteで下記ログが表示され動作しない。
            // Surface size 2064x1008 does not match the expected screen size 2280x1080. Rendering is disabled.
            var screen = glview.GetScreenParams();
            glview.Holder.SetFixedSize(screen.getWidth(), screen.getHeight());

            // Logic, Modelを作成。
            var logic = new Logic();
            var model = new Model();

            // create Renderer and set.
            // Rendrerの作成。
            var renderer = new VrRenderer(this, logic, model);
            glview.SetRenderer(renderer);

            // 30fpsで更新する。
            glview.RenderMode = Android.Opengl.Rendermode.WhenDirty;
            var timer = new System.Timers.Timer(1000 / 30);
            timer.Elapsed += (s, ev) => glview.RequestRender();
            timer.Start();
            onDestroy += (s, e) => timer.Stop();

            // Jumpセンサーか画面タッチでAction実行。
            var sensor = new JumpSensor(this);
            if (sensor.Available)
            {
                sensor.OnJump = () => model.Action();
                onDestroy += (s, e) => sensor.Stop();
                sensor.Start();
            }
            onTouchEvent += (s, e) => model.Action();

            // Viewボタンで主観・上空視点切り替え
            {
                var btn = new Button(this) { Text = "View" };
                frame.AddView(btn, new FrameLayout.LayoutParams(WC, WC, GravityFlags.Top | GravityFlags.CenterHorizontal)); // 下中央

                logic.VrView = true;
                btn.Click += (s, e) =>
                {
                    logic.VrView = !logic.VrView;
                };
            }

            // VRボタンでステレオ・モノラル切り替え
            {
                var btn = new Button(this) { Text = "VR" };
                frame.AddView(btn, new FrameLayout.LayoutParams(WC, WC, GravityFlags.Bottom | GravityFlags.CenterHorizontal)); // 下中央

                glview.SetVRModeEnabled(logic.VrMode);
                btn.Click += (s, e) =>
                {
                    logic.VrMode = !logic.VrMode;
                    glview.SetVRModeEnabled(logic.VrMode);
                };
            }

            // 得点表示用。
            {
                var txt = new TextView(this);
                txt.SetTextColor(Android.Graphics.Color.White);
                frame.AddView(txt, new FrameLayout.LayoutParams(WC, WC, GravityFlags.Center)); // 中央
                model.OnStatusChanged += (status) =>
                {
                    string msg = status == Model.GameStatusType.Title ? "touch screen or shake device to start!" : "";
                    this.RunOnUiThread(() => txt.Text = msg);
                };
            }

            // 得点表示用。
            {
                var txt = new TextView(this);
                txt.SetTextColor(Android.Graphics.Color.White);
                frame.AddView(txt, new FrameLayout.LayoutParams(WC, WC, GravityFlags.Bottom | GravityFlags.Left)); // 左下
                model.GameScoreUpdated += value =>
                {
                    this.RunOnUiThread(() => txt.Text = "⛄:" + value.ToString());
                };
            }

            // バイブレーション
            var vibrate = false;
            {
                var chk = new CheckBox(this) { Text = "📳" };
                chk.CheckedChange += (s, ev) => vibrate = chk.Checked;
                frame.AddView(chk, new FrameLayout.LayoutParams(WC, WC, GravityFlags.Bottom | GravityFlags.Right)); // 右下
            }
            var vibrator = new Vibrator(this);
            if (vibrator.Available)
            {
                model.OnGameOver += () => { if (vibrate) vibrator.OneShot(); };
            }
        }

        private EventHandler onTouchEvent;
        public override bool OnTouchEvent(Android.Views.MotionEvent e)
        {
            onTouchEvent?.Invoke(this, EventArgs.Empty);
            return base.OnTouchEvent(e);
        }

        private EventHandler onDestroy;
        protected override void OnDestroy()
        {
            base.OnDestroy();
            onDestroy.Invoke(this, EventArgs.Empty);
        }
    }

    class VrRenderer : Java.Lang.Object, CardboardView.StereoRenderer
    {
        private Logic Logic;
        private Model Model;

        public VrRenderer(Context context, Logic logic, Model model)
        {
            Logic = logic;
            Model = model;
        }

        public void OnSurfaceCreated(Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            Logic.Init();
        }

        public void OnSurfaceChanged(int width, int height)
        {
            Logic.SetViewSize(width, height);
        }

        // OnNewFrame→OnDrawEye(Left Eye)→OnDrawEye(Right Eye)→OnFinishFrame→OnNewFrame→…の繰り返し(repeat)

        private float[] Forward = new float[3];
        public void OnNewFrame(HeadTransform transform)
        {
            // HeadTransformには、ヘッドセットの向きなどの情報が格納されている。

            // 顔の向いている方向を取得してPlayerにそっちを向かせる。
            transform.getForwardVector(Forward, 0);
            Model.Player.Forward = Forward.ToVector3();

            // ゲームの状態を更新。
            Model.UpdateStatus();

            Logic.Update();
        }

        public void OnDrawEye(EyeTransform transform)
        {
            // EyeTransformには、右目・左目などの情報が格納されている。
            Logic.Draw(transform, Model);
        }

        public void OnFinishFrame(Viewport viewport)
        {
            // 特にやることは無し。
        }

        public void OnRendererShutdown()
        {
            // 特にやることは無し。
        }
    }
}