namespace ForexWidget.App.Views;

using System.Windows;
using System.Windows.Input;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    // Diálogo nativo de Windows: operación de plataforma, no lógica de negocio
    // (mismo criterio que el drag). El resultado se refleja vía ColorHex → ColorBrush.
    private void ColorSwatch_Click(object sender, MouseButtonEventArgs e)
    {
        // El click no debe burbujear al Border raíz (dispararía DragMove)
        e.Handled = true;

        if (sender is not System.Windows.Shapes.Rectangle rect ||
            rect.Tag is not ViewModels.KillzoneToggleViewModel row) return;

        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };
        try { dialog.Color = System.Drawing.ColorTranslator.FromHtml(row.ColorHex); }
        catch { /* hex ilegible: el diálogo abre con su color por defecto */ }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            row.ColorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
    }
}
