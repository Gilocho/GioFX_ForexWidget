namespace ForexWidget.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;

public partial class SupportViewModel : ObservableObject
{
    public bool ShowPlaceholderWarning => SupportInfo.HasUnconfiguredPlaceholders;
    public string Trc20Address => SupportInfo.UsdtTrc20Address;
    public string Bep20Address => SupportInfo.UsdtBep20Address;
    public string Erc20Address => SupportInfo.UsdtErc20Address;

    public System.Action? RequestClose { get; set; }

    [RelayCommand]
    private void OpenPayPal()
    {
        Process.Start(new ProcessStartInfo(SupportInfo.PayPalMeUrl) { UseShellExecute = true });
    }

    [RelayCommand]
    private void CopyTrc20() => Clipboard.SetText(Trc20Address);

    [RelayCommand]
    private void CopyBep20() => Clipboard.SetText(Bep20Address);

    [RelayCommand]
    private void CopyErc20() => Clipboard.SetText(Erc20Address);

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();
}
