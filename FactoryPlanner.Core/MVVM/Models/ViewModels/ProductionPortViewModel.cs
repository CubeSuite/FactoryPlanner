using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.Core.Stores;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Media.MediaProperties;
using Windows.Networking.Connectivity;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public class ProductionPortViewModel : ObservableObject 
    {
        // Fields
        private ProductionPort _port;
        private ProductionStepViewModel _parent;
        private List<ConnectionViewModel> _connections;
        private double _quantity;

        // Properties
        public ProductionPort Port => _port;
        public int ID => _port.ID;
        public ProductionStepViewModel Parent => _parent;
        public PortType Type => _port.Type;
        public int Index => _port.Index;
        public List<ConnectionViewModel> Connections => _connections;

        public Item Item => Type switch {
            PortType.Input => Parent.Recipe.InputEntries[Index].Item,
            PortType.Output => Parent.Recipe.OutputEntries[Index].Item,
            _ => new Item() { Name = "Unknown Item" }
        };

        public double Quantity {
            get => _quantity;
            set => _quantity = value;
        }

        // Constructors

        public ProductionPortViewModel(ProductionPort port, ProductionStepViewModel parent) {
            _port = port;
            _parent = parent;
            _connections = new List<ConnectionViewModel>();
        }

        public ProductionPortViewModel(ProductionPortViewModel port) {
            _port = port._port;
            _parent = port.Parent;
            _connections = new List<ConnectionViewModel>();
        }

        // Public Functions

        public void UpdateConnections(ProductionStepViewModel caller) {
            Quantity = Type switch {
                PortType.Input => caller.NumMachines * caller.Recipe.InputEntries[Index].Rate,
                PortType.Output => caller.NumMachines * caller.Recipe.OutputEntries[Index].Rate,
                _ => 0
            };

            foreach (ConnectionViewModel connection in Connections) {
                connection.UpdateConnections(this);
            }
        }

        public void UpdateConnections(ConnectionViewModel connection) {
            if (Parent.CalculateUpdates) {
                Quantity = Connections.Sum(connection => connection.Quantity);
            }
            else {
                Quantity = Type switch {
                    PortType.Input => Parent.NumMachines * Parent.Recipe.InputEntries[Index].Rate / Connections.Count,
                    PortType.Output => Parent.NumMachines * Parent.Recipe.OutputEntries[Index].Rate / Connections.Count,
                    _ => 0
                };
            }

            Parent.UpdateConnections(this);
        }

        public bool AreNeedsMet() {
            return Connections.Sum(connection => connection.Quantity) >= Parent.NumMachines * Parent.Recipe.InputEntries[Index].Rate;
        }

        public bool AreNeedsMetByPrioritySteps() {
            return Connections
                  .Where(connection => !connection.Input.Parent.CalculateUpdates)
                  .Sum(connection => connection.Quantity) >= Parent.NumMachines * Parent.Recipe.InputEntries[Index].Rate;
        }

        public void PushResources() {
            double available = Parent.NumMachines * Parent.Recipe.OutputEntries[Index].Rate;

            IEnumerable<ConnectionViewModel> priority = Connections.Where(connection => !connection.Output.Parent.CalculateUpdates);
            foreach(ConnectionViewModel connection in priority) {
                connection.Quantity = Math.Min(connection.Output.Quantity, available);
                available -= connection.Quantity;
                connection.UpdateConnections(this);
            }

            IEnumerable<ConnectionViewModel> sharers = Connections.Where(connection => connection.Output.Parent.CalculateUpdates);
            double each = available / sharers.Count();
            foreach(ConnectionViewModel connection in sharers) {
                connection.Quantity = each;
                connection.UpdateConnections(this);
            }
        }

        public void PullResources() {
            double needed = Parent.NumMachines * Parent.Recipe.InputEntries[Index].Rate;

            IEnumerable<ConnectionViewModel> priority = Connections.Where(connection => !connection.Input.Parent.CalculateUpdates);
            foreach(ConnectionViewModel connection in priority) {
                connection.Quantity = Math.Min(connection.Input.Quantity, needed);
                needed -= connection.Quantity;
                connection.UpdateConnections(this);
            }

            IEnumerable<ConnectionViewModel> sharers = Connections.Where(connection => connection.Input.Parent.CalculateUpdates);
            double each = needed / sharers.Count();
            foreach(ConnectionViewModel connection in sharers) {
                connection.Quantity = each;
                connection.UpdateConnections(this);
            }
        }
    }
}
