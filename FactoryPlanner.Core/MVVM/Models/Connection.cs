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
        private List<ProductionStep> _inputs;
        private List<ProductionStep> _outputs;

        // Properties

        public List<ProductionStep> Inputs => _inputs;
        public List<ProductionStep> Outputs => _outputs;

        // Constructors

        public Connection() {
            _inputs = new List<ProductionStep>();
            _outputs = new List<ProductionStep>();
        }
    }
}
