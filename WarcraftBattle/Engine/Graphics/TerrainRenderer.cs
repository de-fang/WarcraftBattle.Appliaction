using System;
using System.Collections.Generic;
using WarcraftBattle.Engine.Animation;

namespace WarcraftBattle.Engine.Graphics
{
    //public class TerrainRenderer : IDisposable
    //{
    //    private Dictionary<long, SKImage> _chunkCache = new Dictionary<long, SKImage>();
    //    private int[,] _lastMapData;

    //    // 保持这个画笔长效存在，不要在运行过程中 Dispose 它！
    //    private SKPaint _paint = new SKPaint
    //    {
    //        IsAntialias = false,
    //        FilterQuality = SKFilterQuality.None
    //    };

    //    // 定义每个块包含 5x5 个格子 (5 * 100 = 500px)
    //    // 必须是 TileSize (100) 的整数倍，否则会有缝隙
    //    private const int CHUNK_DIM = 5;

    //    public void Render(SKCanvas canvas, GameEngine engine, SKRect visibleRect)
    //    {
    //        if (engine == null || engine.MapData == null) return;

    //        // 检测地图数据是否发生变化（如切换关卡）
    //        if (_lastMapData != engine.MapData)
    //        {
    //            ClearCache(); // [修复] 只清理缓存，不销毁画笔
    //            _lastMapData = engine.MapData;
    //        }

    //        int tileSize = engine.TileSize;
    //        int chunkSize = tileSize * CHUNK_DIM;

    //        int startChunkX = (int)Math.Floor(visibleRect.Left / chunkSize);
    //        int startChunkY = (int)Math.Floor(visibleRect.Top / chunkSize);
    //        int endChunkX = (int)Math.Ceiling(visibleRect.Right / chunkSize);
    //        int endChunkY = (int)Math.Ceiling(visibleRect.Bottom / chunkSize);

    //        for (int cx = startChunkX; cx < endChunkX; cx++)
    //        {
    //            for (int cy = startChunkY; cy < endChunkY; cy++)
    //            {
    //                long key = ((long)cx << 32) | (long)(uint)cy;

    //                if (!_chunkCache.TryGetValue(key, out var image))
    //                {
    //                    image = BakeChunk(cx, cy, engine, chunkSize);
    //                    if (image != null)
    //                        _chunkCache[key] = image;
    //                }

    //                if (image != null)
    //                {
    //                    // 这里的坐标是逻辑坐标，Canvas 上的 ISO 矩阵会负责将其转换为屏幕上的菱形
    //                    var destRect = new SKRect(cx * chunkSize, cy * chunkSize, (cx + 1) * chunkSize, (cy + 1) * chunkSize);

    //                    // [微调] 稍微扩大 0.5 像素以消除浮点数计算导致的黑线缝隙
    //                    destRect.Inflate(0.5f, 0.5f);

    //                    canvas.DrawImage(image, destRect, _paint);
    //                }
    //            }
    //        }
    //    }

    //    private SKImage BakeChunk(int cx, int cy, GameEngine engine, int chunkSize)
    //    {
    //        int tileSize = engine.TileSize;
    //        int mapW = engine.MapData.GetLength(0);
    //        int mapH = engine.MapData.GetLength(1);

    //        int startTileX = cx * CHUNK_DIM;
    //        int startTileY = cy * CHUNK_DIM;
    //        int endTileX = startTileX + CHUNK_DIM;
    //        int endTileY = startTileY + CHUNK_DIM;

    //        if (startTileX >= mapW || startTileY >= mapH || endTileX <= 0 || endTileY <= 0)
    //            return null;

    //        // 创建 Surface
    //        var info = new SKImageInfo(chunkSize, chunkSize);
    //        using (var surface = SKSurface.Create(info))
    //        {
    //            var c = surface.Canvas;
    //            c.Clear(SKColors.Transparent);

    //            for (int x = startTileX; x < endTileX; x++)
    //            {
    //                for (int y = startTileY; y < endTileY; y++)
    //                {
    //                    if (x < 0 || y < 0 || x >= mapW || y >= mapH) continue;

    //                    int tileId = engine.MapData[x, y];

    //                    // 从 AssetManager 获取图片
    //                    var tileImg = AssetManager.GetSkiaTerrain(tileId);

    //                    float drawX = (x - startTileX) * tileSize;
    //                    float drawY = (y - startTileY) * tileSize;

    //                    // 绘制区域
    //                    var dest = new SKRect(drawX, drawY, drawX + tileSize, drawY + tileSize);

    //                    // 内部格子也做微小的膨胀处理，防止 Chunk 内部有缝隙
    //                    dest.Inflate(0.5f, 0.5f);

    //                    if (tileImg != null)
    //                    {
    //                        c.DrawBitmap(tileImg, dest, _paint);
    //                    }
    //                    else
    //                    {
    //                        // 兜底：如果没有图，画个色块
    //                        using (var debugPaint = new SKPaint { Color = (x + y) % 2 == 0 ? new SKColor(30, 30, 30) : new SKColor(40, 40, 40) })
    //                        {
    //                            c.DrawRect(dest, debugPaint);
    //                        }
    //                    }
    //                }
    //            }
    //            return surface.Snapshot();
    //        }
    //    }

    //    // [新增] 专门用于清理缓存的方法，不销毁 Paint
    //    public void ClearCache()
    //    {
    //        foreach (var img in _chunkCache.Values) img.Dispose();
    //        _chunkCache.Clear();
    //    }

    //    // 真正的 Dispose 只在关游戏或彻底销毁 Renderer 时调用
    //    public void Dispose()
    //    {
    //        ClearCache();
    //        _paint.Dispose();
    //    }
    //}
}