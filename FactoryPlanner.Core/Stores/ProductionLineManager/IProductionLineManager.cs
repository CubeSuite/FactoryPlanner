using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.Stores.ObjectCache;
using FactoryPlanner.Services;

namespace FactoryPlanner.Core.Stores
{
    public interface IProductionLineManager : IObjectCache<int, ProductionLine>
    {
        // Public Functions
        OperationResult CreateAndAdd(out ProductionLine productionLine, int parentId = -1);
        ProductionLine GetRootLine();

        OperationResult TryAdd(ProductionLine line);
        OperationResult TryUpdate(ProductionLine line);
        OperationResult TryDelete(ProductionLine line);
    }
}