using System.Windows.Threading;
using FormsPowerLineStatus = System.Windows.Forms.PowerLineStatus;
using FormsToolTipIcon = System.Windows.Forms.ToolTipIcon;

namespace MandatoryReminder;

public sealed class BatteryMonitorService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private BatteryReminderSettings _settings;
    private BatteryStatusSnapshot? _lastSnapshot;
    private bool _highReminderArmed = true;
    private bool _lowReminderArmed = true;
    private bool _fullReminderArmed = true;

    public BatteryMonitorService(BatteryReminderSettings settings)
    {
        _settings = settings.Clone().Sanitize();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_settings.MonitorIntervalSeconds)
        };

        _timer.Tick += OnTimerTick;
    }

    public event EventHandler<BatteryStatusSnapshot>? SnapshotUpdated;

    public event EventHandler<BatteryLogEntry>? LogEntryCreated;

    public event EventHandler<BatteryReminder>? ReminderTriggered;

    public void Start()
    {
        RefreshNow();
        _timer.Start();
    }

    public void RefreshNow()
    {
        CheckBattery();
    }

    public void UpdateSettings(BatteryReminderSettings settings)
    {
        _settings = settings.Clone().Sanitize();
        _timer.Interval = TimeSpan.FromSeconds(_settings.MonitorIntervalSeconds);
        RecalculateReminderArming();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        CheckBattery();
    }

    private void CheckBattery()
    {
        var snapshot = BatteryStatusSnapshot.Capture(DateTime.Now);
        var previousSnapshot = _lastSnapshot;
        _lastSnapshot = snapshot;

        SnapshotUpdated?.Invoke(this, snapshot);

        if (ShouldLogStatusChange(previousSnapshot, snapshot))
        {
            LogEntryCreated?.Invoke(this, BatteryLogEntry.FromStatus(snapshot, CreateStatusMessage(previousSnapshot, snapshot)));
        }

        var reminder = CreateReminder(snapshot);
        if (reminder is not null)
        {
            ReminderTriggered?.Invoke(this, reminder);
            LogEntryCreated?.Invoke(this, BatteryLogEntry.FromReminder(reminder));
        }
    }

    private bool ShouldLogStatusChange(BatteryStatusSnapshot? previousSnapshot, BatteryStatusSnapshot snapshot)
    {
        return previousSnapshot is null
            || previousSnapshot.ChargePercent != snapshot.ChargePercent
            || previousSnapshot.PowerLineStatus != snapshot.PowerLineStatus;
    }

    private static string CreateStatusMessage(BatteryStatusSnapshot? previousSnapshot, BatteryStatusSnapshot snapshot)
    {
        if (previousSnapshot is null)
        {
            return "Battery monitoring started.";
        }

        if (previousSnapshot.PowerLineStatus != snapshot.PowerLineStatus)
        {
            return snapshot.IsOnExternalPower ? "Power connected." : "Power disconnected.";
        }

        return $"Battery level changed to {snapshot.ChargePercent}%.";
    }

    private BatteryReminder? CreateReminder(BatteryStatusSnapshot snapshot)
    {
        if (_settings.FullReminderEnabled && snapshot.IsOnExternalPower)
        {
            if (snapshot.ChargePercent >= 100 && _fullReminderArmed)
            {
                _fullReminderArmed = false;
                _highReminderArmed = false;

                return new BatteryReminder(
                    "Battery full",
                    "Battery reached 100%. You can unplug the charger now.",
                    FormsToolTipIcon.Info,
                    "Reminder",
                    snapshot);
            }
        }
        else
        {
            _fullReminderArmed = true;
        }

        if (snapshot.ChargePercent < 100 || snapshot.PowerLineStatus != FormsPowerLineStatus.Online)
        {
            _fullReminderArmed = true;
        }

        if (_settings.HighReminderEnabled && snapshot.IsOnExternalPower)
        {
            if (snapshot.ChargePercent >= _settings.HighReminderPercent && _highReminderArmed)
            {
                _highReminderArmed = false;

                return new BatteryReminder(
                    "Charge target reached",
                    $"Battery reached {_settings.HighReminderPercent}%. Consider unplugging soon.",
                    FormsToolTipIcon.Info,
                    "Reminder",
                    snapshot);
            }
        }
        else
        {
            _highReminderArmed = true;
        }

        if (!snapshot.IsOnExternalPower || snapshot.ChargePercent < _settings.HighReminderPercent)
        {
            _highReminderArmed = true;
        }

        if (_settings.LowReminderEnabled && !snapshot.IsOnExternalPower)
        {
            if (snapshot.ChargePercent <= _settings.LowReminderPercent && _lowReminderArmed)
            {
                _lowReminderArmed = false;

                return new BatteryReminder(
                    "Battery running low",
                    $"Battery dropped to {_settings.LowReminderPercent}% or lower. Plug in the charger soon.",
                    FormsToolTipIcon.Warning,
                    "Reminder",
                    snapshot);
            }
        }
        else
        {
            _lowReminderArmed = true;
        }

        if (snapshot.IsOnExternalPower || snapshot.ChargePercent > _settings.LowReminderPercent)
        {
            _lowReminderArmed = true;
        }

        return null;
    }

    private void RecalculateReminderArming()
    {
        if (_lastSnapshot is null)
        {
            _highReminderArmed = true;
            _lowReminderArmed = true;
            _fullReminderArmed = true;
            return;
        }

        _highReminderArmed = !_lastSnapshot.IsOnExternalPower
            || _lastSnapshot.ChargePercent < _settings.HighReminderPercent;

        _lowReminderArmed = _lastSnapshot.IsOnExternalPower
            || _lastSnapshot.ChargePercent > _settings.LowReminderPercent;

        _fullReminderArmed = !_lastSnapshot.IsOnExternalPower
            || _lastSnapshot.ChargePercent < 100;
    }
}
