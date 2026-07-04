using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using Windows.Foundation;
using static FactoryPlanner.Core.MVVM.Models.ProductionPort;

namespace FactoryPlanner.MVVM.Controls
{
    public abstract class DraggableNodeControl : UserControl
    {
        // Fields
        private bool isDragging = false;
        private Point initialPosition;
        private Point dragStartPoint;

        // Properties
        public abstract ItemsRepeater InputsRepeater { get; }
        public abstract ItemsRepeater OutputsRepeater { get; }

        // Events
        public event EventHandler<ProductionPortViewModel>? PortPressed;

        // Abstract Functions

        protected abstract IProductionNode? GetNode();

        // Drag Listeners

        protected void OnPointerPressed(object sender, PointerRoutedEventArgs e) {
            IProductionNode? node = GetNode();
            if (node == null) return;

            PointerPoint pointer = e.GetCurrentPoint(Parent as UIElement);
            if (pointer.Properties.IsLeftButtonPressed) {
                isDragging = true;
                dragStartPoint = pointer.Position;
                initialPosition = node.Position;
                CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        protected void OnPointerMoved(object sender, PointerRoutedEventArgs e) {
            if (!isDragging) return;
            IProductionNode? node = GetNode();
            if (node == null) return;

            PointerPoint pointer = e.GetCurrentPoint(Parent as UIElement);
            Point current = pointer.Position;

            node.Position = new Point(
                initialPosition.X + (current.X - dragStartPoint.X),
                initialPosition.Y + (current.Y - dragStartPoint.Y)
            );

            e.Handled = true;
        }

        protected void OnPointerReleased(object sender, PointerRoutedEventArgs e) {
            if (!isDragging) return;

            isDragging = false;
            ReleasePointerCapture(e.Pointer);
            GetNode()?.SaveChanges();
            e.Handled = true;
        }

        // Port Listeners

        protected void OnInputPointerPressed(object sender, PointerRoutedEventArgs e) {
            if (e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed) {
                RaisePortPressed(sender, PortType.Input);
                e.Handled = true;
            }
        }

        protected void OnOutputPointerPressed(object sender, PointerRoutedEventArgs e) {
            if (e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed) {
                RaisePortPressed(sender, PortType.Output);
                e.Handled = true;
            }
        }

        protected void RaisePortPressed(object sender, PortType portType) {
            if (sender is not UIElement element) return;
            IProductionNode? node = GetNode();
            if (node == null) return;

            ItemsRepeater repeater = portType switch {
                PortType.Input => InputsRepeater,
                PortType.Output => OutputsRepeater,
                _ => null!
            };

            UIElement? repeaterChild = GetRepeaterChild(element, repeater);
            int portIndex = repeaterChild != null ? repeater.GetElementIndex(repeaterChild) : -1;

            if (portIndex < 0) return;

            ProductionPortViewModel? portVM = portType switch {
                PortType.Input => node.InputPorts[portIndex],
                PortType.Output => node.OutputPorts[portIndex],
                _ => null
            };

            if (portVM == null) {
                Debug.Assert(false, $"Could not handle unknown port type '{portType}'");
                return;
            }

            portVM.VisualIndex = portIndex;
            PortPressed?.Invoke(this, portVM);
        }

        private static UIElement? GetRepeaterChild(UIElement element, ItemsRepeater repeater) {
            DependencyObject? current = element;
            while (current != null) {
                if (VisualTreeHelper.GetParent(current) is ItemsRepeater parent && parent == repeater)
                    return current as UIElement;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
