using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine
{
    public struct GridPos
    {
        public int x,
            y;

        public GridPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static bool operator ==(GridPos a, GridPos b) => a.x == b.x && a.y == b.y;

        public static bool operator !=(GridPos a, GridPos b) => a.x != b.x || a.y != b.y;

        public override bool Equals(object obj) => obj is GridPos p && this == p;

        public override int GetHashCode() => x ^ (y << 16);
    }

    public class PathNode
    {
        public int X,
            Y;
        public bool IsWalkable;
        public byte Cost;
    }

    public interface IPathfinder
    {
        void Initialize(int worldWidth, int worldHeight);
        void Update();
        void UpdateCollision(IEnumerable<Entity> entities);
        FlowField GetFlowField(int targetX, int targetY);
        List<PointD> FindPath(PointD startWorld, PointD endWorld);
    }

    public class Pathfinder : IPathfinder
    {
        private int _width,
            _height;
        private PathNode[,] _grid;
        private const int GRID_SIZE = 50;
        private byte[,] _costMap; // [ThreadSafety] Main thread source of truth for costs

        // [Fix] Cache flow fields by target to prevent thrashing between multiple targets (e.g. Human vs Orc bases)
        private Dictionary<GridPos, FlowField> _flowFieldCache =
            new Dictionary<GridPos, FlowField>();
        private int _dataVersion = 0;

        // [Async Pathfinding]
        private Task<ComputeResult> _calculationTask;
        private GridPos _pendingTarget = new GridPos(-1, -1);
        private FlowField _incompleteField = new FlowField(0, 0) { IsComplete = false };

        // [Optimization] Object Pools
        private ConcurrentStack<FlowField> _flowFieldPool = new ConcurrentStack<FlowField>();
        private ConcurrentStack<int[,]> _scratchPadPool = new ConcurrentStack<int[,]>();
        private ConcurrentStack<byte[,]> _byteMapPool = new ConcurrentStack<byte[,]>();

        private struct ComputeResult
        {
            public FlowField Field;
            public int[,] Scratchpad;
            public byte[,] CostSnapshot;
            public int Version; // [Fix] Add versioning to invalidate stale results
        }

        public Pathfinder(int worldWidth, int worldHeight)
        {
            Initialize(worldWidth, worldHeight);
        }

        public void Initialize(int worldWidth, int worldHeight)
        {
            _width = (int)Math.Ceiling((double)worldWidth / GRID_SIZE);
            _height = (int)Math.Ceiling((double)worldHeight / GRID_SIZE);
            _grid = new PathNode[_width, _height];
            _costMap = new byte[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y] = new PathNode
                    {
                        X = x,
                        Y = y,
                        IsWalkable = true,
                        Cost = 1,
                    };
                    _costMap[x, y] = 1;
                }
            }
            _flowFieldPool = new ConcurrentStack<FlowField>();
            _scratchPadPool = new ConcurrentStack<int[,]>();
            _byteMapPool = new ConcurrentStack<byte[,]>();
            _flowFieldCache.Clear();
        }

        public void Update()
        {
            // [Double Buffering] Check if background task is done and swap the field
            if (_calculationTask != null && _calculationTask.IsCompleted)
            {
                if (_calculationTask.Status == TaskStatus.RanToCompletion)
                {
                    var result = _calculationTask.Result;

                    // [Fix] Only cache if version matches (map hasn't changed during calculation)
                    if (result.Version == _dataVersion)
                    {
                        GridPos p = new GridPos(result.Field.TargetX, result.Field.TargetY);

                        // If we already have one (rare race?), recycle old
                        if (_flowFieldCache.TryGetValue(p, out var old))
                            _flowFieldPool.Push(old);

                        _flowFieldCache[p] = result.Field;
                    }
                    else
                    {
                        // Stale result, recycle immediately
                        _flowFieldPool.Push(result.Field);
                    }

                    // Recycle buffers
                    _scratchPadPool.Push(result.Scratchpad);
                    _byteMapPool.Push(result.CostSnapshot);
                }
                _calculationTask = null;
                _pendingTarget = new GridPos(-1, -1); // Reset pending
            }
        }

        public void UpdateCollision(IEnumerable<Entity> entities)
        {
            _dataVersion++; // [Fix] Invalidate pending tasks

            // [Fix] Recycle cached fields because terrain changed
            foreach (var field in _flowFieldCache.Values)
            {
                if (field != null)
                    _flowFieldPool.Push(field);
            }
            _flowFieldCache.Clear();

            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y].IsWalkable = true;
                    _grid[x, y].Cost = 1;
                    _costMap[x, y] = 1;
                }

            foreach (var e in entities)
            {
                if (!e.IsSolid || e.HP <= 0)
                    continue;
                if (e is Unit)
                    continue;

                int startX = (int)((e.X - e.Width / 2) / GRID_SIZE);
                int endX = (int)((e.X + e.Width / 2) / GRID_SIZE);
                int startY = (int)((e.Y - e.Height / 2) / GRID_SIZE);
                int endY = (int)((e.Y + e.Height / 2) / GRID_SIZE);

                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        if (x >= 0 && x < _width && y >= 0 && y < _height)
                        {
                            _grid[x, y].IsWalkable = false;
                            _grid[x, y].Cost = 255;
                            _costMap[x, y] = 255;
                        }
                    }
                }
            }
        }

        public FlowField GetFlowField(int targetX, int targetY)
        {
            int gx = Math.Clamp(targetX / GRID_SIZE, 0, _width - 1);
            int gy = Math.Clamp(targetY / GRID_SIZE, 0, _height - 1);
            GridPos target = new GridPos(gx, gy);

            // 1. Check Cache
            if (_flowFieldCache.TryGetValue(target, out var cachedField))
            {
                return cachedField;
            }

            // 2. Check Pending (Are we already calculating THIS target?)
            if (_pendingTarget == target && _calculationTask != null)
            {
                return _incompleteField;
            }

            // 3. Start New Task (only if idle, to avoid queueing too many)
            if (_calculationTask == null)
            {
                _pendingTarget = target;

                // [Optimization] Rent from pools
                if (!_byteMapPool.TryPop(out var costSnapshot))
                    costSnapshot = new byte[_width, _height];
                Array.Copy(_costMap, costSnapshot, _costMap.Length);

                if (!_flowFieldPool.TryPop(out var field))
                    field = new FlowField(_width, _height);
                if (!_scratchPadPool.TryPop(out var buffer))
                    buffer = new int[_width, _height];

                int currentVer = _dataVersion;

                _calculationTask = Task.Run(() =>
                {
                    FlowField.Compute(_width, _height, costSnapshot, gx, gy, field, buffer);
                    return new ComputeResult
                    {
                        Field = field,
                        Scratchpad = buffer,
                        CostSnapshot = costSnapshot,
                        Version = currentVer,
                    };
                });
            }

            return _incompleteField;
        }

        public List<PointD> FindPath(PointD startWorld, PointD endWorld)
        {
            int sx = (int)(startWorld.X / GRID_SIZE);
            int sy = (int)(startWorld.Y / GRID_SIZE);
            int ex = (int)(endWorld.X / GRID_SIZE);
            int ey = (int)(endWorld.Y / GRID_SIZE);

            // 限制坐标在网格范围内
            sx = Math.Max(0, Math.Min(_width - 1, sx));
            sy = Math.Max(0, Math.Min(_height - 1, sy));
            ex = Math.Max(0, Math.Min(_width - 1, ex));
            ey = Math.Max(0, Math.Min(_height - 1, ey));

            PathNode startNode = _grid[sx, sy];
            PathNode endNode = _grid[ex, ey];

            // 如果终点不可行走，寻找最近的可行走点
            if (!endNode.IsWalkable)
            {
                bool found = false;
                for (int r = 1; r <= 3 && !found; r++) // 搜索半径
                {
                    for (int x = -r; x <= r; x++)
                    {
                        for (int y = -r; y <= r; y++)
                        {
                            int nx = ex + x;
                            int ny = ey + y;
                            if (
                                nx >= 0
                                && nx < _width
                                && ny >= 0
                                && ny < _height
                                && _grid[nx, ny].IsWalkable
                            )
                            {
                                endNode = _grid[nx, ny];
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            break;
                    }
                }
            }

            // A* 算法实现
            var openSet = new List<PathNode>();
            var closedSet = new HashSet<PathNode>();
            var nodeState = new Dictionary<PathNode, (int g, int h, PathNode parent)>();

            openSet.Add(startNode);
            nodeState[startNode] = (0, GetDistance(startNode, endNode), null);

            while (openSet.Count > 0)
            {
                // 简单的排序，寻找 F Cost (G+H) 最小的节点
                openSet.Sort(
                    (a, b) =>
                    {
                        var stateA = nodeState[a];
                        var stateB = nodeState[b];
                        return (stateA.g + stateA.h).CompareTo(stateB.g + stateB.h);
                    }
                );

                PathNode current = openSet[0];
                openSet.RemoveAt(0);
                closedSet.Add(current);

                if (current == endNode)
                {
                    return RetracePath(startNode, endNode, nodeState, endWorld);
                }

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!neighbor.IsWalkable || closedSet.Contains(neighbor))
                        continue;

                    var currentState = nodeState[current];
                    int newMovementCostToNeighbor = currentState.g + GetDistance(current, neighbor);

                    bool inOpenSet = openSet.Contains(neighbor);
                    if (
                        !inOpenSet
                        || newMovementCostToNeighbor
                            < (
                                nodeState.ContainsKey(neighbor)
                                    ? nodeState[neighbor].g
                                    : int.MaxValue
                            )
                    )
                    {
                        nodeState[neighbor] = (
                            newMovementCostToNeighbor,
                            GetDistance(neighbor, endNode),
                            current
                        );
                        if (!inOpenSet)
                            openSet.Add(neighbor);
                    }
                }
            }

            // 如果找不到路径，返回直线（或者可以返回空列表表示无法到达）
            return new List<PointD> { endWorld };
        }

        private List<PathNode> GetNeighbors(PathNode node)
        {
            var neighbors = new List<PathNode>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;
                    int checkX = node.X + x;
                    int checkY = node.Y + y;
                    if (checkX >= 0 && checkX < _width && checkY >= 0 && checkY < _height)
                    {
                        neighbors.Add(_grid[checkX, checkY]);
                    }
                }
            }
            return neighbors;
        }

        private int GetDistance(PathNode a, PathNode b)
        {
            int dstX = Math.Abs(a.X - b.X);
            int dstY = Math.Abs(a.Y - b.Y);
            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

        private List<PointD> RetracePath(
            PathNode startNode,
            PathNode endNode,
            Dictionary<PathNode, (int g, int h, PathNode parent)> nodeState,
            PointD endWorld
        )
        {
            var path = new List<PointD>();
            PathNode currentNode = endNode;

            while (currentNode != startNode)
            {
                // 转换为世界坐标（网格中心）
                path.Add(
                    new PointD(
                        currentNode.X * GRID_SIZE + GRID_SIZE / 2,
                        currentNode.Y * GRID_SIZE + GRID_SIZE / 2
                    )
                );
                if (nodeState.ContainsKey(currentNode))
                    currentNode = nodeState[currentNode].parent;
                else
                    break; // 异常保护
            }
            path.Reverse();

            // 将最后一个点替换为精确的点击位置，提升体验
            if (path.Count > 0)
                path[path.Count - 1] = endWorld;
            else
                path.Add(endWorld);

            return path;
        }
    }

    public class FlowField
    {
        private int _width,
            _height;
        private PointD[,] _vectorField;
        private const int GRID_SIZE = 50;

        public bool IsComplete { get; set; } = true;
        public int TargetX { get; private set; }
        public int TargetY { get; private set; }

        public FlowField(int w, int h)
        {
            _width = w;
            _height = h;
            _vectorField = new PointD[w, h];
        }

        // [Async] Static method to compute flow field in background
        public static void Compute(
            int w,
            int h,
            byte[,] costs,
            int tx,
            int ty,
            FlowField resultField,
            int[,] integrationField
        )
        {
            resultField.TargetX = tx;
            resultField.TargetY = ty;

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    integrationField[x, y] = 65535;

            // Dijkstra
            var q = new Queue<GridPos>();
            q.Enqueue(new GridPos(tx, ty));
            integrationField[tx, ty] = 0;

            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            while (q.Count > 0)
            {
                var cur = q.Dequeue();

                for (int i = 0; i < 4; i++)
                {
                    int nx = cur.x + dx[i];
                    int ny = cur.y + dy[i];

                    if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                    {
                        byte cost = costs[nx, ny];
                        if (cost == 255)
                            continue; // ǽ

                        int newDist = integrationField[cur.x, cur.y] + cost;
                        if (newDist < integrationField[nx, ny])
                        {
                            integrationField[nx, ny] = newDist;
                            q.Enqueue(new GridPos(nx, ny));
                        }
                    }
                }
            }

            // Generate Vector Field
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (costs[x, y] == 255)
                    {
                        resultField._vectorField[x, y] = new PointD(0, 0);
                        continue;
                    }

                    int min = integrationField[x, y];
                    int bx = 0,
                        by = 0;

                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx[i];
                        int ny = y + dy[i];
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                        {
                            int val = integrationField[nx, ny];
                            if (val < min)
                            {
                                min = val;
                                bx = dx[i];
                                by = dy[i];
                            }
                        }
                    }

                    if (bx != 0 || by != 0)
                        resultField._vectorField[x, y] = new PointD(bx, by);
                    else
                        resultField._vectorField[x, y] = new PointD(0, 0);
                }
            }

            resultField.IsComplete = true;
        }

        public PointD GetDirection(double worldX, double worldY)
        {
            int gx = (int)(worldX / GRID_SIZE);
            int gy = (int)(worldY / GRID_SIZE);
            if (gx >= 0 && gx < _width && gy >= 0 && gy < _height)
            {
                return _vectorField[gx, gy];
            }
            return new PointD(0, 0);
        }
    }
}
