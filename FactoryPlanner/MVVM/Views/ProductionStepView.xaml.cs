using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.MVVM.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace FactoryPlanner.MVVM.Views
{
    public sealed partial class ProductionStepView : DraggableNodeControl
    {
        // Properties
        private ProductionStepViewModel ViewModel => (ProductionStepViewModel)DataContext;
        public override ItemsRepeater InputsRepeater => NodeView.InputsRepeater;
        public override ItemsRepeater OutputsRepeater => NodeView.OutputsRepeater;

        // Constructors

        public ProductionStepView() {
            InitializeComponent();
        }

        // Abstract Overrides
        protected override IProductionNode? GetNode() => DataContext as IProductionNode;

        // Listeners
        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e) {
            if (e.NewValue is ProductionStepViewModel step) {
                step.DuplicateRequested += OnFlyoutButtonClicked;
                step.DeleteRequested += OnFlyoutButtonClicked;
            }
        }

        private void OnFlyoutButtonClicked(ProductionStepViewModel step) {
            NodeView.HideFlyouts();
        }

        private void OnNumMachinesBoxLostFocus(object sender, RoutedEventArgs e) {
            ViewModel.LastTypedNumMachines = ViewModel.NumMachines;
            ViewModel.UpdateConnections();
            ViewModel.SaveChanges();
        }
    }
}

