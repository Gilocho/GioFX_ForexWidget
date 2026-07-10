namespace ForexWidget.App.Views;

using System.Windows;
using System.Windows.Input;

public partial class SupportWindow : Window
{
    public SupportWindow()
    {
        InitializeComponent();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
