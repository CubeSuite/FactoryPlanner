using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.Stores;
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
    public class ConnectionManager : ObjectCache<int, Connection>, IConnectionManager
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Fields
        private LocalObjectRepository<int, Connection> database;

        // Constructors

        public ConnectionManager(IServiceProvider serviceProvider) : base(serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            database = new LocalObjectRepository<int, Connection>(serviceProvider);

            userSettings.SettingChanged += OnSettingChanged;

            RefreshCache();
        }

        // Listeners

        private void OnSettingChanged(string setting) {
            if (setting == nameof(IUserSettings.ActiveGame)) RefreshCache();
        }

        // Public Functions

        public OperationResult CreateAndAdd(ProductionPort input, ProductionPort output, out Connection connection) {
            connection = new Connection(GetNewConnectionID(), userSettings.ActiveGame, input, output);
            return TryAdd(connection);
        }

        public Connection? GetLatest() {
            return Count == 0 ? null : Values.Last();
        }

        // Base Class Wrappers

        public OperationResult TryAdd(Connection connection) {
            OperationResult result = database.TryAdd(connection.ID, connection);
            if (!result) return result;

            return TryAdd(connection.ID, connection);
        }

        public OperationResult TryUpdate(Connection connection) {
            OperationResult result = database.TryUpdate(connection.ID, connection);
            if (!result) return result;

            return TryUpdate(connection.ID, connection);
        }

        public OperationResult TryDelete(Connection connection) {
            OperationResult result = database.TryDelete(connection.ID);
            if (!result) return result;

            return TryDelete(connection.ID);
        }

        public override OperationResult Clear() {
            OperationResult result = database.Clear();
            if (!result) return result;

            return base.Clear();
        }

        // Private Functions

        private int GetNewConnectionID() {
            return database.Count == 0 ? 0 : database.Keys.Max() + 1;
        }

        private void RefreshCache() {
            base.Clear();
            foreach (Connection connection in database.Query(
                $"SELECT * FROM {database.TableName} " +
                $"WHERE _gameId={userSettings.ActiveGame}")
            ) {
                TryAdd(connection.ID, connection);
            }
        }
    }
}
