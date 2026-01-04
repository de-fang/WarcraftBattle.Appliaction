using System.Collections.Generic;

namespace WarcraftBattle3D.Core
{
    public enum FogState
    {
        Unexplored,
        Explored,
        Visible
    }

    public class FogOfWar
    {
        public FogState[,] Grid { get; private set; }
        public byte[] FogAlphaMap { get; private set; }
        public int TextureWidth => _gridWidth;
        public int TextureHeight => _gridHeight;

        private int _gridWidth;
        private int _gridHeight;
        public const int CellSize = 50;

        private readonly Dictionary<int, List<GridPos>> _visionMasks = new Dictionary<int, List<GridPos>>();

        public void Initialize(int worldWidth, int worldHeight)
        {
            _gridWidth = worldWidth / CellSize + 1;
            _gridHeight = worldHeight / CellSize + 1;
            Grid = new FogState[_gridWidth, _gridHeight];
            FogAlphaMap = new byte[_gridWidth * _gridHeight];

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    Grid[x, y] = FogState.Unexplored;
                    FogAlphaMap[y * _gridWidth + x] = 255;
                }
            }
            _visionMasks.Clear();
        }

        public void Update(List<Entity> visionSources)
        {
            if (Grid == null)
            {
                return;
            }

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (Grid[x, y] == FogState.Visible)
                    {
                        Grid[x, y] = FogState.Explored;
                    }
                }
            }

            if (visionSources != null)
            {
                foreach (var source in visionSources)
                {
                    if (source == null || !source.IsAlive || source.SightRadius <= 0)
                    {
                        continue;
                    }

                    int radius = (int)System.Math.Ceiling(source.SightRadius / CellSize);
                    var mask = GetVisionMask(radius);

                    int cx = (int)(source.X / CellSize);
                    int cy = (int)(source.Y / CellSize);

                    foreach (var offset in mask)
                    {
                        int gx = cx + offset.X;
                        int gy = cy + offset.Y;

                        if (gx >= 0 && gx < _gridWidth && gy >= 0 && gy < _gridHeight)
                        {
                            Grid[gx, gy] = FogState.Visible;
                        }
                    }
                }
            }

            for (int y = 0; y < _gridHeight; y++)
            {
                int rowOffset = y * _gridWidth;
                for (int x = 0; x < _gridWidth; x++)
                {
                    byte alpha = 255;
                    switch (Grid[x, y])
                    {
                        case FogState.Visible:
                            alpha = 0;
                            break;
                        case FogState.Explored:
                            alpha = 120;
                            break;
                        case FogState.Unexplored:
                            alpha = 255;
                            break;
                    }
                    FogAlphaMap[rowOffset + x] = alpha;
                }
            }
        }

        private List<GridPos> GetVisionMask(int radius)
        {
            if (_visionMasks.TryGetValue(radius, out var mask))
            {
                return mask;
            }

            mask = new List<GridPos>();
            int rSq = radius * radius;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= rSq)
                    {
                        mask.Add(new GridPos(x, y));
                    }
                }
            }
            _visionMasks[radius] = mask;
            return mask;
        }

        public bool IsVisible(double worldX, double worldY)
        {
            int gx = (int)(worldX / CellSize);
            int gy = (int)(worldY / CellSize);

            if (gx >= 0 && gx < _gridWidth && gy >= 0 && gy < _gridHeight)
            {
                return Grid[gx, gy] == FogState.Visible;
            }

            return false;
        }
    }
}
