using FactoryPlanner.Core.MVVM.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace FactoryPlanner.Core.MVVM.Models
{
    public interface IProductionNode : INotifyPropertyChanged
    {
        // Properties
        public string IconPath { get; }
        public Point Position { get; set; }
        public List<ProductionPortViewModel> InputPorts { get; }
        public List<ProductionPortViewModel> OutputPorts { get; }

        // Methods
        public void SaveChanges();
    }
}
