using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.Stores.ObjectStore;
using FactoryPlanner.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace FactoryPlanner.Core.Stores
{
    public class GameManager : ObjectCache<int, Game>, IGameManager
    {
        // Fields
        private LocalObjectRepository<int, Game> database;

        // Constructors

        public GameManager(IServiceProvider serviceProvider) : base(serviceProvider) {
            database = new LocalObjectRepository<int, Game>(serviceProvider);
            foreach (Game game in database.GetAll()) {
                TryAdd(game.ID, game); // Add to cache, not db
            }
        }

        // Public Functions

        public OperationResult CreateAndAdd(out Game game) {
            game = new Game(GetNewGameID());

            OperationResult result = TryAdd(game);
            if (!result) return result;

            result = TryGet(game.ID, out game);
            if (!result) return result;

            return new OperationResult(true, null, false);
        }

        public Game? GetLatest() {
            return Count == 0 ? null : Values.Last();
        }

        // Base Class Wrappers

        public OperationResult TryAdd(Game game) {
            OperationResult result = database.TryAdd(game.ID, game);
            if (!result) return result;

            return TryAdd(game.ID, game);
        }

        public OperationResult TryUpdate(Game game) {
            OperationResult result = database.TryUpdate(game.ID, game);
            if (!result) return result;

            return TryUpdate(game.ID, game);
        }

        public OperationResult TryDelete(Game game) {
            OperationResult result = database.TryDelete(game.ID);
            if (!result) return result;

            return TryDelete(game.ID);
        }

        public override OperationResult Clear() {
            OperationResult result = database.Clear();
            if (!result) return result;

            return base.Clear();
        }

        // Private Functions

        private int GetNewGameID() {
            return Count == 0 ? 0 : Keys.Max() + 1;
        }
    }
}
