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
    public class ItemManager : LocalObjectRepository<int, Item>, IItemManager
    {
        // Constructors
        public ItemManager(IServiceProvider serviceProvider) : base(serviceProvider) { }

        // Public Functions

        public OperationResult CreateAndAdd(string name, string iconPath) {
            Item item = new Item(GetNewID(), name, iconPath);
            return TryAdd(item.ID, item);
        }

        public OperationResult TryUpdate(Item item) {
            return TryUpdate(item.ID, item);
        }

        public OperationResult TryDelete(Item item) {
            return TryDelete(item.ID);
        }

        public Item? GetLatest() {
            return Query($"SELECT * FROM {tableName} ORDER BY _id DESC LIMIT 1").FirstOrDefault();
        }

        public bool IsNameTaken(string name) {
            return GetAll().Select(item => item.Name).Contains(name);
        }

        // Private Functions

        private int GetNewID() {
            Item? latest = GetLatest();
            if (latest == null) return 0;

            return latest.ID + 1;
        }
    }
}
