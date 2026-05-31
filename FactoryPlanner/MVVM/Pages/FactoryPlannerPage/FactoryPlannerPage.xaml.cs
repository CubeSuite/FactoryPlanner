using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.MVVM.Views;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FactoryPlanner.MVVM.Pages
{
    public sealed partial class FactoryPlannerPage : Page
    {
        // Fields
        private bool hasLoaded = false;
        
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
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

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

            RenderGrid();
            UpdateMainCanvasCenter();

            hasLoaded = true;
            UpdateNodePositions();
        }

        private void OnFactoryPlannerPageSizeChanged(object sender, SizeChangedEventArgs e) {
            RenderGrid();
            UpdateMainCanvasCenter();
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
            else if (e.PropertyName == nameof(FactoryPlannerPageViewModel.CurrentProductionLine)) {
                UpdateNodePositions();
            }
        }

        private void OnNodeCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null) {
                foreach (object item in e.NewItems) {
                    if (item is IProductionNode node) {
                        _ = UpdateNodePosition(node);
                        node.PropertyChanged += OnNodePropertyChanged;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null) {
                foreach (object item in e.OldItems) {
                    if (item is IProductionNode node) {
                        node.PropertyChanged -= OnNodePropertyChanged;
                    }
                }
            }
        }

        private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(IProductionNode.Position) && sender is IProductionNode node) {
                _ = UpdateNodePosition(node);
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

        private void OnProductionNodePortPressed(object sender, ProductionPortViewModel port) {
            FrameworkElement element;
            bool isExposed = false;

            if (sender is ProductionLineView lineView) {
                element = lineView;
                isExposed = true;
            }
            else if (sender is ProductionStepView stepView) element = stepView;
            else return;

            ViewModel.IsDrawingConnection = !ViewModel.IsDrawingConnection;

            if (ViewModel.IsDrawingConnection) {
                if (connectionPath != null) connectionPath.Data = null;
                ViewModel.IsStartPortExposed = isExposed;
                HandleStartDrawingConnection(element, port);
            }
            else {
                ViewModel.IsEndPortExposed = isExposed;
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

        private void UpdateNodePositions() {
            if (ViewModel == null || !hasLoaded) return;

            ViewModel.CurrentProductionLine.SubLines.CollectionChanged += OnNodeCollectionChanged;
            foreach (ProductionLineViewModel lineVM in ViewModel.CurrentProductionLine.SubLines) {
                _ = UpdateNodePosition(lineVM);
                lineVM.PropertyChanged += OnNodePropertyChanged;
            }

            ViewModel.CurrentProductionLine.Steps.CollectionChanged += OnNodeCollectionChanged;
            foreach (ProductionStepViewModel stepVM in ViewModel.CurrentProductionLine.Steps) {
                _ = UpdateNodePosition(stepVM);
                stepVM.PropertyChanged += OnNodePropertyChanged;
            }
        }

        private async Task UpdateNodePosition(IProductionNode node) {
            ItemsControl? itemsControl = TryGetItemsControlForType(node);
            if (itemsControl == null) return;

            UIElement? container = itemsControl.ContainerFromItem(node) as UIElement;
            if (container == null) {
                container = await TryGetContainerAsync(itemsControl, node);
            }

            if (container != null) {
                Canvas.SetLeft(container, node.Position.X);
                Canvas.SetTop(container, node.Position.Y);
            }
        }

        private Task<UIElement?> TryGetContainerAsync(ItemsControl itemsControl, IProductionNode node) {
            TaskCompletionSource<UIElement?> taskCompletionSource = new TaskCompletionSource<UIElement?>();

            bool wasQueued = itemsControl.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => {
                itemsControl.UpdateLayout();
                UIElement? container = itemsControl.ContainerFromItem(node) as UIElement;
                taskCompletionSource.TrySetResult(container);
            });

            if (!wasQueued) {
                taskCompletionSource.TrySetResult(null);
            }

            return taskCompletionSource.Task;
        }

        private ItemsControl? TryGetItemsControlForType(IProductionNode node) {
            if (node is ProductionLineViewModel) return SubLinesItemsControl;
            if (node is ProductionStepViewModel) return StepsItemsControl;
            
            Debug.Assert(false, $"Failed to get ItemsControl for type {node.GetType()}");
            return null;
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

            Point canvasPosition = GetCanvasPosition(viewportPosition);

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
            UpdateMainCanvasCenter();
            e.Handled = true;
        }

        private Point GetCanvasPosition(Point viewportPosition) {
            return new Point(
                (viewportPosition.X - canvasTranslate.X) / canvasScale.ScaleX,
                (viewportPosition.Y - canvasTranslate.Y) / canvasScale.ScaleY
            );
        }

        private void UpdateMainCanvasCenter() {
            if (ViewModel == null) return;

            Point viewportCenter = new Point(MainCanvas.ActualWidth / 2, MainCanvas.ActualHeight / 2);
            ViewModel.MainCanvasCenter = GetCanvasPosition(viewportCenter);
        }

        private void HandleStartDrawingConnection(FrameworkElement stepView, ProductionPortViewModel port) {
            ViewModel.StartPort = port;
            string repeaterName = port.Type == PortType.Input ? "InputsContainer" : "OutputsContainer";
            
            if (stepView.FindName(repeaterName) is ItemsRepeater repeater &&
                repeater.TryGetElement(port.VisualIndex) is FrameworkElement portElement
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
