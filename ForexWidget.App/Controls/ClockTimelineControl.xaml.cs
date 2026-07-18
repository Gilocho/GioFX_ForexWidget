namespace ForexWidget.App.Controls;

using ForexWidget.App.ViewModels;
using ForexWidget.Infrastructure.Theming;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

// Code-behind con geometría polar — misma excepción MVVM ya aceptada para
// SessionTimelineControl desde Sprint 3: los datos vienen calculados del
// ViewModel (fracciones 0-1 del día), aquí solo se convierten a arcos.
public partial class ClockTimelineControl : UserControl
{
    // Sprint 12.2: anillos de 8px (antes 10) — comprime la banda de anillos
    // hacia afuera y deja más fondo limpio alrededor del centro, que se
    // percibía como un "círculo" compitiendo con los anillos
    private const double RingThickness = 8;
    // Killzone como línea fina DESPLAZADA hacia adentro del anillo (cae en el
    // gap), no superpuesta sobre el mismo trazo que el arco de sesión
    private const double KillzoneThickness = 4;
    private const double KillzoneRadiusOffset = 6;
    // Sprint 12.1: gap generoso — con 4px los anillos se veían pegados y
    // costaba distinguir cuál era cuál sin leer la etiqueta
    private const double RingGap = 9;
    private const double HourMarkerMargin = 14;
    private const double LabelFontSize = 8;
    private const double LabelLetterSpacing = 1.2; // px extra entre caracteres
    // Convención de reloj analógico (Sprint 12.2): el MINUTERO es el largo y
    // fino; la manecilla de posición del día (la "de hora") es corta y gruesa
    private const double DayHandLengthRatio = 0.55;
    private const double MinuteHandLengthRatio = 0.90;

    // Tinte por anillo (índice = posición en la colección Sessions: Sydney,
    // Tokyo, London, NY). Se MEZCLA con el SessionClosedBrush del tema en vez
    // de hardcodear colores, para que funcione igual en Dark y Light.
    private static readonly Color[] RingTints =
    [
        // Sydney con tinte propio (teal): con el gris base a secas el círculo
        // de fondo era idéntico al arco de sesión cerrada y no se distinguían
        Color.FromRgb(0x33, 0xDD, 0xAA),  // Sydney: tinte teal/aqua
        Color.FromRgb(0x44, 0x88, 0xFF),  // Tokyo: tinte azulado
        Color.FromRgb(0x88, 0x44, 0xFF),  // London: tinte violeta
        Color.FromRgb(0xFF, 0x55, 0x44),  // New York: tinte cálido
    ];
    private const double RingTintStrength = 0.18;

    // Las manecillas giran cada 1s: se conservan sus RotateTransform para
    // actualizar solo el ángulo sin redibujar los ~30 elementos del reloj.
    private RotateTransform? _nowHandTransform;
    private RotateTransform? _minuteHandTransform;

    public static readonly DependencyProperty NowFractionProperty =
        DependencyProperty.Register(
            nameof(NowFraction),
            typeof(double),
            typeof(ClockTimelineControl),
            new PropertyMetadata(0.0, OnNowFractionChanged));

    public double NowFraction
    {
        get => (double)GetValue(NowFractionProperty);
        set => SetValue(NowFractionProperty, value);
    }

    public static readonly DependencyProperty SessionsSourceProperty =
        DependencyProperty.Register(
            nameof(SessionsSource),
            typeof(IEnumerable),
            typeof(ClockTimelineControl),
            new PropertyMetadata(null, OnSessionsSourceChanged));

    public IEnumerable? SessionsSource
    {
        get => (IEnumerable?)GetValue(SessionsSourceProperty);
        set => SetValue(SessionsSourceProperty, value);
    }

    public ClockTimelineControl()
    {
        InitializeComponent();
    }

    private static void OnNowFractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ClockTimelineControl)d;
        if (control._nowHandTransform is not null)
        {
            control._nowHandTransform.Angle = PolarHelper.FractionToDegrees(control.NowFraction);
            if (control._minuteHandTransform is not null)
                control._minuteHandTransform.Angle = MinuteHandAngle(control.NowFraction);
        }
        else
        {
            control.Redraw();
        }
    }

    // El minutero da una vuelta por hora. NowFraction ya viene en fracción del
    // día EN MODO DISPLAY (UTC/Local aplicado por TimeDisplayHelper, Sprint 5.1),
    // así que la parte fraccionaria de la hora actual es directamente la
    // posición del minutero — sin volver a leer relojes ni offsets aquí.
    private static double MinuteHandAngle(double dayFraction)
        => ((dayFraction * 24.0) % 1.0) * 360.0;

    private static void OnSessionsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ClockTimelineControl)d;

        if (e.OldValue is INotifyCollectionChanged oldIncc)
            oldIncc.CollectionChanged -= control.OnSessionsCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newIncc)
            newIncc.CollectionChanged += control.OnSessionsCollectionChanged;

        control.Redraw();
    }

    // MainViewModel hace Clear() + 4 Add() en cada Refresh (30s): son 5 redraws
    // seguidos de ~25 elementos — barato, y Children.Clear() en Redraw garantiza
    // que no se acumulan elementos fantasma (mismo cuidado que Sprint 6.1).
    private void OnSessionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Redraw();

    private void ClockCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        ClockCanvas.Children.Clear();
        _nowHandTransform = null;
        _minuteHandTransform = null;

        double w = ClockCanvas.ActualWidth;
        double h = ClockCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var sessions = SessionsSource?.OfType<SessionRowViewModel>().ToList();
        if (sessions is not { Count: > 0 }) return;

        var center = new Point(w / 2, h / 2);
        // El radio es el del CENTRO del trazo: medio trazo extra queda fuera,
        // más el margen reservado para los números de hora (00/06/12/18)
        double outerRadius = Math.Min(w, h) / 2 - RingThickness / 2 - HourMarkerMargin;

        double radius = outerRadius;
        for (int i = 0; i < sessions.Count; i++)
        {
            DrawRing(center, radius, sessions[i], i);
            radius -= RingThickness + RingGap;
        }

        DrawHourMarkers(center, outerRadius + RingThickness / 2 + 8);
        double handTipRadius = outerRadius + RingThickness / 2;
        DrawNowHand(center, handTipRadius * DayHandLengthRatio);
        DrawMinuteHand(center, handTipRadius * MinuteHandLengthRatio);
    }

    // Las 24 horas alrededor del dial (Sprint 12.2 — con solo 00/06/12/18 no
    // alcanzaba para ubicarse a simple vista). Jerarquía sutil: los múltiplos
    // de 6 más grandes y brillantes, el resto más chicos y tenues, para que
    // los 24 no se amontonen visualmente. Los valores son fijos en UTC y Local
    // porque las fracciones de los arcos ya vienen en modo de display.
    private void DrawHourMarkers(Point center, double numberRadius)
    {
        var brush = (Brush)Application.Current.FindResource("TextSecondaryBrush");

        for (int hour = 0; hour < 24; hour++)
        {
            bool isCardinal = hour % 6 == 0;
            var tb = new TextBlock
            {
                Text = hour.ToString("00"),
                FontSize = isCardinal ? 8 : 7,
                Foreground = brush,
                Opacity = isCardinal ? 0.9 : 0.55
            };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var pos = PolarHelper.PointOnCircle(
                center, numberRadius, PolarHelper.FractionToDegrees(hour / 24.0));
            Canvas.SetLeft(tb, pos.X - tb.DesiredSize.Width / 2);
            Canvas.SetTop(tb, pos.Y - tb.DesiredSize.Height / 2);
            ClockCanvas.Children.Add(tb);
        }
    }

    // Fondo del anillo: el SessionClosedBrush del tema mezclado con un tinte
    // sutil por posición — los 4 anillos se distinguen de un vistazo sin
    // depender de la etiqueta, y el resultado sigue al tema activo.
    private static Brush MakeRingBackground(int ringIndex)
    {
        var baseBrush = (Brush)Application.Current.FindResource("SessionClosedBrush");
        var tint = RingTints[ringIndex % RingTints.Length];
        if (tint == Colors.Transparent || baseBrush is not SolidColorBrush solid)
            return baseBrush;

        Color b = solid.Color;
        var mixed = Color.FromRgb(
            (byte)(b.R + (tint.R - b.R) * RingTintStrength),
            (byte)(b.G + (tint.G - b.G) * RingTintStrength),
            (byte)(b.B + (tint.B - b.B) * RingTintStrength));
        return new SolidColorBrush(mixed);
    }

    private void DrawRing(Point center, double radius, SessionRowViewModel session, int ringIndex)
    {
        // Fondo: círculo completo — las 24h del día para esta sesión
        var bgCircle = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Stroke = MakeRingBackground(ringIndex),
            StrokeThickness = RingThickness
        };
        Canvas.SetLeft(bgCircle, center.X - radius);
        Canvas.SetTop(bgCircle, center.Y - radius);
        ClockCanvas.Children.Add(bgCircle);

        // Arco(s) de sesión: BarColor ya trae verde si está abierta y gris si no,
        // idéntico al criterio de las barras — mismas fracciones, otra geometría
        DrawArc(center, radius, session.BarStart, session.BarWidth,
                session.BarColor, RingThickness);

        if (session.HasMidnightCross)
        {
            DrawArc(center, radius, session.BarStart2, session.BarWidth2,
                    session.BarColor, RingThickness);
        }

        // Killzones (Sprint 12.2): línea fina desplazada hacia el borde interno
        // del anillo — cae en el gap entre anillos, así no compite en el mismo
        // trazo con el arco de sesión. Atenuada si la sesión no está activa
        // (mismo criterio de Sprint 10).
        double opacity = session.IsOpen ? 0.9 : 0.4;
        foreach (var kz in session.KillzoneOverlays)
            DrawArc(center, radius - KillzoneRadiusOffset,
                    kz.BarStart, kz.BarWidth, kz.Color, KillzoneThickness, opacity);

        DrawRingLabel(center, radius, session);
    }

    // Nombre de la sesión curvado sobre su propio arco (estilo world-clock):
    // sin esto no hay forma de saber qué anillo corresponde a cada mercado.
    private void DrawRingLabel(Point center, double radius, SessionRowViewModel session)
    {
        string text = session.Name.ToUpperInvariant();
        if (text.Length == 0) return;

        // Punto medio del arco de sesión completo. Con cruce de medianoche los
        // dos segmentos son contiguos a través de 1.0→0.0, así que el ancho
        // combinado se mide desde BarStart y se normaliza con módulo.
        double combinedWidth = session.BarWidth + (session.HasMidnightCross ? session.BarWidth2 : 0);
        if (combinedWidth <= 0) return;
        double midFraction = (session.BarStart + combinedWidth / 2) % 1.0;
        double centerAngle = PolarHelper.FractionToDegrees(midFraction);

        // En la mitad inferior el texto tangente quedaría de cabeza: se voltea
        // cada carácter 180° y se recorre el arco en sentido contrario para
        // que siga leyéndose de izquierda a derecha en pantalla
        bool flip = centerAngle > 90 && centerAngle < 270;

        var brush = (Brush)Application.Current.FindResource("TextPrimaryBrush");

        // Medir cada carácter para repartir el arco proporcionalmente
        var blocks = new List<TextBlock>(text.Length);
        var widths = new List<double>(text.Length);
        foreach (char c in text)
        {
            var tb = new TextBlock
            {
                Text = c.ToString(),
                FontSize = LabelFontSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = brush
            };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            blocks.Add(tb);
            widths.Add(tb.DesiredSize.Width + LabelLetterSpacing);
        }

        double toDegrees = 180.0 / (Math.PI * radius); // px de arco → grados
        double totalAngle = (widths.Sum() - LabelLetterSpacing) * toDegrees;
        double angle = flip ? centerAngle + totalAngle / 2 : centerAngle - totalAngle / 2;

        for (int i = 0; i < blocks.Count; i++)
        {
            double halfChar = widths[i] * toDegrees / 2;
            angle += flip ? -halfChar : halfChar;

            var tb = blocks[i];
            var pos = PolarHelper.PointOnCircle(center, radius, angle);
            tb.RenderTransformOrigin = new Point(0.5, 0.5);
            tb.RenderTransform = new RotateTransform(flip ? angle + 180 : angle);
            Canvas.SetLeft(tb, pos.X - tb.DesiredSize.Width / 2);
            Canvas.SetTop(tb, pos.Y - tb.DesiredSize.Height / 2);
            Panel.SetZIndex(tb, 5); // sobre los arcos, debajo de la manecilla
            ClockCanvas.Children.Add(tb);

            angle += flip ? -halfChar : halfChar;
        }
    }

    private void DrawArc(Point center, double radius,
                         double startFraction, double widthFraction,
                         Brush color, double thickness, double opacity = 1.0)
    {
        if (widthFraction <= 0) return;

        // Fracción de día completo: un ArcSegment con inicio == fin degenera
        // en nada — se dibuja como círculo completo
        if (widthFraction >= 1.0)
        {
            var full = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = color,
                StrokeThickness = thickness,
                Opacity = opacity
            };
            Canvas.SetLeft(full, center.X - radius);
            Canvas.SetTop(full, center.Y - radius);
            ClockCanvas.Children.Add(full);
            return;
        }

        double startDeg = PolarHelper.FractionToDegrees(startFraction);
        double endDeg = PolarHelper.FractionToDegrees(startFraction + widthFraction);

        var startPoint = PolarHelper.PointOnCircle(center, radius, startDeg);
        var endPoint = PolarHelper.PointOnCircle(center, radius, endDeg);

        var arcSegment = new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            IsLargeArc = PolarHelper.IsLargeArc(startDeg, endDeg),
            SweepDirection = SweepDirection.Clockwise
        };

        var figure = new PathFigure { StartPoint = startPoint };
        figure.Segments.Add(arcSegment);

        var path = new Path
        {
            Data = new PathGeometry([figure]),
            Stroke = color,
            StrokeThickness = thickness,
            Opacity = opacity
        };
        ClockCanvas.Children.Add(path);
    }

    private void DrawNowHand(Point center, double tipRadius)
    {
        // Centro de rotación explícito en coordenadas del canvas: más robusto
        // que RenderTransformOrigin, que para una Line vertical depende de un
        // bounding box de ancho cero
        var transform = new RotateTransform(
            PolarHelper.FractionToDegrees(NowFraction), center.X, center.Y);

        var line = new Line
        {
            X1 = center.X,
            Y1 = center.Y,
            X2 = center.X,
            Y2 = center.Y - tipRadius,
            Stroke = (Brush)Application.Current.FindResource("AccentBrush"),
            StrokeThickness = 3, // corta y gruesa: la "de hora" del dial de 24h
            RenderTransform = transform
        };
        Panel.SetZIndex(line, 10); // siempre por encima de anillos y killzones
        ClockCanvas.Children.Add(line);

        _nowHandTransform = transform;
    }

    // Minutero: LARGO y fino en color discreto, frente a la manecilla de 24h
    // corta y gruesa — convención estándar de reloj analógico (Sprint 12.2
    // corrigió la relación, que en 12.1 quedó invertida).
    private void DrawMinuteHand(Point center, double length)
    {
        var transform = new RotateTransform(
            MinuteHandAngle(NowFraction), center.X, center.Y);

        var line = new Line
        {
            X1 = center.X,
            Y1 = center.Y,
            X2 = center.X,
            Y2 = center.Y - length,
            Stroke = (Brush)Application.Current.FindResource("TextSecondaryBrush"),
            StrokeThickness = 1.5,
            RenderTransform = transform
        };
        Panel.SetZIndex(line, 9); // bajo la manecilla principal
        ClockCanvas.Children.Add(line);

        _minuteHandTransform = transform;
    }
}
