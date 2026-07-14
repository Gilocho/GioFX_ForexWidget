namespace ForexWidget.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

public partial class SupportViewModel : ObservableObject
{
    [RelayCommand]
    private void OpenSupportPage()
    {
        Process.Start(new ProcessStartInfo(SupportInfo.SupportPageUrl) { UseShellExecute = true });
    }
}
