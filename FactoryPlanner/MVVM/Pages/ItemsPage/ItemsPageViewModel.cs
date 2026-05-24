using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Services;
using FactoryPlanner.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.I2c;
using Windows.Storage;

namespace FactoryPlanner.MVVM.Pages
{
    public partial class ItemsPageViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IItemManager itemManager;
        private readonly IDialogService dialogService;

        // Properties

        public ObservableCollection<ItemViewModel> Items { get; set; }
        
        [ObservableProperty] 
        public partial string ItemName { get; set; }
        
        [ObservableProperty] 
        public partial string IconPath { get; set; }

        [ObservableProperty]
        public partial ItemViewModel? SelectedItem { get; set; }

        public bool IsItemSelected => SelectedItem != null;

        public string AddButtonText => IsItemSelected ? "Update" : "Add";

        public Visibility DeleteButtonVisibility => IsItemSelected ? Visibility.Visible : Visibility.Collapsed;

        [ObservableProperty]
        public partial string OrderBy { get; set; }

        [ObservableProperty]
        public partial string Direction { get; set; }

        // Constructors

        public ItemsPageViewModel(IServiceProvider serviceProvider) {
            itemManager = serviceProvider.GetRequiredService<IItemManager>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();

            Items = new ObservableCollection<ItemViewModel>();
            ItemName = "";
            IconPath = "";
            OrderBy = "ID";
            Direction = "Ascending";
            PopulateItems();
        }

        // Listeners

        partial void OnSelectedItemChanged(ItemViewModel? value) {
            OnPropertyChanged(nameof(AddButtonText));
            OnPropertyChanged(nameof(DeleteButtonVisibility));

            if (value == null) return;
            ItemName = value.Name;
            IconPath = value.IconPath;
        }

        partial void OnOrderByChanged(string value) {
            PopulateItems();
        }

        partial void OnDirectionChanged(string value) {
            PopulateItems();
        }

        // Commands

        [RelayCommand]
        private void BrowseForIcon() {
            BrowseForIconAsync();
        }

        [RelayCommand]
        private void AddItem() {
            if (!ValidateInputs()) return;

            if (!IsItemSelected) TryAddItem(ItemName, IconPath);
            else UpdateSelectedItem();
        }

        [RelayCommand]
        private void DeleteItem() {
            DeleteItemAsync();
        }

        [RelayCommand]
        private void BatchImport() {
            BatchImportAsync();
        }

        // Private Functions

        private void PopulateItems() {
            Items.Clear();
            IEnumerable<Item> allItems = itemManager.GetAll();
            if (OrderBy == "Name") {
                allItems = allItems.OrderBy(item => item.Name);
            }

            if (Direction == "Descending") {
                allItems.Reverse();
            }
            
            foreach(Item item in allItems) {
                Items.Add(new ItemViewModel(item));
            }
        }

        private async void BrowseForIconAsync() {
            StorageFile? icon = await dialogService.PickSingleFile([".jpg", ".jpeg", ".png"]);
            if (icon == null) return;

            if (string.IsNullOrEmpty(ItemName)) {
                ItemName = icon.DisplayName;
            }

            IconPath = icon.Path;
        }

        private async void DeleteItemAsync() {
            if (SelectedItem == null) return;

            if (await dialogService.Confirm($"Delete {SelectedItem.Name}?", "This cannot be undone.")) {
                itemManager.TryDelete(SelectedItem.Item);
                ClearInputs();
                PopulateItems();
            }
        }

        private async void BatchImportAsync() {
            StorageFolder? folderResult = await dialogService.PickSingleFolder();
            if (folderResult == null) return;

            IReadOnlyList<StorageFile> allFiles = await folderResult.GetFilesAsync();
            IEnumerable<StorageFile> images = allFiles.Where(file => 
                file.FileType.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                file.FileType.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                file.FileType.Equals(".png", StringComparison.OrdinalIgnoreCase)
            );

            foreach(StorageFile image in images) {
                string name = image.DisplayName;
                if (itemManager.IsNameTaken(name)) continue;

                TryAddItem(name, image.Path);
            }
        }

        private bool ValidateInputs() {
            if (string.IsNullOrEmpty(ItemName)) {
                dialogService.ShowMessage(MessageType.Error, "Must Enter Item Name");
                return false;
            }

            if (SelectedItem == null && itemManager.IsNameTaken(ItemName)) {
                dialogService.ShowMessage(
                    MessageType.Error,
                    "Name Taken",
                    $"You've already entered an item called '{ItemName}'."
                );

                return false;
            }

            if (!string.IsNullOrEmpty(IconPath) && !File.Exists(IconPath)) {
                dialogService.ShowMessage(
                    MessageType.Error,
                    "Icon File Doesn't Exist",
                    $"The icon path you provided doesn't exist, either pick an existing file or enter nothing."
                );

                return false;
            }

            return true;
        }

        private void TryAddItem(string name, string icon) {
            itemManager.CreateAndAdd(name, icon);

            Item? newItem = itemManager.GetLatest();
            if (newItem != null) {
                Items.Add(new ItemViewModel(newItem));
            }

            ClearInputs();
        }

        private void UpdateSelectedItem() {
            if (SelectedItem == null) return;
            SelectedItem.Name = ItemName;
            SelectedItem.IconPath = IconPath;
            if (itemManager.TryUpdate(SelectedItem.Item)) {
                ClearInputs();
                PopulateItems();
            }
        }

        private void ClearInputs() {
            ItemName = "";
            IconPath = "";
            SelectedItem = null;
        }
    }
}
