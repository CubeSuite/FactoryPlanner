using FactoryPlanner.Core.MVVM.Models.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FactoryPlanner.MVVM.Pages
{
    public sealed partial class FactoryPlannerPage : Page
    {
        // Fields
        private bool isPanning = false;
        private Point panStartPoint;
        private TranslateTransform canvasTranslate = new TranslateTransform();
        private ScaleTransform canvasScale = new ScaleTransform();
        private TransformGroup canvasTransform = new TransformGroup();

        // Properties
        public FactoryPlannerPageViewModel ViewModel => (FactoryPlannerPageViewModel)DataContext;

        // Constructors

        public FactoryPlannerPage() {
            InitializeComponent();
            Loaded += OnFactoryPlannerPageLoaded;
            SizeChanged += OnFactoryPlannerPageSizeChanged;

            canvasTransform.Children.Add(canvasScale);
            canvasTransform.Children.Add(canvasTranslate);
            MainCanvas.RenderTransform = canvasTransform;
        }

        // Page Listeners

        private void OnFactoryPlannerPageLoaded(object sender, RoutedEventArgs e) {
            if (ViewModel != null) {
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                ViewModel.ProductionLine.Steps.CollectionChanged += OnStepsCollectionChanged;
            }

            RenderGrid();
        }

        private void OnFactoryPlannerPageSizeChanged(object sender, SizeChangedEventArgs e) {
            RenderGrid();
        }

        // VM Listeners

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(FactoryPlannerPageViewModel.RenderGrid) || 
                e.PropertyName == nameof(FactoryPlannerPageViewModel.GridSize)
            ) {
                RenderGrid();
            }
        }

        private void OnStepsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null) {
                foreach (object item in e.NewItems) {
                    if (item is ProductionStepViewModel stepVM) {
                        _ = UpdateStepPosition(stepVM);
                        stepVM.PropertyChanged += OnStepPropertyChanged;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null) {
                foreach (object item in e.OldItems) {
                    if (item is ProductionStepViewModel stepVM) {
                        stepVM.PropertyChanged -= OnStepPropertyChanged;
                    }
                }
            }
        }

        private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(ProductionStepViewModel.Position) && 
                sender is ProductionStepViewModel stepVM
            ) {
                _ = UpdateStepPosition(stepVM);
            }
        }

        // Canvas Listeners

        private void OnCanvasClicked(object sender, PointerRoutedEventArgs e) {
            PointerPoint pointer = e.GetCurrentPoint(this);
            Point position = pointer.Position;

            if (pointer.Properties.IsMiddleButtonPressed) {
                HandleMiddleMousePanning(sender, e);
                return;
            }

            if (!isPanning && (e.OriginalSource == (object)MainCanvas || e.OriginalSource == (object)GridCanvas)) {
                ShowAddItemPopup(e);
            }
        }

        private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e) {
            if (!isPanning) return;

            PointerPoint pointer = e.GetCurrentPoint(this);
            Point currentPoint = pointer.Position;

            canvasTranslate.X += (currentPoint.X - panStartPoint.X);
            canvasTranslate.Y += (currentPoint.Y - panStartPoint.Y);
            panStartPoint = currentPoint;
            
            RenderGrid();
            e.Handled = true;
        }

        private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e) {
            if (!isPanning) return;

            isPanning = false;
            (sender as UIElement)?.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void OnCanvasPointerWheelChanged(object sender, PointerRoutedEventArgs e) {
            PointerPoint pointer = e.GetCurrentPoint(this);
            int delta = pointer.Properties.MouseWheelDelta;

            double zoomFactor = delta > 0 ? 1.1 : 0.9;
            Point cursorPosition = pointer.Position;

            double oldScale = canvasScale.ScaleX;
            double newScale = Math.Clamp(oldScale * zoomFactor, 0.1, 10.0);

            canvasScale.ScaleX = newScale;
            canvasScale.ScaleY = newScale;

            double scaleChange = newScale - oldScale;
            canvasTranslate.X -= cursorPosition.X * scaleChange;
            canvasTranslate.Y -= cursorPosition.Y * scaleChange;

            RenderGrid();
            e.Handled = true;
        }

        // Private Functions

        private void RenderGrid() {
            GridCanvas.Children.Clear();
            if (ViewModel == null || !ViewModel.RenderGrid) return;

            int gridSize = ViewModel.GridSize;
            double width = GridCanvas.ActualWidth;
            double height = GridCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            double scaledGridSize = gridSize * canvasScale.ScaleX;
            double offsetX = canvasTranslate.X % scaledGridSize;
            double offsetY = canvasTranslate.Y % scaledGridSize;

            SolidColorBrush gridBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray) { Opacity = 0.2 };

            for (double x = offsetX; x <= width; x += scaledGridSize) {
                GridCanvas.Children.Add(new Line {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                });
            }

            for (double y = offsetY; y <= height; y += scaledGridSize) {
                GridCanvas.Children.Add(new Line {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                });
            }
        }

        private async Task UpdateStepPosition(ProductionStepViewModel stepVM) {
            UIElement? container = StepsItemsControl.ContainerFromItem(stepVM) as UIElement;
            if (container == null) {
                await StepsItemsControl.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    container = StepsItemsControl.ContainerFromItem(stepVM) as UIElement;
                });
            }

            if (container != null) {
                Canvas.SetLeft(container, stepVM.Position.X);
                Canvas.SetTop(container, stepVM.Position.Y);
            }
        }

        private void HandleMiddleMousePanning(object sender, PointerRoutedEventArgs e) {
            PointerPoint pointer = e.GetCurrentPoint(this);
            Point position = pointer.Position;

            if (e.OriginalSource is Button or ToggleButton) return;

            isPanning = true;
            panStartPoint = position;
            (sender as UIElement)?.CapturePointer(e.Pointer);

            e.Handled = true;
        }

        private void ShowAddItemPopup(PointerRoutedEventArgs e) {
            PointerPoint pointer = e.GetCurrentPoint(this);
            Point viewportPosition = pointer.Position;

            Point canvasPosition = new Point(
                (viewportPosition.X - canvasTranslate.X) / canvasScale.ScaleX,
                (viewportPosition.Y - canvasTranslate.Y) / canvasScale.ScaleY
            );

            if (ViewModel != null) {
                ViewModel.CanvasClickCommand.Execute((viewportPosition, canvasPosition));
            }
        }
    }
}
