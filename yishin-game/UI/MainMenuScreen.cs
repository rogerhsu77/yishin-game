using System;
using System.Drawing;

namespace yishin_game.UI
{
    public class MainMenuScreen : UIScreen
    {
        private readonly Action? onStart;
        private readonly Action? onExit;
        private readonly Action? onAbout;

        public MainMenuScreen(Action? onStart = null, Action? onExit = null, Action? onAbout = null)
        {
            this.onStart = onStart;
            this.onExit = onExit;
            this.onAbout = onAbout;
            IsModal = true;

            // 使用虛擬解析度 1280x720 的預設佈局
            int vw = 1280, vh = 720;
            float btnW = 360f, btnH = 80f, spacing = 20f;
            // 三個按鈕：開始、關於我們、離開
            float totalH = btnH * 3 + spacing * 2;
            float startX = (vw - btnW) / 2f;
            float startY = (vh - totalH) / 2f;

            var startBtn = new UIButton { Text = "開始遊戲", Bounds = new RectangleF(startX, startY, btnW, btnH) };
            startBtn.Click = () => onStart?.Invoke();

            var aboutBtn = new UIButton { Text = "關於我們", Bounds = new RectangleF(startX, startY + btnH + spacing, btnW, btnH) };
            aboutBtn.Click = () => onAbout?.Invoke();

            var exitBtn = new UIButton { Text = "離開", Bounds = new RectangleF(startX, startY + (btnH + spacing) * 2, btnW, btnH) };
            exitBtn.Click = () => onExit?.Invoke();

            Elements.Add(startBtn);
            Elements.Add(aboutBtn);
            Elements.Add(exitBtn);
        }
    }
}
