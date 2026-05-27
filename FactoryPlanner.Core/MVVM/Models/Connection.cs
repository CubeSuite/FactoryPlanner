using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class Connection
    {
        // Fields
        private ProductionPort _input;
        private ProductionPort _output;

        // Properties
        public ProductionPort Input => _input;
        public ProductionPort Output => _output;
        
        // Constructors

        public Connection(ProductionPort input, ProductionPort output) {
            _input = input;
            _output = output;
        }
    }
}
