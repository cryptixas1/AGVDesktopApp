using System;
using System.IO;
using System.Text.Json;

namespace AGVDesktop.Services
{
    public class AppSettings
    {
        public bool MinimizeToTray { get; set; } = true;
        public bool UseSystemMica { get; set; } = true;
        public bool ForceAcrylic { get; set; } = false;
    }

    public static class SettingsService
    {
        private static readonly string AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IAOSB");
        private static readonly string SettingsPath = Path.Combine(AppDir, "settings.json");
        public static string SettingsFilePath => SettingsPath;

        public static AppSettings Settings { get; private set; } = new AppSettings();

        public static void Load()
        {
            try
            {
                if (!Directory.Exists(AppDir)) Directory.CreateDirectory(AppDir);
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                Settings = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(AppDir)) Directory.CreateDirectory(AppDir);
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // ignore
            }
        }
    }
}
