namespace WarcraftBattle.Editor.ViewModels
{
    public class ObstacleEntryViewModel : ObjectEntryViewModel
    {
        private double _width;
        private double _height;
        private bool _hasCollision;

        public ObstacleEntryViewModel(string name, string imagePath) : base(name, imagePath)
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

        public bool HasCollision
        {
            get => _hasCollision;
            set
            {
                if (_hasCollision == value)
                {
                    return;
                }

                _hasCollision = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
