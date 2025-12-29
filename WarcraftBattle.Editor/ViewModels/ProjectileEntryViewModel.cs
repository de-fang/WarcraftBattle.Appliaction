namespace WarcraftBattle.Editor.ViewModels
{
    public class ProjectileEntryViewModel : ObjectEntryViewModel
    {
        private double _speed;
        private double _scale;

        public ProjectileEntryViewModel(string name, string imagePath) : base(name, imagePath)
        {
        }

        public double Speed
        {
            get => _speed;
            set
            {
                if (_speed == value)
                {
                    return;
                }

                _speed = value;
                NotifyOfPropertyChange();
            }
        }

        public double Scale
        {
            get => _scale;
            set
            {
                if (_scale == value)
                {
                    return;
                }

                _scale = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
