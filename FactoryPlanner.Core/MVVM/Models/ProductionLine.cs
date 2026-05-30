using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class ProductionLine
    {
        // Fields
        private int _id;
        private int _parentId;
        private int _gameId;
        private string _name;
        private string _iconPath;
        private HashSet<int> _subLineIds;
        private HashSet<int> _stepIds;
        private HashSet<int> _connectionIds;

        private double x;
        private double y;

        // Properties
        public int ID => _id;
        public int GameID => _gameId;
        public HashSet<int> SubLines => _subLineIds;
        public HashSet<int> Steps => _stepIds;
        public HashSet<int> Connections => _connectionIds;

        public int ParentID {
            get => _parentId;
            set => _parentId = value;
        }

        public string Name {
            get => _name;
            set => _name = value;
        }

        public string IconPath {
            get => _iconPath;
            set => _iconPath = value;
        }

        public Point Position {
            get => new Point(x, y);
            set {
                x = value.X;
                y = value.Y;
            }
        }

        // Constructors

        public ProductionLine() {
            _id = -1;
            _gameId = -1;
            _parentId = -1;
            _name = "";
            _iconPath = "";
            _subLineIds = new HashSet<int>();
            _stepIds = new HashSet<int>();
            _connectionIds = new HashSet<int>();
        }

        public ProductionLine(int id, int gameId) : this() {
            _id = id;
            _gameId = gameId;
        }

        public ProductionLine(int id, int parentId, int gameId) : this(id, gameId) {
            _parentId = parentId;
        }
    }
}
