using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Windows11Settings.Controls
{
    public class DpiScaleDecorator : Decorator
    {
        public static readonly StyledProperty<double> ScaleProperty =
            AvaloniaProperty.Register<DpiScaleDecorator, double>(nameof(Scale), 1.0);

        public double Scale
        {
            get => GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        static DpiScaleDecorator()
        {
            ScaleProperty.Changed.AddClassHandler<DpiScaleDecorator>((x, _) =>
            {
                x.InvalidateMeasure();
                x.InvalidateArrange();
            });
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var child = Child;
            if (child == null)
                return default;

            var scale = Scale;
            if (scale <= 0)
                scale = 1.0;

            // Ask the child for more logical space so that, once scaled down,
            // it visually fills the available area.
            var childAvailable = new Size(
                availableSize.Width / scale,
                availableSize.Height / scale);

            child.Measure(childAvailable);

            var desired = child.DesiredSize;
            return new Size(desired.Width * scale, desired.Height * scale);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = Child;
            if (child == null)
                return finalSize;

            var scale = Scale;
            if (scale <= 0)
                scale = 1.0;

            // Apply a top-left anchored RenderTransform to the child.
            child.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
            child.RenderTransform = new ScaleTransform(scale, scale);

            // Arrange the child in unscaled coordinates so that
            // scaled content exactly fills finalSize.
            var unscaledRect = new Rect(
                0,
                0,
                finalSize.Width / scale,
                finalSize.Height / scale);

            child.Arrange(unscaledRect);

            return finalSize;
        }
    }
}