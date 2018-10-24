using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
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

        public bool ToDelete { get; set; } = false;

        #endregion

        #region Public Methods

        public void Move(double left, double top)
        {
            void MoveAnimation(double value, DependencyProperty property, Action<UIElement, double> setter)
            {
                MoveStarted?.Invoke(this, EventArgs.Empty);
                var anim = new DoubleAnimation(value, new Duration(TimeSpan.FromSeconds(0.5))) { FillBehavior = FillBehavior.Stop };
                anim.Completed += (sender, args) =>
                {
                    setter(this, value);
                    Moved?.Invoke(this, args);
                };
                BeginAnimation(property, anim);
            }
            if (Math.Abs(Left - left) > double.Epsilon)
                MoveAnimation(left, Canvas.LeftProperty, Canvas.SetLeft);

            if (Math.Abs(Top - top) > double.Epsilon)
                MoveAnimation(top, Canvas.TopProperty, Canvas.SetTop);
        }

        #endregion

        #region Event

        public event EventHandler MoveStarted;

        public event EventHandler Moved;

        #endregion
    }
}
