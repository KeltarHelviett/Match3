using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Match3.Annotations;
using Match3.Models;
using Match3.Utilities;

namespace Match3.ViewModels
{
    enum GameState
    {
        SelectingTileToSwap, SelectingTileToSwapWith, Animating, ComputingResult,
    }

    class GameViewModel: INotifyPropertyChanged
    {
        #region Ctor

        public GameViewModel(Canvas canvas)
        {
            Canvas = canvas;
            Canvas.MouseMove += (sender, args) => Coords = $"{args.GetPosition(Canvas).X}  {args.GetPosition(Canvas).Y}";
            CloseWindow = new RelayCommand((sender) =>
            {
                if (TimeLeft == 0)
                {
                    MessageBox.Show($"Your score is {Score}", "Game over", MessageBoxButton.OK,
                        MessageBoxImage.Information,
                        MessageBoxResult.OK);
                    (sender as Window).Close();
                }
            });
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
                State = GameState.Animating;
                if (_animationCount == 0)
                    OnAnimationsCompleted();
            }
        }

        public Canvas Canvas { get; }

        public ICommand CloseWindow { get; set; }

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
                if (value == 0)
                    Timer.Stop();
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
            var pt = PossibleTiles[_random.Next(PossibleTiles.Count)];
            var tile = (Tile)Activator.CreateInstance(pt.Item1);
            tile.Row = row;
            tile.Col = col;
            tile.Width = Canvas.ActualWidth / 8 * 0.9;
            tile.Height = Canvas.ActualHeight / 8 * 0.9;
            tile.Fill = pt.Item2;
            tile.MoveStarted += (sender, args) => AnimationCount += 1;
            tile.Moved += (sender, args) => AnimationCount -= 1;
            tile.FadeStarted += (sender, args) => AnimationCount += 1;
            tile.Faded += (sender, args) =>
            {
                AnimationCount -= 1;
                Canvas.Children.Remove(tile);
            };
            Canvas.SetLeft(tile, GetLeft(tile));
            Canvas.SetTop(tile, -(8 - tile.Row) * (Canvas.ActualHeight / 8) - Canvas.ActualHeight / 8 * 0.05);
            tile.MouseLeftButtonUp += TileClick;
            Canvas.Children.Add(tile);
            Tiles[tile.Row, tile.Col] = tile;
            tile.Move(GetLeft(tile), GetTop(tile));
            return tile;
        }

        public void Init()
        {
            Tiles = new Tile[8, 8];
            for (var i = 0; i < 64; ++i)
                CreateTile(i / 8, i % 8);
            Canvas.SizeChanged += CanvasOnSizeChanged;
        }

        private void Swap(Tile t1, Tile t2)
        {
            t2.Move(GetLeft(t1), GetTop(t1));
            t1.Move(GetLeft(t2), GetTop(t2));
            Tiles[t1.Row, t1.Col] = t2;
            Tiles[t2.Row, t2.Col] = t1;
            var t2Row = t2.Row;
            var t2Col = t2.Col;
            t2.Row = t1.Row;
            t2.Col = t1.Col;
            t1.Row = t2Row;
            t1.Col = t2Col;
        }

        private void Move(Tile tile, int row, int col, double left, double top)
        {
            tile.Row = row;
            tile.Col = col;
            Tiles[row, col] = tile;
            tile.Move(left, top);
        }

        public void Select(Tile tile)
        {
            SelectedTile = tile;
            tile?.Select();
        }

        public void Deselect(Tile tile)
        {
            SelectedTile = null;
            tile?.Deselect();
        }

        private bool first = false;
        private void TileClick(object sender, EventArgs args)
        {
            bool CanSwap(int x1, int x2, int y1, int y2) { return x1 == x2 && (y1 == y2 - 1 || y1 == y2 + 1); }
            var tile = sender as Tile;
            switch (State)
            {
                case GameState.SelectingTileToSwap:
                    Select(tile);
                    State = GameState.SelectingTileToSwapWith;
                    break;
                case GameState.SelectingTileToSwapWith:
                    if (CanSwap(SelectedTile.Row, tile.Row, SelectedTile.Col, tile.Col)
                        || CanSwap(SelectedTile.Col, tile.Col, SelectedTile.Row, tile.Row))
                    {
                        SwapTile = tile;
                        SelectedTile?.Deselect();
                        first = !first;
                        Swap(SelectedTile, SwapTile);
                        break;
                    }
                    State = GameState.SelectingTileToSwap;
                    if (SelectedTile == tile)
                    {
                        Deselect(SelectedTile);
                        break;
                    }
                    Deselect(SelectedTile);
                    TileClick(tile, args);
                    break;
            }
            
        }

        private void CanvasOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var uiElem in (sender as Canvas).Children)
            {
                if (uiElem is Tile tile)
                {
                    tile.Width = Canvas.ActualWidth / 8 * 0.9;
                    tile.Height = Canvas.ActualHeight / 8 * 0.9;
                    Canvas.SetLeft(tile, GetLeft(tile));
                    Canvas.SetTop(tile, GetTop(tile));
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
                        emptySpace.Enqueue(new Tuple<int, int, double, double>(tile.Row, tile.Col, GetLeft(tile), GetTop(tile)));
                        tile.Fade();
                    }
                    else
                    {
                        if (emptySpace.Count != 0)
                        {
                            var es = emptySpace.Dequeue();
                            emptySpace.Enqueue(new Tuple<int, int, double, double>(tile.Row, tile.Col, GetLeft(tile), GetTop(tile)));
                            Move(tile, es.Item1, es.Item2, es.Item3, es.Item4);
                        }
                    }
                }
                foreach (var es in emptySpace)
                    CreateTile(es.Item1, es.Item2);
                emptySpace.Clear();
            }
        }

        private double GetLeft(Tile tile)
        {
            return tile.Col * (Canvas.ActualWidth / 8) + (Canvas.ActualWidth / 8) * 0.05;
        }

        private double GetTop(Tile tile)
        {
            return tile.Row * (Canvas.ActualHeight / 8) + (Canvas.ActualHeight / 8) * 0.05;
        }

        private void Compute()
        {
            State = GameState.ComputingResult;
            if (!Check())
            {
                if (SelectedTile != null && SwapTile != null)
                    Swap(SwapTile, SelectedTile);
            }
            else 
                Fill();
            Deselect(SelectedTile);
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
