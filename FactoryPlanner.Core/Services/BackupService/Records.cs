using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Services
{
    public record BackupInfo(string Path, DateTime Created, Version CreatedWith, long Size);
}
