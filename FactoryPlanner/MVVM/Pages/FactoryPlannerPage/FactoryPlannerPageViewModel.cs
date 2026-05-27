using ABI.Windows.ApplicationModel.Activation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Services;
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
        private readonly ISearchService searchService;

        // Fields
        private Dictionary<string, FactoryIconSource> iconSourceMap;
        private List<RecipeViewModel> _allRecipes;
        private IEnumerable<RecipeViewModel> _filteredRecipes;

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
        public partial RecipeViewModel? SelectedRecipe { get; set; }
        public List<RecipeViewModel> AllRecipes => _allRecipes;
        
        public IEnumerable<RecipeViewModel> FilteredByItem {
            get {
                if (StartPort == null) return AllRecipes;
                else if (StartPort.Type == PortType.Input) return AllRecipes.Where(recipe => recipe.Outputs.ContainsKey(StartPort.Item));
                else if (StartPort.Type == PortType.Output) return AllRecipes.Where(recipe => recipe.Inputs.ContainsKey(StartPort.Item));
                return AllRecipes;
            }
        }

        public IEnumerable<RecipeViewModel> FilteredRecipes {
            get => _filteredRecipes;
            set {
                _filteredRecipes = value;
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        public partial string RecipeSearchTerm { get; set; }

        [ObservableProperty]
        public partial bool AddStepPopupIsOpen { get; set; }

        [ObservableProperty]
        public partial Point AddStepPopupPosition { get; set; }

        [ObservableProperty]
        public partial Point LastCanvasClickPosition { get; set; }

        [ObservableProperty]
        public partial bool IsDrawingConnection { get; set; }

        [ObservableProperty]
        public partial ProductionPortViewModel? StartPort { get; set; }

        [ObservableProperty]
        public partial ProductionPortViewModel? EndPort { get; set; }

        // Constructors

        public FactoryPlannerPageViewModel(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            recipeManager = serviceProvider.GetRequiredService<IRecipeManager>();
            _userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            searchService = serviceProvider.GetRequiredService<ISearchService>();

            iconSourceMap = EnumExtensions.GetValuesWithDescriptions<FactoryIconSource>();
            _allRecipes = new List<RecipeViewModel>();
            _filteredRecipes = new List<RecipeViewModel>();

            foreach(Recipe recipe in recipeManager.GetAll()) {
                _allRecipes.Add(new RecipeViewModel(recipe, serviceProvider));
            }

            IconSources = iconSourceMap.Keys.ToArray();
            SelectedIconSource = _userSettings.IconSource.GetDescription();
            SelectedRecipe = null;
            RecipeSearchTerm = "";

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

        partial void OnRecipeSearchTermChanged(string value) {
            _ = SearchForRecipesAsync();
        }

        partial void OnSelectedRecipeChanged(RecipeViewModel? value) {
            if (value == null) return;
            ProductionStep step = new ProductionStep(value.Recipe, LastCanvasClickPosition);
            ProductionStepViewModel stepVM = new ProductionStepViewModel(step, serviceProvider);
            ProductionLineVM.Steps.Add(stepVM);
            AddStepPopupIsOpen = false;

            _ = ResetSelectedRecipeAsync();

            if (IsDrawingConnection && StartPort != null) {
                PortType targetType = StartPort.Type == PortType.Input ? PortType.Output : PortType.Input;
                ProductionPortViewModel? port = stepVM.FindPortForItem(targetType, StartPort.Item);
                if (port == null) {
                    IsDrawingConnection = false;
                    StartPort = null;
                    return;
                }
                
                EndPort = port;
                FormNewConnection();
            }
        }

        partial void OnStartPortChanged(ProductionPortViewModel? value) {
            OnPropertyChanged(nameof(FilteredByItem));
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

        public void FormNewConnection() {
            ProductionPortViewModel inputVM = StartPort.Type == PortType.Input ? EndPort : StartPort;
            ProductionPortViewModel outputVM = StartPort.Type == PortType.Input ? StartPort : EndPort;

            Connection connection = new Connection(inputVM.Port, outputVM.Port);
            ProductionLineVM.ProductionLine.Connections.Add(connection);

            ConnectionViewModel connectionVM = new ConnectionViewModel(connection, inputVM, outputVM);
            inputVM.Connections.Add(connectionVM);
            outputVM.Connections.Add(connectionVM);

            if (outputVM.AreNeedsMetByPrioritySteps()) inputVM.PushResources();
            else outputVM.PullResources();

            ProductionLineVM.Connections.Add(connectionVM);

            StartPort = null;
            EndPort = null;
            IsDrawingConnection = false;
        }

        // Private Functions

        private async Task ResetSelectedRecipeAsync() {
            await Task.Yield();
            SelectedRecipe = null;
        }

        private async Task SearchForRecipesAsync() {
            FilteredRecipes = await searchService.Search(FilteredByItem, RecipeSearchTerm, RecipeViewModel.GetSearchSelectors());
        }
    }
}
