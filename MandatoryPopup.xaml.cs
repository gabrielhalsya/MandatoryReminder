using System.Windows;
namespace MandatoryReminder;

public partial class MandatoryPopup : Window
{
    private readonly Action? _openDashboardAction;

    public MandatoryPopup()
    {
        InitializeComponent();
    }

    public MandatoryPopup(BatteryReminder reminder, Action openDashboardAction)
        : this()
    {
        _openDashboardAction = openDashboardAction;
        UpdateReminder(reminder);
    }

    public void UpdateReminder(BatteryReminder reminder)
    {
        var isLowBattery = !reminder.Snapshot.IsOnExternalPower;
        var sidebarColor = isLowBattery
            ? System.Windows.Media.Color.FromRgb(125, 57, 49)
            : System.Windows.Media.Color.FromRgb(23, 61, 69);

        PopupPercentTextBlock.Text = $"{reminder.Snapshot.ChargePercent}%";
        PopupStateTextBlock.Text = reminder.Snapshot.StatusText;
        ReminderTitleTextBlock.Text = reminder.Title;
        ReminderMessageTextBlock.Text = reminder.Message;
        ReminderSnapshotTextBlock.Text =
            $"{reminder.Snapshot.ChargePercent}% | {reminder.Snapshot.StatusText} | {reminder.Snapshot.Timestamp:g}";

        SidebarBorder.Background = new System.Windows.Media.SolidColorBrush(sidebarColor);

        Activate();
    }

    private void OpenDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        _openDashboardAction?.Invoke();
        Close();
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
