using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Services;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Core.Stores.UserSettings;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

namespace FactoryPlanner.MVVM.Pages
{
    public partial class FactoryPlannerPageViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IRecipeManager recipeManager;
        private readonly IUserSettings _userSettings;
        private readonly ISearchService searchService;
        private readonly IProductionLineManager lineManager;
        private readonly IProductionStepManager stepManager;
        private readonly IProductionPortManager portManager;
        private readonly IConnectionManager connectionManager;

        // Fields
        private Dictionary<string, FactoryIconSource> iconSourceMap;
        private List<RecipeViewModel> _allRecipes;
        private IEnumerable<RecipeViewModel> _filteredRecipes;

        // Properties
        public IUserSettings UserSettings => _userSettings;

        [ObservableProperty]
        public partial ProductionLineViewModel CurrentProductionLine { get; set; }

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
        public bool IsStartPortExposed { get; set; }

        [ObservableProperty]
        public partial ProductionPortViewModel? EndPort { get; set; }
        public bool IsEndPortExposed { get; set; }

        // Constructors

        public FactoryPlannerPageViewModel(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            recipeManager = serviceProvider.GetRequiredService<IRecipeManager>();
            _userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            searchService = serviceProvider.GetRequiredService<ISearchService>();
            lineManager = serviceProvider.GetRequiredService<IProductionLineManager>();
            stepManager = serviceProvider.GetRequiredService<IProductionStepManager>();
            portManager = serviceProvider.GetRequiredService<IProductionPortManager>();
            connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();

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

            ProductionLineViewModel line = new ProductionLineViewModel(lineManager.GetRootLine(), serviceProvider);
            line.StepDuplicationRequested += OnProductionStepDuplicationRequested;
            line.ShowLineRequested += OnShowProductionLineRequested;
            CurrentProductionLine = line;
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
            ProductionStepViewModel? stepVM = CreateProductionStep(value, LastCanvasClickPosition);
            
            AddStepPopupIsOpen = false;
            _ = ResetSelectedRecipeAsync();

            if (stepVM != null && IsDrawingConnection && StartPort != null) {
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

        private void OnProductionStepDuplicationRequested(ProductionStepViewModel requester) {
            Point position = new Point(requester.Position.X, requester.Position.Y - 160);
            CreateProductionStep(requester.Recipe, position);
        }

        private void OnProductionStepDeleteRequested(ProductionStepViewModel requester) {
            CurrentProductionLine.DeleteStep(requester);
        }

        private void OnShowProductionLineRequested(ProductionLineViewModel line) {
            CurrentProductionLine = line;
        }

        // Commands

        [RelayCommand]
        private void UpALevel() {
            if (CurrentProductionLine.Parent != null) {
                CurrentProductionLine = CurrentProductionLine.Parent;
            }
            else if (lineManager.CreateAndAdd(out ProductionLine line, -1)) {
                ProductionLineViewModel lineVM = new ProductionLineViewModel(line, serviceProvider);
                CurrentProductionLine.ParentID = line.ID;
                CurrentProductionLine.Parent = lineVM;

                lineVM.SubLines.Add(CurrentProductionLine);
                lineVM.StepDuplicationRequested += OnProductionStepDuplicationRequested;
                lineVM.ShowLineRequested += OnShowProductionLineRequested;
                CurrentProductionLine = lineVM;
            }
        }

        [RelayCommand]
        private void CanvasClick((Point viewportPosition, Point canvasPosition) positions) {
            AddStepPopupIsOpen = true;
            AddStepPopupPosition = positions.viewportPosition;
            LastCanvasClickPosition = positions.canvasPosition;
        }

        [RelayCommand]
        private void CreateNewProductionLine() {
            if (!lineManager.CreateAndAdd(out ProductionLine newLine, CurrentProductionLine.ID)) return;
            ProductionLineViewModel newLineVM = new ProductionLineViewModel(newLine, CurrentProductionLine, serviceProvider);
            CurrentProductionLine.SubLines.Add(newLineVM);
            CurrentProductionLine = newLineVM;
        }

        [RelayCommand]
        private void AddStorageNode() {

        }

        [RelayCommand]
        private void AddSinkNode() {

        }

        // Public Functions

        public void FormNewConnection() {
            if (StartPort == null || EndPort == null) return;

            if(CurrentProductionLine.DoesConnectionAlreadyExist(StartPort.ID, EndPort.ID, out ConnectionViewModel? existingConnection) && existingConnection != null) {
                DeleteConnection(existingConnection);
                return;
            }

            ProductionPortViewModel inputVM = StartPort.Type == PortType.Input ? EndPort : StartPort;
            ProductionPortViewModel outputVM = StartPort.Type == PortType.Input ? StartPort : EndPort;

            if (!connectionManager.CreateAndAdd(inputVM.Port, outputVM.Port, out Connection connection)) return;
            CurrentProductionLine.ProductionLine.Connections.Add(connection.ID);

            ConnectionViewModel connectionVM = new ConnectionViewModel(connection, inputVM, outputVM, serviceProvider);
            inputVM.Connections.Add(connectionVM);
            outputVM.Connections.Add(connectionVM);

            if (outputVM.AreNeedsMetByPrioritySteps()) inputVM.PushResources();
            else outputVM.PullResources();

            StartPort.IsExposed = IsStartPortExposed;
            EndPort.IsExposed = IsEndPortExposed;

            CurrentProductionLine.Connections.Add(connectionVM);

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

        private ProductionStepViewModel? CreateProductionStep(RecipeViewModel recipe, Point point) {
            if (!stepManager.CreateAndAdd(recipe.Recipe, point, out ProductionStep step)) return null;
            ProductionStepViewModel stepVM = new ProductionStepViewModel(step, serviceProvider);

            stepVM.DuplicateRequested += OnProductionStepDuplicationRequested;
            stepVM.DeleteRequested += OnProductionStepDeleteRequested;

            List<ProductionPortViewModel> inputs = new List<ProductionPortViewModel>();
            for (int i = 0; i < recipe.Inputs.Count; i++) {
                if (!portManager.CreateAndAdd(step, PortType.Input, i, out ProductionPort port)) continue;
                inputs.Add(new ProductionPortViewModel(port, stepVM, serviceProvider));
            }

            List<ProductionPortViewModel> outputs = new List<ProductionPortViewModel>();
            for (int i = 0; i < recipe.Outputs.Count; i++) {
                if (!portManager.CreateAndAdd(step, PortType.Output, i, out ProductionPort port)) continue;
                outputs.Add(new ProductionPortViewModel(port, stepVM, serviceProvider));
            }

            stepVM.InputPorts = inputs;
            stepVM.OutputPorts = outputs;

            CurrentProductionLine.Steps.Add(stepVM);
            return stepVM;
        }

        private void DeleteConnection(ConnectionViewModel connection) {
            connection.Input.Connections.Remove(connection);
            connection.Output.Connections.Remove(connection);
            CurrentProductionLine.Connections.Remove(connection);
            connectionManager.TryDelete(connection.Connection);
        }
    }
}
