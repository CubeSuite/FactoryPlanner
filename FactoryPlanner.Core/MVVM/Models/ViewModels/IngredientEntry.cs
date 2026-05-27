using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public partial class IngredientEntry : ObservableObject
    {
        // Properties
        public RecipeViewModel Parent { get; }
        public Item Item { get; set; }

        [ObservableProperty]
        public partial int Quantity { get; set; }
        public double Rate => Quantity * 60 / Parent.CraftTime;
        public string RateString => $"{Rate:0.##}/m";

        // Constructors

        public IngredientEntry(RecipeViewModel parent, Item item, int quantity) {
            Parent = parent;
            Item = item;
            Quantity = quantity;

            Parent.PropertyChanged += OnRecipePropertyChanged;
        }

        // Listeners

        private void OnRecipePropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(Recipe.CraftTime)) {
                OnPropertyChanged(nameof(RateString));
            }
        }

        partial void OnQuantityChanged(int value) {
            OnPropertyChanged(nameof(RateString));
        }
    }
}
