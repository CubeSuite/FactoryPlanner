using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.Core.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.OnnxRuntime;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public partial class ProductionPortViewModel : ObservableObject 
    {
        // Services & Stores
        private readonly IProductionPortManager portManager;

        // Fields
        private ProductionPort _port;
        private ProductionStepViewModel _parent;
        private List<ConnectionViewModel> _connections;
        private int _visualIndex;

        // Properties
        public ProductionPort Port => _port;
        public int ID => _port.ID;
        public ProductionStepViewModel Parent => _parent;
        public PortType Type => _port.Type;
        public int Index => _port.Index;
        public List<ConnectionViewModel> Connections => _connections;

        [ObservableProperty]
        public partial double Quantity { get; set; }

        public double Max => Type switch {
            PortType.Input => Parent.Recipe.InputEntries[Index].Rate * Parent.NumMachines,
            PortType.Output => Parent.Recipe.OutputEntries[Index].Rate * Parent.NumMachines,
            _ => 0
        };

        public Item Item => Type switch {
            PortType.Input => Parent.Recipe.InputEntries[Index].Item,
            PortType.Output => Parent.Recipe.OutputEntries[Index].Item,
            _ => new Item() { Name = "Unknown Item" }
        };

        public bool IsExposed {
            get => _port.IsExposed;
            set {
                if (_port.IsExposed == value) return;
                _port.IsExposed = value;
                SaveChanges();
                OnPropertyChanged();
            }
        }

        public int VisualIndex {
            get => _visualIndex;
            set => _visualIndex = value;
        }

        public Visibility WarningTintVisibility => Type switch {
            PortType.Input => Connections.Sum(connection => connection.Quantity) >= Max ? Visibility.Collapsed : Visibility.Visible,
            PortType.Output => Connections.Sum(connection => connection.Quantity) == Max ? Visibility.Collapsed : Visibility.Visible,
            _ => Visibility.Collapsed
        };

        // Constructors

        public ProductionPortViewModel(ProductionPortViewModel port, IServiceProvider serviceProvider) {
            portManager = serviceProvider.GetRequiredService<IProductionPortManager>();
            _port = port._port;
            _parent = port.Parent;
            _connections = new List<ConnectionViewModel>();
        }

        public ProductionPortViewModel(ProductionPort port, ProductionStepViewModel parent, IServiceProvider serviceProvider) {
            portManager = serviceProvider.GetRequiredService<IProductionPortManager>();
            _port = port;
            _parent = parent;
            _connections = new List<ConnectionViewModel>();
            SetQuantityFromStep(parent);
        }

        // Listeners

        partial void OnQuantityChanged(double value) {
            OnPropertyChanged(nameof(WarningTintVisibility));
        }

        // Public Functions

        public void UpdateConnections(ProductionStepViewModel caller) {
            SetQuantityFromStep(caller);

            foreach (ConnectionViewModel connection in Connections) {
                connection.UpdateConnections(this);
            }
        }

        public void UpdateConnections(ConnectionViewModel connection) {
            if (Parent.CalculateUpdates) {
                Quantity = Connections.Sum(connection => connection.Quantity);
            }
            else if (Connections.Count != 0){
                Quantity = Max / Connections.Count;
            }

            Parent.UpdateConnections(this);
            OnPropertyChanged(nameof(WarningTintVisibility));
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
            Quantity = 0;

            IEnumerable<ConnectionViewModel> priority = Connections.Where(connection => !connection.Output.Parent.CalculateUpdates);
            foreach(ConnectionViewModel connection in priority) {
                connection.Quantity = Math.Min(connection.Output.Quantity, available);
                available -= connection.Quantity;
                Quantity += connection.Quantity;
                connection.UpdateConnections(this);
            }


            Quantity += available;
            IEnumerable<ConnectionViewModel> sharers = Connections.Where(connection => connection.Output.Parent.CalculateUpdates);
            double each = available / sharers.Count();
            foreach(ConnectionViewModel connection in sharers) {
                connection.Quantity = each;
                connection.UpdateConnections(this);
            }

            OnPropertyChanged(nameof(WarningTintVisibility));
        }

        public void PullResources() {
            double needed = Parent.NumMachines * Parent.Recipe.InputEntries[Index].Rate;
            Quantity = 0;

            IEnumerable<ConnectionViewModel> priority = Connections.Where(connection => !connection.Input.Parent.CalculateUpdates);
            foreach(ConnectionViewModel connection in priority) {
                connection.Quantity = Math.Min(connection.Input.Quantity, needed);
                needed -= connection.Quantity;
                Quantity += connection.Quantity;
                connection.UpdateConnections(this);
            }

            Quantity += needed;
            IEnumerable<ConnectionViewModel> sharers = Connections.Where(connection => connection.Input.Parent.CalculateUpdates);
            double each = needed / sharers.Count();
            foreach(ConnectionViewModel connection in sharers) {
                connection.Quantity = each;
                connection.UpdateConnections(this);
            }

            OnPropertyChanged(nameof(WarningTintVisibility));
        }

        public void SaveChanges() {
            portManager.TryUpdate(Port);
        }

        // Private Functions

        private void SetQuantityFromStep(ProductionStepViewModel step) {
            Quantity = Type switch {
                PortType.Input => step.NumMachines * step.Recipe.InputEntries[Index].Rate,
                PortType.Output => step.NumMachines * step.Recipe.OutputEntries[Index].Rate,
                _ => 0
            };
        }
    }
}
