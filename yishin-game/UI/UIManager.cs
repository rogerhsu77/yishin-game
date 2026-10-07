using System.Collections.Generic;
using System.Drawing;

namespace yishin_game.UI
{
    public class UIManager
    {
        // 虛擬解析度常數（供各畫面佈局使用）
        public static int VirtualWidth { get; } = 1280;
        public static int VirtualHeight { get; } = 720;
        private List<UIScreen> screens = new List<UIScreen>();

        public void Push(UIScreen s) => screens.Add(s);
        public void Pop() { if (screens.Count > 0) screens.RemoveAt(screens.Count - 1); }

        public void Update(double dt)
        {
            for (int i = 0; i < screens.Count; i++) screens[i].Update(dt);
        }

        public void Draw(Graphics g)
        {
            // 限制繪製在虛擬解析度範圍 (1280x720)
            var prevClip = g.Clip;
            try
            {
                g.SetClip(new RectangleF(0, 0, VirtualWidth, VirtualHeight));
                for (int i = 0; i < screens.Count; i++) screens[i].Draw(g);
            }
            finally
            {
                g.Clip = prevClip;
            }
        }

        public bool HandleClick(PointF p)
        {
            for (int i = screens.Count - 1; i >= 0; i--)
            {
                if (screens[i].HandleClick(p)) return true;
                if (screens[i].IsModal) return false;
            }
            return false;
        }

        public void HandleMouseMove(PointF p)
        {
            for (int i = screens.Count - 1; i >= 0; i--)
            {
                screens[i].HandleMouseMove(p);
                if (screens[i].IsModal) break;
            }
        }
    }
}
