using System;
using System.IO;

namespace AGVDesktop.Services
{
    public static class UiLogService
    {
        private static readonly string AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IAOSB");
        private static readonly string LogPath = Path.Combine(AppDir, "agv-desktop.log");
        public static string LogFilePath => LogPath;

        public static event Action<string>? OnLog;

        public static void Init()
        {
            try
            {
                if (!Directory.Exists(AppDir)) Directory.CreateDirectory(AppDir);
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(LogPath, entry + Environment.NewLine);
                OnLog?.Invoke(entry);
            }
            catch { }
        }
    }
}
