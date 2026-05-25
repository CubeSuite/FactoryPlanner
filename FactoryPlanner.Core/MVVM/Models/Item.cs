using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.MVVM.Models
{
    public class Item
    {
        // Fields
        private int _id;
        private int _gameId;
        private string _name;
        private string _iconPath;

        // Properties
        public int ID => _id;
        public int GameID => _gameId;
        
        public string Name {
            get => _name;
            set {
                if (_name == value) return;
                _name = value;
            }
        }

        public string IconPath {
            get => _iconPath;
            set {
                if (_iconPath == value) return;
                _iconPath = value;
            }
        }

        // Constructors

        public Item() {
            _id = -1;
            _gameId = -1;
            _name = "";
            _iconPath = "";
        }

        public Item(int id = -1, int gameId = -1, string name = "", string iconPath = "") {
            _id = id;
            _gameId = gameId;
            _name = name;
            _iconPath = iconPath;
        }
    }
}
