using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Stores.UserSettings;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using static FactoryPlanner.Core.MVVM.Models.ProductionPort;

namespace FactoryPlanner.MVVM.Views
{
    public sealed partial class ProductionLineView : UserControl
    {
        // Fields
        private bool isDragging = false;
        private Point initialPosition;
        private Point dragStartPoint;

        // Properties
        private SubLineViewModel ViewModel => (SubLineViewModel)DataContext;

        // Constructor

        public ProductionLineView() {
            InitializeComponent();
        }

        // Events

        public event EventHandler<ProductionPortViewModel>? PortPressed;

        // Listeners

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e) {
            if (e.NewValue is SubLineViewModel subLine) {
                subLine.LineDeletionRequested += OnLineDeletionFlyoutsClose;
            }
        }

        private void OnLineDeletionFlyoutsClose(SubLineViewModel subLine) {
            OptionsFlyout.Hide();
            DeleteFlyout.Hide();
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e) {
            if (DataContext is not SubLineViewModel subLineVM) return;

            PointerPoint pointer = e.GetCurrentPoint(Parent as UIElement);
            if (pointer.Properties.IsLeftButtonPressed) {
                isDragging = true;
                dragStartPoint = pointer.Position;
                initialPosition = subLineVM.Position;
                CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e) {
            if (!isDragging || DataContext is not SubLineViewModel subLineVM) return;

            PointerPoint pointer = e.GetCurrentPoint(Parent as UIElement);
            Point currentPoint = pointer.Position;

            subLineVM.Position = new Point(
                initialPosition.X + (currentPoint.X - dragStartPoint.X),
                initialPosition.Y + (currentPoint.Y - dragStartPoint.Y)
            );

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
            if (sender is not UIElement element || DataContext is not SubLineViewModel subLineVM) return;

            int portIndex = portType switch {
                PortType.Input => InputsContainer.GetElementIndex(element),
                PortType.Output => OutputsContainer.GetElementIndex(element),
                _ => -1
            };

            if (portIndex < 0) return;

            ProductionPortViewModel? portVM = portType switch {
                PortType.Input => subLineVM.InputPorts[portIndex],
                PortType.Output => subLineVM.OutputPorts[portIndex],
                _ => null
            };

            if (portVM == null) {
                Debug.Assert(false, $"Could not handle unknown port type '{portType}'");
                return;
            }

            portVM.VisualIndex = portIndex;
            PortPressed?.Invoke(this, portVM);
        }
    }
}


