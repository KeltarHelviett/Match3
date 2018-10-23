using System.Windows;
using System.Windows.Media;

namespace Match3.Models
{
    class RectangleTile : Tile
    {
        #region Public Members

        public override TileType Type => TileType.Rectangle;

        #endregion

        #region Protected Members

        protected override Geometry DefiningGeometry => 
            new RectangleGeometry(new Rect(new Point(0, 0), new Point(Width, Height)));

        #endregion
    }
}
