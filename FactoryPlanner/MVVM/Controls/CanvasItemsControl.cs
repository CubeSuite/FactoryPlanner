using FactoryPlanner.Core.MVVM.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FactoryPlanner.MVVM.Controls
{
    public class CanvasItemsControl : ItemsControl
    {
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
            base.PrepareContainerForItemOverride(element, item);

            if (element is UIElement container && item is IProductionNode node) {
                Canvas.SetLeft(container, node.Position.X);
                Canvas.SetTop(container, node.Position.Y);
            }
        }
    }
}
