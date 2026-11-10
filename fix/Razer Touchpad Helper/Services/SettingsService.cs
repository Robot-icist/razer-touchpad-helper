using System;
using System.IO;
using System.Text.Json;
using RazerTouchpadHelper;

namespace RazerTouchpadHelper.Services
{
    public sealed class SettingsService
    {
        private readonly string _settingsPath;
        private readonly Action<AppSettings> _onSettingsChanged;

        public AppSettings Settings { get; private set; }

        public SettingsService(string settingsPath, Action<AppSettings> onSettingsChanged = null)
        {
            _settingsPath = settingsPath;
            _onSettingsChanged = onSettingsChanged;
            Settings = Load();
        }

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    ClampSettings(settings);
                    return settings;
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }

        public void Update(Action<AppSettings> modifier)
        {
            modifier(Settings);
            ClampSettings(Settings);
            Save();
            _onSettingsChanged?.Invoke(Settings);
        }

        private static void ClampSettings(AppSettings s)
        {
            s.MaxMove = Math.Clamp(s.MaxMove, 20, 500);
            s.SwipeThreshold = Math.Clamp(s.SwipeThreshold, 100, 1200);
            s.DragHoldMs = Math.Clamp(s.DragHoldMs, 100, 2000);
        }
    }
}