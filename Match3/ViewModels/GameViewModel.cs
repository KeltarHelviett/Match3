using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Match3.Annotations;
using Match3.Models;

namespace Match3.ViewModels
{
    enum GameState
    {
        SelectingTileToSwap, SelectingTileToSwapWith, ComputingResult,
    }

    class GameViewModel: INotifyPropertyChanged
    {
        #region Ctor

        public GameViewModel(Canvas canvas)
        {
            Canvas = canvas;
            Canvas.MouseMove += (sender, args) => Coords = $"{args.GetPosition(Canvas).X}  {args.GetPosition(Canvas).Y}";
            Timer = new DispatcherTimer(
                new TimeSpan(0, 0, 1), 
                DispatcherPriority.DataBind, 
                (sender, args) => OnPropertyChanged(nameof(TimeLeft)),
                Dispatcher.CurrentDispatcher);
            SpinAnimation.Completed += SpinAnimationOnCompleted;
            SelectedTileStoryBoard.Children.Add(SpinAnimation);
            Timer.Tick += (sender, args) => TimeLeft -= 1;
            Init();
            Timer.Start();
        }

        private void SpinAnimationOnCompleted(object sender, EventArgs e)
        {
            if (State == GameState.SelectingTileToSwapWith)
                SelectedTileStoryBoard.Begin(SelectedTile, true);
        }

        #endregion

        #region Debug

        public string _coords = "";

        public string Coords
        {
            get => _coords;
            set
            {
                if (_coords == value)
                    return;
                _coords = value;
                OnPropertyChanged(nameof(Coords));
            }
        }

        #endregion

        #region Private Fields

        private int _timeLeft = 60;

        #endregion

        #region Public Properties

        public Canvas Canvas { get; }

        public Tile[,] Tiles { get; set; }

        public List<Tuple<Type, SolidColorBrush>> PossibleTiles { get; } = new List<Tuple<Type, SolidColorBrush>>
        {
            new Tuple<Type, SolidColorBrush>(typeof(TriangleTile), Brushes.DeepSkyBlue),
            new Tuple<Type, SolidColorBrush>(typeof(TriangleTile), Brushes.MediumPurple),
            new Tuple<Type, SolidColorBrush>(typeof(RectangleTile), Brushes.DeepSkyBlue),
            new Tuple<Type, SolidColorBrush>(typeof(RectangleTile), Brushes.MediumPurple),
            new Tuple<Type, SolidColorBrush>(typeof(RectangleTile), Brushes.Maroon),
        };

        public int TimeLeft
        {
            get => _timeLeft;
            set
            {
                if (_timeLeft == value)
                    return;
                _timeLeft = value;
                OnPropertyChanged(nameof(TimeLeft));
            }
        }

        public DispatcherTimer Timer { get; }

        public Tile SelectedTile { get; set; }

        public Tile SwapTile { get; set; }

        public GameState State { get; set; } = GameState.SelectingTileToSwap;

        public Storyboard SelectedTileStoryBoard { get; set; } = new Storyboard() { FillBehavior = FillBehavior.Stop };
        public DoubleAnimation SpinAnimation { get; set; } = 
            new DoubleAnimation(-360, new Duration(new TimeSpan(0, 0, 2))) {FillBehavior = FillBehavior.Stop};

        #endregion

        #region Private Methods

        private void Init()
        {
            var tileTypes = Enum.GetValues(typeof(TileType));
            Random random = new Random();
            Tiles = new Tile[8,8];
            var tileHorSpace = Canvas.ActualWidth / 8;
            var tileVerSpace = Canvas.ActualHeight / 8;
            var tileWidth = tileHorSpace * 0.9;
            var marginLeft = tileHorSpace * 0.05;
            var tileHeight = tileVerSpace * 0.9;
            var marginTop = tileVerSpace * 0.05;
            for (var i = 0; i < 64; ++i)
            {
                var pt = PossibleTiles[random.Next(PossibleTiles.Count)];

                var tile = (Tile) Activator.CreateInstance(pt.Item1);
                tile.Row = i / 8;
                tile.Col = i % 8;
                tile.Left = tile.Col * tileHorSpace + marginLeft;
                tile.Top = tile.Row * tileVerSpace + marginTop;
                tile.Stroke = Brushes.Black;
                tile.StrokeThickness = 1;
                tile.TileHeight = tileHeight;
                tile.TileWidth = tileWidth;
                tile.Fill = pt.Item2;
                Canvas.SetLeft(tile, tile.Left);
                Canvas.SetTop(tile, tile.Top);
                tile.MouseLeftButtonUp += TileClick;
                Canvas.SizeChanged += CanvasOnSizeChanged;
                Canvas.Children.Add(tile);
                Tiles[tile.Row, tile.Col] = tile;
            }
        }

        private void Swap(Tile b1, Tile b2)
        {
            
            b1.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(Canvas.GetLeft(b2), new Duration(TimeSpan.FromSeconds(0.5))));
            b1.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(b2), new Duration(TimeSpan.FromSeconds(0.5))));
            b2.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(Canvas.GetLeft(b1), new Duration(TimeSpan.FromSeconds(0.5))));
            b2.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(b1), new Duration(TimeSpan.FromSeconds(0.5))));
        }

        private void TileClick(object sender, EventArgs args)
        {;
            var tile = sender as Tile;
            switch (State)
            {
                case GameState.SelectingTileToSwap:
                {
                    SelectedTile = tile;
                    State = GameState.SelectingTileToSwapWith;
                    break;
                }
                case GameState.SelectingTileToSwapWith:
                {
                    if (tile.Row + 1 != SelectedTile.Row && tile.Row - 1 != SelectedTile.Row
                        && tile.Col + 1 != SelectedTile.Col && tile.Col - 1 != SelectedTile.Col
                        && tile != SelectedTile)
                    {
                        SelectedTileStoryBoard.Stop(SelectedTile);
                        State = GameState.SelectingTileToSwap;
                        TileClick(tile, args);
                        break;
                    }
                    SwapTile = tile;
                    SelectedTileStoryBoard.Stop(SelectedTile);
                    Swap(SelectedTile, SwapTile);
                    State = GameState.ComputingResult;
                    break;
                }
                case GameState.ComputingResult:
                    Swap(SelectedTile, SwapTile);
                    break;
            }
            
        }

        private void CanvasOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var tileHorSpace = Canvas.ActualWidth / 8;
            var tileVerSpace = Canvas.ActualHeight / 8;
            var tileWidth = tileHorSpace * 0.9;
            var marginLeft = tileHorSpace * 0.05;
            var tileHeight = tileVerSpace * 0.9;
            var marginTop = tileVerSpace * 0.05;
            foreach (var uiElem in (sender as Canvas).Children)
            {
                if (uiElem is Tile tile)
                {
                    tile.Left = tile.Col * tileHorSpace + marginLeft;
                    tile.Top = tile.Row * tileVerSpace + marginTop;
                    tile.TileWidth = tileWidth;
                    tile.TileHeight = tileHeight;
                    tile.Width = tileWidth;
                    tile.Height = tileHeight;
                    Canvas.SetLeft(tile, tile.Left);
                    Canvas.SetTop(tile, tile.Top);
                }
            }
            Canvas.InvalidateMeasure();
            Canvas.InvalidateArrange();
            Canvas.InvalidateVisual();
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
