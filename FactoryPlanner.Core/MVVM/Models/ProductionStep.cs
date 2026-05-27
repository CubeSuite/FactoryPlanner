using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class ProductionStep
    {
        // Fields
        private int _recipeId;
        private double _numMachines;
        private Point _position;

        // Properties

        public int RecipeId {
            get => _recipeId;
            set => _numMachines = value;
        }

        public double NumMachines {
            get => _numMachines;
            set => _numMachines = value;
        }

        public Point Position {
            get => _position;
            set => _position = value;
        }

        // Constructors

        public ProductionStep() {
            _recipeId = -1;
            _numMachines = 0;
            _position = new Point();
        }

        public ProductionStep(Recipe recipe, Point position) {
            _recipeId = recipe.ID;
            _numMachines = 0;
            _position = position;
        }
    }
}
