using CommunityToolkit.Mvvm.ComponentModel;
using FactoryPlanner.Stores.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.MVVM.Pages
{
    public partial class HomePageViewModel : ObservableObject
    {
        // Properties

        [ObservableProperty]
        public partial string Title { get; set; }

        // Constructors

        public HomePageViewModel(IServiceProvider serviceProvider) {
            IProgramData programData = serviceProvider.GetRequiredService<IProgramData>();
            Title = programData.ProgramName;
        }
    }
}
