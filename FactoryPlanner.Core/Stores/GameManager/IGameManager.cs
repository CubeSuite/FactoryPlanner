using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Services;
using System.Linq;

namespace FactoryPlanner.Core.Stores
{
    public interface IGameManager : IObjectCache<int, Game>
    {
        // Public Functions
        OperationResult CreateAndAdd(out Game game);
        Game? GetLatest();

        // Base Class Wrappers
        OperationResult TryAdd(Game game);
        OperationResult TryUpdate(Game game);
        OperationResult TryDelete(Game game);
    }
}