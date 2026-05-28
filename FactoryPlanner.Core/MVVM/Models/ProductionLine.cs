using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Profile.SystemManufacturers;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class ProductionLine
    {
        // Fields
        private int _id;
        private int _parentId;
        private int _gameId;
        private string _name;
        private HashSet<int> _subLineIds;
        private HashSet<int> _stepIds;
        private HashSet<int> _connectionIds;

        // Properties
        public int ID => _id;
        public int ParentID => _parentId;
        public int GameID => _gameId;
        public HashSet<int> SubLines => _subLineIds;
        public HashSet<int> Steps => _stepIds;
        public HashSet<int> Connections => _connectionIds;

        public string Name {
            get => _name;
            set => _name = value;
        }

        // Constructors

        public ProductionLine() {
            _id = -1;
            _parentId = -1;
            _name = "";
            _subLineIds = new HashSet<int>();
            _stepIds = new HashSet<int>();
            _connectionIds = new HashSet<int>();
        }

        public ProductionLine(int id, int gameId) {
            _id = id;
            _gameId = gameId;
            _parentId = -1;
            _name = "";
            _subLineIds = new HashSet<int>();
            _stepIds = new HashSet<int>();
            _connectionIds = new HashSet<int>();
        }

        // Public Functions

        //public IEnumerable<Connection> GetConnectionsToPort(ProductionPort port) {
        //    return Connections.Where(connection => connection.InputPortID == port.ID || connection.OutputPortID == port.ID);
        //}
    }
}
