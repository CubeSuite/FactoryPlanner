using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FactoryPlanner.Core.MVVM.Models;
using FactoryPlanner.Core.MVVM.Models.ViewModels;
using FactoryPlanner.Core.Stores;
using FactoryPlanner.Services;
using FactoryPlanner.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace FactoryPlanner.MVVM.Pages
{
    public partial class MachinesPageViewModel : ObservableObject
    {
        // Services & Stores
        private readonly IMachineManager machineManager;
        private readonly IDialogService dialogService;

        // Properties

        public ObservableCollection<MachineViewModel> Machines { get; set; }

        [ObservableProperty] 
        public partial string MachineName { get; set; }

        [ObservableProperty] 
        public partial string IconPath { get; set; }

        [ObservableProperty]
        public partial double PowerCost { get; set; }

        [ObservableProperty]
        public partial MachineViewModel? SelectedMachine { get; set; }

        public bool IsMachineSelected => SelectedMachine != null;

        public string AddButtonText => IsMachineSelected ? "Update" : "Add";

        public Visibility DeleteButtonVisibility => IsMachineSelected ? Visibility.Visible : Visibility.Collapsed;

        [ObservableProperty]
        public partial string OrderBy { get; set; }

        [ObservableProperty]
        public partial string Direction { get; set; }

        // Constructors

        public MachinesPageViewModel(IServiceProvider serviceProvider) {
            machineManager = serviceProvider.GetRequiredService<IMachineManager>();
            dialogService = serviceProvider.GetRequiredService<IDialogService>();

            Machines = new ObservableCollection<MachineViewModel>();
            MachineName = "";
            IconPath = "";
            PowerCost = 0;
            OrderBy = "ID";
            Direction = "Ascending";
            PopulateMachines();
        }

        // Listeners

        partial void OnSelectedMachineChanged(MachineViewModel? value) {
            OnPropertyChanged(nameof(AddButtonText));
            OnPropertyChanged(nameof(DeleteButtonVisibility));

            if (value == null) return;
            MachineName = value.Name;
            IconPath = value.IconPath;
            PowerCost = value.PowerCost;
        }

        partial void OnOrderByChanged(string value) {
            PopulateMachines();
        }

        partial void OnDirectionChanged(string value) {
            PopulateMachines();
        }

        // Commands

        [RelayCommand]
        private void BrowseForIcon() {
            BrowseForIconAsync();
        }

        [RelayCommand]
        private void AddMachine() {
            if (!ValidateInputs()) return;

            if (!IsMachineSelected) TryAddMachine(MachineName, IconPath, PowerCost);
            else UpdateSelectedMachine();
        }

        [RelayCommand]
        private void DeleteMachine() {
            DeleteMachineAsync();
        }

        [RelayCommand]
        private void BatchImport() {
            BatchImportAsync();
        }

        // Private Functions

        private void PopulateMachines() {
            Machines.Clear();
            IEnumerable<Machine> allMachines = machineManager.GetAll();
            if (OrderBy == "Name") {
                allMachines = allMachines.OrderBy(machine => machine.Name);
            }

            if (Direction == "Descending") {
                allMachines = allMachines.Reverse();
            }

            foreach(Machine machine in allMachines) {
                Machines.Add(new MachineViewModel(machine));
            }
        }

        private async void BrowseForIconAsync() {
            StorageFile? icon = await dialogService.PickSingleFile([".jpg", ".jpeg", ".png"]);
            if (icon == null) return;

            if (string.IsNullOrEmpty(MachineName)) {
                MachineName = icon.DisplayName;
            }

            IconPath = icon.Path;
        }

        private async void DeleteMachineAsync() {
            if (SelectedMachine == null) return;

            if (await dialogService.Confirm($"Delete {SelectedMachine.Name}?", "This cannot be undone.")) {
                machineManager.TryDelete(SelectedMachine.Machine);
                ClearInputs();
                PopulateMachines();
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
                if (machineManager.IsNameTaken(name)) continue;

                TryAddMachine(name, image.Path, 0);
            }
        }

        private bool ValidateInputs() {
            if (string.IsNullOrEmpty(MachineName)) {
                dialogService.ShowMessage(MessageType.Error, "Must Enter Machine Name");
                return false;
            }

            if (SelectedMachine == null && machineManager.IsNameTaken(MachineName)) {
                dialogService.ShowMessage(
                    MessageType.Error,
                    "Name Taken",
                    $"You've already entered a machine called '{MachineName}'."
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

        private void TryAddMachine(string name, string icon, double powerCost) {
            if (machineManager.CreateAndAdd(name, icon, powerCost, out Machine machine)) {
                Machines.Add(new MachineViewModel(machine));
            }

            ClearInputs();
        }

        private void UpdateSelectedMachine() {
            if (SelectedMachine == null) return;
            SelectedMachine.Name = MachineName;
            SelectedMachine.IconPath = IconPath;
            SelectedMachine.PowerCost = PowerCost;
            if (machineManager.TryUpdate(SelectedMachine.Machine)) {
                ClearInputs();
                PopulateMachines();
            }
        }

        private void ClearInputs() {
            MachineName = "";
            IconPath = "";
            PowerCost = 0;
            SelectedMachine = null;
        }
    }
}
