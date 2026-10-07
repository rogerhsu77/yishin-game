using System.Drawing;

namespace yishin_game.UI
{
    public abstract class UIElement
    {
        public RectangleF Bounds;
        public bool Visible = true;
        public bool Enabled = true;

        public virtual void Update(double dt) { }
        public virtual void Draw(Graphics g) { }

        // HitTest should also update internal hover state if relevant
        public virtual bool HitTest(PointF p) => Visible && Enabled && Bounds.Contains(p);

        public virtual void OnClick(PointF p) { }
    }
}
