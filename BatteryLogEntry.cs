namespace MandatoryReminder;

public sealed class BatteryLogEntry
{
    public DateTime Timestamp { get; set; }

    public int ChargePercent { get; set; }

    public string PowerState { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public static BatteryLogEntry FromReminder(BatteryReminder reminder)
    {
        return new BatteryLogEntry
        {
            Timestamp = reminder.Snapshot.Timestamp,
            ChargePercent = reminder.Snapshot.ChargePercent,
            PowerState = reminder.Snapshot.StatusText,
            EventType = reminder.EventType,
            Message = reminder.Message
        };
    }

    public static BatteryLogEntry FromStatus(BatteryStatusSnapshot snapshot, string message)
    {
        return new BatteryLogEntry
        {
            Timestamp = snapshot.Timestamp,
            ChargePercent = snapshot.ChargePercent,
            PowerState = snapshot.StatusText,
            EventType = "Status",
            Message = message
        };
    }
}
