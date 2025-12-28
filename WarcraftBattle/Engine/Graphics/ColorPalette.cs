namespace WarcraftBattle.Engine.Graphics
{
    //public static class ColorPalette
    //{
    //    private static readonly Dictionary<string, SKPaint> _floaterPaintCache = new Dictionary<string, SKPaint>();

    //    private static SKPaint CreatePaint(SKColor color, bool stroke = false, float strokeWidth = 1)
    //    {
    //        return new SKPaint
    //        {
    //            Color = color,
    //            Style = stroke ? SKPaintStyle.Stroke : SKPaintStyle.Fill,
    //            StrokeWidth = strokeWidth,
    //            IsAntialias = true
    //        };
    //    }

    //    public static class UI
    //    {
    //        public static readonly SKPaint BorderPen = CreatePaint(SKColors.Black, true, 1);
    //        public static readonly SKPaint SelectionPen = CreatePaint(SKColors.LightGreen, true, 2);
    //        public static readonly SKPaint FloaterBorderPen = CreatePaint(SKColors.Black, true, 2);
    //        public static readonly SKPaint HPBarBg = CreatePaint(new SKColor(30, 30, 30));
    //        public static readonly SKPaint HPBarHighlight = CreatePaint(new SKColor(255, 255, 255, 100));
    //        public static readonly SKPaint HPFriendly = CreatePaint(SKColors.LimeGreen);
    //        public static readonly SKPaint HPEnemy = CreatePaint(SKColors.Red);
    //        public static readonly SKPaint GridPen = CreatePaint(new SKColor(0, 0, 0, 30), true, 1);
    //    }

    //    public static class Factions
    //    {
    //        public static readonly SKPaint HumanStoneLight = CreatePaint(new SKColor(120, 130, 140));
    //        public static readonly SKPaint HumanStoneDark = CreatePaint(new SKColor(80, 90, 100));
    //        public static readonly SKPaint HumanRoof = CreatePaint(new SKColor(40, 80, 160));
    //        public static readonly SKPaint HumanWood = CreatePaint(new SKColor(100, 70, 40));

    //        public static readonly SKPaint OrcWoodLight = CreatePaint(new SKColor(140, 100, 60));
    //        public static readonly SKPaint OrcWoodDark = CreatePaint(new SKColor(100, 70, 40));
    //        public static readonly SKPaint OrcRoof = CreatePaint(new SKColor(160, 40, 40));
    //        public static readonly SKPaint OrcBone = CreatePaint(new SKColor(220, 210, 190));
    //    }

    //    public static class Environment
    //    {
    //        public static readonly SKPaint GroundBase = CreatePaint(new SKColor(50, 65, 40));
    //        public static readonly SKPaint Road = CreatePaint(new SKColor(140, 110, 80, 180));
    //        public static readonly SKPaint Shadow = CreatePaint(new SKColor(0, 0, 0, 140));
    //        public static readonly SKPaint TreeLeaf = CreatePaint(new SKColor(30, 80, 20));
    //        public static readonly SKPaint TreeTrunk = CreatePaint(new SKColor(60, 40, 20));
    //        public static readonly SKPaint Rock = CreatePaint(new SKColor(80, 80, 90));
    //        public static readonly SKPaint Sky = CreatePaint(new SKColor(15, 20, 35)); // Gradient replacement simplified for now
    //    }

    //    public static class Effects
    //    {
    //        public static readonly SKPaint Blood = CreatePaint(new SKColor(180, 20, 20, 220));
    //        public static readonly SKPaint Spark = CreatePaint(new SKColor(255, 160, 60, 220));
    //    }

    //    static ColorPalette()
    //    {
    //        _floaterPaintCache["Red"] = CreatePaint(SKColors.Red);
    //        _floaterPaintCache["Gold"] = CreatePaint(SKColors.Gold);
    //        _floaterPaintCache["White"] = CreatePaint(SKColors.White);
    //        _floaterPaintCache["Green"] = CreatePaint(SKColors.Lime);
    //        _floaterPaintCache["Cyan"] = CreatePaint(SKColors.Cyan);
    //        _floaterPaintCache["Gray"] = CreatePaint(SKColors.Gray);
    //        _floaterPaintCache["Lime"] = CreatePaint(SKColors.LimeGreen);
    //    }

    //    public static SKPaint GetFloaterPaint(string colorName)
    //    {
    //        if (_floaterPaintCache.TryGetValue(colorName, out var paint))
    //        {
    //            return paint;
    //        }

    //        try
    //        {
    //            if (SKColor.TryParse(colorName, out var color))
    //            {
    //                var newPaint = CreatePaint(color);
    //                _floaterPaintCache[colorName] = newPaint;
    //                return newPaint;
    //            }
    //        }
    //        catch { }

    //        return _floaterPaintCache["White"];
    //    }
    //}
}
