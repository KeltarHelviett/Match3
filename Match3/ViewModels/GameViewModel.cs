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
                SelectedTileStoryBoard.Begin(SelectedButton, true);
        }

        #endregion

        #region Private Fields

        private int _timeLeft = 60;

        #endregion

        #region Public Properties

        public Canvas Canvas { get; }

        public Dictionary<TileType, BitmapImage> Images = new Dictionary<TileType, BitmapImage>
        {
            { TileType.Triangle, new BitmapImage(new Uri($"pack://application:,,,/Images/{TileType.Triangle.ToString()}.png"))},
            { TileType.Rectangle, new BitmapImage(new Uri($"pack://application:,,,/Images/{TileType.Rectangle.ToString()}.png"))},
        };

        public ObservableCollection<Tile> Tiles { get; set; }

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

        public Tile SelectedTile => SelectedButton.Tag as Tile;

        public GameState State { get; set; } = GameState.SelectingTileToSwap;

        public Storyboard SelectedTileStoryBoard { get; set; } = new Storyboard() { FillBehavior = FillBehavior.Stop };
        public DoubleAnimation SpinAnimation { get; set; } = 
            new DoubleAnimation(-360, new Duration(new TimeSpan(0, 0, 2))) {FillBehavior = FillBehavior.Stop};

        public Button SelectedButton { get; set; }

        #endregion

        #region Private Methods

        private void TileClick(object sender, EventArgs args)
        {
            var btn = sender as Button;
            var tile = btn.Tag as Tile;
            switch (State)
            {
                case GameState.SelectingTileToSwap:
                {
                    SelectedButton = btn;
                    Storyboard.SetTarget(SpinAnimation, btn);
                    Storyboard.SetTargetProperty(SpinAnimation, new PropertyPath("(Button.RenderTransform).(RotateTransform.Angle)"));
                        SelectedTileStoryBoard.Begin(btn, true);
                    State = GameState.SelectingTileToSwapWith;
                    break;
                }
                case GameState.SelectingTileToSwapWith:
                {
                    if (tile.Row + 1 != SelectedTile.Row && tile.Row - 1 != SelectedTile.Row
                                                         && tile.Col + 1 != SelectedTile.Col 
                                                         && tile.Col - 1 != SelectedTile.Col)
                    {
                        SelectedTileStoryBoard.Stop(SelectedButton);
                        State = GameState.SelectingTileToSwap;
                        TileClick(btn, args);
                        break;
                    }
                    SelectedTileStoryBoard.Stop(SelectedButton);
                    SelectedButton.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(tile.Left, new Duration(TimeSpan.FromSeconds(0.5))));
                    SelectedButton.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(tile.Top, new Duration(TimeSpan.FromSeconds(0.5))));
                    btn.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(SelectedTile.Left, new Duration(TimeSpan.FromSeconds(0.5))));
                    btn.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(SelectedTile.Top, new Duration(TimeSpan.FromSeconds(0.5))));
                    State = GameState.SelectingTileToSwap;
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
            var tileWidth = tileHorSpace * 0.8;
            var marginLeft = tileHorSpace * 0.1;
            var tileHeight = tileVerSpace * 0.8;
            var marginTop = tileVerSpace * 0.1;
            foreach (var uiElem in (sender as Canvas).Children)
            {
                if (uiElem is Button button)
                {
                    if (button.Tag is Tile tile)
                    {
                        tile.Left = tile.Col * tileHorSpace + marginLeft;
                        tile.Top = tile.Row * tileVerSpace + marginTop;
                        tile.Width = tileWidth;
                        tile.Height = tileHeight;
                        button.Width = tileWidth;
                        button.Height = tileHeight;
                        Canvas.SetLeft(button, tile.Left);
                        Canvas.SetTop(button, tile.Top);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public void Init()
        {
            Tiles = new ObservableCollection<Tile>();
            var tileHorSpace = Canvas.ActualWidth / 8;
            var tileVerSpace = Canvas.ActualHeight / 8;
            var tileWidth = tileHorSpace * 0.8;
            var marginLeft = tileHorSpace * 0.1;
            var tileHeight = tileVerSpace * 0.8;
            var marginTop = tileVerSpace * 0.1;
            for (var i = 0; i < 64; ++i)
            {
                var tile = new Tile()
                {
                    Row = i / 8, Col = i % 8,
                    Type = TileType.Triangle,
                    Left = (i % 8) * tileHorSpace + (marginLeft),
                    Top = (i / 8) * tileVerSpace + (marginTop)
                };
                var btn = new Button()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Content = new Image
                    {
                        Source = Images[tile.Type], VerticalAlignment = VerticalAlignment.Center,
                        Stretch = Stretch.Fill,
                    },
                    Tag = tile,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(0),
                };
                btn.Click += TileClick;
                Canvas.SizeChanged += CanvasOnSizeChanged;
                Canvas.Children.Add(btn);
                Canvas.SetLeft(btn, tile.Left);
                Canvas.SetTop(btn, tile.Top);
                Tiles.Add(tile);
            }
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
