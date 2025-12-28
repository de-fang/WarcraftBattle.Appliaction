using System.Windows;
using System.Windows.Input;
using WarcraftBattle.ViewModels;

namespace WarcraftBattle.Views
{
    public partial class ShellView : Window
    {
        public ShellView()
        {
            InitializeComponent();
            KeyDown += ShellView_KeyDown;
        }

        private void ShellView_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is ShellViewModel vm)
            {
                vm.HandleKeyDown(e.Key);

                // Also pass to the engine's input manager for direct control
                if (vm.Engine?.Input != null)
                {
                    vm.Engine.Input.HandleKeyDown(e.Key);
                }
            }
        }
    }
}