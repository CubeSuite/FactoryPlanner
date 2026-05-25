using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class ProductionLine : ProductionStep
    {
        // Fields
        private string _name;
        private List<ProductionStep> _steps;
        private List<Connection> _connections;

        // Properties

        public string Name {
            get => _name;
            set => _name = value;
        }

        public List<ProductionStep> Steps => _steps;
        public List<Connection> Connections => _connections;

        // Constructors

        public ProductionLine() {
            _name = "";
            _steps = new List<ProductionStep>();
            _connections = new List<Connection>();
        }

        // Public Functions

        public IEnumerable<Connection> GetConnectionsToPort(ProductionPort port) {
            return Connections.Where(connection => connection.Input == port || connection.Output == port);
        }
    }
}
