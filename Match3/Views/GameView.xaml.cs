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
            Loaded += delegate
            {
                var timer = new DispatcherTimer
                (
                    new TimeSpan(0, 0, 1),
                    DispatcherPriority.DataBind,
                    (sender, args) =>
                    {
                        //ScoreLabel.Content = (sender as DispatcherTimer).
                    },
                    Dispatcher.CurrentDispatcher
                );
                DataContext = new GameViewModel(MainCanvas);
            };
        }

        #endregion

        #region Private Fields

        private int _timeLeft = 60;

        #endregion

        #region Public Properties

        

        #endregion

    }
}
