using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Core.Stores.UserSettings;
using FactoryPlanner.MVVM.Views;
using FactoryPlanner.Services.Interfaces;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Audio;
using WinRT;

namespace FactoryPlanner.MVVM.Pages
{
    public partial class FactoryPlannerPageViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IRecipeManager recipeManager;
        private readonly IUserSettings _userSettings;

        // Fields
        private Dictionary<string, FactoryIconSource> iconSourceMap;
        //private ProductionPortViewModel _startPort;
        //private ProductionPortViewModel _endPoint;

        // Properties
        public IUserSettings UserSettings => _userSettings;

        public ProductionLineViewModel ProductionLineVM { get; set; }

        [ObservableProperty]
        public partial string SelectedIconSource { get; set; }
        public string[] IconSources { get; }

        public bool SnapToGrid {
            get => _userSettings.SnapToGrid;
            set {
                if (_userSettings.SnapToGrid != value) {
                    _userSettings.SnapToGrid = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool RenderGrid {
            get => _userSettings.RenderGrid;
            set {
                if (_userSettings.RenderGrid != value) {
                    _userSettings.RenderGrid = value;
                    OnPropertyChanged();
                }
            }
        }

        public int GridSize {
            get => _userSettings.GridSize;
            set {
                if (_userSettings.GridSize != value) {
                    _userSettings.GridSize = value;
                    OnPropertyChanged();
                }
            }
        }

        [ObservableProperty]
        public partial Recipe? SelectedRecipe { get; set; }
        public IEnumerable<Recipe> AllRecipes => recipeManager.GetAll();

        [ObservableProperty]
        public partial bool AddStepPopupIsOpen { get; set; }

        [ObservableProperty]
        public partial Point AddStepPopupPosition { get; set; }

        [ObservableProperty]
        public partial Point LastCanvasClickPosition { get; set; }

        // Constructors

        public FactoryPlannerPageViewModel(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            recipeManager = serviceProvider.GetRequiredService<IRecipeManager>();
            _userSettings = serviceProvider.GetRequiredService<IUserSettings>();

            iconSourceMap = EnumExtensions.GetValuesWithDescriptions<FactoryIconSource>();

            IconSources = iconSourceMap.Keys.ToArray();
            SelectedIconSource = _userSettings.IconSource.GetDescription();
            SelectedRecipe = null;

            // ToDo: Load root production line
            ProductionLineVM = new ProductionLineViewModel(new ProductionLine());
        }

        // Listeners

        partial void OnSelectedIconSourceChanged(string value) {
            if (iconSourceMap.TryGetValue(value, out FactoryIconSource iconSource)) {
                _userSettings.IconSource = iconSource;
            }
            else {
                Debug.Assert(false, $"iconSourceMap doesn't contain key '{value}'");
            }
        }

        partial void OnSelectedRecipeChanged(Recipe? value) {
            if (value == null) return;
            ProductionStep step = new ProductionStep(value, LastCanvasClickPosition);
            ProductionLineVM.Steps.Add(new ProductionStepViewModel(step, serviceProvider));
            AddStepPopupIsOpen = false;

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
            AddStepPopupIsOpen = true;
            AddStepPopupPosition = positions.viewportPosition;
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

        // Public Functions

        public void FormNewConnection(ProductionPortViewModel startVM, ProductionPortViewModel endVM) {
            ProductionPortViewModel inputVM = startVM.Type == PortType.Input ? endVM : startVM;
            ProductionPortViewModel outputVM = startVM.Type == PortType.Input ? startVM : endVM;

            Connection connection = new Connection(inputVM.Port, outputVM.Port);
            ProductionLineVM.ProductionLine.Connections.Add(connection);

            ConnectionViewModel connectionVM = new ConnectionViewModel(connection, inputVM, outputVM);
            inputVM.Connections.Add(connectionVM);
            outputVM.Connections.Add(connectionVM);

            if (outputVM.AreNeedsMetByPrioritySteps()) inputVM.PushResources();
            else outputVM.PullResources();

            ProductionLineVM.Connections.Add(connectionVM);
        }
    }
}
