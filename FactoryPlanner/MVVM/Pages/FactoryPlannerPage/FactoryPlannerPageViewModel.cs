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
using System.Collections.ObjectModel;
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
                if (StartPort.Type == PortType.Input) return AllRecipes.Where(recipe => recipe.Outputs.ContainsKey(StartPort.Item));
                if (StartPort.Type == PortType.Output) return AllRecipes.Where(recipe => recipe.Inputs.ContainsKey(StartPort.Item));
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

        public Point MainCanvasCenter { get; set; }

        [ObservableProperty]
        public partial bool IsDrawingConnection { get; set; }

        [ObservableProperty]
        public partial ProductionPortViewModel? StartPort { get; set; }
        public bool IsStartPortExposed { get; set; }

        [ObservableProperty]
        public partial ProductionPortViewModel? EndPort { get; set; }
        public bool IsEndPortExposed { get; set; }

        // Constructor

        public FactoryPlannerPageViewModel(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            recipeManager = serviceProvider.GetRequiredService<IRecipeManager>();
            _userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            searchService = serviceProvider.GetRequiredService<ISearchService>();
            lineManager = serviceProvider.GetRequiredService<IProductionLineManager>();

            iconSourceMap = EnumExtensions.GetValuesWithDescriptions<FactoryIconSource>();
            _allRecipes = new List<RecipeViewModel>();
            _filteredRecipes = new List<RecipeViewModel>();

            foreach (Recipe recipe in recipeManager.GetAll()) {
                _allRecipes.Add(new RecipeViewModel(recipe, serviceProvider));
            }

            IconSources = iconSourceMap.Keys.ToArray();
            SelectedIconSource = _userSettings.IconSource.GetDescription();
            SelectedRecipe = null;
            RecipeSearchTerm = "";

            ProductionLineViewModel rootLine = new ProductionLineViewModel(lineManager.GetRootLine(), serviceProvider);
            CurrentProductionLine = rootLine;
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

            ProductionStepViewModel? stepVM = CurrentProductionLine.AddStep(value, LastCanvasClickPosition);
            //OnPropertyChanged(nameof(Steps));
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

        partial void OnCurrentProductionLineChanged(ProductionLineViewModel oldValue, ProductionLineViewModel newValue) {
            if (oldValue != null) oldValue.SubLineShowRequested -= OnSubLineShowRequested;
            SubscribeToLine(newValue);
            OnPropertyChanged(nameof(CurrentProductionLine.Steps));
            OnPropertyChanged(nameof(CurrentProductionLine.SubLines));
            OnPropertyChanged(nameof(CurrentProductionLine.Connections));
        }

        private void OnSubLineShowRequested(SubLineViewModel subLine) {
            ProductionLineViewModel innerLine = new ProductionLineViewModel(subLine.ProductionLine, serviceProvider) {
                Parent = CurrentProductionLine
            };

            CurrentProductionLine = innerLine;
        }

        // Commands

        [RelayCommand]
        private void UpALevel() {
            if (CurrentProductionLine.Parent != null) {
                foreach(SubLineViewModel subLine in CurrentProductionLine.Parent.SubLines) {
                    subLine.RefreshPorts();
                }

                CurrentProductionLine = CurrentProductionLine.Parent;
            }
            else if (lineManager.CreateAndAdd(out ProductionLine newParentModel, -1)) {
                ProductionLineViewModel newParent = new ProductionLineViewModel(newParentModel, serviceProvider);

                SubLineViewModel currentAsNode = new SubLineViewModel(
                    CurrentProductionLine.ProductionLine, serviceProvider
                ) {
                    ParentID = newParentModel.ID,
                    Position = MainCanvasCenter
                };

                currentAsNode.SaveChanges();
                newParent.SubLines.Add(currentAsNode);
                CurrentProductionLine.Parent = newParent;
                CurrentProductionLine = newParent;
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

            SubLineViewModel newSubLineVM = new SubLineViewModel(newLine, serviceProvider) {
                ParentID = CurrentProductionLine.ID,
                Position = LastCanvasClickPosition
            };

            newSubLineVM.SaveChanges();
            CurrentProductionLine.SubLines.Add(newSubLineVM);
            AddStepPopupIsOpen = false;
        }

        [RelayCommand]
        private void AddStorageNode() { }

        [RelayCommand]
        private void AddSinkNode() { }

        // Public Functions

        public void FormNewConnection() {
            if (StartPort == null || EndPort == null) return;

            if (CurrentProductionLine.DoesConnectionAlreadyExist(StartPort.ID, EndPort.ID, out ConnectionViewModel? existing) && existing != null) {
                CurrentProductionLine.DeleteConnection(existing);
            }
            else {
                CurrentProductionLine.AddConnection(StartPort, EndPort, IsStartPortExposed, IsEndPortExposed);
            }

            StartPort = null;
            EndPort = null;
            IsDrawingConnection = false;
        }

        // Private Functions

        private void SubscribeToLine(ProductionLineViewModel line) {
            line.SubLineShowRequested += OnSubLineShowRequested;
        }

        private async Task ResetSelectedRecipeAsync() {
            await Task.Yield();
            SelectedRecipe = null;
        }

        private async Task SearchForRecipesAsync() {
            FilteredRecipes = await searchService.Search(
                FilteredByItem, RecipeSearchTerm, RecipeViewModel.GetSearchSelectors()
            );
        }
    }
}
