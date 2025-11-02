using Microsoft.Maui.Graphics;

namespace Hefi.Mobile.Controls;

public class ChartView : GraphicsView
{
    public static readonly BindableProperty ProteinProperty =
        BindableProperty.Create(nameof(Protein), typeof(double), typeof(ChartView), 0d, propertyChanged: Redraw);
    public static readonly BindableProperty CarbsProperty =
        BindableProperty.Create(nameof(Carbs), typeof(double), typeof(ChartView), 0d, propertyChanged: Redraw);
    public static readonly BindableProperty FatProperty =
        BindableProperty.Create(nameof(Fat), typeof(double), typeof(ChartView), 0d, propertyChanged: Redraw);

    public double Protein { get => (double)GetValue(ProteinProperty); set => SetValue(ProteinProperty, value); }
    public double Carbs { get => (double)GetValue(CarbsProperty); set => SetValue(CarbsProperty, value); }
    public double Fat { get => (double)GetValue(FatProperty); set => SetValue(FatProperty, value); }

    public ChartView()
    {
        Drawable = new Chart(this);
    }

    static void Redraw(BindableObject b, object o, object n)
    { if (b is ChartView v) v.Invalidate(); }

    class Chart : IDrawable
    {
        private readonly ChartView _v;
        public Chart(ChartView v) => _v = v;

        public void Draw(ICanvas canvas, RectF rect)
        {
            var total = Math.Max(1, _v.Protein + _v.Carbs + _v.Fat);
            float cx = rect.Center.X, cy = rect.Center.Y;
            float rOuter = Math.Min(rect.Width, rect.Height) / 2f;
            float rInner = rOuter * 0.6f;

            // background ring
            canvas.SaveState();
            canvas.FillColor = Colors.LightGray.WithAlpha(0.2f);
            canvas.FillCircle(cx, cy, rOuter);
            canvas.FillColor = Colors.White;
            canvas.FillCircle(cx, cy, rInner);
            canvas.RestoreState();

            float start = -90f;
            void Slice(double value, Color color)
            {
                var sweep = (float)(value / total * 360f);
                if (sweep <= 0) return;

                var x = cx - rOuter;
                var y = cy - rOuter;
                var w = rOuter * 2;
                var h = rOuter * 2;

                var end = start + sweep;

                canvas.FillColor = color;
                canvas.FillArc(x, y, w, h, start, end, true);
                canvas.FillColor = Colors.White;
                canvas.FillCircle(cx, cy, rInner);

                start = end;
            }

            // neutral but distinct; no theme hardcoding
            Slice(_v.Protein, Color.FromArgb("#3B82F6")); // P
            Slice(_v.Carbs, Color.FromArgb("#22C55E")); // C
            Slice(_v.Fat, Color.FromArgb("#F59E0B")); // F
        }
    }
}
