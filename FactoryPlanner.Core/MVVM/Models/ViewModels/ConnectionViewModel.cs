using CommunityToolkit.Mvvm.ComponentModel;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public partial class ConnectionViewModel : ObservableObject
    {
        // Fields
        private Connection _connection;

        // Properties

        [ObservableProperty]
        public partial ConnectedStepViewModel Input { get; set; }
        
        [ObservableProperty]
        public partial ConnectedStepViewModel Output { get; set; }

        // Constructors

        public ConnectionViewModel(Connection connection) {
            _connection = connection;
            Input = new ConnectedStepViewModel(connection.Input);
            Output = new ConnectedStepViewModel(connection.Output);
        }
    }

    public class ConnectedStepViewModel 
    {
        // Fields
        private ProductionPort _connectedStep;

        // Properties
        public ProductionStep Step => _connectedStep.Step;
        public PortType PortType => _connectedStep.PortType;
        public int PortIndex => _connectedStep.PortIndex;

        // Constructors

        public ConnectedStepViewModel(ProductionPort connectedStep) {
            _connectedStep = connectedStep;
        }
    }
}
