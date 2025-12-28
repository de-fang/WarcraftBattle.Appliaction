using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WarcraftBattle.Editor
{
    public partial class MapView : UserControl
    {
        private TimeSpan _lastRenderTime;

        public MapView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private MapViewModel ViewModel => (MapViewModel)DataContext;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EditorSurface.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            EditorSurface.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;

            CompositionTarget.Rendering += OnRendering;
            _lastRenderTime = TimeSpan.FromTicks(DateTime.Now.Ticks);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            EditorSurface.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            EditorSurface.PreviewMouseRightButtonDown -= OnPreviewMouseRightButtonDown;
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            var args = (RenderingEventArgs)e;
            if (args.RenderingTime == _lastRenderTime)
            {
                return;
            }

            var dt = (args.RenderingTime - _lastRenderTime).TotalSeconds;
            _lastRenderTime = args.RenderingTime;

            if (dt > 0.1)
            {
                dt = 0.1;
            }

            ViewModel.Engine.Update(dt);
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.PlacementMode == EditorPlacementMode.Select)
            {
                return;
            }

            if (!ViewModel.CanPlaceAtGhost())
            {
                e.Handled = true;
                return;
            }

            switch (ViewModel.PlacementMode)
            {
                case EditorPlacementMode.Building:
                    ViewModel.PlaceBuildingAtGhost();
                    break;
                case EditorPlacementMode.Obstacle:
                    ViewModel.PlaceObstacleAtGhost();
                    break;
            }

            e.Handled = true;
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.PlacementMode != EditorPlacementMode.Select)
            {
                ViewModel.SetPlacementMode(EditorPlacementMode.Select);
                e.Handled = true;
            }
        }
    }
}
