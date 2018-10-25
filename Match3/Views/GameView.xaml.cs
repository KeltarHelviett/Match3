using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Match3.Models;
using Match3.ViewModels;

namespace Match3.Views
{
    public partial class GameView : Window
    {
        #region Ctor

        public GameView()
        {
            InitializeComponent();
            MainCanvas.Loaded += (sender, args) => DataContext = new GameViewModel(MainCanvas);
        }

        #endregion

        #region Private Methods

        private void GameViewOnClosed(object sender, EventArgs e)
        {
            (DataContext as GameViewModel).Clear();
        }

        #endregion
    }
}
