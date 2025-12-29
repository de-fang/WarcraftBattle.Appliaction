using Caliburn.Micro;

namespace WarcraftBattle.Editor.ViewModels
{
    public class PlacementViewModel : PropertyChangedBase
    {
        private double _x;
        private double _y;

        public PlacementViewModel(string name, string kind, double width, double height)
        {
            Name = name;
            Kind = kind;
            Width = width;
            Height = height;
        }

        public string Name { get; }

        public string Kind { get; }

        public double Width { get; }

        public double Height { get; }

        public double X
        {
            get => _x;
            set
            {
                if (Set(ref _x, value))
                {
                    NotifyOfPropertyChange();
                }
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                if (Set(ref _y, value))
                {
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
