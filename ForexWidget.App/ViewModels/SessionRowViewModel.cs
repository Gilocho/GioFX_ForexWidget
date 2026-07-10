namespace ForexWidget.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;

/// <summary>Segmento donde la sesión se solapa con una killzone habilitada.</summary>
public record KillzoneOverlaySegment(double BarStart, double BarWidth, Brush Color);

public partial class SessionRowViewModel : ObservableObject
{
    public ObservableCollection<KillzoneOverlaySegment> KillzoneOverlays { get; } = new();

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private double _barStart;    // 0.0 - 1.0 (fracción de 24h)
    [ObservableProperty] private double _barWidth;    // 0.0 - 1.0
    [ObservableProperty] private string _openTimeUtc = "";
    [ObservableProperty] private string _closeTimeUtc = "";
    [ObservableProperty] private Brush _barColor = Brushes.Gray;

    // Sesiones que cruzan medianoche generan DOS barras — BarStart2/BarWidth2
    [ObservableProperty] private double _barStart2;
    [ObservableProperty] private double _barWidth2;
    [ObservableProperty] private bool _hasMidnightCross;
}
