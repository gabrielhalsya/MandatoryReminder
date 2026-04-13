namespace MandatoryReminder;

public sealed class BatteryReminderSettings
{
    public bool HighReminderEnabled { get; set; } = true;

    public int HighReminderPercent { get; set; } = 80;

    public bool LowReminderEnabled { get; set; } = true;

    public int LowReminderPercent { get; set; } = 20;

    public bool FullReminderEnabled { get; set; } = true;

    public bool ForcePopup { get; set; } = true;

    public bool PlaySound { get; set; } = true;

    public bool ShowTrayNotification { get; set; } = true;

    public bool RunOnStartup { get; set; }

    public int MonitorIntervalSeconds { get; set; } = 30;

    public int MaxHistoryEntries { get; set; } = 300;

    public BatteryReminderSettings Clone()
    {
        return new BatteryReminderSettings
        {
            HighReminderEnabled = HighReminderEnabled,
            HighReminderPercent = HighReminderPercent,
            LowReminderEnabled = LowReminderEnabled,
            LowReminderPercent = LowReminderPercent,
            FullReminderEnabled = FullReminderEnabled,
            ForcePopup = ForcePopup,
            PlaySound = PlaySound,
            ShowTrayNotification = ShowTrayNotification,
            RunOnStartup = RunOnStartup,
            MonitorIntervalSeconds = MonitorIntervalSeconds,
            MaxHistoryEntries = MaxHistoryEntries
        };
    }

    public BatteryReminderSettings Sanitize()
    {
        HighReminderPercent = Math.Clamp(HighReminderPercent, 1, 99);
        LowReminderPercent = Math.Clamp(LowReminderPercent, 1, 99);
        MonitorIntervalSeconds = Math.Clamp(MonitorIntervalSeconds, 10, 3600);
        MaxHistoryEntries = Math.Clamp(MaxHistoryEntries, 50, 1000);

        if (LowReminderPercent >= HighReminderPercent)
        {
            HighReminderPercent = Math.Clamp(LowReminderPercent + 1, 2, 99);
        }

        return this;
    }
}
