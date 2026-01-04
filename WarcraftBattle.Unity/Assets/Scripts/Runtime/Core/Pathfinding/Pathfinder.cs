using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WarcraftBattle3D.Core
{
    public struct GridPos
    {
        public int X;
        public int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(GridPos a, GridPos b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(GridPos a, GridPos b) => a.X != b.X || a.Y != b.Y;

        public override bool Equals(object obj) => obj is GridPos p && this == p;
        public override int GetHashCode() => X ^ (Y << 16);
    }

    public class PathNode
    {
        public int X;
        public int Y;
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
        private int _width;
        private int _height;
        private PathNode[,] _grid;
        private const int GridSize = 50;
        private byte[,] _costMap;

        private readonly Dictionary<GridPos, FlowField> _flowFieldCache = new Dictionary<GridPos, FlowField>();
        private int _dataVersion;

        private Task<ComputeResult> _calculationTask;
        private GridPos _pendingTarget = new GridPos(-1, -1);
        private readonly FlowField _incompleteField = new FlowField(0, 0) { IsComplete = false };

        private ConcurrentStack<FlowField> _flowFieldPool = new ConcurrentStack<FlowField>();
        private ConcurrentStack<int[,]> _scratchPadPool = new ConcurrentStack<int[,]>();
        private ConcurrentStack<byte[,]> _byteMapPool = new ConcurrentStack<byte[,]>();

        private struct ComputeResult
        {
            public FlowField Field;
            public int[,] Scratchpad;
            public byte[,] CostSnapshot;
            public int Version;
        }

        public Pathfinder(int worldWidth, int worldHeight)
        {
            Initialize(worldWidth, worldHeight);
        }

        public void Initialize(int worldWidth, int worldHeight)
        {
            _width = (int)Math.Ceiling((double)worldWidth / GridSize);
            _height = (int)Math.Ceiling((double)worldHeight / GridSize);
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
                        Cost = 1
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
            if (_calculationTask == null || !_calculationTask.IsCompleted)
            {
                return;
            }

            if (_calculationTask.Status == TaskStatus.RanToCompletion)
            {
                var result = _calculationTask.Result;

                if (result.Version == _dataVersion)
                {
                    GridPos p = new GridPos(result.Field.TargetX, result.Field.TargetY);
                    if (_flowFieldCache.TryGetValue(p, out var old))
                    {
                        _flowFieldPool.Push(old);
                    }
                    _flowFieldCache[p] = result.Field;
                }
                else
                {
                    _flowFieldPool.Push(result.Field);
                }

                _scratchPadPool.Push(result.Scratchpad);
                _byteMapPool.Push(result.CostSnapshot);
            }

            _calculationTask = null;
            _pendingTarget = new GridPos(-1, -1);
        }

        public void UpdateCollision(IEnumerable<Entity> entities)
        {
            _dataVersion++;

            foreach (var field in _flowFieldCache.Values)
            {
                if (field != null)
                {
                    _flowFieldPool.Push(field);
                }
            }
            _flowFieldCache.Clear();

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y].IsWalkable = true;
                    _grid[x, y].Cost = 1;
                    _costMap[x, y] = 1;
                }
            }

            if (entities == null)
            {
                return;
            }

            foreach (var e in entities)
            {
                if (e == null || !e.IsSolid || !e.IsAlive)
                {
                    continue;
                }

                if (e is Unit)
                {
                    continue;
                }

                int startX = (int)((e.X - e.Width / 2) / GridSize);
                int endX = (int)((e.X + e.Width / 2) / GridSize);
                int startY = (int)((e.Y - e.Height / 2) / GridSize);
                int endY = (int)((e.Y + e.Height / 2) / GridSize);

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
            int gx = targetX / GridSize;
            int gy = targetY / GridSize;
            if (gx < 0) gx = 0;
            if (gy < 0) gy = 0;
            if (gx >= _width) gx = _width - 1;
            if (gy >= _height) gy = _height - 1;
            GridPos target = new GridPos(gx, gy);

            if (_flowFieldCache.TryGetValue(target, out var cachedField))
            {
                return cachedField;
            }

            if (_pendingTarget == target && _calculationTask != null)
            {
                return _incompleteField;
            }

            if (_calculationTask == null)
            {
                _pendingTarget = target;

                if (!_byteMapPool.TryPop(out var costSnapshot))
                {
                    costSnapshot = new byte[_width, _height];
                }
                Array.Copy(_costMap, costSnapshot, _costMap.Length);

                if (!_flowFieldPool.TryPop(out var field))
                {
                    field = new FlowField(_width, _height);
                }
                if (!_scratchPadPool.TryPop(out var buffer))
                {
                    buffer = new int[_width, _height];
                }

                int currentVer = _dataVersion;

                _calculationTask = Task.Run(() =>
                {
                    FlowField.Compute(_width, _height, costSnapshot, gx, gy, field, buffer);
                    return new ComputeResult
                    {
                        Field = field,
                        Scratchpad = buffer,
                        CostSnapshot = costSnapshot,
                        Version = currentVer
                    };
                });
            }

            return _incompleteField;
        }

        public List<PointD> FindPath(PointD startWorld, PointD endWorld)
        {
            int sx = (int)(startWorld.X / GridSize);
            int sy = (int)(startWorld.Y / GridSize);
            int ex = (int)(endWorld.X / GridSize);
            int ey = (int)(endWorld.Y / GridSize);

            sx = Clamp(sx, 0, _width - 1);
            sy = Clamp(sy, 0, _height - 1);
            ex = Clamp(ex, 0, _width - 1);
            ey = Clamp(ey, 0, _height - 1);

            PathNode startNode = _grid[sx, sy];
            PathNode endNode = _grid[ex, ey];

            if (!endNode.IsWalkable)
            {
                bool found = false;
                for (int r = 1; r <= 3 && !found; r++)
                {
                    for (int x = -r; x <= r; x++)
                    {
                        for (int y = -r; y <= r; y++)
                        {
                            int nx = ex + x;
                            int ny = ey + y;
                            if (nx >= 0 && nx < _width && ny >= 0 && ny < _height && _grid[nx, ny].IsWalkable)
                            {
                                endNode = _grid[nx, ny];
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                }
            }

            var openSet = new List<PathNode> { startNode };
            var closedSet = new HashSet<PathNode>();
            var nodeState = new Dictionary<PathNode, NodeState> { [startNode] = new NodeState(0, GetDistance(startNode, endNode), null) };

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) =>
                {
                    var stateA = nodeState[a];
                    var stateB = nodeState[b];
                    return (stateA.G + stateA.H).CompareTo(stateB.G + stateB.H);
                });

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
                    {
                        continue;
                    }

                    int newMovementCostToNeighbor = nodeState[current].G + GetDistance(current, neighbor);
                    bool inOpenSet = openSet.Contains(neighbor);
                    int existing = nodeState.TryGetValue(neighbor, out var st) ? st.G : int.MaxValue;

                    if (!inOpenSet || newMovementCostToNeighbor < existing)
                    {
                        nodeState[neighbor] = new NodeState(newMovementCostToNeighbor, GetDistance(neighbor, endNode), current);
                        if (!inOpenSet)
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return new List<PointD> { endWorld };
        }

        private struct NodeState
        {
            public int G;
            public int H;
            public PathNode Parent;

            public NodeState(int g, int h, PathNode parent)
            {
                G = g;
                H = h;
                Parent = parent;
            }
        }

        private List<PathNode> GetNeighbors(PathNode node)
        {
            var neighbors = new List<PathNode>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

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
            {
                return 14 * dstY + 10 * (dstX - dstY);
            }
            return 14 * dstX + 10 * (dstY - dstX);
        }

        private List<PointD> RetracePath(
            PathNode startNode,
            PathNode endNode,
            Dictionary<PathNode, NodeState> nodeState,
            PointD endWorld)
        {
            var path = new List<PointD>();
            PathNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(new PointD(
                    currentNode.X * GridSize + GridSize / 2,
                    currentNode.Y * GridSize + GridSize / 2));

                if (nodeState.TryGetValue(currentNode, out var st))
                {
                    currentNode = st.Parent;
                }
                else
                {
                    break;
                }
            }

            path.Reverse();

            if (path.Count > 0)
            {
                path[path.Count - 1] = endWorld;
            }
            else
            {
                path.Add(endWorld);
            }

            return path;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    public class FlowField
    {
        private readonly int _width;
        private readonly int _height;
        private readonly PointD[,] _vectorField;
        private const int GridSize = 50;

        public bool IsComplete { get; set; } = true;
        public int TargetX { get; private set; }
        public int TargetY { get; private set; }

        public FlowField(int w, int h)
        {
            _width = w;
            _height = h;
            _vectorField = new PointD[w, h];
        }

        public static void Compute(
            int w,
            int h,
            byte[,] costs,
            int tx,
            int ty,
            FlowField resultField,
            int[,] integrationField)
        {
            resultField.TargetX = tx;
            resultField.TargetY = ty;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    integrationField[x, y] = 65535;
                }
            }

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
                    int nx = cur.X + dx[i];
                    int ny = cur.Y + dy[i];

                    if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                    {
                        byte cost = costs[nx, ny];
                        if (cost == 255)
                        {
                            continue;
                        }

                        int newDist = integrationField[cur.X, cur.Y] + cost;
                        if (newDist < integrationField[nx, ny])
                        {
                            integrationField[nx, ny] = newDist;
                            q.Enqueue(new GridPos(nx, ny));
                        }
                    }
                }
            }

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
                    int bx = 0;
                    int by = 0;

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
                    {
                        resultField._vectorField[x, y] = new PointD(bx, by);
                    }
                    else
                    {
                        resultField._vectorField[x, y] = new PointD(0, 0);
                    }
                }
            }

            resultField.IsComplete = true;
        }

        public PointD GetDirection(double worldX, double worldY)
        {
            int gx = (int)(worldX / GridSize);
            int gy = (int)(worldY / GridSize);
            if (gx >= 0 && gx < _width && gy >= 0 && gy < _height)
            {
                return _vectorField[gx, gy];
            }
            return new PointD(0, 0);
        }
    }
}
