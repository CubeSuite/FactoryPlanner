using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Services;
using Windows.Foundation;

namespace FactoryPlanner.Core.Stores
{
    public interface IProductionStepManager : IObjectCache<int, ProductionStep>
    {
        // Public Functions
        OperationResult CreateAndAdd(Recipe recipe, Point position, out ProductionStep step);
        
        OperationResult TryAdd(ProductionStep step);
        OperationResult TryDelete(ProductionStep step);
        OperationResult TryUpdate(ProductionStep step);
    }
}