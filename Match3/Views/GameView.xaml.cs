using System.Windows;
using System.Windows.Media;
using Match3.Models;
using Match3.ViewModels;

namespace Match3.Views
{
    public partial class GameView : Window
    {
        public GameView()
        {
            InitializeComponent();
            Loaded += delegate
            {
                DataContext = new GameViewModel(MainCanvas);
            };
        }
    }
}
