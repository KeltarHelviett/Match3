using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Match3.Models
{
    class TriangleTile : Tile
    {
        #region Public Properties

        public override TileType Type => TileType.Triangle;

        #endregion

        #region Protected Properties

        protected override Geometry DefiningGeometry
        {
            get
            {
                var p1 = new Point(0, TileHeight);
                var p2 = new Point(p1.X + TileWidth, p1.Y);
                var p3 = new Point((p1.X + p2.X) / 2, 0);

                var segments = new List<PathSegment>
                {
                    new LineSegment(p2, true), new LineSegment(p3, true),
                };

                return new PathGeometry(new List<PathFigure> { new PathFigure(p1, segments, true) }, FillRule.EvenOdd, null);
            }
        }

        #endregion
    }
}
