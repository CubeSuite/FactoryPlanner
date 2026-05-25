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

        // Properties
        public ProductionStep ProductionStep => _productionStep;
        public RecipeViewModel Recipe => _recipe;
        public MachineViewModel Machine => _machine;
        public string? MachineNameLabel => NumMachines == 1 ? Machine?.Name : $"{Machine?.Name}s";

        public string IconPath => userSettings.IconSource switch {
            FactoryIconSource.Machine => _machine.IconPath,
            FactoryIconSource.FirstOutput => _recipe.Outputs.Keys.First().IconPath,
            _ => ""
        };

        public int NumMachines {
            get => _productionStep.NumMachines;
            set {
                if (_productionStep.NumMachines == value) return;
                _productionStep.NumMachines = value;
                OnPropertyChanged();
            }
        }

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

        // Constructors
        public ProductionStepViewModel(ProductionStep step, IServiceProvider serviceProvider) {
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            userSettings.SettingChanged += OnSettingChanged;

            _productionStep = step;

            _recipe = new RecipeViewModel(step.RecipeId, serviceProvider);
            _machine = new MachineViewModel(_recipe.Machine ?? new Machine() { Name = "Unknown" });
        }

        // Listeners

        private void OnSettingChanged(string name) {
            if (name == nameof(userSettings.IconSource)) {
                OnPropertyChanged(nameof(IconPath));
            }
        }
    }
}
