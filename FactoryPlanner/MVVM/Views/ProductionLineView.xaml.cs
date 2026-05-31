using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Stores.UserSettings;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FactoryPlanner.MVVM.Views
{
    public sealed partial class ProductionLineView : UserControl
    {
        // Fields
        private bool isDragging = false;
        private Point initialPosition;
        private Point dragStartPoint;

        // Properties

        private ProductionLineViewModel ViewModel => (ProductionLineViewModel)DataContext;

        // Constructors

        public ProductionLineView() {
            InitializeComponent();
        }

        // Events

        public event EventHandler<ProductionPortViewModel>? PortPressed;

        // Listeners

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e) {
            if (e.NewValue is ProductionLineViewModel line) {
                line.LineDeletionRequested += OnFlyoutButtonClicked;
            }
        }

        private void OnFlyoutButtonClicked(ProductionLineViewModel line) {
            OptionsFlyout.Hide();
            DeleteFlyout.Hide();
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e) {
            if (DataContext is not ProductionLineViewModel lineVM) return;

            PointerPoint pointer = e.GetCurrentPoint(Parent as UIElement);
            if (pointer.Properties.IsLeftButtonPressed) {
                isDragging = true;
                dragStartPoint = pointer.Position;
                initialPosition = lineVM.Position;
                CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e) {
            if (!isDragging || DataContext is not ProductionLineViewModel lineVM) return;

            PointerPoint pointer = e.GetCurrentPoint(Parent as UIElement);
            Point currentPoint = pointer.Position;

            double deltaX = currentPoint.X - dragStartPoint.X;
            double deltaY = currentPoint.Y - dragStartPoint.Y;

            Point newPosition = new Point(
                initialPosition.X + deltaX,
                initialPosition.Y + deltaY
            );

            lineVM.Position = newPosition;
            e.Handled = true;
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e) {
            if (!isDragging) return;

            isDragging = false;
            ReleasePointerCapture(e.Pointer);
            ViewModel.SaveChanges();
            e.Handled = true;
        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            ViewModel.RaiseShowLineRequested();
        }

        private void OnInputPointerPressed(object sender, PointerRoutedEventArgs e) {
            if (e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed) {
                RaisePortPressed(sender, PortType.Input);
                e.Handled = true;
            }
        }

        private void OnOutputPointerPressed(object sender, PointerRoutedEventArgs e) {
            if (e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed) {
                RaisePortPressed(sender, PortType.Output);
                e.Handled = true;
            }
        }

        private void OnNameBoxLostFocus(object sender, RoutedEventArgs e) {
            ViewModel.SaveChanges();
        }

        private void OnIconBoxSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs e) {
            ViewModel.IconPath = ((KeyValuePair<string, string>)e.SelectedItem).Value;
            ViewModel.IconSearchTerm = "";
        }

        // Private Functions

        private void RaisePortPressed(object sender, PortType portType) {
            if (sender is not UIElement element || DataContext is not ProductionLineViewModel lineVM) return;

            int portIndex = portType switch {
                PortType.Input => InputsContainer.GetElementIndex(element),
                PortType.Output => OutputsContainer.GetElementIndex(element),
                _ => -1
            };

            if (portIndex < 0) return;

            ProductionPortViewModel? portVM = portType switch {
                PortType.Input => lineVM.InputPorts[portIndex],
                PortType.Output => lineVM.OutputPorts[portIndex],
                _ => null
            };

            if (portVM == null) {
                Debug.Assert(false, $"Could not handle unknown port type '{portType.GetDescription()}'");
                return;
            }

            portVM.VisualIndex = portIndex;
            PortPressed?.Invoke(this, portVM);
        }
    }
}
