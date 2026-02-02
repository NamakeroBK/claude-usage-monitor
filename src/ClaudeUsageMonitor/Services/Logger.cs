using System.IO;

namespace ClaudeUsageMonitor.Services;

public static class Logger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClaudeUsageMonitor",
        "debug.log"
    );

    private static readonly object _lock = new();

    static Logger()
    {
        var dir = Path.GetDirectoryName(LogPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
                File.AppendAllText(LogPath, line);
            }
        }
        catch { }
    }

    public static void Log(string category, string message)
    {
        Log($"[{category}] {message}");
    }

    public static void Error(string message, Exception? ex = null)
    {
        var errorMsg = ex != null ? $"{message}: {ex.Message}\n{ex.StackTrace}" : message;
        Log($"[ERROR] {errorMsg}");
    }

    public static string GetLogPath() => LogPath;
}
