using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WarcraftBattle.Editor.ViewModels;

namespace WarcraftBattle.Editor.Views
{
    public partial class ShellView : Window
    {
        public ShellView()
        {
            InitializeComponent();
        }

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is not Thumb thumb || thumb.Tag is not IEditablePlacement placement)
            {
                return;
            }

            placement.X += e.HorizontalChange;
            placement.Y += e.VerticalChange;
        }

        private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Thumb thumb || thumb.Tag is not IEditablePlacement placement)
            {
                return;
            }

            if (DataContext is ShellViewModel viewModel)
            {
                viewModel.SelectedItem = placement;
            }
        }
    }
}
