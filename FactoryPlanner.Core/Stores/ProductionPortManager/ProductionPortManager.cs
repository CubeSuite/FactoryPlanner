using FactoryPlanner.Core.MVVM.Models;
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
    public class ProductionPortManager : ObjectCache<int, ProductionPort>, IProductionPortManager
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Fields
        private LocalObjectRepository<int, ProductionPort> database;

        // Constructors

        public ProductionPortManager(IServiceProvider serviceProvider) : base(serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            database = new LocalObjectRepository<int, ProductionPort>(serviceProvider);

            userSettings.SettingChanged += OnSettingChanged;

            RefreshCache();
        }

        // Listeners

        private void OnSettingChanged(string setting) {
            if (setting == nameof(IUserSettings.ActiveGame)) RefreshCache();
        }

        // Public Functions

        public OperationResult CreateAndAdd(ProductionStep parent, PortType type, int index, out ProductionPort port) {
            port = new ProductionPort(GetNewPortID(), userSettings.ActiveGame, parent, type, index);
            return TryAdd(port);
        }

        // Base Class Wrappers

        public OperationResult TryAdd(ProductionPort port) {
            OperationResult result = database.TryAdd(port.ID, port);
            if (!result) return result;

            return TryAdd(port.ID, port);
        }

        public OperationResult TryUpdate(ProductionPort port) {
            OperationResult result = database.TryUpdate(port.ID, port);
            if (!result) return result;

            return TryUpdate(port.ID, port);
        }

        public OperationResult TryDelete(ProductionPort port) {
            OperationResult result = database.TryDelete(port.ID);
            if (!result) return result;

            return TryDelete(port.ID);
        }

        public override OperationResult Clear() {
            OperationResult result = database.Clear();
            if (!result) return result;

            return base.Clear();
        }

        // Private Functions

        private int GetNewPortID() {
            return database.Count == 0 ? 0 : database.Keys.Max() + 1;
        }

        private void RefreshCache() {
            base.Clear();
            foreach (ProductionPort port in database.Query(
                $"SELECT * FROM {database.TableName} " +
                $"WHERE _gameId={userSettings.ActiveGame}")
            ) {
                TryAdd(port.ID, port);
            }
        }
    }
}
