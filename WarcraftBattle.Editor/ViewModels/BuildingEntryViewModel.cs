namespace WarcraftBattle.Editor.ViewModels
{
    public class BuildingEntryViewModel : ObjectEntryViewModel
    {
        private double _width;
        private double _height;
        private double _hp;

        public BuildingEntryViewModel(string name, string imagePath) : base(name, imagePath)
        {
        }

        public double Width
        {
            get => _width;
            set
            {
                if (_width == value)
                {
                    return;
                }

                _width = value;
                NotifyOfPropertyChange();
            }
        }

        public double Height
        {
            get => _height;
            set
            {
                if (_height == value)
                {
                    return;
                }

                _height = value;
                NotifyOfPropertyChange();
            }
        }

        public double Hp
        {
            get => _hp;
            set
            {
                if (_hp == value)
                {
                    return;
                }

                _hp = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
