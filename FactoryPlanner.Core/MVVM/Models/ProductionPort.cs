using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class ProductionPort
    {
        // Fields
        private int _id;
        private int _gameId;
        private int _parentStepId; // ToDo: Remove if not needed
        private PortType _type;
        private int _index;

        // Properties
        public int ID => _id;
        public int GameID => _gameId;
        public int ParentStepID => _parentStepId;
        public PortType Type => _type;
        public int Index => _index;

        // Constructors

        public ProductionPort(){
            _id = -1;
            _gameId = -1;
            _parentStepId = -1;
            _type = PortType.Input;
            _index = -1;
        }

        public ProductionPort(int id, int gameId, ProductionStep parent, PortType type, int index) {
            _id = id;
            _gameId = gameId;
            _parentStepId = parent.ID;
            _type = type;
            _index = index;
        }

        public ProductionPort(ProductionPort other) {
            _gameId = other.GameID;
            _parentStepId = other.ParentStepID;
            _type = other.Type;
            _index = other.Index;
        }
    }

    public enum PortType
    {
        Input,
        Output
    }
}
