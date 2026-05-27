using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class ProductionPort
    {
        // Fields
        private ProductionStep _parent;
        private PortType _type;
        private int _index;

        // Properties
        public ProductionStep Parent => _parent;
        public PortType Type => _type;
        public int Index => _index;

        // Constructors

        public ProductionPort(ProductionStep parent, PortType type, int index) {
            _parent = parent;
            _type = type;
            _index = index;
        }

        public ProductionPort(ProductionPort other) {
            _parent = other.Parent;
            _type = other.Type;
            _index = other.Index;
        }
    }

    public enum PortType
    {
        Input,
        Output
    }
}
