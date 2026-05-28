using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.Core.Stores;
using Microsoft.Extensions.DependencyInjection;
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
        // Services & Stores
        private readonly IConnectionManager connectionManager;

        // Fields
        private Connection _connection;

        // Properties
        public Connection Connection => _connection;

        public double Quantity {
            get => _connection.Quantity;
            set {
                if (_connection.Quantity == value) return;
                _connection.Quantity = value;
                OnPropertyChanged();
                SaveChanges();
            }
        }

        [ObservableProperty]
        public partial ProductionPortViewModel Input { get; set; }
        
        [ObservableProperty]
        public partial ProductionPortViewModel Output { get; set; }

        // Constructors

        public ConnectionViewModel(
            Connection connection, 
            ProductionPortViewModel input, 
            ProductionPortViewModel output, 
            IServiceProvider serviceProvider
        ) {
            connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();
            _connection = connection;
            Input = input;
            Output = output;
        }

        // Listeners

        partial void OnInputChanged(ProductionPortViewModel value) {
            _connection.InputPortID = value.Port.ID;
            SaveChanges();
        }

        partial void OnOutputChanged(ProductionPortViewModel value) {
            _connection.OutputPortID = value.Port.ID;
            SaveChanges();
        }

        // Public Functions

        public void UpdateConnections(ProductionPortViewModel? caller = null) {
            if (Input != caller) Input.UpdateConnections(this);
            if (Output != caller) Output.UpdateConnections(this);
        }

        public void SaveChanges() {
            connectionManager.TryUpdate(_connection);
        }
    }

    public class ConnectionUpdateRequest {
        public bool IsPush { get; set; }
        public int NumResources { get; set; }
    }
}
