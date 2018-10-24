﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            Timer = new DispatcherTimer
            (
                new TimeSpan(0, 0, 1), 
                DispatcherPriority.DataBind, 
                (sender, args) => OnPropertyChanged(nameof(TimeLeft)),
                Dispatcher.CurrentDispatcher
            );
            Timer.Tick += (sender, args) => TimeLeft -= 1;
            Init();
            Timer.Start();
            Compute();
        }

        #endregion

        #region Debug

        private string _coords = string.Empty;

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

        private readonly Random _random = new Random();

        private int _score = 0;

        private int _animationCount = 0;

        #endregion

        #region Public Properties

        public int AnimationCount
        {
            get => _animationCount;
            set
            {
                _animationCount = value;
                if (_animationCount == 0)
                    OnAnimationsCompleted();
            }
        }

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

        public int Score
        {
            get => _score;
            set
            {
                if (_score == value)
                    return;
                _score = value;
                OnPropertyChanged(nameof(Score));
            }
        }

        public Tile SelectedTile { get; set; }

        public Tile SwapTile { get; set; }

        public GameState State { get; set; } = GameState.ComputingResult;

        #endregion

        #region Private Methods

        private Tile CreateTile(int row, int col)
        {
            var tileHorSpace = Canvas.ActualWidth / 8;
            var tileVerSpace = Canvas.ActualHeight / 8;
            var tileWidth = tileHorSpace * 0.9;
            var marginLeft = tileHorSpace * 0.05;
            var tileHeight = tileVerSpace * 0.9;
            var marginTop = tileVerSpace * 0.05;
            var pt = PossibleTiles[_random.Next(PossibleTiles.Count)];

            var tile = (Tile)Activator.CreateInstance(pt.Item1);
            tile.Row = row;
            tile.Col = col;
            tile.Left = tile.Col * tileHorSpace + marginLeft;
            tile.Top = tile.Row * tileVerSpace + marginTop;
            tile.Stroke = Brushes.Black;
            tile.StrokeThickness = 1;
            tile.Width = tileWidth;
            tile.Height = tileHeight;
            tile.Fill = pt.Item2;
            tile.MoveStarted += (sender, args) => AnimationCount += 1;
            tile.Moved += (sender, args) => AnimationCount -= 1;
            tile.FadeStarted += (sender, args) => AnimationCount += 1;
            tile.Faded += (sender, args) =>
            {
                AnimationCount -= 1;
                Canvas.Children.Remove(tile);
            };
            Canvas.SetLeft(tile, tile.Left);
            Canvas.SetTop(tile, -(8 - tile.Row) * tileVerSpace - marginTop);
            tile.MouseLeftButtonUp += TileClick;
            Canvas.Children.Add(tile);
            Tiles[tile.Row, tile.Col] = tile;
            tile.Move(tile.Col * tileHorSpace + marginLeft, tile.Row * tileVerSpace + marginTop);
            return tile;
        }

        public void Init()
        {
            Tiles = new Tile[8, 8];
            for (var i = 0; i < 64; ++i)
            {
                var tile = CreateTile(i / 8, i % 8);
            }
            Canvas.SizeChanged += CanvasOnSizeChanged;
        }

        private void Swap(Tile t1, Tile t2)
        {
            t2.Move(t1.Left, t1.Top);
            t1.Move(t2.Left, t2.Top);
            Tiles[t1.Row, t1.Col] = t2;
            Tiles[t2.Row, t2.Col] = t1;
            int t2Row = t2.Row, t2Col = t2.Col;
            double t2Left = t2.Left, t2Top = t2.Top;
            t2.Row = t1.Row; t2.Col = t1.Col;
            t1.Row = t2Row; t1.Col = t2Col;
            t2.Left = t1.Left; t2.Top = t1.Top;
            t1.Left = t2Left; t1.Top = t2Top;
        }

        private void Move(Tile tile, int row, int col, double left, double top)
        {
            tile.Row = row;
            tile.Col = col;
            Tiles[row, col] = tile;
            tile.Move(left, top);
            tile.Left = left;
            tile.Top = top;
        }

        private void TileClick(object sender, EventArgs args)
        {
            bool CanSwap(int x1, int x2, int y1, int y2) { return x1 == x2 && (y1 == y2 - 1 || y1 == y2 + 1); }
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
                    if (CanSwap(SelectedTile.Row, tile.Row, SelectedTile.Col, tile.Col)
                        || CanSwap(SelectedTile.Col, tile.Col, SelectedTile.Row, tile.Row))
                    {
                        SwapTile = tile;
                        Swap(SelectedTile, SwapTile);
                        break;
                    }
                    State = GameState.SelectingTileToSwap;
                    TileClick(tile, args);
                    break;
                }
                case GameState.ComputingResult:
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

        private void OnAnimationsCompleted()
        {
            Compute();
        }

        private void Fill()
        {
            var emptySpace = new Queue<Tuple<int, int, double, double>>(); // Row, Col, Left, Top
            for (var i = 0; i < 8; ++i)
            {
                for (var j = 7; j >= 0; --j)
                {
                    var tile = Tiles[j, i];
                    if (tile.ToDelete)
                    {
                        emptySpace.Enqueue(new Tuple<int, int, double, double>(tile.Row, tile.Col, tile.Left, tile.Top));
                        tile.Fade();
                    }
                    else
                    {
                        if (emptySpace.Count != 0)
                        {
                            var es = emptySpace.Dequeue();
                            emptySpace.Enqueue(new Tuple<int, int, double, double>(tile.Row, tile.Col, tile.Left, tile.Top));
                            Move(tile, es.Item1, es.Item2, es.Item3, es.Item4);
                        }
                    }
                }
                foreach (var es in emptySpace)
                {
                    var tile = CreateTile(es.Item1, es.Item2);
                }
                emptySpace.Clear();
            }
        }

        private void Compute()
        {
            if (!Check())
            {
                if (SelectedTile != null && SwapTile != null)
                    Swap(SwapTile, SelectedTile);
            }
            else 
                Fill();
            SelectedTile = null;
            SwapTile = null;
            State = GameState.SelectingTileToSwap;
        }

        private bool Check()
        {
            var curRowType = Tiles[0, 0].Type;
            var curColType = curRowType;
            var curRowColor = Tiles[0, 0].Fill;
            var curColColor = curRowColor;
            var rowToDelete = new List<Tile>();
            var colToDelete = new List<Tile>();
            var result = false;

            void SetToDelete(List<Tile> tiles)
            {
                if (tiles.Count >= 3)
                {
                    tiles.ForEach(t =>
                    {
                        if (!t.ToDelete) Score++;
                        t.ToDelete = true;
                    });
                    result = true;
                }
                tiles.Clear();
            }

            void CheckTile(Tile tile, ref TileType type, ref Brush color, List<Tile> tiles)
            {
                if (tile.Type == type && tile.Fill == color)
                {
                    tiles.Add(tile);
                }
                else
                {
                    SetToDelete(tiles);
                    type = tile.Type;
                    color = tile.Fill;
                    tiles.Add(tile);
                }
            }

            for (var i = 0; i < 8; ++i)
            {
                for (var j = 0; j < 8; ++j)
                {
                    CheckTile(Tiles[i, j], ref curRowType, ref curRowColor, rowToDelete);
                    CheckTile(Tiles[j, i], ref curColType, ref curColColor, colToDelete);
                }
                SetToDelete(rowToDelete);
                SetToDelete(colToDelete);
            }
            return result;
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
