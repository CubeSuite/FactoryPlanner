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
using Windows.Foundation;

namespace FactoryPlanner.Core.Stores
{
    public class ProductionStepManager : ObjectCache<int, ProductionStep>, IProductionStepManager
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Fields
        private IObjectRepository<int, ProductionStep> database;

        // Constructors

        public ProductionStepManager(IServiceProvider serviceProvider) : base(serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            database = new LocalObjectRepository<int, ProductionStep>(serviceProvider);

            userSettings.SettingChanged += OnSettingChanged;

            RefreshCache();
        }

        // Listeners

        private void OnSettingChanged(string setting) {
            if (setting == nameof(IUserSettings.ActiveGame)) RefreshCache();
        }

        // Public Functions

        public OperationResult CreateAndAdd(Recipe recipe, Point position, out ProductionStep step) {
            step = new ProductionStep(GetNewStepID(), userSettings.ActiveGame, recipe, position);
            return TryAdd(step);
        }

        // Base Class Wrappers

        public OperationResult TryAdd(ProductionStep step) {
            OperationResult result = database.TryAdd(step.ID, step);
            if (!result) return result;

            return TryAdd(step.ID, step);
        }

        public OperationResult TryUpdate(ProductionStep step) {
            OperationResult result = database.TryUpdate(step.ID, step);
            if (!result) return result;

            return TryUpdate(step.ID, step);
        }

        public OperationResult TryDelete(ProductionStep step) {
            OperationResult result = database.TryDelete(step.ID);
            if (!result) return result;

            return TryDelete(step.ID);
        }

        public override OperationResult Clear() {
            OperationResult result = database.Clear();
            if (!result) return result;

            return base.Clear();
        }

        // Private Functions

        private int GetNewStepID() {
            return database.Count == 0 ? 0 : database.Keys.Max() + 1;
        }

        private void RefreshCache() {
            base.Clear();
            foreach (ProductionStep step in database.Query(
                $"SELECT * FROM {database.TableName} " +
                $"WHERE _gameId={userSettings.ActiveGame}")
            ) {
                TryAdd(step.ID, step);
            }
        }
    }
}
