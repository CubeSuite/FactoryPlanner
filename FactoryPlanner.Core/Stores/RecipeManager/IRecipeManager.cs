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
    public interface IRecipeManager : IObjectCache<int, Recipe>
    {
        // Public Functions
        Recipe CreateAndAdd();
        Recipe? GetLatest();
        OperationResult TryAdd(Recipe recipe);
        OperationResult TryUpdate(Recipe recipe);
        OperationResult TryDelete(Recipe recipe);
    }
}
