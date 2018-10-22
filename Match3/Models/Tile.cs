using System.Windows.Controls;
using System.Windows.Shapes;

namespace Match3.Models
{
    public enum TileType
    {
        Rectangle, Triangle
    }

    public abstract class Tile: Shape
    {
        #region Ctor

        protected Tile() { }

        #endregion

        #region Public Properties

        public double Left { get; set; }

        public virtual TileType Type { get; }

        public double Top { get; set; }

        public int Row { get; set; }

        public int Col { get; set; }

        public double TileWidth { get; set; }

        public double TileHeight { get; set; }

        #endregion
    }
}
