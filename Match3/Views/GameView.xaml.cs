using System.Windows;
using Match3.ViewModels;

namespace Match3.Views
{
    public partial class GameView : Window
    {
        public GameView()
        {
            InitializeComponent();
            DataContext = new GameViewModel();
        }
    }
}
