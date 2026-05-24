using FactoryPlanner.Stores.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Stores
{
    public class ProgramData : IProgramData
    {
        // Properties
        public bool IsDebugBuild {
            get {
                #if DEBUG
                    return true;
                #else
                    return false;
                #endif
            }
        }
        public string ProgramName => "Factory Planner";
        public string ProgramNameNoSpaces => ProgramName.Replace(" ", "");
        public Version ProgramVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        public IFilePaths FilePaths { get; }
        public bool EnableBackups { get; } = true;
        public EncryptionLevel EncryptionLevel { get; } = EncryptionLevel.Settings;
        public bool UsesApi { get; } = false;
        public bool UsesRemoteDatabase { get; } = false;

        // Constructors 

        public ProgramData() {
            FilePaths = new FilePaths();
        }
    }
}
