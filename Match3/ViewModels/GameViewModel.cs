using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Match3.Annotations;

namespace Match3.ViewModels
{
    class GameViewModel: INotifyPropertyChanged
    {
        #region Ctor

        public GameViewModel()
        {
            Timer = new DispatcherTimer(
                new TimeSpan(0, 0, 1), 
                DispatcherPriority.DataBind, 
                (sender, args) => OnPropertyChanged(nameof(TimeLeft)),
                Dispatcher.CurrentDispatcher);
            Timer.Tick += (sender, args) => TimeLeft -= 1;
            Timer.Start();
        }

        #endregion

        #region Private Fields

        private int _timeLeft = 60;

        #endregion

        #region Public Properties

        public DispatcherTimer Timer { get; }
        
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
