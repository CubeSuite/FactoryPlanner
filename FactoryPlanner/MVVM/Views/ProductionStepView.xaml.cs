using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FactoryPlanner.MVVM.Views
{
    public sealed partial class ProductionStepView : UserControl
    {
        // Fields
        private bool isDragging = false;
        private Point initialPosition;
        private Point dragStartPoint;

        // Constructors

        public ProductionStepView() {
            InitializeComponent();
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
        }

        // Events

        public event EventHandler<ProductionStepPortPressedEventArgs>? PortPressed;

        // Listeners

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e) {
            if (DataContext is not ProductionStepViewModel stepVM) return;

            PointerPoint pointer = e.GetCurrentPoint(Parent as UIElement);
            if (pointer.Properties.IsLeftButtonPressed) {
                isDragging = true;
                dragStartPoint = pointer.Position;
                initialPosition = stepVM.Position;
                CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e) {
            if (!isDragging || DataContext is not ProductionStepViewModel stepVM) return;

            PointerPoint pointer = e.GetCurrentPoint(Parent as UIElement);
            Point currentPoint = pointer.Position;

            double deltaX = currentPoint.X - dragStartPoint.X;
            double deltaY = currentPoint.Y - dragStartPoint.Y;

            Point newPosition = new Point(
                initialPosition.X + deltaX,
                initialPosition.Y + deltaY
            );

            stepVM.Position = newPosition;
            e.Handled = true;
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e) {
            if (!isDragging) return;

            isDragging = false;
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void OnInputPointerPressed(object sender, PointerRoutedEventArgs e) {
            RaiseProductionStepPortPressed(sender, PortType.Input);
            e.Handled = true;
        }

        private void OnOutputPointerPressed(object sender, PointerRoutedEventArgs e) {
            RaiseProductionStepPortPressed(sender, PortType.Output);
            e.Handled = true;
        }

        private void RaiseProductionStepPortPressed(object sender, PortType portType) {
            if (sender is not UIElement element || DataContext is not ProductionStepViewModel stepVM) return;

            int portIndex = portType switch {
                PortType.Input => InputsContainer.GetElementIndex(element),
                PortType.Output => OutputsContainer.GetElementIndex(element),
                _ => -1
            };

            if (portIndex < 0) return;

            PortPressed?.Invoke(this, new ProductionStepPortPressedEventArgs(stepVM, portType, portIndex));
        }
    }

    public class ProductionStepPortPressedEventArgs : EventArgs 
    {
        // Properties
        public ProductionStepViewModel ProductionStepVM { get; set; }
        public PortType PortType { get; set; }
        public int PortIndex { get; set; }

        // Constructors

        public ProductionStepPortPressedEventArgs(ProductionStepViewModel productionStep, PortType portType, int portIndex) {
            ProductionStepVM = productionStep;
            PortType = portType;
            PortIndex = portIndex;
        }
    }
}
