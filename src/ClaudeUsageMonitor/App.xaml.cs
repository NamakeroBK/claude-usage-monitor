using System.Drawing;
using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using ClaudeUsageMonitor.Services;
using ClaudeUsageMonitor.ViewModels;
using ClaudeUsageMonitor.Views;

namespace ClaudeUsageMonitor;

public partial class App : Application
{
    private static Mutex? _mutex;
    private TaskbarIcon? _trayIcon;
    private MainViewModel? _viewModel;
    private int _lastNotifiedLevel = 0;
    private DateTime _lastResetTime = DateTime.MinValue;

    protected override void OnStartup(StartupEventArgs e)
    {
        Logger.Log("App", $"Starting... Log file: {Logger.GetLogPath()}");
        
        // Prevent multiple instances
        const string mutexName = "ClaudeUsageMonitor_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool createdNew);
        
        if (!createdNew)
        {
            Logger.Log("App", "Already running, exiting");
            MessageBox.Show("Claude 使用量モニターは既に起動しています。\nタスクトレイを確認してください。", 
                "多重起動エラー", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        try
        {
            Logger.Log("App", "Creating ViewModel...");
            _viewModel = new MainViewModel();
            
            // Use system icon
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "Claude 使用量モニター",
                Icon = SystemIcons.Information,
                TrayPopup = new MainPopup { DataContext = _viewModel }
            };

            // Update icon and check for notifications
            _viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.Utilization) ||
                    args.PropertyName == nameof(MainViewModel.CurrentLevel))
                {
                    UpdateTrayIcon();
                    CheckAndNotify();
                }
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"起動エラー: {ex.Message}\n\n{ex.StackTrace}", "Claude 使用量モニター", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon == null || _viewModel == null) return;

        _trayIcon.ToolTipText = $"Claude 使用量: {_viewModel.Utilization}%";
        
        // Update icon based on level using system icons
        _trayIcon.Icon = _viewModel.CurrentLevel switch
        {
            Models.UsageLevel.Safe => SystemIcons.Information,      // Blue i
            Models.UsageLevel.Moderate => SystemIcons.Warning,      // Yellow !
            Models.UsageLevel.Critical => SystemIcons.Error,        // Red X
            _ => SystemIcons.Information
        };
    }

    private void CheckAndNotify()
    {
        if (_trayIcon == null || _viewModel == null) return;

        var utilization = _viewModel.Utilization;

        // Notify at 80% (only once)
        if (utilization >= 80 && _lastNotifiedLevel < 80)
        {
            ShowNotification("⚠️ 使用量 80%", 
                $"Claude の使用量が {utilization}% に達しました。\nペースを落とすことを検討してください。",
                BalloonIcon.Warning);
            _lastNotifiedLevel = 80;
        }
        // Notify at 90% (only once)
        else if (utilization >= 90 && _lastNotifiedLevel < 90)
        {
            ShowNotification("🚨 使用量 90%", 
                $"Claude の使用量が {utilization}% に達しました！\nまもなく制限に達します。",
                BalloonIcon.Error);
            _lastNotifiedLevel = 90;
        }

        // Check for reset (utilization dropped significantly and time changed)
        if (_lastNotifiedLevel > 0 && utilization < 20)
        {
            ShowNotification("🔄 リセット完了", 
                "使用量がリセットされました。\n新しいセッションを開始できます。",
                BalloonIcon.Info);
            _lastNotifiedLevel = 0;
        }
    }

    private void ShowNotification(string title, string message, BalloonIcon icon)
    {
        _trayIcon?.ShowBalloonTip(title, message, icon);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _trayIcon?.Dispose();
            _viewModel?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
        finally
        {
            base.OnExit(e);
            // Force kill any remaining threads
            Environment.Exit(0);
        }
    }
}
