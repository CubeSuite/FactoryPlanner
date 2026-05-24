using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.Stores
{
    public interface IMachineManager : IObjectCache<int, Machine>
    {
        // Public Functions
        public OperationResult TryAdd(Machine machine);
        public OperationResult TryUpdate(Machine machine);
        public OperationResult TryDelete(Machine machine);
        public Machine? GetLatest();
        public bool IsNameTaken(string name);
    }
}
