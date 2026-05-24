using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class Machine
    {
        // Members
        private int _id;
        private string _name;
        private string _iconPath;
        private double _powerCost;

        // Properties
        public int ID => _id;

        public string Name {
            get => _name;
            set {
                if (_name == value) return;
                _name = value;
            }
        }

        public string IconPath {
            get => _iconPath;
            set {
                if (_iconPath == value) return;
                _iconPath = value;
            }
        }

        public double PowerCost {
            get => _powerCost;
            set {
                if (_powerCost == value) return;
                _powerCost = value;
            }
        }

        // Constructors

        public Machine() {
            _id = -1;
            _name = "";
            _iconPath = "";
            _powerCost = 0;
        }

        public Machine(int id, Machine details) {
            _id = id;
            _name = details.Name;
            _iconPath = details.IconPath;
            _powerCost = details.PowerCost;
        }

        public Machine(string name, string iconPath, double powerCost) {
            _id = -1;
            _name = name;
            _iconPath = iconPath;
            _powerCost = powerCost;
        }
    }
}
