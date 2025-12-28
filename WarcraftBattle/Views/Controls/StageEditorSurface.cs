using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Views.Controls
{
    public class StageEditorSurface : Control
    {
        private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(20, 25, 20));
        private static readonly Brush GroundBrush = new SolidColorBrush(Color.FromRgb(55, 70, 45));
        private static readonly Brush BuildingBrush = new SolidColorBrush(Color.FromRgb(70, 90, 140));
        private static readonly Brush BuildingEnemyBrush = new SolidColorBrush(Color.FromRgb(140, 70, 70));
        private static readonly Brush ObstacleBrush = new SolidColorBrush(Color.FromRgb(80, 80, 90));
        private static readonly Brush TreeBrush = new SolidColorBrush(Color.FromRgb(40, 90, 50));
        private static readonly Pen GridPen = new Pen(new SolidColorBrush(Color.FromArgb(35, 0, 0, 0)), 1);
        private static readonly Pen OutlinePen = new Pen(new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)), 1);
        private static readonly Pen SelectionPen = new Pen(new SolidColorBrush(Color.FromRgb(140, 220, 140)), 2);

        static StageEditorSurface()
        {
            BackgroundBrush.Freeze();
            GroundBrush.Freeze();
            BuildingBrush.Freeze();
            BuildingEnemyBrush.Freeze();
            ObstacleBrush.Freeze();
            TreeBrush.Freeze();
            GridPen.Freeze();
            OutlinePen.Freeze();
            SelectionPen.Freeze();
        }

        public StageEditorSurface()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public static readonly DependencyProperty StageInfoProperty = DependencyProperty.Register(
            nameof(StageInfo),
            typeof(StageInfo),
            typeof(StageEditorSurface),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CameraXProperty = DependencyProperty.Register(
            nameof(CameraX),
            typeof(double),
            typeof(StageEditorSurface),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty CameraYProperty = DependencyProperty.Register(
            nameof(CameraY),
            typeof(double),
            typeof(StageEditorSurface),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
            nameof(Zoom),
            typeof(double),
            typeof(StageEditorSurface),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceZoom)
        );

        public static readonly DependencyProperty GridSizeProperty = DependencyProperty.Register(
            nameof(GridSize),
            typeof(double),
            typeof(StageEditorSurface),
            new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty SelectedPlacementIndexProperty = DependencyProperty.Register(
            nameof(SelectedPlacementIndex),
            typeof(int),
            typeof(StageEditorSurface),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty SelectedObstacleIndexProperty = DependencyProperty.Register(
            nameof(SelectedObstacleIndex),
            typeof(int),
            typeof(StageEditorSurface),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty AutoRefreshProperty = DependencyProperty.Register(
            nameof(AutoRefresh),
            typeof(bool),
            typeof(StageEditorSurface),
            new FrameworkPropertyMetadata(true)
        );

        public StageInfo StageInfo
        {
            get => (StageInfo)GetValue(StageInfoProperty);
            set => SetValue(StageInfoProperty, value);
        }

        public double CameraX
        {
            get => (double)GetValue(CameraXProperty);
            set => SetValue(CameraXProperty, value);
        }

        public double CameraY
        {
            get => (double)GetValue(CameraYProperty);
            set => SetValue(CameraYProperty, value);
        }

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public double GridSize
        {
            get => (double)GetValue(GridSizeProperty);
            set => SetValue(GridSizeProperty, value);
        }

        public int SelectedPlacementIndex
        {
            get => (int)GetValue(SelectedPlacementIndexProperty);
            set => SetValue(SelectedPlacementIndexProperty, value);
        }

        public int SelectedObstacleIndex
        {
            get => (int)GetValue(SelectedObstacleIndexProperty);
            set => SetValue(SelectedObstacleIndexProperty, value);
        }

        public bool AutoRefresh
        {
            get => (bool)GetValue(AutoRefreshProperty);
            set => SetValue(AutoRefreshProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));

            var stage = StageInfo;
            if (stage == null) return;

            var matrix = new Matrix();
            matrix.Translate(-CameraX, -CameraY);
            matrix.Scale(Zoom, Zoom);
            dc.PushTransform(new MatrixTransform(matrix));

            DrawGround(dc, stage);
            DrawGrid(dc, stage);
            DrawPlacements(dc, stage);
            DrawObstacles(dc, stage);

            dc.Pop();
        }

        private static object CoerceZoom(DependencyObject d, object value)
        {
            var zoom = (double)value;
            if (double.IsNaN(zoom) || double.IsInfinity(zoom)) return 1.0;
            return Math.Max(0.1, zoom);
        }

        private void DrawGround(DrawingContext dc, StageInfo stage)
        {
            var mapRect = new Rect(0, 0, stage.MapWidth, stage.MapHeight);
            dc.DrawRectangle(GroundBrush, OutlinePen, mapRect);
        }

        private void DrawGrid(DrawingContext dc, StageInfo stage)
        {
            var grid = GridSize <= 0 ? 100.0 : GridSize;
            for (double x = 0; x <= stage.MapWidth; x += grid)
            {
                dc.DrawLine(GridPen, new Point(x, 0), new Point(x, stage.MapHeight));
            }

            for (double y = 0; y <= stage.MapHeight; y += grid)
            {
                dc.DrawLine(GridPen, new Point(0, y), new Point(stage.MapWidth, y));
            }
        }

        private void DrawPlacements(DrawingContext dc, StageInfo stage)
        {
            for (var i = 0; i < stage.Placements.Count; i++)
            {
                var placement = stage.Placements[i];
                var size = ResolveSize(placement.Width, placement.Height, 160);
                var rect = new Rect(placement.X - size.Width / 2, placement.Y - size.Height / 2, size.Width, size.Height);

                var brush = string.Equals(placement.Team, "Orc", StringComparison.OrdinalIgnoreCase)
                    ? BuildingEnemyBrush
                    : BuildingBrush;

                if (Math.Abs(placement.Rotation) > 0.01)
                {
                    dc.PushTransform(new RotateTransform(placement.Rotation, placement.X, placement.Y));
                }

                dc.DrawRoundedRectangle(brush, OutlinePen, rect, 6, 6);

                if (i == SelectedPlacementIndex)
                {
                    dc.DrawRoundedRectangle(Brushes.Transparent, SelectionPen, rect, 6, 6);
                }

                if (Math.Abs(placement.Rotation) > 0.01)
                {
                    dc.Pop();
                }
            }
        }

        private void DrawObstacles(DrawingContext dc, StageInfo stage)
        {
            for (var i = 0; i < stage.FixedObstacles.Count; i++)
            {
                var obstacle = stage.FixedObstacles[i];
                var size = ResolveSize(obstacle.Width, obstacle.Height, 120);
                var rect = new Rect(obstacle.X - size.Width / 2, obstacle.Y - size.Height / 2, size.Width, size.Height);

                var brush = obstacle.Type != null && obstacle.Type.Contains("tree", StringComparison.OrdinalIgnoreCase)
                    ? TreeBrush
                    : ObstacleBrush;

                if (Math.Abs(obstacle.Rotation) > 0.01)
                {
                    dc.PushTransform(new RotateTransform(obstacle.Rotation, obstacle.X, obstacle.Y));
                }

                dc.DrawEllipse(brush, OutlinePen, new Point(obstacle.X, obstacle.Y), size.Width / 2, size.Height / 2);

                if (i == SelectedObstacleIndex)
                {
                    dc.DrawEllipse(Brushes.Transparent, SelectionPen, new Point(obstacle.X, obstacle.Y), size.Width / 2, size.Height / 2);
                }

                if (Math.Abs(obstacle.Rotation) > 0.01)
                {
                    dc.Pop();
                }
            }
        }

        private static Size ResolveSize(double width, double height, double fallback)
        {
            var resolvedW = width <= 0 ? fallback : width;
            var resolvedH = height <= 0 ? fallback : height;
            return new Size(resolvedW, resolvedH);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!AutoRefresh || StageInfo == null) return;
            InvalidateVisual();
        }
    }
}
