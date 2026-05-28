using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.MVVM.Views;
using Microsoft.UI;
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
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

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

        private Point portPosition;
        private Point pointerPosition;
        private Path? connectionPath;

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

            portPosition = new Point(0, 0);
            pointerPosition = new Point(0, 0);
        }

        // Page Listeners

        private void OnFactoryPlannerPageLoaded(object sender, RoutedEventArgs e) {
            if (ViewModel != null) {
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                ViewModel.ProductionLineVM.Steps.CollectionChanged += OnStepsCollectionChanged;
                foreach(ProductionStepViewModel stepVM in ViewModel.ProductionLineVM.Steps) {
                    _ = UpdateStepPosition(stepVM);
                    stepVM.PropertyChanged += OnStepPropertyChanged;
                }

                Color lineColour = ViewModel.UserSettings.DarkMode ? Colors.White : Colors.Black;
                SolidColorBrush lineBrush = new SolidColorBrush(lineColour);
                connectionPath = new Path() {
                    Stroke = lineBrush,
                    StrokeThickness = 2,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                MainCanvas.Children.Insert(0, connectionPath);
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
            else if (e.PropertyName == nameof(FactoryPlannerPageViewModel.IsDrawingConnection)) {
                if (!ViewModel.IsDrawingConnection) connectionPath?.Data = null;
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
            if (FocusManager.GetFocusedElement(XamlRoot) is TextBox) {
                MainCanvas.Focus(FocusState.Programmatic);
                e.Handled = true;
                return;
            }

            PointerPoint pointer = e.GetCurrentPoint(this);
            Point position = pointer.Position;

            if (pointer.Properties.IsMiddleButtonPressed) {
                HandleMiddleMousePanning(sender, e);
                return;
            }

            if (pointer.Properties.IsRightButtonPressed) {
                ViewModel.IsDrawingConnection = false;
                connectionPath?.Data = null;
                ViewModel.StartPort = null;
                return;
            }

            if (!isPanning && (e.OriginalSource == (object)MainCanvas || e.OriginalSource == (object)GridCanvas || e.OriginalSource == (object?)connectionPath)) {
                ShowAddItemPopup(e);
                RecipeBox.Focus(FocusState.Keyboard);
            }
        }

        private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e) {
            if (isPanning) HandlePanMove(e);
            else if (ViewModel.IsDrawingConnection) DrawConnectionInProgress(e);
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

        private void OnProductionStepPortPressed(object sender, ProductionPortViewModel port) {
            if (sender is not ProductionStepView stepView) return;

            ViewModel.IsDrawingConnection = !ViewModel.IsDrawingConnection;

            if (ViewModel.IsDrawingConnection) {
                if (connectionPath != null) connectionPath.Data = null;
                HandleStartDrawingConnection(stepView, port);
            }
            else {
                HandleEndDrawingConnection(port);
            }
        }

        private void OnRecipeBoxSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs e) {
            ViewModel.SelectedRecipe = (RecipeViewModel)e.SelectedItem;
            ViewModel.RecipeSearchTerm = "";
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

        private void HandlePanMove(PointerRoutedEventArgs e) {
            PointerPoint pointer = e.GetCurrentPoint(this);
            Point currentPoint = pointer.Position;

            canvasTranslate.X += (currentPoint.X - panStartPoint.X);
            canvasTranslate.Y += (currentPoint.Y - panStartPoint.Y);
            panStartPoint = currentPoint;

            RenderGrid();
            e.Handled = true;
        }

        private void HandleStartDrawingConnection(ProductionStepView stepView, ProductionPortViewModel port) {
            ViewModel.StartPort = port;
            string repeaterName = port.Type == PortType.Input ? "InputsContainer" : "OutputsContainer";
            
            if (stepView.FindName(repeaterName) is ItemsRepeater repeater &&
                repeater.TryGetElement(port.Index) is FrameworkElement portElement
            ) {
                Point portTopLeft = portElement.TransformToVisual(MainCanvas).TransformPoint(new Point(0, 0));
                portPosition = new Point(
                    portTopLeft.X + (portElement.ActualWidth / 2),
                    portTopLeft.Y + (portElement.ActualHeight / 2)
                );
            }
        }

        private void DrawConnectionInProgress(PointerRoutedEventArgs e) {
            if (!ViewModel.IsDrawingConnection || connectionPath == null) return;

            pointerPosition = e.GetCurrentPoint(MainCanvas).Position;

            double dx = Math.Abs(pointerPosition.X - portPosition.X);
            double handle = Math.Max(40, dx * 0.5);

            Point controlPoint1 = new Point(portPosition.X + handle, portPosition.Y);
            Point controlPoint2 = new Point(pointerPosition.X - handle, pointerPosition.Y);

            PathFigure figure = new PathFigure() {
                StartPoint = portPosition,
                IsClosed = false,
                IsFilled = false
            };

            figure.Segments.Add(new BezierSegment() {
                Point1 = controlPoint1,
                Point2 = controlPoint2,
                Point3 = pointerPosition
            });

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            connectionPath.Data = geometry;
        }

        private void HandleEndDrawingConnection(ProductionPortViewModel port) {
            connectionPath?.Data = null;
            ViewModel.EndPort = port;
            ViewModel.FormNewConnection();
        }
    }
}
