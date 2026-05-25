using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Services;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FactoryPlanner.MVVM.Pages
{
    public partial class RecipesPageViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IRecipeManager recipeManager;
        private readonly ISearchService searchService;

        // Fields
        private List<RecipeViewModel> recipes;
        private CancellationTokenSource? searchCancellationTokenSource;
        private readonly DispatcherQueue dispatcherQueue;

        // Properties

        [ObservableProperty]
        public partial string SearchTerm { get; set; }

        public ObservableCollection<RecipeViewModel> FilteredRecipes { get; }

        // Constructors

        public RecipesPageViewModel(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            recipeManager = serviceProvider.GetRequiredService<IRecipeManager>();
            searchService = serviceProvider.GetRequiredService<ISearchService>();

            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            recipes = new List<RecipeViewModel>();
            foreach(Recipe recipe in recipeManager.GetAll()) {
                RecipeViewModel viewModel = CreateRecipeViewModel(recipe, serviceProvider);
                recipes.Add(viewModel);
            }

            FilteredRecipes = new ObservableCollection<RecipeViewModel>();

            SearchTerm = "";
        }

        // Listeners

        partial void OnSearchTermChanged(string value) {
            searchCancellationTokenSource?.Cancel();
            searchCancellationTokenSource = new CancellationTokenSource();
            SearchRecipesWithDebounceAsync(searchCancellationTokenSource.Token);
        }

        private void OnRecipeDeleted(RecipeViewModel deletedRecipe) {
            recipes.Remove(deletedRecipe);
            if (FilteredRecipes.Contains(deletedRecipe)) {
                FilteredRecipes.Remove(deletedRecipe);
            }
        }

        // Commands

        [RelayCommand]
        private void CreateNewRecipe() {
            Recipe recipe = recipeManager.CreateAndAdd();
            RecipeViewModel viewModel = CreateRecipeViewModel(recipe, serviceProvider);
            recipes.Add(viewModel);
            AddRecipeToGUIAsync(viewModel);
            SearchTerm = "";
        }

        // Private Functions

        private RecipeViewModel CreateRecipeViewModel(Recipe recipe, IServiceProvider serviceProvider) {
            RecipeViewModel viewModel = new RecipeViewModel(recipe, serviceProvider);
            viewModel.RecipeDeleted += OnRecipeDeleted;
            return viewModel;
        }

        private async void SearchRecipesWithDebounceAsync(CancellationToken cancellationToken) {
            try {
                await Task.Delay(250, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                IEnumerable<RecipeViewModel> searchResults = await searchService.Search(
                    recipes, SearchTerm, RecipeViewModel.GetSearchSelectors()
                );

                if (cancellationToken.IsCancellationRequested) return;
                dispatcherQueue.TryEnqueue(() => {
                    FilteredRecipes.Clear();
                    foreach(RecipeViewModel recipe in searchResults) {
                        FilteredRecipes.Add(recipe);
                    }

                    OnPropertyChanged(nameof(FilteredRecipes));
                });
            }
            catch (TaskCanceledException) {}
        }

        private async void AddRecipeToGUIAsync(RecipeViewModel recipe) {
            if (await searchService.AppearsInSearch(
                recipe, SearchTerm, RecipeViewModel.GetSearchSelectors()
            )) {
                dispatcherQueue.TryEnqueue(() => {
                    FilteredRecipes.Add(recipe);
                });
            }
        }
    }
}
