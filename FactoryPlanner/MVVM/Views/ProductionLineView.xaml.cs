using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.MVVM.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using Windows.Foundation;

namespace FactoryPlanner.MVVM.Views
{
    public sealed partial class ProductionLineView : DraggableNodeControl
    {
        // Properties
        private SubLineViewModel ViewModel => (SubLineViewModel)DataContext;
        public ItemsRepeater InputsRepeater => NodeView.InputsRepeater;
        public ItemsRepeater OutputsRepeater => NodeView.OutputsRepeater; // ToDo: replace with getters

        // Constructors
        public ProductionLineView() {
            InitializeComponent();
        }

        // Abstract Overrides
        protected override IProductionNode? GetNode() => DataContext as IProductionNode;
        protected override ItemsRepeater GetInputsRepeater() => NodeView.InputsRepeater;
        protected override ItemsRepeater GetOutputsRepeater() => NodeView.OutputsRepeater;

        // Listeners
        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e) {
            if (e.NewValue is SubLineViewModel subLine) {
                subLine.LineDeletionRequested += OnLineDeletionFlyoutsClose;
            }
        }

        private void OnLineDeletionFlyoutsClose(SubLineViewModel subLine) {
            NodeView.HideFlyouts();
        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            ViewModel.RaiseShowLineRequested();
        }

        private void OnNameBoxLostFocus(object sender, RoutedEventArgs e) {
            ViewModel.SaveChanges();
        }

        private void OnIconBoxSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs e) {
            ViewModel.IconPath = ((KeyValuePair<string, string>)e.SelectedItem).Value;
            ViewModel.IconSearchTerm = "";
        }
    }
}



