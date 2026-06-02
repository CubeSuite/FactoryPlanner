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
        public ItemsRepeater InputsRepeater => NodeView.InputsRepeater;
        public ItemsRepeater OutputsRepeater => NodeView.OutputsRepeater; // ToDo: replace with getters

        // Constructors

        public ProductionStepView() {
            InitializeComponent();
        }

        // Abstract Overrides
        protected override IProductionNode? GetNode() => DataContext as IProductionNode;
        protected override ItemsRepeater GetInputsRepeater() => NodeView.InputsRepeater;
        protected override ItemsRepeater GetOutputsRepeater() => NodeView.OutputsRepeater;

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

