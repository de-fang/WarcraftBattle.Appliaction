using System;
using System.Collections.Generic;
using WarcraftBattle.Engine.Components;

namespace WarcraftBattle.Engine
{
    public enum FogState { Unexplored, Explored, Visible }

    public class FogOfWar
    {
        public FogState[,] Grid { get; private set; }
        // [Visual Optimization] Raw fog alpha map for texture generation (0=Visible, 120=Explored, 255=Unexplored)
        public byte[] FogAlphaMap { get; private set; }
        public int TextureWidth => _gridWidth;
        public int TextureHeight => _gridHeight;

        private int _gridWidth, _gridHeight;
        public const int CELL_SIZE = 50;

        // [Optimization] Precomputed vision masks
        private Dictionary<int, List<GridPos>> _visionMasks = new Dictionary<int, List<GridPos>>();

        public void Initialize(int worldWidth, int worldHeight)
        {
            _gridWidth = worldWidth / CELL_SIZE + 1;
            _gridHeight = worldHeight / CELL_SIZE + 1;
            Grid = new FogState[_gridWidth, _gridHeight];
            FogAlphaMap = new byte[_gridWidth * _gridHeight];

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    Grid[x, y] = FogState.Unexplored;
                    FogAlphaMap[y * _gridWidth + x] = 255; // Default opaque
                }
            }
            _visionMasks.Clear();
        }

        public void Update(List<Entity> visionSources)
        {
            for (int x = 0; x < _gridWidth; x++)
                for (int y = 0; y < _gridHeight; y++)
                    if (Grid[x, y] == FogState.Visible)
                        Grid[x, y] = FogState.Explored;

            foreach (var source in visionSources)
            {
                var vision = source.GetComponent<VisionComponent>();
                if (vision == null) continue;

                // Convert radius to cell count
                int radius = (int)Math.Ceiling(vision.SightRadius / CELL_SIZE);
                var mask = GetVisionMask(radius);

                int cx = (int)(source.X / CELL_SIZE);
                int cy = (int)(source.Y / CELL_SIZE);

                foreach (var offset in mask)
                {
                    int gx = cx + offset.x;
                    int gy = cy + offset.y;

                    if (gx >= 0 && gx < _gridWidth && gy >= 0 && gy < _gridHeight)
                    {
                        Grid[gx, gy] = FogState.Visible;
                    }
                }
            }

            // [Visual Optimization] Update Alpha Map for Texture Generation
            // Map logic state to visual alpha: Visible(0), Explored(120), Unexplored(255)
            for (int y = 0; y < _gridHeight; y++)
            {
                int rowOffset = y * _gridWidth;
                for (int x = 0; x < _gridWidth; x++)
                {
                    byte alpha = 255;
                    switch (Grid[x, y])
                    {
                        case FogState.Visible: alpha = 0; break;
                        case FogState.Explored: alpha = 120; break; // Semi-transparent for explored terrain
                        case FogState.Unexplored: alpha = 255; break; // Opaque for unknown
                    }
                    FogAlphaMap[rowOffset + x] = alpha;
                }
            }
        }

        private List<GridPos> GetVisionMask(int radius)
        {
            if (_visionMasks.TryGetValue(radius, out var mask))
                return mask;

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
            int gx = (int)(worldX / CELL_SIZE);
            int gy = (int)(worldY / CELL_SIZE);

            if (gx >= 0 && gx < _gridWidth && gy >= 0 && gy < _gridHeight)
            {
                return Grid[gx, gy] == FogState.Visible;
            }
            return false;
        }
    }
}