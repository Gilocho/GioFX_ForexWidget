namespace ForexWidget.Infrastructure.Logging;

using System;
using System.IO;

/// <summary>
/// Logger mínimo a archivo diario. El logging NUNCA debe tumbar la app:
/// cualquier fallo de disco se traga silenciosamente.
/// </summary>
public class FileLogger
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public FileLogger(string? baseDirectory = null)
    {
        var baseDir = baseDirectory ?? Configuration.AppPaths.DataRoot;
        _logDirectory = Path.Combine(baseDir, "Logs");
        Directory.CreateDirectory(_logDirectory);
    }

    private string TodayLogPath =>
        Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.log");

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);
    public void Error(Exception ex) => Write("ERROR", $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");

    private void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                var line = $"[{DateTime.UtcNow:HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(TodayLogPath, line);
            }
        }
        catch { /* logging must never crash the app */ }
    }
}
