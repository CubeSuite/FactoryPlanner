using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public partial class RecipeViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IItemManager itemManager;
        private readonly IMachineManager machineManager;
        private readonly IRecipeManager recipeManager;
        private readonly IDialogService dialogService;

        // Members
        private Recipe _recipe;

        // Properties
        public IEnumerable<Item> AllItems => itemManager.GetAll();
        public IEnumerable<Machine> AllMachines => machineManager.GetAll();
        
        public Recipe Recipe => _recipe;
        public int ID => _recipe.ID;

        public string Name {
            get => _recipe.Name;
            set {
                if (_recipe.Name == value) return;
                _recipe.Name = value;
                UpdateRecipe();
            }
        }

        public double CraftTime {
            get => _recipe.CraftTime;
            set {
                if (_recipe.CraftTime == value) return;
                _recipe.CraftTime = value;
                UpdateRecipe();
                OnPropertyChanged(nameof(CraftTime));
            }
        }

        public int MachineID {
            get => _recipe.MachineId;
            set {
                if (_recipe.MachineId == value) return;
                _recipe.MachineId = value;
                UpdateRecipe();
            }
        }

        public Machine Machine {
            get {
                if (machineManager.TryGet(MachineID, out Machine machine)) return machine;
                else return new Machine() { Name = "Unknown Machine" };
            }
            set {
                if (value == null) return;
                if (_recipe.MachineId == value.ID) return;
                _recipe.MachineId = value.ID;
                UpdateRecipe();
                OnPropertyChanged(nameof(MachineID));
            }
        }

        public Dictionary<Item, int> Inputs => SubstituteItems(_recipe.Inputs);
        public Dictionary<Item, int> Outputs => SubstituteItems(_recipe.Outputs);

        public ObservableCollection<IngredientEntry> InputEntries { get; private set; }
        public ObservableCollection<IngredientEntry> OutputEntries { get; private set; }

        [ObservableProperty]
        public partial Item? SelectedIngredient { get; set; }
        
        [ObservableProperty]
        public partial Item? SelectedOutput { get; set; }

        // Events

        public event Action<RecipeViewModel>? RecipeDeleted;

        // Constructors

        public RecipeViewModel(Recipe recipe, IServiceProvider serviceProvider) {
            itemManager = serviceProvider.GetRequiredService<IItemManager>();
            machineManager = serviceProvider.GetRequiredService<IMachineManager>();
            recipeManager = serviceProvider.GetRequiredService<IRecipeManager>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();

            _recipe = recipe;

            InputEntries = new ObservableCollection<IngredientEntry>();
            OutputEntries = new ObservableCollection<IngredientEntry>();

            LoadIngredientEntries();
        }

        public RecipeViewModel(int recipeId, IServiceProvider serviceProvider) {
            itemManager = serviceProvider.GetRequiredService<IItemManager>();
            machineManager = serviceProvider.GetRequiredService<IMachineManager>();
            recipeManager = serviceProvider.GetRequiredService<IRecipeManager>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();

            recipeManager.TryGet(recipeId, out _recipe);

            InputEntries = new ObservableCollection<IngredientEntry>();
            OutputEntries = new ObservableCollection<IngredientEntry>();

            LoadIngredientEntries();
        }

        // Commands

        [RelayCommand]
        private async Task DeleteRecipe() {
            if (!await dialogService.Confirm($"Delete '{Name}' Recipe?", "This cannot be undone.")) return;

            if (recipeManager.TryDelete(Recipe)) {
                RecipeDeleted?.Invoke(this);
            }
        }

        [RelayCommand]
        private void AddIngredient() {
            if (SelectedIngredient == null) return;
            _recipe.Inputs.Add(SelectedIngredient.ID, 1);
            InputEntries.Add(CreateInputEntry(SelectedIngredient, 1));
            UpdateRecipe();
            OnPropertyChanged(nameof(Inputs));
        }

        [RelayCommand]
        private void AddOutput() {
            if (SelectedOutput == null) return;
            _recipe.Outputs.Add(SelectedOutput.ID, 1);
            if (string.IsNullOrEmpty(Name)) {
                Name = SelectedOutput.Name;
                OnPropertyChanged(nameof(Name));
            }

            OutputEntries.Add(CreateOutputEntry(SelectedOutput, 1));
            UpdateRecipe();
            OnPropertyChanged(nameof(Outputs));
        }

        [RelayCommand]
        private void RemoveIngredient(IngredientEntry entry) {
            if (entry == null) return;
            _recipe.Inputs.Remove(entry.Item.ID);
            InputEntries.Remove(entry);
            UpdateRecipe();
            OnPropertyChanged(nameof(Inputs));
        }

        [RelayCommand]
        private void RemoveOutput(IngredientEntry entry) {
            if (entry == null) return;
            _recipe.Outputs.Remove(entry.Item.ID);
            OutputEntries.Remove(entry);
            UpdateRecipe();
            OnPropertyChanged(nameof(Outputs));
        }

        // Public Functions

        public static Func<RecipeViewModel, object?>[] GetSearchSelectors() => [
            recipe => recipe.Name,
            recipe => recipe.Outputs.Keys.Select(item => item.Name)
        ];

        // Private Functions

        private Dictionary<Item, int> SubstituteItems(Dictionary<int, int> itemsAndAmounts) {
            return itemManager.TryGetRange(itemsAndAmounts.Keys.ToArray(), out Item[] items)
                 ? items.ToDictionary(item => item, item => itemsAndAmounts[item.ID])
                 : [];
        }

        private void LoadIngredientEntries() {
            InputEntries.Clear();
            OutputEntries.Clear();

            foreach (KeyValuePair<Item, int> input in Inputs) {
                InputEntries.Add(CreateInputEntry(input.Key, input.Value));
            }

            foreach (KeyValuePair<Item, int> output in Outputs) {
                OutputEntries.Add(CreateOutputEntry(output.Key, output.Value));
            }
        }

        private IngredientEntry CreateInputEntry(Item item, int quantity) {
            IngredientEntry entry = new IngredientEntry(this, item, quantity);
            entry.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(IngredientEntry.Quantity) && s is IngredientEntry ing) {
                    UpdateIngredientQuantity(ing.Item.ID, ing.Quantity);
                }
            };

            return entry;
        }

        private IngredientEntry CreateOutputEntry(Item item, int quantity) {
            IngredientEntry entry = new IngredientEntry(this, item, quantity);
            entry.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(IngredientEntry.Quantity) && s is IngredientEntry ing) {
                    UpdateOutputQuantity(ing.Item.ID, ing.Quantity);
                }
            };

            return entry;
        }

        private void UpdateIngredientQuantity(int itemId, int quantity) {
            if (_recipe.Inputs.ContainsKey(itemId)) {
                _recipe.Inputs[itemId] = quantity;
                UpdateRecipe();
                OnPropertyChanged(nameof(Inputs));
            }
        }

        private void UpdateOutputQuantity(int itemId, int quantity) {
            if (_recipe.Outputs.ContainsKey(itemId)) {
                _recipe.Outputs[itemId] = quantity;
                UpdateRecipe();
                OnPropertyChanged(nameof(Outputs));
            }
        }

        private void UpdateRecipe() {
            recipeManager.TryUpdate(Recipe);
        }
    }
}
