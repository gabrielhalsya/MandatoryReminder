using System.Drawing;
using System.Media;
using System.Windows;
using FormsContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using FormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using FormsToolTipIcon = System.Windows.Forms.ToolTipIcon;
using WpfApplication = System.Windows.Application;

namespace MandatoryReminder;

public sealed class TrayApp : IDisposable
{
    private readonly AppDataStore _store = new();
    private readonly StartupRegistrationService _startupRegistrationService = new();

    private FormsNotifyIcon? _trayIcon;
    private BatteryMonitorService? _monitorService;
    private MainWindow? _mainWindow;
    private MandatoryPopup? _popupWindow;
    private BatteryReminderSettings _settings = new();
    private List<BatteryLogEntry> _history = new();
    private BatteryStatusSnapshot? _lastSnapshot;
    private bool _isDisposed;
    private bool _isExiting;

    public BatteryReminderSettings CurrentSettings => _settings.Clone();

    public IReadOnlyList<BatteryLogEntry> CurrentHistory => _history;

    public BatteryStatusSnapshot? LastSnapshot => _lastSnapshot;

    public void Initialize()
    {
        _settings = _store.LoadSettings();
        ApplyStartupPreference(_settings.RunOnStartup);
        _settings.RunOnStartup = _startupRegistrationService.IsEnabled();
        _store.SaveSettings(_settings);

        _history = _store.LoadHistory();

        CreateTrayIcon();

        _monitorService = new BatteryMonitorService(_settings);
        _monitorService.SnapshotUpdated += OnSnapshotUpdated;
        _monitorService.LogEntryCreated += OnLogEntryCreated;
        _monitorService.ReminderTriggered += OnReminderTriggered;
        _monitorService.Start();
    }

    public void RefreshNow()
    {
        _monitorService?.RefreshNow();
    }

    public void OpenDashboard()
    {
        if (_mainWindow is null)
        {
            _mainWindow = new MainWindow(this);
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }

        _mainWindow.ApplyState(_settings, _history, _lastSnapshot);
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void SaveSettings(BatteryReminderSettings settings)
    {
        _settings = settings.Clone().Sanitize();
        ApplyStartupPreference(_settings.RunOnStartup);
        _settings.RunOnStartup = _startupRegistrationService.IsEnabled();
        _store.SaveSettings(_settings);

        _monitorService?.UpdateSettings(_settings);
        _mainWindow?.ApplyState(_settings, _history, _lastSnapshot);

        ShowTrayBalloon(
            "Settings saved",
            "Battery reminder preferences were updated.",
            FormsToolTipIcon.Info);
    }

    public void ShowTestReminder()
    {
        var snapshot = _lastSnapshot ?? BatteryStatusSnapshot.Capture(DateTime.Now);
        var reminder = new BatteryReminder(
            "Test reminder",
            "This is a test notification from Battery Charge Reminder.",
            FormsToolTipIcon.Info,
            "Test",
            snapshot);

        AddHistoryEntry(BatteryLogEntry.FromReminder(reminder));
        PresentReminder(reminder);
    }

    public void ClearHistory()
    {
        _history.Clear();
        _store.SaveHistory(_history);
        _mainWindow?.SetHistory(_history);
    }

    public void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;

        _popupWindow?.Close();

        if (_mainWindow is not null)
        {
            _mainWindow.PrepareForExit();
            _mainWindow.Close();
            _mainWindow = null;
        }

        Dispose();
        WpfApplication.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_monitorService is not null)
        {
            _monitorService.SnapshotUpdated -= OnSnapshotUpdated;
            _monitorService.LogEntryCreated -= OnLogEntryCreated;
            _monitorService.ReminderTriggered -= OnReminderTriggered;
            _monitorService.Dispose();
            _monitorService = null;
        }

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new FormsNotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "Battery Charge Reminder"
        };

        var contextMenu = new FormsContextMenuStrip();
        contextMenu.Items.Add("Open Dashboard", null, (_, _) => OpenDashboard());
        contextMenu.Items.Add("Check Now", null, (_, _) => RefreshNow());
        contextMenu.Items.Add("Show Test Reminder", null, (_, _) => ShowTestReminder());
        contextMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (_, _) => OpenDashboard();
    }

    private void OnSnapshotUpdated(object? sender, BatteryStatusSnapshot snapshot)
    {
        _lastSnapshot = snapshot;

        if (_trayIcon is not null)
        {
            var trayText = $"Battery {snapshot.ChargePercent}% - {snapshot.StatusText}";
            _trayIcon.Text = trayText.Length > 63 ? trayText[..63] : trayText;
        }

        _mainWindow?.SetSnapshot(snapshot);
    }

    private void OnLogEntryCreated(object? sender, BatteryLogEntry entry)
    {
        AddHistoryEntry(entry);
    }

    private void OnReminderTriggered(object? sender, BatteryReminder reminder)
    {
        PresentReminder(reminder);
    }

    private void PresentReminder(BatteryReminder reminder)
    {
        if (_settings.PlaySound)
        {
            SystemSounds.Exclamation.Play();
        }

        if (_settings.ShowTrayNotification || !_settings.ForcePopup)
        {
            ShowTrayBalloon(reminder.Title, reminder.Message, reminder.Icon);
        }

        if (_settings.ForcePopup)
        {
            ShowPopup(reminder);
        }
    }

    private void ShowPopup(BatteryReminder reminder)
    {
        if (_popupWindow is null || !_popupWindow.IsLoaded)
        {
            _popupWindow = new MandatoryPopup(reminder, OpenDashboard);
            _popupWindow.Closed += (_, _) => _popupWindow = null;
            _popupWindow.Show();
            return;
        }

        _popupWindow.UpdateReminder(reminder);
        _popupWindow.Show();
        _popupWindow.Activate();
    }

    private void ShowTrayBalloon(string title, string message, FormsToolTipIcon icon)
    {
        _trayIcon?.ShowBalloonTip(5000, title, message, icon);
    }

    private void AddHistoryEntry(BatteryLogEntry entry)
    {
        _history.Insert(0, entry);
        if (_history.Count > _settings.MaxHistoryEntries)
        {
            _history = _history.Take(_settings.MaxHistoryEntries).ToList();
        }

        _store.SaveHistory(_history);
        _mainWindow?.SetHistory(_history);
    }

    private void ApplyStartupPreference(bool enabled)
    {
        try
        {
            _startupRegistrationService.SetEnabled(enabled);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Startup registration could not be updated.\n\n{ex.Message}",
                "Battery Charge Reminder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
