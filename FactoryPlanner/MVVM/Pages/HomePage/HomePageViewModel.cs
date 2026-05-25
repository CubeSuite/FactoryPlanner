using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers.Provider;

namespace FactoryPlanner.MVVM.Pages
{
    public partial class HomePageViewModel : ObservableObject
    {
        // ToDo: Online Games
        // Services & Stores
        private readonly IServiceProvider serviceProvider;
        private readonly IGameManager gameManager;

        // Properties
        public ObservableCollection<GameViewModel> Games { get; }

        // Constructors

        public HomePageViewModel(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
            gameManager = serviceProvider.GetRequiredService<IGameManager>();

            Games = new ObservableCollection<GameViewModel>();
            PopulateGames();
        }

        // Listeners

        private void OnGameDeleted(GameViewModel game) {
            Games.Remove(game);
        }

        // Commands

        [RelayCommand]
        private void AddNewGame() {
            if (gameManager.CreateAndAdd(out Game game)) {
                AddGameToList(game);
            }
        }

        // Private Functions

        private void PopulateGames() {
            Games.Clear();
            foreach (Game game in gameManager.GetAll()) {
                AddGameToList(game);
            }
        }

        private void AddGameToList(Game game) {
            GameViewModel viewModel = new GameViewModel(game, serviceProvider);
            viewModel.Deleted += OnGameDeleted;
            Games.Add(viewModel);
        }
    }
}
