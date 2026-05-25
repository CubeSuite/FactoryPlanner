using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public class ProductionLineViewModel : ObservableObject
    {
        // Fields
        private ProductionLine _productionLine;
        private ObservableCollection<ProductionStepViewModel> _steps;
        private ObservableCollection<ConnectionViewModel> _connections;

        // Properties
        public ProductionLine? Parent { get; }

        public ProductionLine ProductionLine => _productionLine;
        public ObservableCollection<ProductionStepViewModel> Steps => _steps;
        public ObservableCollection<ConnectionViewModel> Connections => _connections;

        public string Name {
            get => _productionLine.Name;
            set {
                if (_productionLine.Name == value) return;
                _productionLine.Name = value;
            }
        }

        // Constructors

        public ProductionLineViewModel(ProductionLine productionLine) {
            _productionLine = productionLine;
            _steps = new ObservableCollection<ProductionStepViewModel>();
            _connections = new ObservableCollection<ConnectionViewModel>();
        }

        public ProductionLineViewModel(ProductionLineViewModel parent) {
            Parent = parent.ProductionLine;
            _productionLine = new ProductionLine();
            _steps = new ObservableCollection<ProductionStepViewModel>();
            _connections = new ObservableCollection<ConnectionViewModel>();
        }
    }
}
