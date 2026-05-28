using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public class ProductionLineViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IProductionLineManager lineManager;
        private readonly IProductionStepManager stepManager;
        private readonly IConnectionManager connectionManager;

        // Fields
        private ProductionLine _productionLine;
        private ObservableCollection<ProductionLineViewModel> _subLines;
        private ObservableCollection<ProductionStepViewModel> _steps;
        private ObservableCollection<ConnectionViewModel> _connections;
        private Dictionary<int, ProductionPortViewModel> portMap;

        // Properties
        public ProductionLine? Parent { get; }

        public ProductionLine ProductionLine => _productionLine;
        public ObservableCollection<ProductionLineViewModel> SubLines => _subLines;
        public ObservableCollection<ProductionStepViewModel> Steps => _steps;
        public ObservableCollection<ConnectionViewModel> Connections => _connections;

        public string Name {
            get => _productionLine.Name;
            set {
                if (_productionLine.Name == value) return;
                _productionLine.Name = value;
                SaveChanges();
            }
        }

        // Constructors

        public ProductionLineViewModel(ProductionLine productionLine, IServiceProvider serviceProvider) {
            lineManager = serviceProvider.GetRequiredService<IProductionLineManager>();
            stepManager = serviceProvider.GetRequiredService<IProductionStepManager>();
            connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();

            _productionLine = productionLine;

            _subLines = new ObservableCollection<ProductionLineViewModel>();
            foreach(int id in _productionLine.SubLines) {
                if (lineManager.TryGet(id, out ProductionLine subLine)) {
                    _subLines.Add(new ProductionLineViewModel(subLine, serviceProvider));
                }
            }

            portMap = new Dictionary<int, ProductionPortViewModel>();
            _steps = new ObservableCollection<ProductionStepViewModel>();
            foreach(int id in _productionLine.Steps) {
                if(stepManager.TryGet(id, out ProductionStep step)) {
                    ProductionStepViewModel stepVM = new ProductionStepViewModel(step, serviceProvider);
                    foreach (ProductionPortViewModel port in stepVM.InputPorts) portMap.Add(port.ID, port);
                    foreach (ProductionPortViewModel port in stepVM.OutputPorts) portMap.Add(port.ID, port);
                    _steps.Add(stepVM);
                }
            }

            _connections = new ObservableCollection<ConnectionViewModel>();
            foreach(int id in _productionLine.Connections) {
                if (connectionManager.TryGet(id, out Connection connection)) {
                    ProductionPortViewModel inputPort = portMap[connection.InputPortID];
                    ProductionPortViewModel outputPort = portMap[connection.OutputPortID];
                    ConnectionViewModel connectionVM = new ConnectionViewModel(
                        connection, 
                        portMap[connection.InputPortID], 
                        portMap[connection.OutputPortID], 
                        serviceProvider
                    );

                    inputPort.Connections.Add(connectionVM);
                    outputPort.Connections.Add(connectionVM);
                    Connections.Add(connectionVM);
                }
            }

            _subLines.CollectionChanged += OnSubLinesCollectionChanged;
            _steps.CollectionChanged += OnStepsCollectionChanged;
            _connections.CollectionChanged += OnConnectionsCollectionChanged;
        }

        //public ProductionLineViewModel(ProductionLineViewModel parent, IServiceProvider serviceProvider) {
        //    lineManager = serviceProvider.GetRequiredService<IProductionLineManager>();
        //    Parent = parent.ProductionLine;
        //    _productionLine = new ProductionLine();
        //    _subLines = new ObservableCollection<ProductionLineViewModel>();
        //    _steps = new ObservableCollection<ProductionStepViewModel>();
        //    _connections = new ObservableCollection<ConnectionViewModel>();

        //    _steps.CollectionChanged += OnStepsCollectionChanged;
        //    _connections.CollectionChanged += OnConnectionsCollectionChanged;
        //}

        // Listeners

        private void OnSubLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (ProductionLineViewModel line in e.NewItems) {
                    _productionLine.SubLines.Add(line.ProductionLine.ID);
                }
            }
            else if (e.OldItems != null) {
                foreach (ProductionLineViewModel line in e.OldItems) {
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
                foreach(ProductionStepViewModel step in e.OldItems) {
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
                foreach(ConnectionViewModel connection in e.OldItems) {
                    _productionLine.Connections.Remove(connection.Connection.ID);
                }
            }

            SaveChanges();
        }

        // Public Functions

        public void SaveChanges() {
            lineManager.TryUpdate(_productionLine);
        }
    }
}
