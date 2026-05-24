using FactoryPlanner.Core.Stores.UserSettings;
using FactoryPlanner.Services;
using FactoryPlanner.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Stores.Interfaces
{
    public interface IUserSettings
    {
        // Properties
        bool Loaded { get; }
        bool IsFirstLaunch { get; set; }

        bool LogDebugMessages { get; set; }
        int MaxLogs { get; set; }

        bool DarkMode { get; set; }
        bool RememberLayout { get; set; }
        bool OpenMaximised { get; set; }
        int DefaultWidth { get; set; }
        int DefaultHeight { get; set; }
        IThemeService.Backdrop Backdrop { get; set; }
        string AccentColour { get; set; }

        string BackupsFolder { get; set; }
        int MaxBackups { get; set; }
        bool AutomaticBackups { get; set; }

        int ApiTimeout { get; set; }
        int ApiMaxRetries { get; set; }

        string DatabaseHost { get; set; }
        int DatabasePort { get; set; }
        string DatabaseName { get; set; }
        string DatabaseUsername { get; set; }
        string DatabasePassword { get; set; }
        int DatabaseConnectionTimeout { get; set; }

        bool SearchCaseSensitive { get; set; }
        bool SearchSplitQuery { get; set; }

        FactoryIconSource IconSource { get; set; }
        bool SnapToGrid { get; set; }
        bool RenderGrid { get; set; }
        int GridSize { get; set; }

        // Events
        event Action? SettingsLoaded;
        event Action<string>? SettingChanged;

        // Public Functions
        Task Load();
        void RestoreDefaults();
    }
}
