using FactoryPlanner.Core.Stores;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class Recipe
    {
        // Fields
        private int _id;
        private string _name;
        private int _machineId;
        private double _craftTime;
        private Dictionary<int, int> _inputs;
        private Dictionary<int, int> _outputs;

        // Properties
        public int ID => _id;
        public Dictionary<int, int> Inputs => _inputs;
        public Dictionary<int, int> Outputs => _outputs;

        public string Name {
            get => _name;
            set {
                if (_name == value) return;
                _name = value;
            }
        }

        public int MachineId {
            get => _machineId;
            set {
                if (_machineId == value) return;
                _machineId = value;
            }
        }

        public double CraftTime {
            get => _craftTime;
            set {
                if (_craftTime == value) return;
                _craftTime = value;
            }
        }

        // Constructors
        
        public Recipe() {
            _id = -1;
            _name = "";
            _machineId = -1;
            _craftTime = 0;
            _inputs = new Dictionary<int, int>();
            _outputs = new Dictionary<int, int>();
        }
        
        public Recipe(int id) {
            _id = id;
            _name = "";
            _machineId = -1;
            _craftTime = 0;
            _inputs = new Dictionary<int, int>();
            _outputs = new Dictionary<int, int>();
        }

        public Recipe(string name, Dictionary<int, int> inputs, Dictionary<int, int> outputs) {
            _name = name;
            _machineId = -1;
            _craftTime = 0;
            _inputs = inputs;
            _outputs = outputs;
        }
    }
}
