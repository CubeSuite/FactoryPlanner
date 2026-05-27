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
        private readonly IUserSettings userSettings;

        // Fields
        private ProductionStep _productionStep;
        private RecipeViewModel _recipe;
        private MachineViewModel _machine;
        private List<ProductionPortViewModel> _inputPorts;
        private List<ProductionPortViewModel> _outputPorts;
        private double _lastTypedNumMachines;

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
            }
        }

        public double LastTypedNumMachines {
            get => _lastTypedNumMachines;
            set => _lastTypedNumMachines = value;
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
            }
        }

        public List<ProductionPortViewModel> InputPorts => _inputPorts;
        public List<ProductionPortViewModel> OutputPorts => _outputPorts;

        // Constructors

        public ProductionStepViewModel(ProductionStep step, IServiceProvider serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            userSettings.SettingChanged += OnSettingChanged;

            _productionStep = step;
            _recipe = new RecipeViewModel(step.RecipeId, serviceProvider);
            _machine = new MachineViewModel(_recipe.Machine ?? new Machine() { Name = "Unknown" });
            _inputPorts = new List<ProductionPortViewModel>();
            _outputPorts= new List<ProductionPortViewModel>();
            _lastTypedNumMachines = -1;

            for (int i = 0; i < Recipe.Inputs.Count; i++) {
                InputPorts.Add(new ProductionPortViewModel(new ProductionPort(step, PortType.Input, i), this));
            }

            for (int i = 0; i < Recipe.Outputs.Count; i++) {
                OutputPorts.Add(new ProductionPortViewModel(new ProductionPort(step, PortType.Output, i), this));
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
    }
}
