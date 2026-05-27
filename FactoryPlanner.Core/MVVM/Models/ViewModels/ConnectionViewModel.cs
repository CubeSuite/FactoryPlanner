using CommunityToolkit.Mvvm.ComponentModel;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models.ViewModels
{
    public partial class ConnectionViewModel : ObservableObject
    {
        // Fields
        private Connection _connection;

        // Properties

        [ObservableProperty]
        public partial double Quantity { get; set; }

        //public double Quantity {
        //    get => _connection.Quantity;
        //    set {
        //        if (_connection.Quantity == value) return;
        //        _connection.Quantity = value;
        //        OnPropertyChanged();
        //    }
        //}

        [ObservableProperty]
        public partial ProductionPortViewModel Input { get; set; }
        
        [ObservableProperty]
        public partial ProductionPortViewModel Output { get; set; }

        // Constructors

        public ConnectionViewModel(Connection connection, ProductionPortViewModel input, ProductionPortViewModel output) {
            _connection = connection;
            Input = input;
            Output = output;
        }

        // Public Functions

        public void UpdateConnections(ProductionPortViewModel? caller = null) {
            if (Input != caller) Input.UpdateConnections(this);
            if (Output != caller) Output.UpdateConnections(this);
        }
    }

    public class ConnectionUpdateRequest {
        public bool IsPush { get; set; }
        public int NumResources { get; set; }
    }
}
