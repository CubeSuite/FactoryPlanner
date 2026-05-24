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
    public class MachineManager : LocalObjectRepository<int, Machine>, IMachineManager
    {
        // Constructors
        public MachineManager(IServiceProvider serviceProvider) : base(serviceProvider) { }

        // Public Functions

        public OperationResult TryAdd(Machine machine) {
            int id = GetNewID();
            return TryAdd(id, new Machine(id, machine));
        }

        public OperationResult TryUpdate(Machine machine) {
            return TryUpdate(machine.ID, machine);
        }

        public OperationResult TryDelete(Machine machine) {
            return TryDelete(machine.ID);
        }

        public Machine? GetLatest() {
            return Query($"SELECT * FROM {tableName} ORDER BY _id DESC LIMIT 1").FirstOrDefault();
        }

        public bool IsNameTaken(string name) {
            return GetAll().Select(item => item.Name).Contains(name);
        }

        // Private Functions

        private int GetNewID() {
            Machine? latest = GetLatest();
            if (latest == null) return 0;
            else return latest.ID + 1;
        }
    }
}
