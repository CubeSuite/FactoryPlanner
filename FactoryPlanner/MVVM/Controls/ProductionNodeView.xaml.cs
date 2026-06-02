using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections;
using System;
using Windows.UI.Xaml.Markup;

namespace FactoryPlanner.MVVM.Controls
{
    [ContentProperty(Name = nameof(NodeContent))]
    public sealed partial class ProductionNodeView : UserControl
    {
        // Dependency Properties

        public static readonly DependencyProperty NodeContentProperty = DependencyProperty.Register(
            nameof(NodeContent), typeof(UIElement), typeof(ProductionNodeView), new PropertyMetadata(null, OnNodeContentChanged)
        );

        public static readonly DependencyProperty OptionsFlyoutContentProperty = DependencyProperty.Register(
            nameof(OptionsFlyoutContent), typeof(UIElement), typeof(ProductionNodeView), new PropertyMetadata(null, OnOptionsFlyoutContentChanged)
        );

        public static readonly DependencyProperty DeleteFlyoutContentProperty = DependencyProperty.Register(
            nameof(DeleteFlyoutContent), typeof(UIElement), typeof(ProductionNodeView), new PropertyMetadata(null, OnDeleteFlyoutContentChanged)
        );

        public static readonly DependencyProperty IconPathProperty = DependencyProperty.Register(
            nameof(IconPath), typeof(string), typeof(ProductionNodeView), new PropertyMetadata(null, OnIconPathChanged)
        );

        public static readonly DependencyProperty InputPortsProperty = DependencyProperty.Register(
            nameof(InputPorts), typeof(IList), typeof(ProductionNodeView), new PropertyMetadata(null, OnInputPortsChanged)
        );

        public static readonly DependencyProperty OutputPortsProperty = DependencyProperty.Register(
            nameof(OutputPorts), typeof(IList), typeof(ProductionNodeView), new PropertyMetadata(null, OnOutputPortsChanged)
        );

        // Properties

        public UIElement? NodeContent {
            get => (UIElement?)GetValue(NodeContentProperty);
            set => SetValue(NodeContentProperty, value);
        }

        public UIElement? OptionsFlyoutContent {
            get => (UIElement?)GetValue(OptionsFlyoutContentProperty);
            set => SetValue(OptionsFlyoutContentProperty, value);
        }

        public UIElement? DeleteFlyoutContent {
            get => (UIElement?)GetValue(DeleteFlyoutContentProperty);
            set => SetValue(DeleteFlyoutContentProperty, value);
        }

        public string? IconPath {
            get => (string?)GetValue(IconPathProperty);
            set => SetValue(IconPathProperty, value);
        }

        public IList? InputPorts {
            get => (IList?)GetValue(InputPortsProperty);
            set => SetValue(InputPortsProperty, value);
        }

        public IList? OutputPorts {
            get => (IList?)GetValue(OutputPortsProperty);
            set => SetValue(OutputPortsProperty, value);
        }

        public ItemsRepeater InputsRepeater => InputsContainer;
        public ItemsRepeater OutputsRepeater => OutputsContainer;

        // Constructors

        public ProductionNodeView() {
            InitializeComponent();
        }

        // Events
        public event PointerEventHandler? InputPortPressed;
        public event PointerEventHandler? OutputPortPressed;

        // DP Change Listeners

        private static void OnNodeContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ProductionNodeView)d).NodeContentPresenter.Content = e.NewValue;
        }

        private static void OnOptionsFlyoutContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ProductionNodeView)d).OptionsFlyoutPresenter.Content = e.NewValue;
        }

        private static void OnDeleteFlyoutContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ProductionNodeView)d).DeleteFlyoutPresenter.Content = e.NewValue;
        }

        private static void OnIconPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ProductionNodeView node = (ProductionNodeView)d;
            string? path = e.NewValue as string;
            bool hasIcon = !string.IsNullOrEmpty(path);
            if (hasIcon) {
                try { node.IconImage.Source = new BitmapImage(new Uri(path!)); }
                catch { node.IconImage.Source = null; }
            }
            else {
                node.IconImage.Source = null;
            }
            node.DefaultIcon.Visibility = hasIcon ? Visibility.Collapsed : Visibility.Visible;
        }

        private static void OnInputPortsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ProductionNodeView)d).InputsContainer.ItemsSource = e.NewValue as IList;
        }

        private static void OnOutputPortsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ProductionNodeView)d).OutputsContainer.ItemsSource = e.NewValue as IList;
        }

        // Port Listeners

        private void OnInputPointerPressed(object sender, PointerRoutedEventArgs e) {
            InputPortPressed?.Invoke(sender, e);
        }

        private void OnOutputPointerPressed(object sender, PointerRoutedEventArgs e) {
            OutputPortPressed?.Invoke(sender, e);
        }

        // Public Methods

        public void HideFlyouts() {
            OptionsFlyout.Hide();
            DeleteFlyout.Hide();
        }

    }
}
