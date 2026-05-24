using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public class ConnectionViewModel : ObservableObject
    {
        // Fields
        private Connection _connection;
        private List<ProductionStepViewModel> _inputs;
        private List<ProductionStepViewModel> _outputs;

        // Properties
        public Connection Connection => _connection;
        public List<ProductionStepViewModel> Inputs => _inputs;
        public List<ProductionStepViewModel> Outputs => _outputs;

        // Constructors

        public ConnectionViewModel(Connection connection) {
            _connection = connection;
            _inputs = new List<ProductionStepViewModel>();
            _outputs = new List<ProductionStepViewModel>();
        }
    }
}
