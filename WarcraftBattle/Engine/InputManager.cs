using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine
{
    public interface IInputManager
    {
        void HandleInputStart(double x, double y);
        void HandleDrag(double x, double y);
        void HandleInputUp(double x, double y);
        void HandleZoom(double delta, double mouseX, double mouseY);
        void HandleKeyDown(System.Windows.Input.Key key);
        void HandleMouseMove(double screenX, double screenY);
        void HandleDoubleClick(double x, double y);
        void HandleRightClick(double x, double y);
        void HandleMouseClickInput(double screenX, double screenY);
    }

    public class InputManager : IInputManager
    {
        private GameEngine _engine;
        private PointD _mouseDownPos;
        private DateTime _mouseDownTime;
        private double _touchStartX;
        private double _touchStartY;
        private double _lastCameraX;
        private double _lastCameraY;
        private bool _dragIsSelection = false;

        // [New] Double-tap detection for control groups
        private int _lastGroupKeyPressed = -1;
        private DateTime _lastGroupKeyPressTime;

        public InputManager(GameEngine engine)
        {
            _engine = engine;
        }

        public void HandleInputStart(double x, double y)
        {
            if (_engine.IsBuildMode) { _engine.HandleMouseClick(); return; }
            _touchStartX = x; _touchStartY = y;
            _lastCameraX = _engine.CameraX; _lastCameraY = _engine.CameraY;
            _mouseDownPos = new PointD(x, y);
            _mouseDownTime = DateTime.Now;
            _engine.IsDraggingSelection = false;
            _engine.SelectionRect = Rect.Empty;
            _dragIsSelection = System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift);
        }

        public void HandleDrag(double x, double y)
        {
            if (_engine.State != Shared.Enums.GameState.Playing) return;

            if (System.Windows.Input.Mouse.MiddleButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                // Middle mouse pan
                double diffX = (x - _touchStartX) / _engine.Zoom;
                double diffY = (y - _touchStartY) / _engine.Zoom;
                _engine.CameraX = _lastCameraX - diffX;
                _engine.CameraY = _lastCameraY - diffY;
                _engine.ClampCamera();
            }
            else if (!_engine.IsBuildMode && System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (_dragIsSelection)
                {
                    // Left mouse drag (Selection)
                    double dx = x - _touchStartX;
                    double dy = y - _touchStartY;
                    if (Math.Abs(dx) > 5 || Math.Abs(dy) > 5)
                    {
                        _engine.IsDraggingSelection = true;
                        double rx = Math.Min(_touchStartX, x);
                        double ry = Math.Min(_touchStartY, y);
                        double rw = Math.Abs(dx);
                        double rh = Math.Abs(dy);
                        _engine.SelectionRect = new Rect(rx, ry, rw, rh);
                    }
                }
                else
                {
                    // Left mouse drag (Pan)
                    double diffX = (x - _touchStartX) / _engine.Zoom;
                    double diffY = (y - _touchStartY) / _engine.Zoom;
                    _engine.CameraX = _lastCameraX - diffX;
                    _engine.CameraY = _lastCameraY - diffY;
                    _engine.ClampCamera();
                }
            }
        }

        public void HandleInputUp(double x, double y)
        {
            if (_engine.IsDraggingSelection)
            {
                PerformBoxSelection();
                _engine.IsDraggingSelection = false;
                _engine.SelectionRect = Rect.Empty;
            }
            else
            {
                double dist = Math.Abs(x - _mouseDownPos.X) + Math.Abs(y - _mouseDownPos.Y);
                double time = (DateTime.Now - _mouseDownTime).TotalMilliseconds;

                // 判定为单击
                if (dist < 10 && time < 300)
                {
                    HandleMouseClickInput(x, y);
                }
            }
        }

        public void HandleZoom(double delta, double mouseX, double mouseY)
        {
            // 1. 计算理论上的最小缩放值：让地图刚好填满屏幕，不能再小了
            double minZoomX = _engine.ViewportWidth / _engine.WorldWidth;
            double minZoomY = _engine.ViewportHeight / _engine.MapDepth;

            // 取两者中较大的一个，保证宽或高至少有一边是填满的
            double calculatedMinZoom = Math.Max(minZoomX, minZoomY) * 1.05;

            // 2. 更新全局 MinZoom (或者直接在这里用)
            double limitMin = Math.Max(0.5, calculatedMinZoom);

            double oldZoom = _engine.Zoom;
            if (delta > 0) _engine.Zoom *= 1.1;
            else _engine.Zoom /= 1.1;

            // 3. 应用限制
            _engine.Zoom = Math.Max(limitMin, Math.Min(_engine.MaxZoom, _engine.Zoom));

            double factor = 1.0 / oldZoom - 1.0 / _engine.Zoom;
            _engine.CameraX += mouseX * factor;
            _engine.CameraY += mouseY * factor;
            _engine.ClampCamera();
        }

        public void HandleKeyDown(System.Windows.Input.Key key)
        {
            if (key >= System.Windows.Input.Key.D0 && key <= System.Windows.Input.Key.D9)
            {
                int groupNumber = key - System.Windows.Input.Key.D0;

                if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                {
                    // Create or overwrite control group
                    _engine.SetControlGroup(groupNumber);
                }
                else
                {
                    // Select control group
                    // Check for double-tap
                    TimeSpan timeSinceLastPress = DateTime.Now - _lastGroupKeyPressTime;
                    if (_lastGroupKeyPressed == groupNumber && timeSinceLastPress.TotalMilliseconds < 300)
                    {
                        // Double-tap: Select and center camera
                        _engine.SelectControlGroup(groupNumber);
                        _engine.CenterCameraOnSelection();
                        _lastGroupKeyPressed = -1; // Reset double-tap state
                    }
                    else
                    {
                        // Single-tap: Just select
                        _engine.SelectControlGroup(groupNumber);
                        _lastGroupKeyPressed = groupNumber;
                        _lastGroupKeyPressTime = DateTime.Now;
                    }
                }
            }
        }

        public void HandleMouseMove(double screenX, double screenY)
        {
            var wp = _engine.ScreenToWorld(screenX, screenY);
            if (_engine.IsBuildMode && _engine.PendingBuildingInfo != null)
            {
                double w = _engine.PendingBuildingInfo.Width;
                double h = _engine.PendingBuildingInfo.Height;
                int tilesW = (int)Math.Round(w / _engine.TileSize);
                int tilesH = (int)Math.Round(h / _engine.TileSize);
                double gx, gy;
                if (tilesW % 2 != 0) { gx = Math.Round(wp.X / _engine.TileSize) * _engine.TileSize + _engine.TileSize / 2; }
                else { gx = Math.Round(wp.X / _engine.TileSize) * _engine.TileSize; }
                if (tilesH % 2 != 0) { gy = Math.Round(wp.Y / _engine.TileSize) * _engine.TileSize + _engine.TileSize / 2; }
                else { gy = Math.Round(wp.Y / _engine.TileSize) * _engine.TileSize; }
                gx = Math.Max(w / 2, Math.Min(_engine.WorldWidth - w / 2, gx));
                gy = Math.Max(h / 2, Math.Min(_engine.MapDepth - h / 2, gy));
                _engine.GhostPosition = new PointD(gx, gy);
                _engine.CanBuildAtGhost = _engine.CheckBuildValidity(_engine.GhostPosition.X, _engine.GhostPosition.Y, w, h);
            }
            if (_engine.IsTargetingMode) { _engine.GhostPosition = wp; }

            _engine.HoveredEntity = _engine.GetEntityAtScreenPos(screenX, screenY);
        }

        public void HandleDoubleClick(double x, double y)
        {
            if (_engine.State != Shared.Enums.GameState.Playing) return;

            var clickedEntity = _engine.GetEntityAtScreenPos(x, y);

            if (clickedEntity is Unit clickedUnit && clickedUnit.Team == TeamType.Human)
            {
                _engine.ClearSelection();
                var screenRect = new Rect(0, 0, _engine.ViewportWidth, _engine.ViewportHeight);

                foreach (var entity in _engine.Entities)
                {
                    // 检查是否为同种类型的单位
                    if (entity is Unit potentialMatch && potentialMatch.Key == clickedUnit.Key && potentialMatch.Team == clickedUnit.Team)
                    {
                        // 检查是否在屏幕内
                        PointD iso = GameEngine.WorldToIso(potentialMatch.X, potentialMatch.Y);
                        double sx = (iso.X - _engine.CameraX) * _engine.Zoom;
                        double sy = (iso.Y - _engine.CameraY) * _engine.Zoom;
                        if (screenRect.Contains(new Point(sx, sy)))
                        {
                            _engine.AddToSelection(potentialMatch);
                        }
                    }
                }
                _engine.RefreshSelectionState();
            }
        }

        public void HandleRightClick(double x, double y)
        {
            if (_engine.State != Shared.Enums.GameState.Playing) return;
            var wp = _engine.ScreenToWorld(x, y);

            var target = _engine.GetEntityAtScreenPos(x, y);

            bool isAttack = (target != null && target.Team != TeamType.Human && target.Team != TeamType.Neutral);

            var moveUnits = _engine.SelectedEntities.OfType<Unit>().Where(u => u.Team == TeamType.Human).ToList();
            if (moveUnits.Count == 0) return;

            if (isAttack)
            {
                var cmd = new AttackCommand(target);
                foreach (var u in moveUnits) u.IssueCommand(cmd, _engine);
                var vfx = _engine.VisualEffectPool.Get();
                vfx.Init(target.X, target.Y, "ClickAttack", _engine);
                _engine.VisualEffects.Add(vfx);
                _engine.AddFloater("Attack!", moveUnits[0].X, moveUnits[0].Y - 60, "Red");
                return;
            }

            if (moveUnits.Count == 1)
            {
                moveUnits[0].IssueCommand(new MoveCommand(wp.X, wp.Y), _engine);
            }
            else
            {
                var formations = _engine.CalculateFormation(new PointD(wp.X, wp.Y), moveUnits.Count, 50);
                var availablePoints = new List<PointD>(formations);

                foreach (var unit in moveUnits)
                {
                    if (availablePoints.Count == 0) break;

                    PointD bestPoint = availablePoints[0];
                    double minDstSq = double.MaxValue;
                    int bestIndex = 0;

                    for (int i = 0; i < availablePoints.Count; i++)
                    {
                        double d2 = Math.Pow(unit.X - availablePoints[i].X, 2) + Math.Pow(unit.Y - availablePoints[i].Y, 2);
                        if (d2 < minDstSq)
                        {
                            minDstSq = d2;
                            bestPoint = availablePoints[i];
                            bestIndex = i;
                        }
                    }

                    unit.IssueCommand(new MoveCommand(bestPoint.X, bestPoint.Y), _engine);
                    availablePoints.RemoveAt(bestIndex);
                }
            }

            _engine.AddFloater("Move", wp.X, wp.Y - 20, "Lime");
        }

        public void HandleMouseClickInput(double screenX, double screenY)
        {
            if (_engine.IsBuildMode) return;

            var clickedEntity = _engine.GetEntityAtScreenPos(screenX, screenY);

            _engine.ClearSelection();
            if (clickedEntity != null)
            {
                _engine.AddToSelection(clickedEntity);
            }
            _engine.RefreshSelectionState();
        }

        private void PerformBoxSelection()
        {
            if (_engine.SelectionRect.Width < 5 && _engine.SelectionRect.Height < 5) return;

            _engine.ClearSelection();

            var p1 = _engine.ScreenToWorld(_engine.SelectionRect.Left, _engine.SelectionRect.Top);
            var p2 = _engine.ScreenToWorld(_engine.SelectionRect.Right, _engine.SelectionRect.Top);
            var p3 = _engine.ScreenToWorld(_engine.SelectionRect.Left, _engine.SelectionRect.Bottom);
            var p4 = _engine.ScreenToWorld(_engine.SelectionRect.Right, _engine.SelectionRect.Bottom);

            double minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
            double maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
            double minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
            double maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

            var queryRect = new Rect(minX, minY, maxX - minX, maxY - minY);
            var potentialTargets = _engine.GetEntitiesInRect(queryRect);

            foreach (var e in potentialTargets)
            {
                if (e.Team == TeamType.Human && e is Unit && e.HP > 0)
                {
                    PointD iso = GameEngine.WorldToIso(e.X, e.Y);
                    double sx = (iso.X - _engine.CameraX) * _engine.Zoom;
                    double sy = (iso.Y - _engine.CameraY) * _engine.Zoom;

                    if (_engine.SelectionRect.Contains(new Point(sx, sy)))
                    {
                        _engine.AddToSelection(e);
                    }
                }
            }
            _engine.RefreshSelectionState();
        }
    }
}
