using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Services;
using FactoryPlanner.Stores;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public partial class ProductionLineViewModel : ObservableObject, IProductionNode
    {
        // Services & Stores
        private readonly IUserSettings userSettings;
        private readonly IProductionLineManager lineManager;
        private readonly IProductionStepManager stepManager;
        private readonly IProductionPortManager portManager;
        private readonly IConnectionManager connectionManager;
        
        // Fields
        private ProductionLine _productionLine;
        private ObservableCollection<ProductionLineViewModel> _subLines;
        private ObservableCollection<ProductionStepViewModel> _steps;
        private ObservableCollection<ConnectionViewModel> _connections;
        private Dictionary<int, ProductionPortViewModel> portMap;

        // Properties
        public int ID => _productionLine.ID;
        public ProductionLineViewModel? Parent { get; set; }

        public ProductionLine ProductionLine => _productionLine;
        public ObservableCollection<ProductionLineViewModel> SubLines => _subLines;
        public ObservableCollection<ProductionStepViewModel> Steps => _steps;
        public ObservableCollection<ConnectionViewModel> Connections => _connections;

        public int ParentID {
            get => _productionLine.ParentID;
            set {
                if (_productionLine.ParentID == value) return;
                _productionLine.ParentID = value;
                SaveChanges();
            }
        }

        public string Name {
            get => _productionLine.Name;
            set {
                if (_productionLine.Name == value) return;
                _productionLine.Name = value;
                SaveChanges();
            }
        }

        public string IconPath {
            get => _productionLine.IconPath;
            set {
                if (_productionLine.IconPath == value) return;
                _productionLine.IconPath = value;
                SaveChanges();
                OnPropertyChanged(nameof(DefaultIconVisibility));
            }
        }

        public Visibility DefaultIconVisibility => string.IsNullOrEmpty(IconPath) ? Visibility.Visible : Visibility.Collapsed;

        public Point Position {
            get => _productionLine.Position;
            set {
                Point newPosition = value;
                if (userSettings.SnapToGrid) {
                    int gridSize = userSettings.GridSize;
                    newPosition = new Point(
                        Math.Round(value.X / gridSize) * gridSize,
                        Math.Round(value.Y / gridSize) * gridSize
                    );
                }

                if (_productionLine.Position == newPosition) return;
                _productionLine.Position = newPosition;
                OnPropertyChanged();
            }
        }

        public List<ProductionPortViewModel> InputPorts => Steps.SelectMany(step => step.InputPorts)
                                                                .Where(port => port.Connections.Count == 0 || port.IsExposed)
                                                                .ToList();

        public List<ProductionPortViewModel> OutputPorts => Steps.SelectMany(step => step.OutputPorts)
                                                                 .Where(port => port.Connections.Count == 0 || port.IsExposed)
                                                                 .ToList();

        // Events

        public event Action<ProductionStepViewModel>? StepDuplicationRequested;
        public event Action<ProductionLineViewModel>? LineDuplicationRequested;
        public event Action<ProductionLineViewModel>? LineDeletionRequested;
        public event Action<ProductionLineViewModel>? ShowLineRequested;

        // Constructors

        public ProductionLineViewModel(ProductionLine productionLine, IServiceProvider serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            lineManager = serviceProvider.GetRequiredService<IProductionLineManager>();
            portManager = serviceProvider.GetRequiredService<IProductionPortManager>();
            stepManager = serviceProvider.GetRequiredService<IProductionStepManager>();
            connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();

            _productionLine = productionLine;
            portMap = new Dictionary<int, ProductionPortViewModel>();

            _steps = new ObservableCollection<ProductionStepViewModel>();
            foreach (int id in _productionLine.Steps) {
                if (stepManager.TryGet(id, out ProductionStep step)) {
                    ProductionStepViewModel stepVM = new ProductionStepViewModel(step, serviceProvider);
                    stepVM.DuplicateRequested += DuplicateStep;
                    stepVM.DeleteRequested += DeleteStep;

                    foreach (ProductionPortViewModel port in stepVM.InputPorts) portMap.Add(port.ID, port);
                    foreach (ProductionPortViewModel port in stepVM.OutputPorts) portMap.Add(port.ID, port);
                    _steps.Add(stepVM);
                }
            }

            _subLines = new ObservableCollection<ProductionLineViewModel>();
            foreach (int id in _productionLine.SubLines) {
                if (lineManager.TryGet(id, out ProductionLine subLine)) {
                    ProductionLineViewModel subLineVM = new ProductionLineViewModel(subLine, serviceProvider) {
                        Parent = this
                    };

                    subLineVM.ShowLineRequested += OnShowSubLineRequested;

                    foreach (ProductionPortViewModel port in subLineVM.InputPorts) portMap.Add(port.ID, port);
                    foreach (ProductionPortViewModel port in subLineVM.OutputPorts) portMap.Add(port.ID, port);

                    _subLines.Add(subLineVM);
                }
            }

            _connections = new ObservableCollection<ConnectionViewModel>();
            foreach (int id in _productionLine.Connections) {
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

        public ProductionLineViewModel(ProductionLine productionLine, ProductionLineViewModel parent, IServiceProvider serviceProvider) : this(productionLine, serviceProvider) {
            Parent = parent;
        }

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

        private void OnShowSubLineRequested(ProductionLineViewModel subLine) {
            ShowLineRequested?.Invoke(subLine);
        }

        // Commands

        [RelayCommand]
        private void Duplicate() {
            LineDuplicationRequested?.Invoke(this);
        }

        [RelayCommand]
        private void Delete() {
            LineDeletionRequested?.Invoke(this);
        }

        // Public Functions

        public void SaveChanges() {
            lineManager.TryUpdate(_productionLine);
        }

        public void DuplicateStep(ProductionStepViewModel step) {
            StepDuplicationRequested?.Invoke(step);
        }

        public void DeleteStep(ProductionStepViewModel step) {
            HashSet<ProductionStepViewModel> connectedSteps = new HashSet<ProductionStepViewModel>();

            foreach (ProductionPortViewModel port in step.InputPorts) {
                foreach (ConnectionViewModel connection in port.Connections) {
                    connectedSteps.Add(connection.Input.Parent);
                    connectionManager.TryDelete(connection.Connection);
                    Connections.Remove(connection);
                    connection.Input.Connections.Remove(connection);
                }

                portManager.TryDelete(port.Port);
            }

            foreach (ProductionPortViewModel port in step.OutputPorts) {
                foreach (ConnectionViewModel connection in port.Connections) {
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

        public void RaiseShowLineRequested() {
            ShowLineRequested?.Invoke(this);
        }

        public bool DoesConnectionAlreadyExist(int id1, int id2, out ConnectionViewModel? connection) {
            foreach(ConnectionViewModel connectionVM in Connections) {
                if ((connectionVM.Input.ID == id1 && connectionVM.Output.ID == id2) ||
                    (connectionVM.Input.ID == id2 && connectionVM.Output.ID == id1)) {
                    connection = connectionVM;
                    return true;
                }
            }

            connection = null;
            return false;
        }
    }
}
