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
    }
}
