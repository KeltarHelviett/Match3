namespace Match3.Models
{
    public enum TileType
    {
        Triangle, Rectangle,
    }

    public class Tile
    {
        #region Ctor

        public Tile() { }

        public Tile(TileType type)
        {
            Type = type;
        }

        public Tile(TileType type, double left, double top): this(type)
        {
            Left = left;
            Top = top;
        }

        #endregion

        #region Public Properties

        public double Left { get; set; }

        public TileType Type { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public int Row { get; set; }

        public int Col { get; set; }

        #endregion


    }
}
