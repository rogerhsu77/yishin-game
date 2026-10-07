using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using yishin_game.UI;

namespace yishin_game
{
    public partial class Form1 : Form
    {
        private const double TargetDt = 1.0 / 60.0; // 固定更新步長
        private Thread? gameThread;
        private volatile bool running = false;
        private bool isFullscreen = true;

        private readonly Size designSize = new Size(1280, 720);
        private UIManager? uiManager;

        public Form1()
        {
            InitializeComponent();

            // 高 DPI 設定（主要在 Program.Main 設定 HighDpiMode）
            this.AutoScaleMode = AutoScaleMode.Dpi;

            // 預設進入無邊框全螢幕（非獨佔模式）
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.DoubleBuffered = true; // 雙緩衝避免閃爍

            this.MinimumSize = new Size(800, 600);

            this.KeyDown += OnKeyDown;
            this.Resize += Form1_Resize;
            this.Load += Form1_Load;

            StartGameLoop();

            // 初始化 UIManager 與主選單
            uiManager = new UIManager();
            MainMenuScreen? mainMenu = null;
            mainMenu = new MainMenuScreen(
                onStart: () =>
                {
                    // 移除主選單，推入對話；對話結束時回到主選單
                    uiManager?.Pop();
                    uiManager?.Push(new DialogueScreen(() =>
                    {
                        uiManager?.Pop(); // 移除 DialogueScreen
                        if (mainMenu != null) uiManager?.Push(mainMenu);
                    }));
                },
                onExit: () => this.BeginInvoke((Action)(() => this.Close())),
                onAbout: () => uiManager?.Push(new AboutScreen(() => uiManager?.Pop()))
            );
            uiManager.Push(mainMenu);
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // Designer 會綁定此事件；初始化後的邏輯可放在此處
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                // 結束遊戲
                this.Close();
            }

            if (e.KeyCode == Keys.F11)
            {
                ToggleFullscreen();
            }

            // TODO: 傳給 InputManager
        }

        private void ToggleFullscreen()
        {
            if (isFullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                this.TopMost = false;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                this.TopMost = true;
            }

            isFullscreen = !isFullscreen;
        }

        private void StartGameLoop()
        {
            running = true;
            gameThread = new Thread(GameLoop) { IsBackground = true };
            gameThread.Start();
        }

        private void GameLoop()
        {
            var sw = Stopwatch.StartNew();
            double previous = sw.Elapsed.TotalSeconds;
            double accumulator = 0.0;

            while (running)
            {
                double now = sw.Elapsed.TotalSeconds;
                double elapsed = now - previous;
                previous = now;
                accumulator += elapsed;

                while (accumulator >= TargetDt)
                {
                    UpdateGame(TargetDt);
                    accumulator -= TargetDt;
                }

                double alpha = accumulator / TargetDt;
                RenderFrame(alpha);

                Thread.Sleep(1);
            }
        }

        private void UpdateGame(double dt)
        {
            // 更新輸入、場景、實體、物理等（dt 為秒）
            // 更新 UI
            uiManager?.Update(dt);
        }

        private void RenderFrame(double alpha)
        {
            // 在 WinForms 中將繪製委派回 UI 執行緒進行
            if (!this.IsDisposed && this.IsHandleCreated)
            {
                try
                {
                    this.BeginInvoke((Action)(() => this.Invalidate()));
                }
                catch
                {
                    // 忽略已關閉情況
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            // 計算等比縮放與 letterbox
            float scale = Math.Min((float)ClientSize.Width / designSize.Width, (float)ClientSize.Height / designSize.Height);
            float destW = designSize.Width * scale;
            float destH = designSize.Height * scale;
            float destX = (ClientSize.Width - destW) / 2f;
            float destY = (ClientSize.Height - destH) / 2f;

            var prevTransform = g.Transform;
            g.TranslateTransform(destX, destY);
            g.ScaleTransform(scale, scale);

            // 清背景
            g.Clear(Color.Black);

            // TODO: 呼叫 SceneManager.Draw(g) 由場景負責渲染
            // 範例：在中央畫一個簡單文字
            using (var sf = new Font("微軟正黑體", 24f))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString("宜心會所 - 測試畫面", sf, brush, 20f, 20f);
            }

            // 繪製 UI（在虛擬解析度座標系下）
            uiManager?.Draw(g);

            // 還原 transform
            g.Transform = prevTransform;
        }

        private PointF ScreenToVirtual(Point screenPt)
        {
            float scale = Math.Min((float)ClientSize.Width / designSize.Width, (float)ClientSize.Height / designSize.Height);
            float destW = designSize.Width * scale;
            float destH = designSize.Height * scale;
            float offsetX = (ClientSize.Width - destW) / 2f;
            float offsetY = (ClientSize.Height - destH) / 2f;
            return new PointF((screenPt.X - offsetX) / scale, (screenPt.Y - offsetY) / scale);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var v = ScreenToVirtual(e.Location);
            uiManager?.HandleClick(v);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var v = ScreenToVirtual(e.Location);
            uiManager?.HandleMouseMove(v);
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            // 目前以自動縮放與等比 letterbox 處理，不使用 Form.Scale 的累積縮放
            // 可在此重新計算 UI 佈局或通知 UIManager
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            running = false;
            try
            {
                gameThread?.Join(500);
            }
            catch { }

            base.OnFormClosing(e);
        }
    }
}
