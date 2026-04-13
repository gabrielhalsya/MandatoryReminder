using FormsToolTipIcon = System.Windows.Forms.ToolTipIcon;

namespace MandatoryReminder;

public sealed record BatteryReminder(
    string Title,
    string Message,
    FormsToolTipIcon Icon,
    string EventType,
    BatteryStatusSnapshot Snapshot);
