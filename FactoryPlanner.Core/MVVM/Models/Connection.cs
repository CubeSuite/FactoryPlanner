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
        //private double _quantity;

        // Properties
        public ProductionPort Input => _input;
        public ProductionPort Output => _output;
        
        //public double Quantity {
        //    get => _quantity;
        //    set => _quantity = value;
        //}

        // Constructors

        public Connection(ProductionPort input, ProductionPort output/*, double quantity*/) {
            _input = input;
            _output = output;
            //_quantity = quantity;
        }
    }
}
