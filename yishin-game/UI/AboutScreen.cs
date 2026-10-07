using System;
using System.Drawing;

namespace yishin_game.UI
{
    public class AboutScreen : UIScreen
    {
        private readonly Action? onBack;

        public AboutScreen(Action? onBack = null)
        {
            this.onBack = onBack;
            IsModal = true;

            int vw = 1280, vh = 720;
            // Back button
            float btnW = 240f, btnH = 60f;
            float bx = (vw - btnW) / 2f;
            float by = vh - btnH - 60f;

            var back = new UIButton { Text = "返回", Bounds = new RectangleF(bx, by, btnW, btnH) };
            back.Click = () => onBack?.Invoke();

            Elements.Add(back);
        }

        public override void Draw(Graphics g)
        {
            // 繪製半透明遮罩與 About 文字
            using (var brush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                g.FillRectangle(brush, 0, 0, 1280, 720);
            }

            using (var font = new Font("微軟正黑體", 20f))
            using (var brush = new SolidBrush(Color.White))
            {
                var text = "關於我們\n\n宜心會所遊戲示範\n作者: 鈞睿\n版本: 0.1";
                var rect = new RectangleF(140f, 120f, 1000f, 400f);
                var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
                g.DrawString(text, font, brush, rect, sf);
            }

            base.Draw(g);
        }
    }
}
