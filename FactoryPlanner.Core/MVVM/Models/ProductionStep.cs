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
        private int _id;
        private int _gameId;
        private int _recipeId;
        private double _numMachines;
        private double _lastTypedNumMachines;
        private HashSet<int> _inputPortIds;
        private HashSet<int> _outputPortIds;

        private double x;
        private double y;

        // Properties
        public int ID => _id;
        public int GameID => _gameId;
        public int RecipeID => _recipeId;

        public HashSet<int> InputPortIDs {
            get => _inputPortIds;
            set => _inputPortIds = value;
        }

        public HashSet<int> OutputPortIDs {
            get => _outputPortIds;
            set => _outputPortIds = value;
        }

        public double NumMachines {
            get => _numMachines;
            set => _numMachines = value;
        }

        public double LastTypedNumMachines {
            get => _lastTypedNumMachines;
            set => _lastTypedNumMachines = value;
        }

        public Point Position {
            get => new Point(x, y);
            set {
                x = value.X;
                y = value.Y;
            }
        }

        // Constructors

        public ProductionStep() {
            _id = -1;
            _gameId = -1;
            _recipeId = -1;
            _numMachines = 0;
            _lastTypedNumMachines = -1;
            _inputPortIds = new HashSet<int>();
            _outputPortIds = new HashSet<int>();
            x = 0;
            y = 0;
        }

        public ProductionStep(int id, int gameId, Recipe recipe, Point position) {
            _id = id;
            _gameId = gameId;
            _recipeId = recipe.ID;
            _numMachines = 0;
            _lastTypedNumMachines = -1;
            _inputPortIds = new HashSet<int>();
            _outputPortIds = new HashSet<int>();
            Position = position;
        }
    }
}
