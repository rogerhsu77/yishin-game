using System.Collections.Generic;
using System.Drawing;

namespace yishin_game.UI
{
    public class UIScreen
    {
        public bool IsModal = false;
        public List<UIElement> Elements { get; } = new List<UIElement>();

        public virtual void Update(double dt)
        {
            for (int i = 0; i < Elements.Count; i++) Elements[i].Update(dt);
        }

        public virtual void Draw(Graphics g)
        {
            for (int i = 0; i < Elements.Count; i++) Elements[i].Draw(g);
        }

        public bool HandleClick(PointF p)
        {
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                var el = Elements[i];
                if (!el.Visible || !el.Enabled) continue;
                if (el.HitTest(p)) { el.OnClick(p); return true; }
            }
            return false;
        }

        public void HandleMouseMove(PointF p)
        {
            // 更新 hover 狀態，讓元素在 Draw 時顯示 hover
            for (int i = 0; i < Elements.Count; i++)
            {
                var el = Elements[i];
                if (!el.Visible || !el.Enabled) continue;
                el.HitTest(p);
            }
        }
    }
}
