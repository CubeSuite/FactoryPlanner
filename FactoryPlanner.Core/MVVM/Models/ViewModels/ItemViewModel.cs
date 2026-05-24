using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public class ItemViewModel : ObservableObject
    {
        // Fields
        private readonly Item item;

        // Properties
        public Item Item => item;
        public int ID => item.ID;
        
        public string Name {
            get => item.Name;
            set {
                if (item.Name == value) return;
                item.Name = value;
            }
        }

        public string IconPath {
            get => item.IconPath;
            set {
                if (item.IconPath == value) return;
                item.IconPath = value;
                OnPropertyChanged(nameof(UseDefaultIcon));
            }
        }

        public bool UseDefaultIcon => string.IsNullOrEmpty(IconPath) || !File.Exists(IconPath);

        // Constructors
        public ItemViewModel(Item item) {
            this.item = item;
        }
    }
}
