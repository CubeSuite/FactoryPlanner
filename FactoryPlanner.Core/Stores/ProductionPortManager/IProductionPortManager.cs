using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Services;

namespace FactoryPlanner.Core.Stores
{
    public interface IProductionPortManager : IObjectCache<int, ProductionPort>
    {
        // Public Functions
        OperationResult CreateAndAdd(ProductionStep parent, PortType type, int index, out ProductionPort port);
        
        OperationResult TryAdd(ProductionPort port);
        OperationResult TryDelete(ProductionPort port);
        OperationResult TryUpdate(ProductionPort port);
    }
}