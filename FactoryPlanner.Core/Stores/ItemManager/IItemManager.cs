using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.Stores.ObjectCache;
using FactoryPlanner.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.Stores
{
    public interface IItemManager : IObjectCache<int, Item>
    {
        // Public Functions
        public OperationResult CreateAndAdd(string name, string iconPath);
        public OperationResult TryUpdate(Item item);
        public OperationResult TryDelete(Item item);
        public Item? GetLatest();
        public bool IsNameTaken(string name);
    }
}
