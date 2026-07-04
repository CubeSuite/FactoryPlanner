using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.Services;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Services;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    /// <summary>
    /// Represents a production line as a draggable node on a parent canvas.
    /// Handles only visual/node concerns: position, name, icon, exposed ports.
    /// </summary>
    public partial class SubLineViewModel : ObservableObject, IProductionNode
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IUserSettings userSettings;
        private readonly ISearchService searchService;
        private readonly IProductionLineManager lineManager;
        private readonly IProductionStepManager stepManager;
        private readonly IConnectionManager connectionManager;

        // Fields
        private readonly ProductionLine _productionLine;
        private readonly List<ProductionPortViewModel> _inputPorts;
        private readonly List<ProductionPortViewModel> _outputPorts;
        private readonly HashSet<int> _connectedPortIds;

        // Properties
        public int ID => _productionLine.ID;
        public ProductionLine ProductionLine => _productionLine;

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
            }
        }

        [ObservableProperty]
        public partial string IconSearchTerm { get; set; }

        public List<KeyValuePair<string, string>> AllIcons { get; }
        public List<KeyValuePair<string, string>> FilteredIcons { get; set; }

        public string IconPath {
            get => _productionLine.IconPath;
            set {
                if (_productionLine.IconPath == value) return;
                _productionLine.IconPath = value;
                SaveChanges();
                OnPropertyChanged();
            }
        }

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

        public List<ProductionPortViewModel> InputPorts =>
            _inputPorts.Where(p => !_connectedPortIds.Contains(p.ID) || p.IsExposed).ToList();

        public List<ProductionPortViewModel> OutputPorts =>
            _outputPorts.Where(p => !_connectedPortIds.Contains(p.ID) || p.IsExposed).ToList();

        // Events
        public event Action<SubLineViewModel>? ShowLineRequested;
        public event Action<SubLineViewModel>? LineDeletionRequested;
        public event Action<SubLineViewModel>? DeleteRequested;

        // Constructor

        public SubLineViewModel(ProductionLine productionLine, IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            searchService = serviceProvider.GetRequiredService<ISearchService>();
            lineManager = serviceProvider.GetRequiredService<IProductionLineManager>();
            stepManager = serviceProvider.GetRequiredService<IProductionStepManager>();
            connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();

            _productionLine = productionLine;
            _inputPorts = new List<ProductionPortViewModel>();
            _outputPorts = new List<ProductionPortViewModel>();
            _connectedPortIds = new HashSet<int>();

            foreach (int stepId in _productionLine.Steps) {
                if (!stepManager.TryGet(stepId, out ProductionStep step)) continue;
                ProductionStepViewModel stepVM = new ProductionStepViewModel(step, serviceProvider);
                foreach (ProductionPortViewModel port in stepVM.InputPorts) _inputPorts.Add(port);
                foreach (ProductionPortViewModel port in stepVM.OutputPorts) _outputPorts.Add(port);
            }

            foreach (int connectionId in _productionLine.Connections) {
                if (!connectionManager.TryGet(connectionId, out Connection connection)) continue;
                _connectedPortIds.Add(connection.InputPortID);
                _connectedPortIds.Add(connection.OutputPortID);
            }

            IconSearchTerm = "";
            AllIcons = new List<KeyValuePair<string, string>>();
            FilteredIcons = new List<KeyValuePair<string, string>>();

            KeyValuePair<string, string> none = new KeyValuePair<string, string>("Default", "");
            AllIcons.Add(none);
            FilteredIcons.Add(none);

            foreach (Item item in serviceProvider.GetRequiredService<IItemManager>().GetAll()) {
                KeyValuePair<string, string> pair = new KeyValuePair<string, string>(item.Name, item.IconPath);
                AllIcons.Add(pair);
                FilteredIcons.Add(pair);
            }

            foreach (Machine machine in serviceProvider.GetRequiredService<IMachineManager>().GetAll()) {
                KeyValuePair<string, string> pair = new KeyValuePair<string, string>(machine.Name, machine.IconPath);
                AllIcons.Add(pair);
                FilteredIcons.Add(pair);
            }
        }

        // Listeners

        partial void OnIconSearchTermChanged(string value) {
            if (AllIcons == null || string.IsNullOrEmpty(value)) return;
            _ = SearchIconsAsync();
        }

        // Commands

        [RelayCommand]
        private void Delete() {
            LineDeletionRequested?.Invoke(this);
            DeleteRequested?.Invoke(this);
        }

        // Public Functions

        public void RaiseShowLineRequested() {
            ShowLineRequested?.Invoke(this);
        }

        public void SaveChanges() {
            lineManager.TryUpdate(_productionLine);
        }

        public async Task SearchIconsAsync() {
            FilteredIcons = (await searchService.Search(AllIcons, IconSearchTerm, pair => pair.Key)).ToList();
            OnPropertyChanged(nameof(FilteredIcons));
        }

        public void RefreshPorts() {
            _inputPorts.Clear();
            _outputPorts.Clear();
            foreach (int stepId in _productionLine.Steps) {
                if (!stepManager.TryGet(stepId, out ProductionStep step)) continue;
                ProductionStepViewModel stepVM = new ProductionStepViewModel(step, serviceProvider);
                foreach (ProductionPortViewModel port in stepVM.InputPorts) _inputPorts.Add(port);
                foreach (ProductionPortViewModel port in stepVM.OutputPorts) _outputPorts.Add(port);
            }

            _connectedPortIds.Clear();
            foreach (int connectionId in _productionLine.Connections) {
                if (!connectionManager.TryGet(connectionId, out Connection connection)) continue;
                _connectedPortIds.Add(connection.InputPortID);
                _connectedPortIds.Add(connection.OutputPortID);
            }
        }
    }
}
