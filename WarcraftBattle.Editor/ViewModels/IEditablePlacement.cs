namespace WarcraftBattle.Editor.ViewModels
{
    public interface IEditablePlacement
    {
        string DisplayName { get; }
        string Kind { get; }
        double X { get; set; }
        double Y { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        double Rotation { get; set; }
    }
}
