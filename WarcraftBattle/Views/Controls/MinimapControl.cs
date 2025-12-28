using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WarcraftBattle.Engine;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Views.Controls
{
    public class MinimapControl : Control
    {
        // [Optimization] Layered rendering
        private readonly DrawingVisual _terrainLayer = new DrawingVisual();
        private bool _isTerrainDirty = true;

        // [Visual Optimization] Smooth Fog Texture
        private WriteableBitmap _fogBitmap;
        private byte[] _fogPixelBuffer;

        public static readonly DependencyProperty EngineProperty = DependencyProperty.Register(
            "Engine",
            typeof(GameEngine),
            typeof(MinimapControl),
            new PropertyMetadata(null, OnEngineChanged));

        public GameEngine Engine { get => (GameEngine)GetValue(EngineProperty); set => SetValue(EngineProperty, value); }

        public MinimapControl()
        {
            // Enable Bilinear Filtering for smooth fog edges
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Linear);

            CompositionTarget.Rendering += (s, e) => InvalidateVisual();
            MouseLeftButtonDown += HandleMouse;
            MouseMove += HandleMouse;

            // Add the visual child for the rendering system
            AddVisualChild(_terrainLayer);
        }

        private void HandleMouse(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && Engine != null)
            {
                var pos = e.GetPosition(this);

                // 1. 小地图点击的是 "逻辑坐标" (2D 俯视)
                double ratioX = pos.X / ActualWidth;
                double ratioY = pos.Y / ActualHeight;

                double targetLogicX = ratioX * Engine.WorldWidth;
                double targetLogicY = ratioY * Engine.MapDepth;

                // 2. 将目标逻辑点转换为 ISO 屏幕坐标
                // 因为 CameraX/Y 是基于 ISO 空间的偏移量
                PointD targetIso = GameEngine.WorldToIso(targetLogicX, targetLogicY);

                // 3. 计算摄像机位置 (让目标点位于屏幕中心)
                double viewW = Engine.ViewportWidth / Engine.Zoom;
                double viewH = Engine.ViewportHeight / Engine.Zoom;

                Engine.CameraX = targetIso.X - viewW / 2;
                Engine.CameraY = targetIso.Y - viewH / 2;

                // (可选) 这里可以调用 ClampCamera() 限制范围，但在外部 Engine 更新时通常会再次 Clamp
            }
        }

        private static void OnEngineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MinimapControl control)
            {
                control._isTerrainDirty = true;
            }
        }

        // Override VisualChildrenCount and GetVisualChild for DrawingVisual integration
        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index)
        {
            if (index == 0) return _terrainLayer;
            throw new ArgumentOutOfRangeException();
        }

        // [Fix] Override Measure and Arrange to prevent InvalidCastException
        // Control's default implementation tries to cast VisualChildren to UIElement,
        // but we are using DrawingVisual, which is not a UIElement.
        protected override Size MeasureOverride(Size constraint)
        {
            return new Size(MinWidth, MinHeight);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return arrangeBounds;
        }

        private void RedrawTerrainLayer()
        {
            if (Engine == null || ActualWidth == 0 || ActualHeight == 0) return;

            using (var dc = _terrainLayer.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ActualWidth, ActualHeight));

                // Here you can add logic to draw the actual terrain from MapData if available
                // For now, it's just a black background.
            }
            _isTerrainDirty = false;
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Engine == null) return;

            // 1. Redraw static terrain layer if needed
            if (_isTerrainDirty)
            {
                RedrawTerrainLayer();
            }

            // The base OnRender will draw the visual children, which includes our _terrainLayer.
            base.OnRender(dc);

            // 计算小地图缩放比例 (逻辑坐标 -> 小地图像素)
            double scaleX = ActualWidth / Engine.WorldWidth;
            double scaleY = ActualHeight / Engine.MapDepth;

            // 2. Draw dynamic elements (units, camera view) on top
            foreach (var entity in Engine.Entities)
            {
                if (entity.HP <= 0) continue;

                double mx = entity.X * scaleX;
                double my = entity.Y * scaleY;

                Brush b = ThemePalette.NeutralBrush;
                if (entity.Team == TeamType.Human)
                    b = ThemePalette.TeamHumanBrush;
                else if (entity.Team == TeamType.Orc)
                    b = ThemePalette.TeamOrcBrush;

                if (entity is Building)
                    b =
                        (entity.Team == TeamType.Human)
                            ? ThemePalette.BuildingHumanBrush
                            : ThemePalette.BuildingOrcBrush;
                if (entity is LootItem)
                    b = ThemePalette.LootBrush;

                double size = (entity is Building) ? 4 : 2;
                dc.DrawEllipse(b, null, new Point(mx, my), size, size);
            }

            // [Visual Optimization] Draw Smooth Fog of War Overlay
            if (Engine.Fog != null && Engine.Fog.FogAlphaMap != null)
            {
                int fw = Engine.Fog.TextureWidth;
                int fh = Engine.Fog.TextureHeight;

                // Initialize or resize bitmap if needed
                if (_fogBitmap == null || _fogBitmap.PixelWidth != fw || _fogBitmap.PixelHeight != fh)
                {
                    _fogBitmap = new WriteableBitmap(fw, fh, 96, 96, PixelFormats.Bgra32, null);
                    _fogPixelBuffer = new byte[fw * fh * 4];
                }

                var alphas = Engine.Fog.FogAlphaMap;
                // Convert AlphaMap (byte) to BGRA (byte[4]) for the WriteableBitmap
                for (int i = 0; i < alphas.Length; i++)
                {
                    int ptr = i * 4;
                    byte a = alphas[i];
                    _fogPixelBuffer[ptr] = 0;     // B (Black)
                    _fogPixelBuffer[ptr + 1] = 0; // G (Black)
                    _fogPixelBuffer[ptr + 2] = 0; // R (Black)
                    _fogPixelBuffer[ptr + 3] = a; // A (Alpha from Fog Map)
                }

                _fogBitmap.WritePixels(new Int32Rect(0, 0, fw, fh), _fogPixelBuffer, fw * 4, 0);
                dc.DrawImage(_fogBitmap, new Rect(0, 0, ActualWidth, ActualHeight));
            }

            // 3. Draw camera viewport rectangle
            if (Engine.ViewportWidth > 0)
            {
                double w = Engine.ViewportWidth;
                double h = Engine.ViewportHeight;

                PointD p1 = Engine.ScreenToWorld(0, 0);
                PointD p2 = Engine.ScreenToWorld(w, 0);
                PointD p3 = Engine.ScreenToWorld(w, h);
                PointD p4 = Engine.ScreenToWorld(0, h);

                double minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
                double maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
                double minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
                double maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

                double rectX = minX * scaleX;
                double rectY = minY * scaleY;
                double rectW = (maxX - minX) * scaleX;
                double rectH = (maxY - minY) * scaleY;

                dc.DrawRectangle(
                    null,
                    ThemePalette.MinimapViewportPen,
                    new Rect(rectX, rectY, rectW, rectH)
                );
            }
        }
    }
}
