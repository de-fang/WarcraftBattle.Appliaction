namespace WarcraftBattle.Shared.Models
{
    public class FloaterInfo
    {
        public string Text { get; set; } = "";
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public string Color { get; set; } = "White";
        public double VelX { get; set; } = 0;
        public double VelY { get; set; } = 0;
        public double Life { get; set; } = 1.0;
        public double Size { get; set; } = 20;
    }
}