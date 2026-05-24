using FactoryPlanner.Core.MVVM.Models.ViewModels;
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
    public sealed partial class LocalGameView : UserControl
    {
        public LocalGameView() {
            InitializeComponent();
        }

        // Listeners

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            if (args.NewValue is GameViewModel game) game.Deleted += OnGameDeleted;
        }

        private void OnGameDeleted(GameViewModel model) {
            ConfirmDeleteFlyout.Hide();
        }

        private void OnNameBoxLostFocus(object sender, RoutedEventArgs e) {
            if (sender is not TextBox nameBox || nameBox.DataContext is not GameViewModel game) return;
            game.TrySave();
        }
    }
}
