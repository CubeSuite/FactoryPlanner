using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Services;
using FactoryPlanner.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FactoryPlanner.MVVM.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Services & Stores
        IServiceProvider serviceProvider;
        INavigationService navigationService;

        public MainPage(IServiceProvider serviceProvider) {
            InitializeComponent();

            this.serviceProvider = serviceProvider;
            navigationService = serviceProvider.GetRequiredService<INavigationService>();
            navigationService.NavigationRequested += OnNavigationRequested;
        }

        // Listeners

        private void OnNavigationRequested(ObservableObject pageViewModel) {
            Type? pageType = pageViewModel switch {
                HomePageViewModel => typeof(HomePage),
                ItemsPageViewModel => typeof(ItemsPage),
                RecipesPageViewModel => typeof(RecipesPage),
                MachinesPageViewModel => typeof(MachinesPage),
                BoostsPageViewModel => typeof(BoostsPage),
                FactoryPlannerPageViewModel => typeof(FactoryPlannerPage),

                BackupsPageViewModel => typeof(BackupsPage),
                SettingsPageViewModel => typeof(SettingsPage),
                _ => null
            };

            if (pageType == null) {
                Debug.Assert(false, "Could not navigate as page type {pageType} has not been configured");
                return;
            }

            pageFrame.Navigate(pageType);
            ((FrameworkElement)pageFrame.Content).DataContext = pageViewModel;
        }

        private void OnNavigationViewItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args) {
            switch (args.InvokedItem.ToString()) {
                case "Home": navigationService.Navigate(new HomePageViewModel(serviceProvider)); break;
                case "Items": navigationService.Navigate(new ItemsPageViewModel(serviceProvider)); break;
                case "Recipes": navigationService.Navigate(new RecipesPageViewModel(serviceProvider)); break;
                case "Machines": navigationService.Navigate(new MachinesPageViewModel(serviceProvider)); break;
                case "Boosts": navigationService.Navigate(new BoostsPageViewModel(serviceProvider)); break;
                case "Factory Planner": navigationService.Navigate(new FactoryPlannerPageViewModel(serviceProvider)); break;

                case "Backups": navigationService.Navigate(new BackupsPageViewModel(serviceProvider)); break;
                case "Settings": navigationService.Navigate(new SettingsPageViewModel(serviceProvider)); break;
            }
        }
    }
}
