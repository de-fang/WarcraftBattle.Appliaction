using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WarcraftBattle.Engine;
using WarcraftBattle.Engine.Animation;
using WarcraftBattle.Engine.Components;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Views.Controls
{
    public class Floater
    {
        public string Text = "";
        public double IsoX,
            IsoY;
        public double Life = 1.0;
        public double VelY = -1.5;
        public double Gravity = 5.0;
        public double Size = 20;
        public Brush Color = Brushes.White;

        // [Optimization] Cache the expensive geometry
        public Geometry CachedGeometry;
        public double Width;
    }

    public static class GfxCache
    {
        public static readonly Pen BorderPen = new Pen(Brushes.Black, 1);
        public static readonly Pen SelectionPen = new Pen(Brushes.LightGreen, 2);
        public static readonly Pen FloaterBorderPen = new Pen(Brushes.Black, 2);
        public static readonly Brush ShadowBrush = new SolidColorBrush(
            Color.FromArgb(140, 0, 0, 0)
        );
        public static readonly Brush GroundBaseBrush = new SolidColorBrush(
            Color.FromRgb(50, 65, 40)
        );
        public static readonly Brush HPBarBg = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        public static readonly Brush HPBarHighlight = new SolidColorBrush(
            Color.FromArgb(100, 255, 255, 255)
        );
        public static readonly Brush BloodBrush = new SolidColorBrush(
            Color.FromArgb(220, 180, 20, 20)
        );
        public static readonly Brush SparkBrush = new SolidColorBrush(
            Color.FromArgb(220, 255, 160, 60)
        );

        // ������ˢ
        public static readonly Brush HumanStoneLight = new SolidColorBrush(
            Color.FromRgb(120, 130, 140)
        );
        public static readonly Brush HumanRoof = new SolidColorBrush(Color.FromRgb(40, 80, 160));
        public static readonly Brush OrcRoof = new SolidColorBrush(Color.FromRgb(160, 40, 40));

        static GfxCache()
        {
            BorderPen.Freeze();
            SelectionPen.Freeze();
            FloaterBorderPen.Freeze();
            ShadowBrush.Freeze();
            GroundBaseBrush.Freeze();
            HPBarBg.Freeze();
            HPBarHighlight.Freeze();
            BloodBrush.Freeze();
            SparkBrush.Freeze();
            HumanStoneLight.Freeze();
            HumanRoof.Freeze();
            OrcRoof.Freeze();
        }
    }

    public class GameSurface : Control
    {
        private object _lastMapData;

        private void ResetTerrainCache()
        {
            _cachedTerrainDrawing = null;
            _lastMapData = null;
        }

        private List<Entity> _cachedVisionSources = new List<Entity>();
        public static readonly DependencyProperty EngineProperty = DependencyProperty.Register(
            "Engine",
            typeof(GameEngine),
            typeof(GameSurface),
            new PropertyMetadata(
                null,
                (d, e) =>
                {
                    var gs = d as GameSurface;
                    if (gs == null) return;

                    // Unsubscribe from old engine's event
                    if (e.OldValue is GameEngine oldEngine)
                    {
                        oldEngine.OnMapChanged -= gs.OnEngineMapChanged;
                    }

                    // Subscribe to new engine's event
                    if (e.NewValue is GameEngine newEngine)
                    {
                        newEngine.OnMapChanged += gs.OnEngineMapChanged;
                    }

                    // When Engine instance changes, always clear cache
                    gs.ResetTerrainCache();
                }
            )
        );

        public static readonly DependencyProperty EnableGameInputProperty =
            DependencyProperty.Register(
                "EnableGameInput",
                typeof(bool),
                typeof(GameSurface),
                new PropertyMetadata(true)
            );

        private void OnEngineMapChanged()
        {
            // This is called by the engine's OnMapChanged event
            // We need to invalidate our drawing cache for the terrain.
            _cachedTerrainDrawing = null;
        }


        public GameEngine Engine
        {
            get => (GameEngine)GetValue(EngineProperty);
            set => SetValue(EngineProperty, value);
        }

        public bool EnableGameInput
        {
            get => (bool)GetValue(EnableGameInputProperty);
            set => SetValue(EnableGameInputProperty, value);
        }

        private List<Floater> _floaters = new List<Floater>();

        // [Optimization] Cache for static text (Building names)
        private Dictionary<string, FormattedText> _textCache =
            new Dictionary<string, FormattedText>();

        private Pen _borderPen = new Pen(Brushes.Black, 1);
        private Pen _selectionPen = new Pen(Brushes.LightGreen, 2);
        private Pen _floaterBorderPen = new Pen(Brushes.Black, 2);

        private Brush _skyBrush = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromRgb(15, 20, 35), 0.0),
                new GradientStop(Color.FromRgb(50, 60, 90), 1.0),
            },
            90
        );
        private Brush _groundBaseBrush = new SolidColorBrush(Color.FromRgb(50, 65, 40));
        private Brush _roadBrush = new SolidColorBrush(Color.FromArgb(180, 140, 110, 80));
        private Pen _gridPen = new Pen(new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)), 1);

        private Brush _shadowBrush = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0));
        private Brush _hpFriendlyBrush = Brushes.LimeGreen;
        private Brush _hpEnemyBrush = Brushes.Red;
        private Brush _bloodBrush = new SolidColorBrush(Color.FromArgb(220, 180, 20, 20));
        private Brush _sparkBrush = new SolidColorBrush(Color.FromArgb(220, 255, 160, 60));

        private Brush _treeLeafBrush = new SolidColorBrush(Color.FromRgb(30, 80, 20));
        private Brush _treeTrunkBrush = new SolidColorBrush(Color.FromRgb(60, 40, 20));
        private Brush _rockBrush = new SolidColorBrush(Color.FromRgb(80, 80, 90));

        private Brush _humanStoneLight = new SolidColorBrush(Color.FromRgb(120, 130, 140));
        private Brush _humanStoneDark = new SolidColorBrush(Color.FromRgb(80, 90, 100));
        private Brush _humanRoof = new SolidColorBrush(Color.FromRgb(40, 80, 160));
        private Brush _humanWood = new SolidColorBrush(Color.FromRgb(100, 70, 40));

        private Brush _orcWoodLight = new SolidColorBrush(Color.FromRgb(140, 100, 60));
        private Brush _orcWoodDark = new SolidColorBrush(Color.FromRgb(100, 70, 40));
        private Brush _orcRoof = new SolidColorBrush(Color.FromRgb(160, 40, 40));
        private Brush _orcBone = new SolidColorBrush(Color.FromRgb(220, 210, 190));

        private Dictionary<int, double> _smoothHpValues = new Dictionary<int, double>(); // [����] ����Ѫ��ƽ������

        private DrawingGroup _groundTextureCache;
        private Brush _unexploredFogBrush = new SolidColorBrush(Color.FromRgb(10, 5, 0));
        private Brush _exploredFogBrush = new SolidColorBrush(Color.FromArgb(200, 20, 15, 10));

        private Random _rnd = new Random();
        private List<Entity> _renderList = new List<Entity>();

        // [Optimization] Y-Bucket Rendering
        private Dictionary<int, List<Entity>> _renderBuckets = new Dictionary<int, List<Entity>>();
        private List<int> _sortedBucketKeys = new List<int>();
        private const double RENDER_BUCKET_SIZE = 100.0;

        // [Optimization] Throttling for Render Sort
        private int _sortFrameSkip = 0;
        private double _lastSortCamX = 0;
        private double _lastSortCamY = 0;

        private WriteableBitmap _fogBitmap;
        private int[] _fogPixels;
        private double _time = 0;

        // [Refactor] Multi-layer rendering
        private DrawingVisual _staticLayer = new DrawingVisual();
        private DrawingVisual _dynamicLayer = new DrawingVisual();
        private DrawingVisual _fogLayer = new DrawingVisual(); // [Fix] 独立的迷雾层
        private double _lastStaticCamX = -99999;
        private double _lastStaticCamY = -99999;
        private double _lastStaticZoom = -99999;

        // [Optimization] Autotiling Bitmask
        private Dictionary<int, Int32Rect> _terrainTileRects = new Dictionary<int, Int32Rect>();

        public GameSurface()
        {
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            _unexploredFogBrush.Freeze();
            _exploredFogBrush.Freeze();
            GenerateGroundTexture();
            this.Focusable = true;

            // Register visual children
            AddVisualChild(_staticLayer);
            AddVisualChild(_dynamicLayer);
            AddVisualChild(_fogLayer); // [Fix] 注册迷雾层

            // [Fix] 分别设置缩放模式：单位/地形保持像素风，迷雾使用平滑插值
            RenderOptions.SetBitmapScalingMode(_staticLayer, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(_dynamicLayer, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(_fogLayer, BitmapScalingMode.Linear);

            // [Fix] 防止控件尺寸坍缩导致不可见
            MinHeight = 100;
            MinWidth = 100;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            // Pre-calculate the bitmask rectangles for a standard 4x4 tileset
            InitializeBitmaskMap(128); // Assuming 128x128 tiles in the tileset

            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Q && Engine != null)
                {
                    Engine.ShowDebug = !Engine.ShowDebug;
                }
            };

            MouseLeftButtonDown += (s, e) =>
            {
                this.Focus(); // [Fix] 点击时强制获取焦点，确保键盘事件(如Q键)能触发
                if (!EnableGameInput)
                {
                    return;
                }
                var pos = e.GetPosition(this);
                if (e.ClickCount == 2)
                    Engine?.Input.HandleDoubleClick(pos.X, pos.Y);
                else
                    Engine?.Input.HandleInputStart(pos.X, pos.Y);
            };
            MouseLeftButtonUp += (s, e) =>
            {
                if (!EnableGameInput)
                {
                    return;
                }
                var pos = e.GetPosition(this);
                Engine?.Input.HandleInputUp(pos.X, pos.Y);
            };
            MouseMove += (s, e) =>
            {
                if (!EnableGameInput)
                {
                    return;
                }
                var pos = e.GetPosition(this);
                Engine?.Input.HandleMouseMove(pos.X, pos.Y);
                Engine?.Input.HandleDrag(pos.X, pos.Y);
            };
            MouseWheel += (s, e) =>
            {
                if (!EnableGameInput)
                {
                    return;
                }
                var pos = e.GetPosition(this);
                Engine?.Input.HandleZoom(e.Delta, pos.X, pos.Y);
            };
            MouseRightButtonUp += (s, e) =>
            {
                if (!EnableGameInput)
                {
                    return;
                }
                if (Engine != null)
                {
                    if (Engine.IsTargetingMode)
                    {
                        Engine.IsTargetingMode = false;
                        Engine.PendingSkill = null;
                        Engine.GhostPosition = new PointD(0, 0);
                        return;
                    }
                    var pos = e.GetPosition(this);
                    Engine.Input.HandleRightClick(pos.X, pos.Y);
                }
            };

            CompositionTarget.Rendering += (s, e) =>
            {
                _time += 0.05;

                // [Fix] 处理非游戏状态 (Menu/Loading)
                // 如果 Engine 为空或不在 Playing 状态，绘制黑色背景，防止窗口透明/不显示
                if (Engine == null || Engine.State != GameState.Playing)
                {
                    // 即使 ActualWidth 为 0，也尝试用 RenderSize 或 MinSize 绘制，确保窗口有底色
                    double w = ActualWidth > 0 ? ActualWidth : MinWidth;
                    double h = ActualHeight > 0 ? ActualHeight : MinHeight;

                    if (w > 0 && h > 0)
                    {
                        using (var dc = _dynamicLayer.RenderOpen())
                        {
                            dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, w, h));
                        }
                        using (var dc = _fogLayer.RenderOpen()) { } // 清理迷雾
                        // 清空静态层，防止残留画面
                        using (var dc = _staticLayer.RenderOpen()) { }
                        _lastStaticCamX = -99999; // 重置缓存，确保下次进入游戏时立即重绘
                    }
                    return;
                }

                if (Engine != null && Engine.State == GameState.Playing)
                {
                    Engine.ViewportWidth = ActualWidth;
                    Engine.ViewportHeight = ActualHeight;

                    // Check if static layer needs redraw (Camera move/zoom or first run)
                    bool cameraChanged =
                        Math.Abs(Engine.CameraX - _lastStaticCamX) > 0.1
                        || Math.Abs(Engine.CameraY - _lastStaticCamY) > 0.1
                        || Math.Abs(Engine.Zoom - _lastStaticZoom) > 0.001;

                    if (cameraChanged)
                    {
                        RedrawStaticLayer();
                    }
                    RedrawDynamicLayer();
                    RedrawFogLayer(); // [Fix] 独立绘制迷雾
                }
            };
        }

        private void RedrawStaticLayer()
        {
            _lastStaticCamX = Engine.CameraX;
            _lastStaticCamY = Engine.CameraY;
            _lastStaticZoom = Engine.Zoom;

            using (var dc = _staticLayer.RenderOpen())
            {
                dc.DrawRectangle(
                    AssetManager.BackgroundBrush ?? Brushes.Black,
                    null,
                    new Rect(0, 0, ActualWidth, ActualHeight)
                );

                var isoMatrix = new Matrix(1, 0.5, -1, 0.5, 0, 0);

                dc.PushTransform(new ScaleTransform(Engine.Zoom, Engine.Zoom));
                dc.PushTransform(new TranslateTransform(-Engine.CameraX, -Engine.CameraY));

                // Draw Environment (Ground)
                dc.PushTransform(new MatrixTransform(isoMatrix));
                DrawEnvironment(dc);
                dc.Pop();

                // Draw Static Obstacles
                DrawStaticObstacles(dc);

                dc.Pop(); // Translate
                dc.Pop(); // Zoom
            }
        }

        private void DrawStaticObstacles(DrawingContext dc)
        {
            // Calculate visible rect (same logic as dynamic)
            double viewW = Engine.ViewportWidth / Engine.Zoom;
            double viewH = Engine.ViewportHeight / Engine.Zoom;
            double l = Engine.CameraX;
            double t = Engine.CameraY;
            double r = l + viewW;
            double b = t + viewH;

            PointD p1 = GameEngine.IsoToWorld(l, t);
            PointD p2 = GameEngine.IsoToWorld(r, t);
            PointD p3 = GameEngine.IsoToWorld(l, b);
            PointD p4 = GameEngine.IsoToWorld(r, b);

            double minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X)) - 200;
            double maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X)) + 200;
            double minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y)) - 200;
            double maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y)) + 200;

            Rect visibleRect = new Rect(minX, minY, maxX - minX, maxY - minY);

            // Reuse _renderList temporarily for static query
            Engine.GetEntitiesInRectNonAlloc(visibleRect, _renderList);

            // [Fix] Sort static obstacles by Y to ensure correct occlusion
            _renderList.Sort((x, y) => (x.X + x.Y).CompareTo(y.X + y.Y));

            // Draw only obstacles
            foreach (var e in _renderList)
            {
                if (e is Obstacle obs)
                {
                    PointD isoPos = GameEngine.WorldToIso(obs.X, obs.Y);
                    dc.PushTransform(new TranslateTransform(isoPos.X, isoPos.Y));
                    if (Math.Abs(obs.Rotation) > 0.01)
                    {
                        dc.PushTransform(new RotateTransform(obs.Rotation));
                        DrawObstacle(dc, obs, obs.Animator?.GetScale() ?? 1.0);
                        dc.Pop();
                    }
                    else
                    {
                        DrawObstacle(dc, obs, obs.Animator?.GetScale() ?? 1.0);
                    }
                    dc.Pop();
                }
            }
        }

        private void RedrawDynamicLayer()
        {
            using (var dc = _dynamicLayer.RenderOpen())
            {
                // Update Vision
                UpdateVision();

                var isoMatrix = new Matrix(1, 0.5, -1, 0.5, 0, 0);

                dc.PushTransform(new ScaleTransform(Engine.Zoom, Engine.Zoom));
                dc.PushTransform(new TranslateTransform(-Engine.CameraX, -Engine.CameraY));

                // [Fix] Draw Water Ripples in Dynamic Layer (so they animate)
                dc.PushTransform(new MatrixTransform(isoMatrix));
                DrawWaterRipples(dc);
                dc.Pop();

                // Shake effect
                double shakeX = (_rnd.NextDouble() - 0.5) * Engine.ShakeIntensity;
                double shakeY = (_rnd.NextDouble() - 0.5) * Engine.ShakeIntensity;
                dc.PushTransform(new TranslateTransform(shakeX, shakeY));

                // Draw Dynamic Entities (Units, Buildings)
                DrawDynamicEntities(dc);
                DrawSelectionGizmo(dc);

                // Draw effects and projectiles
                var sortedEffects = Engine.VisualEffects.OrderBy(e => e.X + e.Y).ToList();
                foreach (var e in sortedEffects)
                {
                    PointD isoP = GameEngine.WorldToIso(e.X, e.Y);
                    DrawEffect(dc, e, isoP.X, isoP.Y);
                }
                DrawProjectilesAndParticles(dc);

                DrawIndicators(dc);
                DrawCommandMarkers(dc);
                if (Engine.IsBuildMode && Engine.PendingBuildingInfo != null)
                {
                    PointD ghostIso = GameEngine.WorldToIso(
                        Engine.GhostPosition.X,
                        Engine.GhostPosition.Y
                    );
                    DrawGhost(
                        dc,
                        ghostIso.X,
                        ghostIso.Y,
                        Engine.PendingBuildingInfo.Width,
                        Engine.PendingBuildingInfo.Height
                    );
                }

                dc.Pop(); // Shake
                dc.Pop(); // Translate
                dc.Pop(); // Zoom

                // [Fix] 恢复光照叠加和暗角效果
                DrawLightingOverlay(dc);
                DrawVignette(dc);

                DrawSelectionRect(dc);
                DrawFloaters(dc);
            }
        }

        private void DrawCommandMarkers(DrawingContext dc)
        {
            foreach (var e in Engine.SelectedEntities)
            {
                if (e is Unit u && (u.State == UnitState.Move || u.State == UnitState.AttackMove))
                {
                    PointD startIso = GameEngine.WorldToIso(u.X, u.Y);
                    PointD endIso = new PointD(0, 0);
                    bool hasTarget = false;
                    Color color = Colors.Lime;

                    if (u.Target != null && u.Target.HP > 0)
                    {
                        endIso = GameEngine.WorldToIso(u.Target.X, u.Target.Y);
                        hasTarget = true;
                        color = Colors.Red;
                    }
                    else if (u.CommandTargetPos.HasValue)
                    {
                        endIso = GameEngine.WorldToIso(
                            u.CommandTargetPos.Value.X,
                            u.CommandTargetPos.Value.Y
                        );
                        hasTarget = true;
                        color = u.IsAttackMove ? Colors.OrangeRed : Colors.Lime;
                    }

                    if (hasTarget)
                    {
                        dc.DrawLine(
                            new Pen(
                                new SolidColorBrush(Color.FromArgb(100, color.R, color.G, color.B)),
                                1
                            )
                            {
                                DashStyle = DashStyles.Dash,
                            },
                            new Point(startIso.X, startIso.Y),
                            new Point(endIso.X, endIso.Y)
                        );
                        dc.DrawEllipse(
                            null,
                            new Pen(new SolidColorBrush(color), 1),
                            new Point(endIso.X, endIso.Y),
                            3,
                            1.5
                        );
                    }
                }
            }
        }

        // [Fix] 新增迷雾层绘制方法
        private void RedrawFogLayer()
        {
            using (var dc = _fogLayer.RenderOpen())
            {
                if (Engine.EnableFog && !Engine.ShowDebug)
                {
                    var isoMatrix = new Matrix(1, 0.5, -1, 0.5, 0, 0);
                    dc.PushTransform(new ScaleTransform(Engine.Zoom, Engine.Zoom));
                    dc.PushTransform(new TranslateTransform(-Engine.CameraX, -Engine.CameraY));

                    // 迷雾是在世界坐标系下的，需要应用 ISO 变换
                    dc.PushTransform(new MatrixTransform(isoMatrix));
                    DrawFogOfWar(dc);
                    dc.Pop();
                }
            }
        }

        private void UpdateVision()
        {
            _cachedVisionSources.Clear();
            // 2. ʹ for ѭɸѡ LINQ ڴ
            // ע⣺ֱӷ Engine.Entities̰߳ȫģڵ߳ģû⣩
            if (Engine.EnableFog)
            {
                Engine.Fog?.Update(_cachedVisionSources);
            }
        }

        private void DrawDebugBounds(DrawingContext dc, Entity e)
        {
            var color = e.IsSolid ? Brushes.Red : Brushes.Yellow;
            var pen = new Pen(color, 2);
            if (!e.IsSolid)
                pen.DashStyle = DashStyles.Dash;

            if (e is Obstacle || e is Building)
            {
                double w = e.Width;
                double h = e.Height;
                Point pTop = ToIsoOffset(0, -h / 2);
                Point pRight = ToIsoOffset(w / 2, 0);
                Point pBottom = ToIsoOffset(0, h / 2);
                Point pLeft = ToIsoOffset(-w / 2, 0);

                StreamGeometry geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(pTop, true, true);
                    ctx.LineTo(pRight, true, false);
                    ctx.LineTo(pBottom, true, false);
                    ctx.LineTo(pLeft, true, false);
                }
                dc.DrawGeometry(null, pen, geo);

                var crossPen = new Pen(color, 1);
                dc.DrawLine(crossPen, new Point(-5, 0), new Point(5, 0));
                dc.DrawLine(crossPen, new Point(0, -5), new Point(0, 5));
            }
            else
            {
                double r = e.Width * 0.4;
                dc.DrawEllipse(null, pen, new Point(0, 0), r * 1.414, r * 0.707);
            }

            // [Debug] 绘制视野范围示意圈 (Cyan)，帮助确认该单位是否被视为视野源
            if (e.Team == Shared.Enums.TeamType.Human)
            {
                // 假设默认视野为 300 (具体需看 Entity.SightRange)
                dc.DrawEllipse(
                    null,
                    new Pen(Brushes.Cyan, 1) { DashStyle = DashStyles.Dash },
                    new Point(0, 0),
                    300,
                    150
                );
            }
        }

        private Point ToIsoOffset(double dx, double dy)
        {
            return new Point(dx - dy, (dx + dy) * 0.5);
        }

        private void GenerateGroundTexture()
        {
            _groundTextureCache = new DrawingGroup();
            using (var dc = _groundTextureCache.Open())
            {
                for (int i = 0; i < 4000; i++)
                {
                    double x = _rnd.Next(0, 3500);
                    double y = _rnd.Next(0, 800);
                    bool isDark = _rnd.NextDouble() > 0.5;
                    byte alpha = (byte)_rnd.Next(10, 30);
                    Color c = isDark ? Color.FromRgb(30, 45, 20) : Color.FromRgb(70, 90, 40);
                    var brush = new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
                    double w = 4 + _rnd.NextDouble() * 6;
                    double h = 2 + _rnd.NextDouble() * 3;
                    dc.DrawEllipse(brush, null, new Point(x, y), w, h);
                }
            }
            _groundTextureCache.Freeze();
        }

        public void AddFloater(
            string text,
            double worldX,
            double worldY,
            string colorHex,
            double size = 20,
            double velY = -1.5
        )
        {
            Color c = Colors.White;
            try
            {
                c = (Color)ColorConverter.ConvertFromString(colorHex);
            }
            catch { }

            PointD isoP = GameEngine.WorldToIso(worldX, worldY);
            double startY = isoP.Y - 60;

            var f = new Floater
            {
                Text = text,
                IsoX = isoP.X,
                IsoY = startY,
                Color = new SolidColorBrush(c),
                Life = 1.0,
                Size = size > 0 ? size : 20,
                VelY = velY,
            };

            // [Deep Optimization] Pre-calculate geometry once at creation
            // We create it at (0,0) and translate it later during rendering
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Arial"),
                    FontStyles.Normal,
                    FontWeights.Bold,
                    FontStretches.Normal
                ),
                f.Size, // Base size
                f.Color, // The color stored in geometry will be overridden by pen/brush in DrawGeometry?
                         // No, BuildGeometry creates a path. We stroke/fill it later.
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            f.Width = ft.Width;
            f.CachedGeometry = ft.BuildGeometry(new Point(0, 0));
            // Freeze to make it thread-safe and faster
            if (f.CachedGeometry.CanFreeze)
                f.CachedGeometry.Freeze();

            _floaters.Add(f);
        }

        protected override int VisualChildrenCount => 3;

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
                return _staticLayer;
            if (index == 1)
                return _dynamicLayer;
            if (index == 2)
                return _fogLayer; // [Fix] 返回迷雾层 (最上层)
            throw new ArgumentOutOfRangeException();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            // [Fix] 覆盖 Control 的默认测量逻辑。
            // Control.MeasureOverride 会尝试将 VisualChildren 转换为 UIElement 以便测量它们。
            // 但我们使用 DrawingVisual 作为子元素，它们不是 UIElement，会导致 InvalidCastException。
            // 因此我们需要手动处理测量，直接返回 MinWidth/MinHeight 作为期望大小。
            return new Size(MinWidth, MinHeight);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            // [Fix] 覆盖 Control 的默认排列逻辑，防止访问 DrawingVisual 出错。
            return arrangeBounds;
        }

        private void InitializeBitmaskMap(int tileSize)
        {
            _terrainTileRects.Clear();
            // This assumes a standard 16-tile autotile sheet layout (4x4)
            // You would adjust this mapping based on your specific tileset image.
            for (int i = 0; i < 16; i++)
            {
                int x = (i % 4) * tileSize;
                int y = (i / 4) * tileSize;
                _terrainTileRects[i] = new Int32Rect(x, y, tileSize, tileSize);
            }
        }

        private void DrawLightsForList<T>(DrawingContext dc, List<T> list)
        {
            foreach (var item in list)
            {
                LightSourceComponent light = null;
                double x = 0,
                    y = 0;
                bool isAlive = true;

                if (item is Entity e)
                {
                    light = e.GetComponent<LightSourceComponent>();
                    x = e.X;
                    y = e.Y;
                    isAlive = e.HP > 0;
                }
                else if (item is Projectile p)
                {
                    light = p.Light;
                    x = p.X;
                    y = p.Y;
                    isAlive = p.Active;
                }

                if (light == null || !isAlive)
                    continue;

                PointD isoPos = GameEngine.WorldToIso(x, y);
                double sx = (isoPos.X - Engine.CameraX) * Engine.Zoom;
                double sy = (isoPos.Y - Engine.CameraY) * Engine.Zoom;

                double radius = light.CurrentRadius * Engine.Zoom;
                if (
                    sx < -radius
                    || sx > ActualWidth + radius
                    || sy < -radius
                    || sy > ActualHeight + radius
                )
                    continue;

                var gradient = new RadialGradientBrush(
                    Color.FromArgb(180, light.LightColor.R, light.LightColor.G, light.LightColor.B),
                    Colors.Transparent
                )
                {
                    RadiusX = 0.5,
                    RadiusY = 0.5,
                };
                gradient.Freeze();

                dc.DrawEllipse(gradient, null, new Point(sx, sy), radius, radius);
            }
        }

        private void DrawFogOfWar(DrawingContext dc)
        {
            if (Engine?.Fog?.Grid == null)
                return;

            var fogGrid = Engine.Fog.Grid;
            int gridW = fogGrid.GetLength(0);
            int gridH = fogGrid.GetLength(1);
            int cellSize = FogOfWar.CELL_SIZE;

            // 1. 初始化或调整 Bitmap 大小
            if (
                _fogBitmap == null
                || _fogBitmap.PixelWidth != gridW
                || _fogBitmap.PixelHeight != gridH
            )
            {
                _fogBitmap = new WriteableBitmap(gridW, gridH, 96, 96, PixelFormats.Pbgra32, null);
                _fogPixels = new int[gridW * gridH];
            }

            // 2. 定义颜色 (Pbgra32 格式: 0xAARRGGBB)
            // Unexplored: 纯黑 (Alpha 255)
            int colorUnexplored = unchecked((int)0xFF000000);

            // Explored: 半透明灰色 (Alpha 100)
            int colorExplored = unchecked((int)0x64000000); // Alpha 100 = 0x64

            // Visible: 全透明
            int colorVisible = 0x00000000;

            // 3. 填充像素数据
            for (int x = 0; x < gridW; x++)
            {
                for (int y = 0; y < gridH; y++)
                {
                    var state = fogGrid[x, y];
                    int color;
                    switch (state)
                    {
                        case FogState.Unexplored:
                            color = colorUnexplored;
                            break;
                        case FogState.Explored:
                            color = colorExplored;
                            break;
                        default:
                            color = colorVisible;
                            break;
                    }
                    _fogPixels[y * gridW + x] = color;
                }
            }

            // 4. [性能优化] 使用 Lock() 快速写入像素数据
            _fogBitmap.Lock();
            System.Runtime.InteropServices.Marshal.Copy(
                _fogPixels,
                0,
                _fogBitmap.BackBuffer,
                _fogPixels.Length
            );
            _fogBitmap.AddDirtyRect(new Int32Rect(0, 0, gridW, gridH));
            _fogBitmap.Unlock();

            // [Fix] 移除这里的 SetBitmapScalingMode(this, ...)，因为它会污染全局设置。
            // 我们已经在构造函数中对 _fogLayer 单独设置了 Linear。
            // 绘制拉伸后的迷雾图层
            dc.DrawImage(_fogBitmap, new Rect(0, 0, gridW * cellSize, gridH * cellSize));
        }

        private void DrawLightingOverlay(DrawingContext dc)
        {
            if (Engine.AmbientColor.A <= 5) // Almost transparent, no need to draw
                return;

            var overlayBrush = new SolidColorBrush(Engine.AmbientColor);
            overlayBrush.Freeze();

            // 1. Start with a full-screen rectangle
            Geometry combinedGeo = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));

            // 2. Subtract light sources
            if (Engine.DayNightFactor > 0.1) // Only "dig holes" when it's dark enough
            {
                var allLights = new List<object>();
                allLights.AddRange(Engine.Entities);
                allLights.AddRange(Engine.VisualEffects);
                allLights.AddRange(Engine.Projectiles);

                foreach (var item in allLights)
                {
                    var light =
                        (item as Entity)?.GetComponent<LightSourceComponent>()
                        ?? (item as Projectile)?.Light;
                    if (light == null)
                        continue;

                    PointD isoPos = GameEngine.WorldToIso(
                        (item as Entity)?.X ?? (item as Projectile).X,
                        (item as Entity)?.Y ?? (item as Projectile).Y
                    );
                    Point screenPos = new Point(
                        (isoPos.X - Engine.CameraX) * Engine.Zoom,
                        (isoPos.Y - Engine.CameraY) * Engine.Zoom
                    );
                    double radius = light.CurrentRadius * Engine.Zoom;

                    var lightGeometry = new EllipseGeometry(screenPos, radius, radius);
                    combinedGeo = new CombinedGeometry(
                        GeometryCombineMode.Exclude,
                        combinedGeo,
                        lightGeometry
                    );
                }
            }

            // 3. Draw the final shape
            dc.DrawGeometry(overlayBrush, null, combinedGeo);

            // 4. [Fix] 绘制彩色光晕 (Additive Light)
            // 之前这部分代码丢失了，现在补回来，让光源不仅挖洞，还有颜色
            DrawLightsForList(dc, Engine.Entities);
            DrawLightsForList(dc, Engine.VisualEffects);
            DrawLightsForList(dc, Engine.Projectiles);
        }

        private Color Lerp(Color from, Color to, float amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return Color.FromArgb(
                (byte)(from.A + (to.A - from.A) * amount),
                (byte)(from.R + (to.R - from.R) * amount),
                (byte)(from.G + (to.G - from.G) * amount),
                (byte)(from.B + (to.B - from.B) * amount)
            );
        }

        private void DrawSelectionRect(DrawingContext dc)
        {
            if (!Engine.IsDraggingSelection || Engine.SelectionRect.IsEmpty)
                return;
            // Ч߻
            var pen = new Pen(Brushes.LightGreen, 1)
            {
                DashStyle = new DashStyle(new double[] { 4, 4 }, _time * 20),
            };
            //
            dc.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(30, 50, 255, 50)),
                pen,
                Engine.SelectionRect
            );
        }

        private void DrawSelectionGizmo(DrawingContext dc)
        {
            if (Engine?.SelectedEntity == null)
                return;

            var entity = Engine.SelectedEntity;
            double halfW = entity.Width / 2;
            double halfH = entity.Height / 2;

            double left = entity.X - halfW;
            double right = entity.X + halfW;
            double top = entity.Y - halfH;
            double bottom = entity.Y + halfH;

            var center = GameEngine.WorldToIso(entity.X, entity.Y);
            var p1 = GameEngine.WorldToIso(left, top);
            var p2 = GameEngine.WorldToIso(right, top);
            var p3 = GameEngine.WorldToIso(right, bottom);
            var p4 = GameEngine.WorldToIso(left, bottom);

            if (Math.Abs(entity.Rotation) > 0.01)
            {
                p1 = RotatePoint(p1, center, entity.Rotation);
                p2 = RotatePoint(p2, center, entity.Rotation);
                p3 = RotatePoint(p3, center, entity.Rotation);
                p4 = RotatePoint(p4, center, entity.Rotation);
            }

            var outline = new StreamGeometry();
            using (var ctx = outline.Open())
            {
                ctx.BeginFigure(new Point(p1.X, p1.Y), false, true);
                ctx.LineTo(new Point(p2.X, p2.Y), true, false);
                ctx.LineTo(new Point(p3.X, p3.Y), true, false);
                ctx.LineTo(new Point(p4.X, p4.Y), true, false);
            }
            outline.Freeze();
            dc.DrawGeometry(null, _selectionPen, outline);

            double handleSize = 6;
            DrawGizmoHandle(dc, p1, handleSize);
            DrawGizmoHandle(dc, p2, handleSize);
            DrawGizmoHandle(dc, p3, handleSize);
            DrawGizmoHandle(dc, p4, handleSize);

            var topCenter = new PointD((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
            var dirX = topCenter.X - center.X;
            var dirY = topCenter.Y - center.Y;
            var len = Math.Sqrt(dirX * dirX + dirY * dirY);
            if (len > 0.001)
            {
                dirX /= len;
                dirY /= len;
                var rotateHandle = new PointD(
                    topCenter.X + dirX * 20,
                    topCenter.Y + dirY * 20
                );
                DrawGizmoHandle(dc, rotateHandle, handleSize + 2);
            }

            if (Engine.EditorDragActive)
            {
                var dragPen = new Pen(Brushes.Gold, 2) { DashStyle = DashStyles.Dot };
                dc.DrawGeometry(null, dragPen, outline);

                var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                int tileX = (int)Math.Floor(entity.X / Engine.TileSize);
                int tileY = (int)Math.Floor(entity.Y / Engine.TileSize);
                var label = $"W:{entity.X:0.0},{entity.Y:0.0}  T:{tileX},{tileY}";
                var text = new FormattedText(
                    label,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Bahnschrift"),
                    12,
                    Brushes.Gold,
                    dpi
                );
                var centerIso = GameEngine.WorldToIso(entity.X, entity.Y);
                dc.DrawText(text, new Point(centerIso.X + 12, centerIso.Y - 24));
            }
        }

        private void DrawGizmoHandle(DrawingContext dc, PointD position, double size)
        {
            double half = size / 2;
            dc.DrawRectangle(
                Brushes.White,
                _borderPen,
                new Rect(position.X - half, position.Y - half, size, size)
            );
        }

        private static PointD RotatePoint(PointD point, PointD center, double degrees)
        {
            double rad = degrees * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;
            return new PointD(
                center.X + (dx * cos - dy * sin),
                center.Y + (dx * sin + dy * cos)
            );
        }

        // 滻ԭе DrawEntities
        // 滻ԭе DrawEntities
        private void DrawDynamicEntities(DrawingContext dc)
        {
            // [Ż] ʹøб new List
            // 1. ɼ
            double viewW = Engine.ViewportWidth / Engine.Zoom;
            double viewH = Engine.ViewportHeight / Engine.Zoom;
            double l = Engine.CameraX;
            double t = Engine.CameraY;
            double r = l + viewW;
            double b = t + viewH;

            PointD p1 = GameEngine.IsoToWorld(l, t);
            PointD p2 = GameEngine.IsoToWorld(r, t);
            PointD p3 = GameEngine.IsoToWorld(l, b);
            PointD p4 = GameEngine.IsoToWorld(r, b);

            double minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X)) - 200;
            double maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X)) + 200;
            double minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y)) - 200;
            double maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y)) + 200;

            Rect visibleRect = new Rect(minX, minY, maxX - minX, maxY - minY);

            // Always update dynamic list
            Engine.GetEntitiesInRectNonAlloc(visibleRect, _renderList);
            // Filter out obstacles from dynamic layer
            _renderList.RemoveAll(e => e is Obstacle);

            // [Optimization] Bucket Sort instead of full Sort
            foreach (var list in _renderBuckets.Values)
                list.Clear();
            _sortedBucketKeys.Clear();

            for (int i = 0; i < _renderList.Count; i++)
            {
                var e = _renderList[i];
                double sortKey = e.X + e.Y;
                int bucketIndex = (int)(sortKey / RENDER_BUCKET_SIZE);

                if (!_renderBuckets.TryGetValue(bucketIndex, out var bucket))
                {
                    bucket = new List<Entity>();
                    _renderBuckets[bucketIndex] = bucket;
                }
                if (bucket.Count == 0)
                    _sortedBucketKeys.Add(bucketIndex);
                bucket.Add(e);
            }
            _sortedBucketKeys.Sort();

            // [Ż] Ӱ (Shadow Batching)
            var shadowGroup = new GeometryGroup();
            shadowGroup.FillRule = FillRule.Nonzero;

            foreach (var e in _renderList)
            {
                // ųϰ(ͨԴ)Ͳɼλ
                if (e is Obstacle || e.HP <= 0)
                    continue;

                //  ()

                PointD isoPos = GameEngine.WorldToIso(e.X, e.Y);
                // Բ嵽
                shadowGroup.Children.Add(
                    new EllipseGeometry(
                        new Point(isoPos.X, isoPos.Y),
                        e.Width * 0.65,
                        e.Width * 0.3
                    )
                );
            }

            if (shadowGroup.Children.Count > 0)
            {
                shadowGroup.Freeze();
                dc.DrawGeometry(GfxCache.ShadowBrush, null, shadowGroup);
            }

            // ʵ屾
            foreach (var key in _sortedBucketKeys)
            {
                var bucket = _renderBuckets[key];
                bucket.Sort((a, b) => (a.X + a.Y).CompareTo(b.X + b.Y));

                foreach (var e in bucket)
                {
                    if (
                        Engine.EnableFog
                        && !Engine.ShowDebug
                        && e.Team != TeamType.Human
                        && !(e is Obstacle)
                        && !Engine.IsVisibleToPlayer(e)
                    )
                        continue;

                    bool pushedOpacity = false;
                    if (e.HP <= 0 && e.DeathTimer > 3.0)
                    {
                        double progress = (e.DeathTimer - 3.0) / 2.0;
                        dc.PushOpacity(1.0 - Math.Min(1.0, progress));
                        pushedOpacity = true;
                    }

                    // [Hit Flash]
                    if (e.HitFlashTime > 0)
                    {
                        double flashOpacity = Math.Sin(e.HitFlashTime * Math.PI / 0.1); // Quick fade in and out
                        dc.PushOpacityMask(
                            new SolidColorBrush(
                                Color.FromArgb((byte)(flashOpacity * 255), 255, 255, 255)
                            )
                        );
                    }

                    PointD isoPos = GameEngine.WorldToIso(e.X, e.Y);
                    double sinkOffset =
                        (e.HP <= 0 && e.DeathTimer > 1.0)
                            ? Math.Min(30, (e.DeathTimer - 1.0) * 10)
                            : 0;

                    DrawEntity(dc, e, isoPos.X, isoPos.Y + sinkOffset);

                    if (pushedOpacity)
                        dc.Pop();
                    if (e.HitFlashTime > 0)
                        dc.Pop(); // Pop OpacityMask
                }
            }
        }

        // [Fix] 重构 DrawEntity，统一使用 TranslateTransform，简化坐标计算
        private void DrawEntity(DrawingContext dc, Entity e, double x, double y)
        {
            // 1. 统一应用位移 (Translate) 到单位位置
            dc.PushTransform(new TranslateTransform(x, y));

            if (e.HP > 0 && !(e is Obstacle))
            {
                if (e == Engine.HoveredEntity)
                {
                    double flash = 0.5 + 0.5 * Math.Sin(_time * 15.0);
                    dc.DrawEllipse(
                        new SolidColorBrush(Color.FromArgb((byte)(60 * flash), 255, 255, 255)),
                        GfxCache.BorderPen,
                        new Point(0, 0), // 局部坐标 0,0
                        e.Width * 0.7,
                        e.Width * 0.35
                    );
                }
                if (e.IsSelected)
                {
                    var glowColor =
                        (e.Team == Shared.Enums.TeamType.Human)
                            ? Color.FromRgb(0, 255, 100)
                            : Color.FromRgb(255, 50, 50);
                    var pulse = 1.0 + 0.1 * Math.Sin(_time * 5.0);
                    dc.DrawEllipse(
                        null,
                        new Pen(new SolidColorBrush(glowColor), 2),
                        new Point(0, 0),
                        e.Width * 0.7 * pulse,
                        e.Width * 0.35 * pulse
                    );
                }
            }

            // 2. 绘制单位/建筑内容
            bool pushedRotation = false;
            if (Math.Abs(e.Rotation) > 0.01)
            {
                dc.PushTransform(new RotateTransform(e.Rotation));
                pushedRotation = true;
            }

            if (e is Unit u && u.Animator != null)
            {
                double shakeX = 0,
                    shakeY = 0;
                if (e.HitFlashTime > 0)
                {
                    shakeX = (_rnd.NextDouble() - 0.5) * 4;
                    shakeY = (_rnd.NextDouble() - 0.5) * 4;
                }

                double scaleX = u.Animator.GetScale() * 1.5 * u.Facing;

                // 应用缩放和震动 (注意：Translate 已经在最外层应用了，这里只需处理局部偏移)
                dc.PushTransform(new TranslateTransform(shakeX, shakeY));
                dc.PushTransform(new ScaleTransform(scaleX, Math.Abs(scaleX))); // 默认以 (0,0) 为中心缩放

                var frame = u.Animator.GetCurrentFrame();
                if (frame != null)
                {
                    var f = frame.Value;
                    // 绘制 Sprite，居中对齐底部
                    DrawSprite(
                        dc,
                        f,
                        new Rect(
                            -f.PixelWidth / 2,
                            -f.PixelHeight + 45.0,
                            f.PixelWidth,
                            f.PixelHeight
                        )
                    );
                }

                dc.Pop(); // Pop Scale
                dc.Pop(); // Pop Translate (Shake)
            }
            else
            {
                if (e is Building b)
                    DrawBuilding(dc, b, 1);

                // 障碍物绘制逻辑 (如果需要)
                // if (e is Obstacle o) DrawObstacle(dc, o, 1);
            }

            // 3. 绘制血条和调试框 (所有实体通用)
            if (pushedRotation)
                dc.Pop();

            DrawSmoothHPBar(dc, e);

            if (Engine.ShowDebug)
                DrawDebugBounds(dc, e);

            // 4. 恢复位移
            dc.Pop(); // Pop Translate (x, y)
        }

        private void DrawSmoothHPBar(DrawingContext dc, Entity e)
        {
            if (e.HP <= 0 || e.MaxHP <= 0)
                return;
            if (
                !(
                    e.HP < e.MaxHP
                    || e.IsSelected
                    || System.Windows.Input.Keyboard.Modifiers.HasFlag(
                        System.Windows.Input.ModifierKeys.Alt
                    )
                )
            )
                return;

            int id = e.GetHashCode();
            double targetPct = e.HP / e.MaxHP;
            if (!_smoothHpValues.TryGetValue(id, out double currentPct))
                currentPct = targetPct;
            currentPct += (targetPct - currentPct) * 0.15; // ֵ
            _smoothHpValues[id] = currentPct;

            double barW = 40,
                barH = 5;
            double yOff = (e is Building) ? -e.Height - 10 : -60;

            dc.DrawRectangle(GfxCache.HPBarBg, null, new Rect(-barW / 2, yOff, barW, barH));
            Brush hpBrush = e.Team == Shared.Enums.TeamType.Human ? Brushes.LimeGreen : Brushes.Red;
            dc.DrawRectangle(hpBrush, null, new Rect(-barW / 2, yOff, barW * currentPct, barH));
            dc.DrawRectangle(
                GfxCache.HPBarHighlight,
                null,
                new Rect(-barW / 2, yOff, barW * currentPct, 1)
            );
        }

        private void DrawGhost(DrawingContext dc, double x, double y, double w, double h)
        {
            Color stateColor = Engine.CanBuildAtGhost ? Colors.LimeGreen : Colors.Red;
            dc.PushTransform(new TranslateTransform(x, y));
            // Ƶػռؾ
            dc.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(50, stateColor.R, stateColor.G, stateColor.B)),
                new Pen(Brushes.White, 1),
                new Rect(-w / 2, -w / 2, w, w)
            );

            var anim = AssetManager.CreateBuildingAnimator(Engine.PendingBuildingInfo?.Id);
            var frame = anim?.GetCurrentFrame();
            if (frame != null)
            {
                dc.PushOpacity(0.6);
                var f = frame.Value;
                double ratio = f.PixelHeight / f.PixelWidth;
                double drawW = w * (Engine.PendingBuildingInfo?.Scale ?? 1);
                // Use the new DrawSprite helper
                DrawSprite(
                    dc,
                    f,
                    new Rect(
                        -drawW / 2,
                        -drawW * ratio + (Engine.PendingBuildingInfo?.OffsetY ?? 0),
                        drawW,
                        drawW * ratio
                    )
                );
                dc.Pop();
            }
            dc.Pop();
        }

        private void DrawFloaters(DrawingContext dc)
        {
            while (Engine.GetFloaterCount() > 0)
            {
                var nf = Engine.PopFloater();
                AddFloater(nf.Text, nf.X, nf.Y, nf.Color, nf.Size, nf.VelY);
            }

            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                var f = _floaters[i];
                f.Life -= 0.02;
                f.VelY += f.Gravity * 0.02; // Apply gravity
                f.IsoY += f.VelY;
                if (f.Life <= 0)
                {
                    _floaters.RemoveAt(i);
                    continue;
                }

                double sx = (f.IsoX - Engine.CameraX) * Engine.Zoom;
                double sy = (f.IsoY - Engine.CameraY) * Engine.Zoom;
                dc.PushTransform(new TranslateTransform(sx - f.Width / 2 * Engine.Zoom, sy));
                dc.PushTransform(new ScaleTransform(Engine.Zoom, Engine.Zoom));
                dc.PushOpacity(Math.Min(1.0, f.Life * 2.0));
                dc.DrawGeometry(f.Color, GfxCache.FloaterBorderPen, f.CachedGeometry);
                dc.Pop();
                dc.Pop();
                dc.Pop();
            }
        }

        private void DrawVignette(DrawingContext dc)
        {
            double intensity = Engine.CombatIntensity;
            var brush = new RadialGradientBrush();
            Color outerColor = Color.FromArgb(
                (byte)(100 + intensity * 100),
                (byte)(intensity * 50),
                0,
                0
            );
            brush.GradientStops.Add(
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.6 - intensity * 0.2)
            );
            brush.GradientStops.Add(new GradientStop(outerColor, 1.0));
            brush.RadiusX = 1.0;
            brush.RadiusY = 1.0;
            dc.DrawRectangle(brush, null, new Rect(0, 0, ActualWidth, ActualHeight));
        }

        // [Ż] λƻ
        private DrawingGroup _cachedTerrainDrawing;

        private void DrawEnvironment(DrawingContext dc)
        {
            if (Engine.MapData != _lastMapData)
            {
                _cachedTerrainDrawing = null; // Invalidate cache
                _lastMapData = Engine.MapData;
            }

            // [Fix] 当缓存为空时，重新生成地形绘图。此前的逻辑只清空了缓存但没有重建，导致地面不显示。
            if (_cachedTerrainDrawing == null && Engine.MapData != null)
            {
                var newCache = new DrawingGroup();
                using (var cacheDc = newCache.Open())
                {
                    int mapW = Engine.MapData.GetLength(0);
                    int mapH = Engine.MapData.GetLength(1);
                    int tileSize = Engine.TileSize;

                    for (int x = 0; x < mapW; x++)
                    {
                        for (int y = 0; y < mapH; y++)
                        {
                            int tileId = Engine.MapData[x, y];
                            // 假设 AssetManager 中存在一个方法可以根据 ID 获取地形瓦片图像
                            var image = AssetManager.GetTerrainImage(tileId);
                            if (image != null)
                            {
                                cacheDc.DrawImage(image, new Rect(x * tileSize, y * tileSize, tileSize, tileSize));
                            }
                        }
                    }
                }
                newCache.Freeze();
                _cachedTerrainDrawing = newCache;
            }

            // ֱӻƻ
            dc.DrawDrawing(_cachedTerrainDrawing);
        }

        private int CalculateBitmask(int x, int y, int[,] mapData)
        {
            int targetId = mapData[x, y];
            int width = mapData.GetLength(0);
            int height = mapData.GetLength(1);
            int mask = 0;

            // Check neighbors: Up, Down, Left, Right
            // Bit 1 (Up)
            if (y > 0 && mapData[x, y - 1] == targetId)
                mask |= 1;
            // Bit 2 (Left)
            if (x > 0 && mapData[x - 1, y] == targetId)
                mask |= 2;
            // Bit 3 (Right)
            if (x < width - 1 && mapData[x + 1, y] == targetId)
                mask |= 4;
            // Bit 4 (Down)
            if (y < height - 1 && mapData[x, y + 1] == targetId)
                mask |= 8;

            return mask;
        }

        // [Fix] Extracted ripple logic to separate method for dynamic update
        private void DrawWaterRipples(DrawingContext dc)
        {
            if (Engine.MapData != null)
            {
                int tileSize = Engine.TileSize;
                // Only iterate visible tiles for performance
                // Reuse visible rect calculation logic if possible, or just iterate all for simplicity in this snippet
                // For better performance, calculate visible range:
                int startX = (int)(Engine.CameraX / tileSize);
                int startY = (int)(Engine.CameraY / tileSize);
                int endX = startX + (int)(ActualWidth / Engine.Zoom / tileSize) + 2;
                int endY = startY + (int)(ActualHeight / Engine.Zoom / tileSize) + 2;

                int mapW = Engine.MapData.GetLength(0);
                int mapH = Engine.MapData.GetLength(1);

                for (int x = Math.Max(0, startX); x < Math.Min(mapW, endX); x++)
                {
                    for (int y = Math.Max(0, startY); y < Math.Min(mapH, endY); y++)
                    {
                        if (IsWater(Engine.MapData[x, y]))
                        {
                            double centerX = (x + 0.5) * tileSize;
                            double centerY = (y + 0.5) * tileSize;
                            double rippleSize = 10 + 5 * Math.Sin(_time * 3 + x * 0.5 + y * 0.5);
                            byte alpha = (byte)(30 + 20 * Math.Sin(_time * 2 + x));
                            var brush = new SolidColorBrush(Color.FromArgb(alpha, 200, 220, 255));
                            dc.DrawEllipse(
                                null,
                                new Pen(brush, 1),
                                new Point(centerX, centerY),
                                rippleSize,
                                rippleSize * 0.5
                            );
                        }
                    }
                }
            }
        }

        private bool IsWater(int tileId)
        {
            // TODO: Replace with actual water tile IDs from your config
            // Example: return tileId >= 100 && tileId <= 120;
            return false;
        }

        private void DrawBuilding(DrawingContext dc, Building b, double scale)
        {
            double visualTopY = -b.Height;

            if (b.Animator != null)
            {
                var frame = b.Animator.GetCurrentFrame();
                if (frame != null)
                {
                    var f = frame.Value;
                    double ratio = (double)f.PixelHeight / f.PixelWidth;
                    double drawW = b.Width * scale * b.Animator.GetScale();
                    double drawH = drawW * ratio;
                    double drawX = -drawW / 2;
                    double drawY = -drawH + b.RenderOffsetY;
                    DrawSprite(dc, f, new Rect(drawX, drawY, drawW, drawH));
                    visualTopY = drawY;
                }
            }
            else
            {
                double w = b.Width * scale;
                double h = b.Height * scale;
                dc.DrawRectangle(_humanRoof, _borderPen, new Rect(-w / 2, -h, w, h * 0.2));
                dc.DrawRectangle(
                    _humanStoneLight,
                    _borderPen,
                    new Rect(-w / 2, -h * 0.8, w, h * 0.8)
                );
                visualTopY = -h;
            }

            // [Optimization] Use cached FormattedText
            if (!_textCache.TryGetValue(b.Name, out var ft))
            {
                ft = new FormattedText(
                    b.Name,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    12,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                _textCache[b.Name] = ft;
            }

            dc.DrawText(ft, new Point(-ft.Width / 2, visualTopY - 20));
        }

        private void DrawObstacle(DrawingContext dc, Obstacle obs, double scale)
        {
            if (obs.Animator != null)
            {
                var frame = obs.Animator.GetCurrentFrame();
                if (frame != null)
                {
                    var f = frame.Value;
                    double ratio = (double)f.PixelHeight / f.PixelWidth;
                    double s = 1.5 * scale * obs.Animator.GetScale();
                    double drawW = obs.Width * s;
                    double drawH = drawW * ratio;
                    double drawX = -drawW / 2;
                    double drawY = -drawH + obs.RenderOffsetY;
                    DrawSprite(dc, f, new Rect(drawX, drawY, drawW, drawH));
                    return;
                }
            }
            dc.DrawRectangle(_rockBrush, _borderPen, new Rect(-15, -30, 30, 30));
        }

        private void DrawIndicators(DrawingContext dc)
        {
            if (Engine.SelectedEntity == null)
                return;

            var entity = Engine.SelectedEntity;
            double baseRange = 0;
            Color baseColor = Colors.White;
            bool isTargeting = false;
            SkillDef skill = null;

            if (Engine.IsTargetingMode && Engine.PendingSkill != null)
            {
                isTargeting = true;
                skill = Engine.PendingSkill;
                baseRange = skill.Range;
                baseColor = Color.FromRgb(0, 255, 255);
            }
            else if (Engine.IsAttackMoveMode)
            {
                // Attack Move Cursor Indicator
                PointD cursorIso = GameEngine.WorldToIso(
                    Engine.GhostPosition.X,
                    Engine.GhostPosition.Y
                );
                dc.PushTransform(new TranslateTransform(cursorIso.X, cursorIso.Y));

                var attackPen = new Pen(Brushes.Red, 2);
                dc.DrawLine(attackPen, new Point(-10, -10), new Point(10, 10));
                dc.DrawLine(attackPen, new Point(-10, 10), new Point(10, -10));

                var circlePen = new Pen(Brushes.Red, 1) { DashStyle = DashStyles.Dash };
                dc.DrawEllipse(null, circlePen, new Point(0, 0), 20, 10);

                dc.Pop();
                return; // Skip normal range indicator if just A-moving
            }
            else if (entity.Team == Shared.Enums.TeamType.Human)
            {
                if (entity is Unit u)
                    baseRange = u.Stats.Range;
                else if (entity is Building b && b.Damage > 0)
                    baseRange = b.Range;
                baseColor = Color.FromRgb(60, 140, 255);
            }
            else if (entity.Team == TeamType.Orc || entity.Team == TeamType.Neutral)
            {
                if (entity is Unit u)
                    baseRange = u.Stats.Range;
                else if (entity is Building b && b.Damage > 0)
                    baseRange = b.Range;
                baseColor = Color.FromRgb(255, 60, 60);
            }
            if (baseRange <= 0)
                return;

            double visualRange = baseRange;
            if (!isTargeting)
            {
                visualRange += entity.Width * 0.45;
            }

            double radiusX = visualRange * 1.4142;
            double radiusY = visualRange * 0.7071;

            var glowColor = baseColor;
            var solidPen = new Pen(
                new SolidColorBrush(Color.FromArgb(100, glowColor.R, glowColor.G, glowColor.B)),
                1
            );
            var dashPen = new Pen(new SolidColorBrush(baseColor), 2);
            dashPen.DashStyle = new DashStyle(new double[] { 2, 4 }, -_time * 2.0);

            var fillBrush = new RadialGradientBrush();
            fillBrush.GradientStops.Add(
                new GradientStop(Color.FromArgb(50, glowColor.R, glowColor.G, glowColor.B), 0.0)
            );
            fillBrush.GradientStops.Add(
                new GradientStop(Color.FromArgb(10, glowColor.R, glowColor.G, glowColor.B), 1.0)
            );

            PointD casterIso = GameEngine.WorldToIso(entity.X, entity.Y);
            dc.PushTransform(new TranslateTransform(casterIso.X, casterIso.Y));

            dc.DrawEllipse(fillBrush, null, new Point(0, 0), radiusX, radiusY);
            dc.DrawEllipse(null, solidPen, new Point(0, 0), radiusX, radiusY);
            dc.DrawEllipse(null, dashPen, new Point(0, 0), radiusX, radiusY);

            dc.DrawLine(
                new Pen(Brushes.White, 2),
                new Point(radiusX - 5, 0),
                new Point(radiusX + 5, 0)
            );
            dc.DrawLine(
                new Pen(Brushes.White, 2),
                new Point(-radiusX - 5, 0),
                new Point(-radiusX + 5, 0)
            );
            dc.DrawLine(
                new Pen(Brushes.White, 2),
                new Point(0, -radiusY - 3),
                new Point(0, -radiusY + 3)
            );
            dc.DrawLine(
                new Pen(Brushes.White, 2),
                new Point(0, radiusY - 3),
                new Point(0, radiusY + 3)
            );

            dc.Pop();

            if (isTargeting)
            {
                PointD cursorIso = GameEngine.WorldToIso(
                    Engine.GhostPosition.X,
                    Engine.GhostPosition.Y
                );
                double checkRange =
                    skill.Target == "Unit" ? (skill.Range + entity.Width * 0.45) : skill.Range;

                double dx = Engine.GhostPosition.X - entity.X;
                double dy = Engine.GhostPosition.Y - entity.Y;
                bool outOfRange = (dx * dx + dy * dy) > (checkRange * checkRange);

                Color stateColor = outOfRange ? Colors.Red : Colors.Cyan;
                var cursorPen = new Pen(new SolidColorBrush(stateColor), 2);
                var cursorFill = new SolidColorBrush(
                    Color.FromArgb(50, stateColor.R, stateColor.G, stateColor.B)
                );

                var linePen = new Pen(new SolidColorBrush(stateColor), 1);
                linePen.DashStyle = new DashStyle(new double[] { 4, 4 }, -_time * 5.0);
                dc.DrawLine(
                    linePen,
                    new Point(casterIso.X, casterIso.Y),
                    new Point(cursorIso.X, cursorIso.Y)
                );

                dc.PushTransform(new TranslateTransform(cursorIso.X, cursorIso.Y));

                double radius = skill.Radius > 0 ? skill.Radius : 30;
                double aoeRadiusX = radius * 1.4142;
                double aoeRadiusY = radius * 0.7071;

                dc.DrawEllipse(cursorFill, cursorPen, new Point(0, 0), aoeRadiusX, aoeRadiusY);

                var crossPen = new Pen(Brushes.White, 2);
                dc.DrawLine(crossPen, new Point(-5, 0), new Point(5, 0));
                dc.DrawLine(crossPen, new Point(0, -3), new Point(0, 3));

                if (outOfRange)
                {
                    var xPen = new Pen(Brushes.Red, 3);
                    dc.DrawLine(xPen, new Point(-8, -4), new Point(8, 4));
                    dc.DrawLine(xPen, new Point(-8, 4), new Point(8, -4));
                }

                dc.Pop();
            }
        }

        private void DrawSprite(DrawingContext dc, SpriteFrame frame, Rect destRect)
        {
            var brush = new ImageBrush(frame.Sheet)
            {
                Viewbox = new Rect(
                    frame.SourceRect.X,
                    frame.SourceRect.Y,
                    frame.SourceRect.Width,
                    frame.SourceRect.Height
                ),
                ViewboxUnits = BrushMappingMode.Absolute,
            };
            dc.DrawRectangle(brush, null, destRect);
        }

        private void DrawEffect(DrawingContext dc, VisualEffect effect, double x, double y)
        {
            dc.PushTransform(new TranslateTransform(x, y));

            var lightBrush = new RadialGradientBrush();
            lightBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, 255, 255, 200), 0.0));
            lightBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 200, 100), 1.0));
            dc.DrawEllipse(lightBrush, null, new Point(0, 0), 40, 20);

            if (effect.Animator != null)
            {
                var frame = effect.Animator.GetCurrentFrame();
                if (frame != null)
                {
                    var f = frame.Value;
                    // Use the new DrawSprite helper
                    DrawSprite(
                        dc,
                        f,
                        new Rect(
                            -f.PixelWidth / 2,
                            -f.PixelHeight / 2 - 20,
                            f.PixelWidth,
                            f.PixelHeight
                        )
                    );
                }
            }
            else
            {
                dc.DrawEllipse(_sparkBrush, null, new Point(0, -20), 10, 10);
            }
            dc.Pop();
        }

        private void DrawProjectilesAndParticles(DrawingContext dc)
        {
            foreach (var p in Engine.Projectiles)
            {
                PointD isoP = GameEngine.WorldToIso(p.X, p.Y);

                dc.PushTransform(new TranslateTransform(isoP.X, isoP.Y));

                double shadowFactor = 1.0 - (p.Height / 300.0);
                double shadowScale = Math.Max(0.4, shadowFactor);
                byte shadowAlpha = (byte)(100 * shadowScale);

                dc.PushTransform(new ScaleTransform(shadowScale, shadowScale));
                dc.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(shadowAlpha, 0, 0, 0)),
                    null,
                    new Point(0, 0),
                    8,
                    4
                );
                dc.Pop();
                dc.Pop();

                dc.PushTransform(new TranslateTransform(isoP.X, isoP.Y - p.Height));

                double distW = p.TX - p.StartX;
                double distH = p.TY - p.StartY;

                double screenVx = distW - distH;
                double screenVy = (distW + distH) * 0.5;

                double heightRate = p.ArcHeight * Math.PI * Math.Cos(p.Progress * Math.PI);

                double finalVx = screenVx;
                double finalVy = screenVy - heightRate;

                double angle = Math.Atan2(finalVy, finalVx) * 180.0 / Math.PI;

                dc.PushTransform(new RotateTransform(angle));

                if (p.Animator != null)
                {
                    var frame = p.Animator.GetCurrentFrame();
                    if (frame != null)
                    {
                        double s = 1.5;
                        var f = frame.Value;
                        // Use the new DrawSprite helper
                        DrawSprite(
                            dc,
                            f,
                            new Rect(
                                -f.PixelWidth / 2 * s,
                                -f.PixelHeight / 2 * s,
                                f.PixelWidth * s,
                                f.PixelHeight * s
                            )
                        );
                    }
                }
                else
                {
                    dc.DrawEllipse(Brushes.Gold, new Pen(Brushes.White, 1), new Point(0, 0), 4, 4);
                    var trailPen = new Pen(
                        new SolidColorBrush(Color.FromArgb(150, 255, 200, 0)),
                        2
                    );
                    dc.DrawLine(trailPen, new Point(-6, 0), new Point(-18, 0));
                }

                dc.Pop();
                dc.Pop();
            }

            // [DOD] Update particle drawing loop
            for (int i = 0; i < Engine.Particles.Length; i++)
            {
                ref var pt = ref Engine.Particles[i];
                if (!pt.Active)
                    continue;

                PointD isoP = GameEngine.WorldToIso(pt.X, pt.Y);

                // Culling
                double sx = (isoP.X - Engine.CameraX) * Engine.Zoom;
                double sy = (isoP.Y - Engine.CameraY) * Engine.Zoom;
                if (sx < -20 || sx > ActualWidth + 20 || sy < -20 || sy > ActualHeight + 20)
                    continue;

                dc.PushTransform(new TranslateTransform(sx, sy));
                dc.PushTransform(new RotateTransform(pt.Rotation));
                dc.PushTransform(new ScaleTransform(pt.Scale, pt.Scale));

                var color = Engine.GetParticleColor(ref pt);
                Brush brush;

                if (pt.BlendMode == ParticleBlendMode.Additive)
                {
                    // Simulate additive blending with a radial gradient
                    var additiveBrush = new RadialGradientBrush(
                        Color.FromArgb((byte)(color.A * 0.8), color.R, color.G, color.B),
                        Colors.Transparent
                    );
                    additiveBrush.Freeze();
                    brush = additiveBrush;
                }
                else
                {
                    brush = new SolidColorBrush(color);
                    brush.Freeze();
                }

                // Draw a simple square for particles. Could be an image.
                dc.DrawRectangle(brush, null, new Rect(-4, -4, 8, 8));

                dc.Pop(); // Scale
                dc.Pop(); // Rotate
                dc.Pop(); // Translate
            }
        }
    }
}
