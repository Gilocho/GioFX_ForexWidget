namespace ForexWidget.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

public partial class KillzoneBarViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private double _barStart;   // 0.0 - 1.0
    [ObservableProperty] private double _barWidth;   // 0.0 - 1.0
    [ObservableProperty] private Brush _color = Brushes.Orange;
    [ObservableProperty] private string _timeUntil = "";
}
