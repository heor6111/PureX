namespace PureX.Core.Services;

public class UserSettings
{
    public int MaxConcurrency { get; set; } = 3;
    public string? LastOutputDirectory { get; set; }
    public string? LastSelectedFormat { get; set; }
}

public class UserSettingsService
{
    private static UserSettingsService? _instance;
    private static readonly object _lock = new();
    private readonly string _settingsPath;
    private UserSettings _settings;

    public static UserSettingsService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new UserSettingsService();
                }
            }
            return _instance;
        }
    }

    private UserSettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileConverter"
        );
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        
        _settingsPath = Path.Combine(appDataPath, "settings.json");
        _settings = LoadSettings();
    }

    public UserSettings Settings => _settings;

    public int MaxConcurrency
    {
        get => _settings.MaxConcurrency;
        set
        {
            _settings.MaxConcurrency = Math.Clamp(value, 1, 5);
            SaveSettings();
        }
    }

    public string? LastOutputDirectory
    {
        get => _settings.LastOutputDirectory;
        set
        {
            _settings.LastOutputDirectory = value;
            SaveSettings();
        }
    }

    private UserSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<UserSettings>(json) 
                    ?? new UserSettings();
            }
        }
        catch
        {
        }
        
        return new UserSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_settings, new 
                System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
        }
    }

    public void ResetToDefaults()
    {
        _settings = new UserSettings();
        SaveSettings();
    }
}
