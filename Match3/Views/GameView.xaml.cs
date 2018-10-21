using System.Windows;
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
                MainCanvas.UpdateLayout();
                MainCanvas.InvalidateArrange();
                MainCanvas.InvalidateMeasure();
                DataContext = new GameViewModel(MainCanvas);
            };
            
        }
    }
}
