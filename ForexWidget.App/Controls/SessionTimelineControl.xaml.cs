namespace ForexWidget.App.Controls;

using ForexWidget.App.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

public partial class SessionTimelineControl : UserControl
{
    // Rectángulos de overlay agregados dinámicamente — se limpian en cada
    // redraw para no acumular elementos fantasma en el Canvas
    private readonly List<Rectangle> _overlayRects = new();

    private const double BarHeight = 14;

    public static readonly DependencyProperty NowFractionProperty =
        DependencyProperty.Register(
            nameof(NowFraction),
            typeof(double),
            typeof(SessionTimelineControl),
            new PropertyMetadata(0.0, OnNowFractionChanged));

    public double NowFraction
    {
        get => (double)GetValue(NowFractionProperty);
        set => SetValue(NowFractionProperty, value);
    }

    public SessionTimelineControl()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Redraw();
    }

    private static void OnNowFractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((SessionTimelineControl)d).Redraw();

    private void BarCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        if (DataContext is not SessionRowViewModel vm) return;

        double w = BarCanvas.ActualWidth;
        if (w <= 0) return;

        // La fila crece cuando se muestra el horario bajo el nombre: la barra se
        // centra contra la etiqueta en vez de quedar clavada arriba.
        double h = BarCanvas.ActualHeight;
        double barTop = (h - BarHeight) / 2;

        RailRect.Width = w;
        RailRect.SetValue(Canvas.TopProperty, barTop);

        BarRect.Fill = vm.BarColor;
        BarRect.SetValue(Canvas.LeftProperty, vm.BarStart * w);
        BarRect.SetValue(Canvas.TopProperty, barTop);
        BarRect.Width = vm.BarWidth * w;

        if (vm.HasMidnightCross)
        {
            BarRect2.Fill = vm.BarColor;
            BarRect2.Visibility = Visibility.Visible;
            BarRect2.SetValue(Canvas.LeftProperty, vm.BarStart2 * w);
            BarRect2.SetValue(Canvas.TopProperty, barTop);
            BarRect2.Width = vm.BarWidth2 * w;
        }
        else
        {
            BarRect2.Visibility = Visibility.Collapsed;
        }

        // Overlays de killzones: limpiar los del redraw anterior y redibujar
        foreach (var old in _overlayRects)
            BarCanvas.Children.Remove(old);
        _overlayRects.Clear();

        foreach (var overlay in vm.KillzoneOverlays)
        {
            var rect = new Rectangle
            {
                Width = overlay.BarWidth * w,
                Height = BarHeight,
                Fill = overlay.Color,
                // Atenuado sobre una sesión cerrada: distingue "esto pasa ahora"
                // de "acá caerá esta killzone cuando la sesión abra"
                Opacity = vm.IsOpen ? 0.85 : 0.35,
                RadiusX = 2,
                RadiusY = 2
            };
            Canvas.SetLeft(rect, overlay.BarStart * w);
            Canvas.SetTop(rect, barTop);
            BarCanvas.Children.Add(rect);
            _overlayRects.Add(rect);
        }

        NowLine.Height = h;
        NowLine.SetValue(Canvas.LeftProperty, NowFraction * w);
    }
}
