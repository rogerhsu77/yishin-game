using System;
using System.Drawing;

namespace yishin_game.UI
{
    public class UIButton : UIElement
    {
        public string Text = "Button";
        public Action? Click;

        private bool hovered = false;

        public override void Update(double dt)
        {
            // 可加入動畫或狀態過渡
        }

        public override void Draw(Graphics g)
        {
            var rect = Bounds;
            using (var brush = new SolidBrush(hovered ? Color.FromArgb(200, 70, 130, 180) : Color.FromArgb(200, 50, 50, 50)))
            using (var pen = new Pen(Color.White))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }

            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var font = new Font("微軟正黑體", 20f))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(Text, font, brush, rect, sf);
            }
        }

        public override bool HitTest(PointF p)
        {
            hovered = Visible && Enabled && Bounds.Contains(p);
            return hovered;
        }

        public override void OnClick(PointF p)
        {
            Click?.Invoke();
        }
    }
}
