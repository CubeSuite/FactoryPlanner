using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FactoryPlanner.MVVM.Views
{
    public sealed partial class ConnectionView : UserControl
    {
        public ConnectionView() {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnConnectionViewSizeChanged;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            AttachToSteps();
            UpdateConnectionVisual();
        }

        private void OnConnectionViewSizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateConnectionVisual();
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            AttachToSteps();
            UpdateConnectionVisual();
        }

        private void AttachToSteps() {
            if (DataContext is not ConnectionViewModel connectionVM) return;

            if (FindStepView(connectionVM.Input) is ProductionStepView inputView && inputView.DataContext is ProductionStepViewModel inputVM) {
                inputVM.PropertyChanged -= OnStepPropertyChanged;
                inputVM.PropertyChanged += OnStepPropertyChanged;
            }

            if (FindStepView(connectionVM.Output) is ProductionStepView outputView && outputView.DataContext is ProductionStepViewModel outputVM) {
                outputVM.PropertyChanged -= OnStepPropertyChanged;
                outputVM.PropertyChanged += OnStepPropertyChanged;
            }
        }

        private void OnStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(ProductionStepViewModel.Position)) {
                UpdateConnectionVisual();
            }
        }

        private void UpdateConnectionVisual() {
            if (DataContext is not ConnectionViewModel connectionVM) return;

            Point? start = GetPortCenter(connectionVM.Input);
            Point? end = GetPortCenter(connectionVM.Output);
            if (start is null || end is null) return;

            Point startPoint = start.Value;
            Point endPoint = end.Value;

            double dx = Math.Abs(endPoint.X - startPoint.X);
            double handle = Math.Max(40, dx * 0.5);

            Point controlPoint1 = new(startPoint.X + handle, startPoint.Y);
            Point controlPoint2 = new(endPoint.X - handle, endPoint.Y);

            PathFigure figure = new() {
                StartPoint = startPoint,
                IsClosed = false,
                IsFilled = false
            };

            figure.Segments.Add(new BezierSegment() {
                Point1 = controlPoint1,
                Point2 = controlPoint2,
                Point3 = endPoint
            });

            PathGeometry geometry = new();
            geometry.Figures.Add(figure);
            ConnectionPath.Data = geometry;

            Point midpoint = GetBezierPoint(startPoint, controlPoint1, controlPoint2, endPoint, 0.5);
            double textBoxWidth = MidpointTextBox.ActualWidth > 0 ? MidpointTextBox.ActualWidth : MidpointTextBox.MinWidth;
            double textBoxHeight = MidpointTextBox.ActualHeight > 0 ? MidpointTextBox.ActualHeight : 32;
            Canvas.SetLeft(MidpointTextBox, midpoint.X - (textBoxWidth / 2));
            Canvas.SetTop(MidpointTextBox, midpoint.Y - (textBoxHeight / 2));
        }

        private ProductionStepView? FindStepView(ConnectedStepViewModel connectedStep) {
            if (XamlRoot?.Content is not FrameworkElement root) return null;

            return FindDescendant<ProductionStepView>(root)
                .FirstOrDefault(view => view.DataContext is ProductionStepViewModel vm && vm.ProductionStep == connectedStep.Step);
        }

        private Point? GetPortCenter(ConnectedStepViewModel connectedStep) {
            ProductionStepView? stepView = FindStepView(connectedStep);
            if (stepView == null) return null;

            string repeaterName = connectedStep.PortType == PortType.Input ? "InputsContainer" : "OutputsContainer";
            if (stepView.FindName(repeaterName) is not ItemsRepeater repeater) return null;
            if (repeater.TryGetElement(connectedStep.PortIndex) is not FrameworkElement portElement) return null;

            Point topLeft = portElement.TransformToVisual(this).TransformPoint(new Point(0, 0));
            return new Point(
                topLeft.X + (portElement.ActualWidth / 2),
                topLeft.Y + (portElement.ActualHeight / 2)
            );
        }

        private static Point GetBezierPoint(Point p0, Point p1, Point p2, Point p3, double t) {
            double oneMinusT = 1 - t;
            double x = (oneMinusT * oneMinusT * oneMinusT * p0.X)
                     + (3 * oneMinusT * oneMinusT * t * p1.X)
                     + (3 * oneMinusT * t * t * p2.X)
                     + (t * t * t * p3.X);
            double y = (oneMinusT * oneMinusT * oneMinusT * p0.Y)
                     + (3 * oneMinusT * oneMinusT * t * p1.Y)
                     + (3 * oneMinusT * t * t * p2.Y)
                     + (t * t * t * p3.Y);

            return new Point(x, y);
        }

        private static IEnumerable<T> FindDescendant<T>(DependencyObject root) where T : DependencyObject {
            int childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++) {
                DependencyObject child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T match) {
                    yield return match;
                }

                foreach (T descendant in FindDescendant<T>(child)) {
                    yield return descendant;
                }
            }
        }
    }
}
