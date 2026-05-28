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
    public class MachineManager : ObjectCache<int, Machine>, IMachineManager 
    {
        // Services & Stores
        private readonly IUserSettings userSettings;

        // Fields
        private IObjectRepository<int, Machine> database;

        // Constructors

        public MachineManager(IServiceProvider serviceProvider) : base(serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            database = new LocalObjectRepository<int, Machine>(serviceProvider);

            userSettings.SettingChanged += OnSettingChanged;

            RefreshCache();
        }

        // Listeners

        private void OnSettingChanged(string setting) {
            if (setting == nameof(userSettings.ActiveGame)) RefreshCache();
        }

        // Public Functions

        public OperationResult CreateAndAdd(string name, string icon, double powerCost, out Machine machine) {
            machine = new Machine(GetNewMachineID(), userSettings.ActiveGame, name, icon, powerCost);
            return TryAdd(machine);
        }

        public Machine? GetLatest() {
            return Count == 0 ? null : Values.Last();
        }

        public bool IsNameTaken(string name) {
            return GetAll().Select(item => item.Name).Contains(name);
        }

        // Base Class Wrappers

        public OperationResult TryAdd(Machine machine) {
            OperationResult result = database.TryAdd(machine.ID, machine);
            if (!result) return result;

            return TryAdd(machine.ID, machine);
        }

        public OperationResult TryUpdate(Machine machine) {
            OperationResult result = database.TryUpdate(machine.ID, machine);
            if (!result) return result;

            return TryUpdate(machine.ID, machine);
        }

        public OperationResult TryDelete(Machine machine) {
            OperationResult result = database.TryDelete(machine.ID);
            if (!result) return result;

            return TryDelete(machine.ID);
        }

        public override OperationResult Clear() {
            OperationResult result = database.Clear();
            if (!result) return result;
            
            return base.Clear();
        }

        // Private Functions

        private void RefreshCache() {
            base.Clear();
            foreach (Machine machine in database.Query(
                $"SELECT * FROM {database.TableName} " +
                $"WHERE _gameId={userSettings.ActiveGame}")
            ) {
                TryAdd(machine.ID, machine); // Add to cache, not db
            }
        }

        private int GetNewMachineID() {
            return database.Count == 0 ? 0 : database.Keys.Max() + 1;
        }
    }
}
