using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Services;

namespace FactoryPlanner.Core.Stores
{
    public interface IConnectionManager : IObjectCache<int, Connection>
    {
        // Public Functions
        OperationResult CreateAndAdd(ProductionPort input, ProductionPort output, out Connection connection);
        OperationResult TryAdd(Connection connection);
        OperationResult TryDelete(Connection connection);
        OperationResult TryUpdate(Connection connection);
        Connection? GetLatest();
    }
}