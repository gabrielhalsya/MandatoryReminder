using FormsPowerLineStatus = System.Windows.Forms.PowerLineStatus;
using FormsSystemInformation = System.Windows.Forms.SystemInformation;

namespace MandatoryReminder;

public sealed class BatteryStatusSnapshot
{
    public DateTime Timestamp { get; init; }

    public int ChargePercent { get; init; }

    public FormsPowerLineStatus PowerLineStatus { get; init; }

    public bool IsOnExternalPower => PowerLineStatus == FormsPowerLineStatus.Online;

    public string StatusText =>
        PowerLineStatus switch
        {
            FormsPowerLineStatus.Online when ChargePercent >= 100 => "Fully charged",
            FormsPowerLineStatus.Online => "Plugged in",
            FormsPowerLineStatus.Offline => "Running on battery",
            _ => "Power status unavailable"
        };

    public static BatteryStatusSnapshot Capture(DateTime timestamp)
    {
        var powerStatus = FormsSystemInformation.PowerStatus;
        var rawPercent = powerStatus.BatteryLifePercent;
        var chargePercent = rawPercent < 0
            ? 0
            : Math.Clamp((int)Math.Round(rawPercent * 100f), 0, 100);

        return new BatteryStatusSnapshot
        {
            Timestamp = timestamp,
            ChargePercent = chargePercent,
            PowerLineStatus = powerStatus.PowerLineStatus
        };
    }
}
