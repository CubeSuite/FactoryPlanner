using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    /// <summary>
    /// Represents the canvas currently being viewed. Owns its steps, sub-line nodes,
    /// and connections. All graph-mutation operations live here.
    /// </summary>
    public partial class ProductionLineViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IProductionLineManager lineManager;
        private readonly IProductionStepManager stepManager;
        private readonly IProductionPortManager portManager;
        private readonly IConnectionManager connectionManager;

        // Fields
        private readonly ProductionLine _productionLine;
        private readonly ObservableCollection<SubLineViewModel> _subLines;
        private readonly ObservableCollection<ProductionStepViewModel> _steps;
        private readonly ObservableCollection<ConnectionViewModel> _connections;
        private readonly Dictionary<int, ProductionPortViewModel> portMap;

        // Properties
        public int ID => _productionLine.ID;
        public ProductionLine ProductionLine => _productionLine;
        public ProductionLineViewModel? Parent { get; set; }

        public ObservableCollection<SubLineViewModel> SubLines => _subLines;
        public ObservableCollection<ProductionStepViewModel> Steps => _steps;
        public ObservableCollection<ConnectionViewModel> Connections => _connections;

        // Events
        public event Action<SubLineViewModel>? SubLineShowRequested;

        // Constructors

        public ProductionLineViewModel(ProductionLine productionLine, IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            lineManager = serviceProvider.GetRequiredService<IProductionLineManager>();
            portManager = serviceProvider.GetRequiredService<IProductionPortManager>();
            stepManager = serviceProvider.GetRequiredService<IProductionStepManager>();
            connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();

            _productionLine = productionLine;
            portMap = new Dictionary<int, ProductionPortViewModel>();

            _steps = new ObservableCollection<ProductionStepViewModel>();
            foreach (int id in _productionLine.Steps) {
                if (!stepManager.TryGet(id, out ProductionStep step)) continue;

                ProductionStepViewModel stepVM = new ProductionStepViewModel(step, serviceProvider);
                WireStepEvents(stepVM);

                foreach (ProductionPortViewModel port in stepVM.InputPorts) portMap[port.ID] = port;
                foreach (ProductionPortViewModel port in stepVM.OutputPorts) portMap[port.ID] = port;
                _steps.Add(stepVM);
            }

            _subLines = new ObservableCollection<SubLineViewModel>();
            foreach (int id in _productionLine.SubLines) {
                if (!lineManager.TryGet(id, out ProductionLine subLine)) continue;

                SubLineViewModel subLineVM = new SubLineViewModel(subLine, serviceProvider);
                WireSubLineEvents(subLineVM);

                foreach (ProductionPortViewModel port in subLineVM.InputPorts) portMap[port.ID] = port;
                foreach (ProductionPortViewModel port in subLineVM.OutputPorts) portMap[port.ID] = port;
                _subLines.Add(subLineVM);
            }

            _connections = new ObservableCollection<ConnectionViewModel>();
            foreach (int id in _productionLine.Connections) {
                if (!connectionManager.TryGet(id, out Connection connection)) continue;
                if (!portMap.TryGetValue(connection.InputPortID, out ProductionPortViewModel? inputPort)) continue;
                if (!portMap.TryGetValue(connection.OutputPortID, out ProductionPortViewModel? outputPort)) continue;

                ConnectionViewModel connectionVM = new ConnectionViewModel(connection, inputPort, outputPort, serviceProvider);
                inputPort.Connections.Add(connectionVM);
                outputPort.Connections.Add(connectionVM);
                _connections.Add(connectionVM);
            }

            _subLines.CollectionChanged += OnSubLinesCollectionChanged;
            _steps.CollectionChanged += OnStepsCollectionChanged;
            _connections.CollectionChanged += OnConnectionsCollectionChanged;
        }

        // Collection Listeners

        private void OnSubLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (SubLineViewModel line in e.NewItems) {
                    _productionLine.SubLines.Add(line.ProductionLine.ID);
                    WireSubLineEvents(line);
                }
            }
            else if (e.OldItems != null) {
                foreach (SubLineViewModel line in e.OldItems) {
                    _productionLine.SubLines.Remove(line.ProductionLine.ID);
                }
            }

            SaveChanges();
        }

        private void OnStepsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (ProductionStepViewModel step in e.NewItems) {
                    _productionLine.Steps.Add(step.ProductionStep.ID);
                }
            }
            else if (e.OldItems != null) {
                foreach (ProductionStepViewModel step in e.OldItems) {
                    _productionLine.Steps.Remove(step.ProductionStep.ID);
                }
            }

            SaveChanges();
        }

        private void OnConnectionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (ConnectionViewModel connection in e.NewItems) {
                    _productionLine.Connections.Add(connection.Connection.ID);
                }
            }
            else if (e.OldItems != null) {
                foreach (ConnectionViewModel connection in e.OldItems) {
                    _productionLine.Connections.Remove(connection.Connection.ID);
                }
            }

            SaveChanges();
        }

        // Public Graph-Mutation Methods

        public ProductionStepViewModel? AddStep(RecipeViewModel recipe, Point position) {
            if (!stepManager.CreateAndAdd(recipe.Recipe, position, out ProductionStep step)) return null;

            ProductionStepViewModel stepVM = new ProductionStepViewModel(step, serviceProvider);
            WireStepEvents(stepVM);

            List<ProductionPortViewModel> inputs = new List<ProductionPortViewModel>();
            for (int i = 0; i < recipe.Inputs.Count; i++) {
                if (!portManager.CreateAndAdd(step, PortType.Input, i, out ProductionPort port)) continue;
                ProductionPortViewModel portVM = new ProductionPortViewModel(port, stepVM, serviceProvider);
                inputs.Add(portVM);
                portMap[portVM.ID] = portVM;
            }

            List<ProductionPortViewModel> outputs = new List<ProductionPortViewModel>();
            for (int i = 0; i < recipe.Outputs.Count; i++) {
                if (!portManager.CreateAndAdd(step, PortType.Output, i, out ProductionPort port)) continue;
                ProductionPortViewModel portVM = new ProductionPortViewModel(port, stepVM, serviceProvider);
                outputs.Add(portVM);
                portMap[portVM.ID] = portVM;
            }

            stepVM.InputPorts = inputs;
            stepVM.OutputPorts = outputs;

            Steps.Add(stepVM);
            return stepVM;
        }

        public void DuplicateStep(ProductionStepViewModel step) {
            AddStep(step.Recipe, new Point(step.Position.X, step.Position.Y - 160));
        }

        public void DeleteStep(ProductionStepViewModel step) {
            HashSet<ProductionStepViewModel> connectedSteps = new HashSet<ProductionStepViewModel>();

            foreach (ProductionPortViewModel port in step.InputPorts) {
                foreach (ConnectionViewModel connection in port.Connections.ToList()) {
                    connectedSteps.Add(connection.Input.Parent);
                    connectionManager.TryDelete(connection.Connection);
                    Connections.Remove(connection);
                    connection.Input.Connections.Remove(connection);
                }

                portManager.TryDelete(port.Port);
            }

            foreach (ProductionPortViewModel port in step.OutputPorts) {
                foreach (ConnectionViewModel connection in port.Connections.ToList()) {
                    connectedSteps.Add(connection.Output.Parent);
                    connectionManager.TryDelete(connection.Connection);
                    Connections.Remove(connection);
                    connection.Output.Connections.Remove(connection);
                }

                portManager.TryDelete(port.Port);
            }

            stepManager.TryDelete(step.ProductionStep);
            Steps.Remove(step);

            foreach (ProductionStepViewModel connectedStep in connectedSteps) {
                connectedStep.UpdateConnections();
            }
        }

        public void DeleteSubLine(SubLineViewModel subLine) {
            foreach (ProductionPortViewModel port in subLine.InputPorts.Concat(subLine.OutputPorts)) {
                foreach (ConnectionViewModel conn in port.Connections.ToList()) {
                    DeleteConnection(conn);
                }
            }

            ProductionLineViewModel innerLine = new ProductionLineViewModel(subLine.ProductionLine, serviceProvider);
            foreach (SubLineViewModel innerSubLine in innerLine.SubLines.ToList()) {
                innerLine.DeleteSubLine(innerSubLine);
            }
            foreach (ProductionStepViewModel innerStep in innerLine.Steps.ToList()) {
                innerLine.DeleteStep(innerStep);
            }

            lineManager.TryDelete(subLine.ProductionLine);
            SubLines.Remove(subLine);
        }

        public void AddConnection(
            ProductionPortViewModel startPort,
            ProductionPortViewModel endPort,
            bool isStartExposed,
            bool isEndExposed
        ) {
            ProductionPortViewModel inputVM = startPort.Type == PortType.Input ? endPort : startPort;
            ProductionPortViewModel outputVM = startPort.Type == PortType.Input ? startPort : endPort;

            if (!connectionManager.CreateAndAdd(inputVM.Port, outputVM.Port, out Connection connection)) return;
            ProductionLine.Connections.Add(connection.ID);

            ConnectionViewModel connectionVM = new ConnectionViewModel(connection, inputVM, outputVM, serviceProvider);
            inputVM.Connections.Add(connectionVM);
            outputVM.Connections.Add(connectionVM);

            if (outputVM.AreNeedsMetByPrioritySteps()) inputVM.PushResources();
            else outputVM.PullResources();

            startPort.IsExposed = isStartExposed;
            endPort.IsExposed = isEndExposed;

            Connections.Add(connectionVM);
        }

        public void DeleteConnection(ConnectionViewModel connection) {
            connection.Input.Connections.Remove(connection);
            connection.Output.Connections.Remove(connection);
            Connections.Remove(connection);
            connectionManager.TryDelete(connection.Connection);
        }

        public bool DoesConnectionAlreadyExist(int id1, int id2, out ConnectionViewModel? connection) {
            foreach (ConnectionViewModel connectionVM in Connections) {
                if ((connectionVM.Input.ID == id1 && connectionVM.Output.ID == id2) ||
                    (connectionVM.Input.ID == id2 && connectionVM.Output.ID == id1)) {
                    connection = connectionVM;
                    return true;
                }
            }

            connection = null;
            return false;
        }

        public void SaveChanges() {
            lineManager.TryUpdate(_productionLine);
        }

        // Private Helpers

        private void WireStepEvents(ProductionStepViewModel stepVM) {
            stepVM.DuplicateRequested += DuplicateStep;
            stepVM.DeleteRequested += DeleteStep;
        }

        private void WireSubLineEvents(SubLineViewModel subLineVM) {
            subLineVM.ShowLineRequested += (sl) => SubLineShowRequested?.Invoke(sl);
            subLineVM.DeleteRequested += DeleteSubLine;
        }
    }
}
