using System.Text.Json;

namespace MandatoryReminder;

public sealed class AppDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;
    private readonly string _historyPath;

    public AppDataStore()
    {
        var appDirectory = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MandatoryReminder");

        System.IO.Directory.CreateDirectory(appDirectory);

        _settingsPath = System.IO.Path.Combine(appDirectory, "settings.json");
        _historyPath = System.IO.Path.Combine(appDirectory, "history.json");
    }

    public BatteryReminderSettings LoadSettings()
    {
        try
        {
            if (System.IO.File.Exists(_settingsPath))
            {
                var json = System.IO.File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<BatteryReminderSettings>(json);
                if (settings is not null)
                {
                    return settings.Sanitize();
                }
            }
        }
        catch
        {
        }

        return new BatteryReminderSettings().Sanitize();
    }

    public void SaveSettings(BatteryReminderSettings settings)
    {
        var json = JsonSerializer.Serialize(settings.Clone().Sanitize(), JsonOptions);
        System.IO.File.WriteAllText(_settingsPath, json);
    }

    public List<BatteryLogEntry> LoadHistory()
    {
        try
        {
            if (System.IO.File.Exists(_historyPath))
            {
                var json = System.IO.File.ReadAllText(_historyPath);
                var history = JsonSerializer.Deserialize<List<BatteryLogEntry>>(json);
                if (history is not null)
                {
                    return history.OrderByDescending(entry => entry.Timestamp).ToList();
                }
            }
        }
        catch
        {
        }

        return new List<BatteryLogEntry>();
    }

    public void SaveHistory(IEnumerable<BatteryLogEntry> history)
    {
        var orderedHistory = history.OrderByDescending(entry => entry.Timestamp).ToList();
        var json = JsonSerializer.Serialize(orderedHistory, JsonOptions);
        System.IO.File.WriteAllText(_historyPath, json);
    }
}
