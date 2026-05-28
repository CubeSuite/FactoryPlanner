using System.Collections.Generic;

namespace FactoryPlanner.Stores.Interfaces
{
    public interface IFilePaths
    {
        // Properties
        string RootFolder { get; }
        string DataFolder { get; }
        string LogsFolder { get; }
        string CrashReportsFolder { get; }
        string ProductionLinesFolder { get; }

        List<string> Folders { get; }

        string TempMetadataFile { get; }
        string SettingsFile { get; }
        string KeyFile { get; }
        string Database { get; }
    }
}