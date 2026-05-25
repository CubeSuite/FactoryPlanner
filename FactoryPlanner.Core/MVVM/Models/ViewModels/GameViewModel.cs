using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Services;
using FactoryPlanner.Services.Interfaces;
using FactoryPlanner.Stores;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public partial class GameViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IGameManager gameManager;
        private readonly IUserSettings userSettings;
        private readonly IDialogService dialogService;

        // Fields
        private Game _game;

        // Properties
        public int ID => _game.ID;
        
        public string Name {
            get => _game.Name;
            set => _game.Name = value;
        }

        public bool IsActive {
            get => userSettings.ActiveGame == ID;
            set { if (value) userSettings.ActiveGame = ID; }
        }

        [ObservableProperty]
        public partial string DeleteConfirmation { get; set; }
        public bool AllowDelete => DeleteConfirmation == DeleteString.Replace("'", "");
        public string DeleteString => $"'Delete {Name}'";

        // Constructors

        public GameViewModel(Game game, IServiceProvider serviceProvider) {
            gameManager = serviceProvider.GetRequiredService<IGameManager>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();

            _game = game;
        }

        // Events

        public event Action<GameViewModel>? Deleted;

        // Listeners

        partial void OnDeleteConfirmationChanged(string value) {
            OnPropertyChanged(nameof(AllowDelete));
        }

        // Commands

        [RelayCommand]
        private async Task DeleteGame() {
            if (!await dialogService.Confirm(
                $"Delete {Name}?",
                $"Are you certain you want to delete the game '{Name}'?\n\n" +
                $"This will delete all items, recipes etc. that are associated with that game.")) return;

            // ToDo: Delete Items, Machines, Recipes and Boosts for game 
            gameManager.TryDelete(_game);
            Deleted?.Invoke(this);
        }

        // Public Functions

        public OperationResult TrySave() {
            return gameManager.TryUpdate(_game);
        }
    }
}
