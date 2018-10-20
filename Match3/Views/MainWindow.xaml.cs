using System.Windows;
using Match3.Views;

namespace Match3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PlayBtnClick(object sender, RoutedEventArgs e)
        {
            new GameView().Show();
        }
    }
}
