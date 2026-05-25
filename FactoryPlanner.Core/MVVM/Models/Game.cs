using ABI.System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class Game
    {
        // Fields
        private int _id;
        private string _name;

        // Properties
        public int ID => _id;
        
        public string Name {
            get => _name;
            set => _name = value;
        }

        // Constructors

        public Game() {
            _id = -1;
            _name = "New Game";
        }

        public Game(int id) {
            _id = id;
            _name = "New Game";
        }
    }
}
