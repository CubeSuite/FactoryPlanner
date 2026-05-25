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
    public class ItemManager : ObjectCache<int, Item>, IItemManager 
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Fields
        private IObjectRepository<int, Item> database;

        // Constructors

        public ItemManager(IServiceProvider serviceProvider) : base(serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            database = new LocalObjectRepository<int, Item>(serviceProvider);

            userSettings.SettingChanged += OnSettingChanged;

            RefreshCache();
        }

        // Listeners

        private void OnSettingChanged(string setting) {
            if (setting == nameof(userSettings.ActiveGame)) RefreshCache();
        }

        // Public Functions

        public OperationResult CreateAndAdd(string name, string iconPath) {
            Item item = new Item(GetNewItemID(), userSettings.ActiveGame, name, iconPath);

            OperationResult result = TryAdd(item);
            if (!result) return result;

            result = TryGet(item.ID, out item);
            if (!result) return result;

            return new OperationResult(true, null, false);
        }

        public Item? GetLatest() {
            return Count == 0 ? null : Values.Last();
        }

        public bool IsNameTaken(string name) {
            return GetAll().Select(item => item.Name).Contains(name);
        }

        // Base Class Wrappers

        public OperationResult TryAdd(Item item) {
            OperationResult result = database.TryAdd(item.ID, item);
            if (!result) return result;

            return TryAdd(item.ID, item);
        }

        public OperationResult TryUpdate(Item item) {
            OperationResult result = database.TryUpdate(item.ID, item);
            if (!result) return result;

            return TryUpdate(item.ID, item);
        }

        public OperationResult TryDelete(Item item) {
            OperationResult result = database.TryDelete(item.ID);
            if (!result) return result;

            return TryDelete(item.ID);
        }

        public override OperationResult Clear() {
            OperationResult result = database.Clear();
            if (!result) return result;

            return base.Clear();
        }

        // Private Functions

        private void RefreshCache() {
            base.Clear();
            foreach (Item item in database.Query(
                $"SELECT * FROM {database.TableName} " +
                $"WHERE _gameId={userSettings.ActiveGame}")
            ) {
                TryAdd(item.ID, item); // Add to cache, not db
            }
        }

        private int GetNewItemID() {
            return database.Count == 0 ? 0 : database.Keys.Max() + 1;
        }
    }
}
