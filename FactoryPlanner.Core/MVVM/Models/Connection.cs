using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class Connection
    {
        // Fields
        private int _id;
        private int _gameId;
        private int _inputPortId;
        private int _outputPortId;
        private double _quantity;

        // Properties
        public int ID => _id;
        public int GameID => _gameId;

        public int InputPortID {
            get => _inputPortId;
            set => _inputPortId = value;
        }

        public int OutputPortID {
            get => _outputPortId;
            set => _outputPortId = value;
        }

        public double Quantity {
            get => _quantity;
            set => _quantity = value;
        }
        
        // Constructors

        public Connection() {
            _id = -1;
            _gameId = -1;
            _inputPortId = -1;
            _outputPortId = -1;
            _quantity = 0;
        }

        public Connection(int id ,int gameId, ProductionPort input, ProductionPort output) {
            _id = id;
            _gameId = gameId;
            _inputPortId = input.ID;
            _outputPortId = output.ID;
            _quantity = 0;
        }
    }
}
