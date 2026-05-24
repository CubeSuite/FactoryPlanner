using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.Stores.ObjectStore;
using FactoryPlanner.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.Stores
{
    public class RecipeManager : LocalObjectRepository<int, Recipe>, IRecipeManager
    {
        // Constructors
        public RecipeManager(IServiceProvider serviceProvider) : base(serviceProvider) { }

        // Public Functions

        public Recipe CreateAndAdd() {
            Recipe recipe = new Recipe(GetNewID());
            TryAdd(recipe.ID, recipe);
            return recipe;
        }

        public OperationResult TryUpdate(Recipe recipe) {
            return TryUpdate(recipe.ID, recipe);
        }

        public Recipe? GetLatest() {
            return Query($"SELECT * FROM {tableName} ORDER BY _id DESC LIMIT 1").FirstOrDefault();
        }

        public OperationResult TryDelete(Recipe recipe) {
            return TryDelete(recipe.ID);
        }

        // Private Functions

        private int GetNewID() {
            Recipe? latest = GetLatest();
            if (latest == null) return 0;
            else return latest.ID + 1;
        }
    }
}
