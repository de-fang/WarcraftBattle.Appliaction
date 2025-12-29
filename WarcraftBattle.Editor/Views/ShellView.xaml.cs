using System.Windows;
using System.Windows.Input;

namespace WarcraftBattle.Editor.Views
{
    public partial class ShellView : Window
    {
        public ShellView()
        {
            InitializeComponent();
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                return;
            }

            DragMove();
        }
    }
}
