using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Core.Stores.UserSettings;
using FactoryPlanner.Services.Interfaces;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using WinRT;

namespace FactoryPlanner.MVVM.Pages
{
    public partial class FactoryPlannerPageViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IUserSettings userSettings;

        // Fields
        private Dictionary<string, FactoryIconSource> iconSourceMap;
        private IEnumerable<Item> allItemsCache;
        private IEnumerable<Machine> allMachinesCache;

        // Properties
        public ProductionLineViewModel ProductionLine { get; set; }

        [ObservableProperty]
        public partial string SelectedIconSource { get; set; }
        public string[] IconSources { get; }

        public bool SnapToGrid {
            get => userSettings.SnapToGrid;
            set {
                if (userSettings.SnapToGrid != value) {
                    userSettings.SnapToGrid = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool RenderGrid {
            get => userSettings.RenderGrid;
            set {
                if (userSettings.RenderGrid != value) {
                    userSettings.RenderGrid = value;
                    OnPropertyChanged();
                }
            }
        }

        public int GridSize {
            get => userSettings.GridSize;
            set {
                if (userSettings.GridSize != value) {
                    userSettings.GridSize = value;
                    OnPropertyChanged();
                }
            }
        }

        [ObservableProperty]
        public partial Recipe? SelectedRecipe { get; set; }
        public IEnumerable<Recipe> RecipesCache { get; }

        [ObservableProperty]
        public partial bool AddItemPopupIsOpen { get; set; }

        [ObservableProperty]
        public partial Point LastClickPosition { get; set; }

        [ObservableProperty]
        public partial Point LastCanvasClickPosition { get; set; }

        // Constructors

        public FactoryPlannerPageViewModel(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();

            iconSourceMap = EnumExtensions.GetValuesWithDescriptions<FactoryIconSource>();

            allItemsCache = serviceProvider.GetRequiredService<IItemManager>().GetAll();
            allMachinesCache = serviceProvider.GetRequiredService<IMachineManager>().GetAll();

            IconSources = iconSourceMap.Keys.ToArray();
            SelectedIconSource = userSettings.IconSource.GetDescription();
            RecipesCache = serviceProvider.GetRequiredService<IRecipeManager>().GetAll();
            SelectedRecipe = null;

            // ToDo: Load root production line
            ProductionLine = new ProductionLineViewModel(new ProductionLine());
        }

        // Listeners

        partial void OnSelectedIconSourceChanged(string value) {
            if (iconSourceMap.TryGetValue(value, out FactoryIconSource iconSource)) {
                userSettings.IconSource = iconSource;
            }
            else {
                Debug.Assert(false, $"iconSourceMap doesn't contain key '{value}'");
            }
        }

        partial void OnSelectedRecipeChanged(Recipe? value) {
            if (value == null) return;
            ProductionStep step = new ProductionStep(value, LastCanvasClickPosition);
            ProductionLine.Steps.Add(new ProductionStepViewModel(step, serviceProvider, allItemsCache, allMachinesCache));
            AddItemPopupIsOpen = false;

            // Defer resetting to avoid binding timing issues
            _ = ResetSelectedRecipeAsync();
        }

        private async Task ResetSelectedRecipeAsync() {
            await Task.Yield();
            SelectedRecipe = null;
        }

        // Commands

        [RelayCommand]
        private void UpALevelClicked() {

        }

        [RelayCommand]
        private void CanvasClick((Point viewportPosition, Point canvasPosition) positions) {
            AddItemPopupIsOpen = true;
            LastClickPosition = positions.viewportPosition;
            LastCanvasClickPosition = positions.canvasPosition;
        }

        [RelayCommand]
        private void CreateNewProductionLine() {

        }

        [RelayCommand]
        private void AddStorageNode() {

        }

        [RelayCommand]
        private void AddSinkNode() {

        }

    }
}
