using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.Stores.ObjectCache;
using FactoryPlanner.Core.Stores.ObjectStore;
using FactoryPlanner.Services;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.Stores
{
    public class RecipeManager : ObjectCache<int, Recipe>, IRecipeManager 
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Fields
        private IObjectRepository<int, Recipe> database;

        // Constructors

        public RecipeManager(IServiceProvider serviceProvider) : base(serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            database = new LocalObjectRepository<int, Recipe>(serviceProvider);

            userSettings.SettingChanged += OnSettingChanged;
            
            RefreshCache();
        }

        // Listeners

        private void OnSettingChanged(string setting) {
            if (setting == nameof(userSettings.ActiveGame)) RefreshCache();
        }

        // Public Functions

        public Recipe CreateAndAdd() {
            Recipe recipe = new Recipe(GetNewRecipeID(), userSettings.ActiveGame);
            TryAdd(recipe);
            return recipe;
        }

        public Recipe? GetLatest() {
            return Values.Last();
        }

        // Base Class Wrappers

        public OperationResult TryAdd(Recipe recipe) {
            OperationResult result = database.TryAdd(recipe.ID, recipe);
            if (!result) return result;

            return TryAdd(recipe.ID, recipe);
        }
        
        public OperationResult TryUpdate(Recipe recipe) {
            OperationResult result = database.TryUpdate(recipe.ID, recipe);
            if (!result) return result;

            return TryUpdate(recipe.ID, recipe);
        }

        public OperationResult TryDelete(Recipe recipe) {
            OperationResult result = database.TryDelete(recipe.ID);
            if (!result) return result;

            return TryDelete(recipe.ID);
        }

        public override OperationResult Clear() {
            OperationResult result = database.Clear();
            if (!result) return result;
            
            return base.Clear();
        }

        // Private Functions

        private void RefreshCache() {
            base.Clear();
            foreach (Recipe recipe in database.Query(
                $"SELECT * FROM {database.TableName} " +
                $"WHERE _gameId={userSettings.ActiveGame}")
            ) {
                TryAdd(recipe.ID, recipe);
            }
        }

        private int GetNewRecipeID() {
            return database.Count == 0 ? 0 : database.Keys.Max() + 1;
        }
    }
}
