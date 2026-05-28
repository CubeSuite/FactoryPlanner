using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Core.Stores.UserSettings;
using FactoryPlanner.Services.Interfaces;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.Foundation;
using Windows.UI.StartScreen;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public class ProductionStepViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IProductionStepManager stepManager;
        private readonly IProductionPortManager portManager;
        private readonly IUserSettings userSettings;

        // Fields
        private ProductionStep _productionStep;
        private RecipeViewModel _recipe;
        private MachineViewModel _machine;
        private List<ProductionPortViewModel> _inputPorts;
        private List<ProductionPortViewModel> _outputPorts;

        // Properties
        public ProductionStep ProductionStep => _productionStep;
        public RecipeViewModel Recipe => _recipe;
        public MachineViewModel Machine => _machine;
        public string? MachineName => NumMachines == 1 ? Machine?.Name : $"{Machine?.Name}s";

        public string IconPath => userSettings.IconSource switch {
            FactoryIconSource.Machine => _machine.IconPath,
            FactoryIconSource.FirstOutput => _recipe.Outputs.Keys.First().IconPath,
            _ => ""
        };

        public double NumMachines {
            get => _productionStep.NumMachines;
            set {
                if (_productionStep.NumMachines == value) return;
                _productionStep.NumMachines = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MachineName));
                SaveChanges();
            }
        }

        public double LastTypedNumMachines {
            get => _productionStep.LastTypedNumMachines;
            set {
                _productionStep.LastTypedNumMachines = value;
                SaveChanges();
            }
        }

        public bool CalculateUpdates => LastTypedNumMachines == -1;

        public Point Position {
            get => _productionStep.Position;
            set {
                Point newPosition = value;
                if (userSettings.SnapToGrid) {
                    int gridSize = userSettings.GridSize;
                    newPosition = new Point(
                        Math.Round(value.X / gridSize) * gridSize,
                        Math.Round(value.Y / gridSize) * gridSize
                    );
                }

                if (_productionStep.Position == newPosition) return;
                _productionStep.Position = newPosition;
                OnPropertyChanged();
                SaveChanges();
            }
        }

        public List<ProductionPortViewModel> InputPorts {
            get => _inputPorts;
            set {
                _inputPorts = value;
                _productionStep.InputPortIDs = _inputPorts.Select(port => port.ID).ToHashSet();
                SaveChanges();
            }
        }

        public List<ProductionPortViewModel> OutputPorts {
            get => _outputPorts;
            set {
                _outputPorts = value;
                _productionStep.OutputPortIDs = _outputPorts.Select(port => port.ID).ToHashSet();
                SaveChanges();
            }
        }

        // Constructors

        public ProductionStepViewModel(ProductionStep step, IServiceProvider serviceProvider) {
            stepManager = serviceProvider.GetRequiredService<IProductionStepManager>();
            portManager = serviceProvider.GetRequiredService<IProductionPortManager>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            userSettings.SettingChanged += OnSettingChanged;

            _productionStep = step;
            _recipe = new RecipeViewModel(step.RecipeID, serviceProvider);
            _machine = new MachineViewModel(_recipe.Machine ?? new Machine() { Name = "Unknown" });
            _inputPorts = new List<ProductionPortViewModel>();
            _outputPorts= new List<ProductionPortViewModel>();

            foreach(int id in step.InputPortIDs) {
                if (portManager.TryGet(id, out ProductionPort port)) {
                    _inputPorts.Add(new ProductionPortViewModel(port, this));
                }
            }

            foreach (int id in step.OutputPortIDs) {
                if (portManager.TryGet(id, out ProductionPort port)) {
                    _outputPorts.Add(new ProductionPortViewModel(port, this));
                }
            }
        }

        // Listeners

        private void OnSettingChanged(string name) {
            if (name == nameof(userSettings.IconSource)) {
                OnPropertyChanged(nameof(IconPath));
            }
        }

        // Public Functions

        public void UpdateConnections(ProductionPortViewModel? caller = null) {
            foreach (ProductionPortViewModel port in InputPorts) {
                if (caller == null) port.PullResources();
                else if (port != caller) port.UpdateConnections(this);
            }

            foreach (ProductionPortViewModel port in OutputPorts) {
                if (caller == null) port.PushResources();
                else if (port != caller) port.UpdateConnections(this); // ToDo: figure out how to remove this clause without stack overflow
            }

            if (caller != null && LastTypedNumMachines == -1) {
                NumMachines = caller.Type switch {
                    PortType.Input => InputPorts.Min(port => port.Quantity / Recipe.InputEntries[caller.Index].Rate),
                    PortType.Output => OutputPorts.Max(port => port.Quantity / Recipe.OutputEntries[caller.Index].Rate),
                    _ => 0
                };
            }
            else if (caller?.Type == PortType.Input) {
                //double quantity = 
            }
        }

        public ProductionPortViewModel? FindPortForItem(PortType type, Item item) {
            return type switch {
                PortType.Input => InputPorts.FirstOrDefault(port => port.Item == item),
                PortType.Output => OutputPorts.FirstOrDefault(port => port.Item == item),
                _ => null
            };
        }

        public void SaveChanges() {
            stepManager.TryUpdate(_productionStep);
        }
    }
}
