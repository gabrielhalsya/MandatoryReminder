using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace MandatoryReminder;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<BatteryLogEntry> _historyEntries = new();
    private readonly TrayApp? _trayApp;
    private BatteryReminderSettings _currentSettings = new();
    private BatteryStatusSnapshot? _currentSnapshot;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        HistoryDataGrid.ItemsSource = _historyEntries;
        UpdateVisualSummary();
    }

    internal MainWindow(TrayApp trayApp)
        : this()
    {
        _trayApp = trayApp;
    }

    public void ApplyState(
        BatteryReminderSettings settings,
        IReadOnlyList<BatteryLogEntry> history,
        BatteryStatusSnapshot? snapshot)
    {
        LoadSettings(settings);
        SetHistory(history);

        if (snapshot is not null)
        {
            SetSnapshot(snapshot);
        }
        else
        {
            UpdateVisualSummary();
        }
    }

    public void SetSnapshot(BatteryStatusSnapshot snapshot)
    {
        _currentSnapshot = snapshot;

        CurrentPercentTextBlock.Text = $"{snapshot.ChargePercent}%";
        CurrentStatusTextBlock.Text = snapshot.StatusText;
        CurrentDetailsTextBlock.Text = BuildDetailText(snapshot);
        PowerSourceValueTextBlock.Text = snapshot.IsOnExternalPower ? "AC connected" : "Battery mode";
        LastCheckedTextBlock.Text = $"Last checked: {snapshot.Timestamp:g}";
        ChargeProgressBar.Value = snapshot.ChargePercent;

        UpdateVisualSummary();
    }

    public void SetHistory(IReadOnlyList<BatteryLogEntry> history)
    {
        _historyEntries.Clear();
        foreach (var entry in history)
        {
            _historyEntries.Add(entry);
        }

        EmptyHistoryTextBlock.Visibility = _historyEntries.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public void PrepareForExit()
    {
        _allowClose = true;
    }

    private void LoadSettings(BatteryReminderSettings settings)
    {
        _currentSettings = settings.Clone().Sanitize();

        HighReminderCheckBox.IsChecked = _currentSettings.HighReminderEnabled;
        HighReminderTextBox.Text = _currentSettings.HighReminderPercent.ToString();
        LowReminderCheckBox.IsChecked = _currentSettings.LowReminderEnabled;
        LowReminderTextBox.Text = _currentSettings.LowReminderPercent.ToString();
        FullReminderCheckBox.IsChecked = _currentSettings.FullReminderEnabled;
        ForcePopupCheckBox.IsChecked = _currentSettings.ForcePopup;
        PlaySoundCheckBox.IsChecked = _currentSettings.PlaySound;
        TrayNotificationCheckBox.IsChecked = _currentSettings.ShowTrayNotification;
        RunOnStartupCheckBox.IsChecked = _currentSettings.RunOnStartup;
        MonitorIntervalTextBox.Text = _currentSettings.MonitorIntervalSeconds.ToString();
    }

    private void UpdateVisualSummary()
    {
        HeaderStatusTextBlock.Text = _currentSnapshot?.IsOnExternalPower == true
            ? "Charging watch active"
            : _currentSnapshot is null
                ? "Tray monitoring active"
                : "Battery watch active";

        DeliveryValueTextBlock.Text = BuildDeliverySummary();
        NextReminderValueTextBlock.Text = BuildNextReminderSummary();

        if (_currentSnapshot is null)
        {
            StatusBadgeTextBlock.Text = "Starting up";
            StatusBadgeBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 217, 200));
            ChargeProgressBar.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 61, 69));
            return;
        }

        var badgeBackground = System.Windows.Media.Color.FromRgb(225, 240, 232);
        var badgeText = "Watching";
        var progressColor = System.Windows.Media.Color.FromRgb(23, 77, 89);

        if (!_currentSnapshot.IsOnExternalPower && _currentSnapshot.ChargePercent <= _currentSettings.LowReminderPercent)
        {
            badgeBackground = System.Windows.Media.Color.FromRgb(247, 212, 206);
            badgeText = "Low battery";
            progressColor = System.Windows.Media.Color.FromRgb(181, 77, 58);
        }
        else if (_currentSnapshot.IsOnExternalPower && _currentSnapshot.ChargePercent >= 100)
        {
            badgeBackground = System.Windows.Media.Color.FromRgb(244, 229, 191);
            badgeText = "Fully charged";
            progressColor = System.Windows.Media.Color.FromRgb(120, 104, 53);
        }
        else if (_currentSnapshot.IsOnExternalPower)
        {
            badgeBackground = System.Windows.Media.Color.FromRgb(232, 241, 214);
            badgeText = "Charging";
            progressColor = System.Windows.Media.Color.FromRgb(58, 97, 70);
        }

        StatusBadgeTextBlock.Text = badgeText;
        StatusBadgeBorder.Background = new SolidColorBrush(badgeBackground);
        ChargeProgressBar.Foreground = new SolidColorBrush(progressColor);
    }

    private string BuildDetailText(BatteryStatusSnapshot snapshot)
    {
        if (snapshot.IsOnExternalPower)
        {
            if (_currentSettings.FullReminderEnabled && snapshot.ChargePercent >= 100)
            {
                return "Battery is already full. The next full-charge alert will arm again after the level drops below 100%.";
            }

            if (_currentSettings.HighReminderEnabled && snapshot.ChargePercent < _currentSettings.HighReminderPercent)
            {
                return $"Charging reminder is armed for {_currentSettings.HighReminderPercent}%. You can leave the app in the tray and wait.";
            }

            if (_currentSettings.FullReminderEnabled)
            {
                return "The plugged-in threshold has already been reached. Full-charge reminder is still watching for 100%.";
            }

            return "Charging reminders are disabled right now, so the app is monitoring quietly.";
        }

        if (_currentSettings.LowReminderEnabled && snapshot.ChargePercent > _currentSettings.LowReminderPercent)
        {
            return $"Low-battery reminder is armed for {_currentSettings.LowReminderPercent}%. You still have room before the next alert.";
        }

        if (_currentSettings.LowReminderEnabled)
        {
            return "Low-battery threshold has already been crossed. The reminder will arm again once the battery rises above the target.";
        }

        return "The device is running on battery, but low-battery reminders are disabled right now.";
    }

    private string BuildNextReminderSummary()
    {
        if (_currentSnapshot is null)
        {
            return "Waiting for data";
        }

        if (_currentSnapshot.IsOnExternalPower)
        {
            if (_currentSettings.HighReminderEnabled && _currentSnapshot.ChargePercent < _currentSettings.HighReminderPercent)
            {
                return $"{_currentSettings.HighReminderPercent}% charging";
            }

            if (_currentSettings.FullReminderEnabled && _currentSnapshot.ChargePercent < 100)
            {
                return "100% full charge";
            }

            return "No charging alert armed";
        }

        if (_currentSettings.LowReminderEnabled && _currentSnapshot.ChargePercent > _currentSettings.LowReminderPercent)
        {
            return $"{_currentSettings.LowReminderPercent}% low battery";
        }

        return "No battery alert armed";
    }

    private string BuildDeliverySummary()
    {
        var parts = new List<string>();

        if (ForcePopupCheckBox.IsChecked == true)
        {
            parts.Add("Popup");
        }

        if (TrayNotificationCheckBox.IsChecked == true)
        {
            parts.Add("Tray");
        }

        if (PlaySoundCheckBox.IsChecked == true)
        {
            parts.Add("Sound");
        }

        return parts.Count == 0 ? "Silent monitoring" : string.Join(" + ", parts);
    }

    private bool TryBuildSettings(out BatteryReminderSettings settings)
    {
        settings = new BatteryReminderSettings();

        if (!int.TryParse(HighReminderTextBox.Text, out var highReminderPercent)
            || highReminderPercent < 1
            || highReminderPercent > 99)
        {
            System.Windows.MessageBox.Show(
                "Enter a valid high reminder threshold between 1 and 99.",
                "Battery Charge Reminder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        if (!int.TryParse(LowReminderTextBox.Text, out var lowReminderPercent)
            || lowReminderPercent < 1
            || lowReminderPercent > 99)
        {
            System.Windows.MessageBox.Show(
                "Enter a valid low reminder threshold between 1 and 99.",
                "Battery Charge Reminder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        if (lowReminderPercent >= highReminderPercent)
        {
            System.Windows.MessageBox.Show(
                "The low-battery threshold must be lower than the plugged-in threshold.",
                "Battery Charge Reminder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        if (!int.TryParse(MonitorIntervalTextBox.Text, out var intervalSeconds)
            || intervalSeconds < 10
            || intervalSeconds > 3600)
        {
            System.Windows.MessageBox.Show(
                "Enter a check interval between 10 and 3600 seconds.",
                "Battery Charge Reminder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        settings = new BatteryReminderSettings
        {
            HighReminderEnabled = HighReminderCheckBox.IsChecked == true,
            HighReminderPercent = highReminderPercent,
            LowReminderEnabled = LowReminderCheckBox.IsChecked == true,
            LowReminderPercent = lowReminderPercent,
            FullReminderEnabled = FullReminderCheckBox.IsChecked == true,
            ForcePopup = ForcePopupCheckBox.IsChecked == true,
            PlaySound = PlaySoundCheckBox.IsChecked == true,
            ShowTrayNotification = TrayNotificationCheckBox.IsChecked == true,
            RunOnStartup = RunOnStartupCheckBox.IsChecked == true,
            MonitorIntervalSeconds = intervalSeconds
        }.Sanitize();

        return true;
    }

    private void RefreshNowButton_Click(object sender, RoutedEventArgs e)
    {
        _trayApp?.RefreshNow();
    }

    private void TestReminderButton_Click(object sender, RoutedEventArgs e)
    {
        _trayApp?.ShowTestReminder();
    }

    private void HideToTrayButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_trayApp is null || !TryBuildSettings(out var settings))
        {
            return;
        }

        _trayApp.SaveSettings(settings);
        LoadSettings(_trayApp.CurrentSettings);
        UpdateVisualSummary();
    }

    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        _trayApp?.ClearHistory();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
