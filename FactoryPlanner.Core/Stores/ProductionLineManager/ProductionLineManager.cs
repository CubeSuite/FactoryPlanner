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
    public class ProductionLineManager : ObjectCache<int, ProductionLine>, IProductionLineManager
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Fields
        private IObjectRepository<int, ProductionLine> database;

        // Constructors

        public ProductionLineManager(IServiceProvider serviceProvider) : base(serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            database = new LocalObjectRepository<int, ProductionLine>(serviceProvider);

            userSettings.SettingChanged += OnSettingChanged;

            RefreshCache();
        }

        // Listeners

        private void OnSettingChanged(string setting) {
            if (setting == nameof(IUserSettings.ActiveGame)) RefreshCache();
        }

        // Public Functions

        public OperationResult CreateAndAdd(out ProductionLine productionLine, int parentID = -1) {
            productionLine = new ProductionLine(GetNewLineID(), parentID, userSettings.ActiveGame);
            return TryAdd(productionLine);
        }

        public ProductionLine GetRootLine() {
            ProductionLine? root = GetAll().FirstOrDefault(line => line.ParentID == -1);
            if (root == null) CreateAndAdd(out root);
            return root;
        }

        // Base Class Wrappers

        public OperationResult TryAdd(ProductionLine line) {
            OperationResult result = database.TryAdd(line.ID, line);
            if (!result) return result;

            return TryAdd(line.ID, line);
        }

        public OperationResult TryUpdate(ProductionLine line) {
            OperationResult result = database.TryUpdate(line.ID, line);
            if (!result) return result;

            return TryUpdate(line.ID, line);
        }

        public OperationResult TryDelete(ProductionLine line) {
            OperationResult result = database.TryDelete(line.ID);
            if (!result) return result;

            return TryDelete(line.ID);
        }

        public override OperationResult Clear() {
            OperationResult result = database.Clear();
            if (!result) return result;

            return base.Clear();
        }

        // Private Functions

        private int GetNewLineID() {
            return database.Count == 0 ? 0 : database.Keys.Max() + 1;
        }

        private void RefreshCache() {
            base.Clear();
            foreach (ProductionLine line in database.Query(
                $"SELECT * FROM {database.TableName} " +
                $"WHERE _gameId={userSettings.ActiveGame}")
            ) {
                TryAdd(line.ID, line);
            }
        }
    }
}
