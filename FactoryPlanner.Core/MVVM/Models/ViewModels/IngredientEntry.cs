using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public partial class IngredientEntry : ObservableObject
    {
        // Fields
        private int _quantity;
 
        // Properties
        public RecipeViewModel Parent { get; }
        public Item Item { get; set; }

        [ObservableProperty]
        public partial int Quantity { get; set; }
        //public int Quantity
        //{
        //    get => _quantity;
        //    set => SetProperty(ref _quantity, value);
        //}

        public string Rate => $"{Quantity * 60 / Parent.CraftTime:0.##}/m";

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
                OnPropertyChanged(nameof(Rate));
            }
        }

        partial void OnQuantityChanged(int value) {
            OnPropertyChanged(nameof(Rate));
        }
    }
}
