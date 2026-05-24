using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public class MachineViewModel : ObservableObject
    {
        // Members
        private Machine _machine;

        // Properties
        public Machine Machine => _machine;
        public int ID => _machine.ID;
        public bool UseDefaultIcon => string.IsNullOrEmpty(IconPath) || !File.Exists(IconPath);
        public string PowerCostString => FormatPowerCost();

        public string Name {
            get => _machine.Name;
            set {
                if (_machine.Name == value) return;
                _machine.Name = value;
            }
        }

        public string IconPath {
            get => _machine.IconPath;
            set {
                if (_machine.IconPath == value) return;
                _machine.IconPath = value;
            }
        }

        public double PowerCost {
            get => _machine.PowerCost;
            set {
                if (_machine.PowerCost == value) return;
                _machine.PowerCost = value;
            }
        }

        // Constructors

        public MachineViewModel(Machine machine) {
            _machine = machine;
        }

        // Private Functions

        private string FormatPowerCost() {
            return PowerCost switch {
                >= 1e15 => $"{PowerCost / 1e15:0.##} PW",
                >= 1e12 => $"{PowerCost / 1e12:0.##} TW",
                >= 1e9  => $"{PowerCost / 1e9:0.##} GW",
                >= 1e6  => $"{PowerCost / 1e6:0.##} MW",
                >= 1e3  => $"{PowerCost / 1e3:0.##} kW",
                _ => $"{PowerCost:0.##} W"
            };
        }
    }
}
