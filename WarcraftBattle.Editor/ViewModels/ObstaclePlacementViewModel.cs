using Caliburn.Micro;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Editor.ViewModels
{
    public class ObstaclePlacementViewModel : PropertyChangedBase, IEditablePlacement
    {
        private readonly StageInfo _stage;
        private readonly int _index;
        private readonly string _displayName;

        public ObstaclePlacementViewModel(StageInfo stage, int index, string displayName)
        {
            _stage = stage;
            _index = index;
            _displayName = displayName;
        }

        public string DisplayName => _displayName;

        public string Kind => "Obstacle";

        public double X
        {
            get => _stage.FixedObstacles[_index].X;
            set => UpdateModel(model => model.X = value, nameof(X));
        }

        public double Y
        {
            get => _stage.FixedObstacles[_index].Y;
            set => UpdateModel(model => model.Y = value, nameof(Y));
        }

        public double Width
        {
            get => _stage.FixedObstacles[_index].Width;
            set => UpdateModel(model => model.Width = value, nameof(Width));
        }

        public double Height
        {
            get => _stage.FixedObstacles[_index].Height;
            set => UpdateModel(model => model.Height = value, nameof(Height));
        }

        public double Rotation
        {
            get => _stage.FixedObstacles[_index].Rotation;
            set => UpdateModel(model => model.Rotation = value, nameof(Rotation));
        }

        private void UpdateModel(System.Action<LevelObstacleDef> update, string propertyName)
        {
            var model = _stage.FixedObstacles[_index];
            update(model);
            _stage.FixedObstacles[_index] = model;
            NotifyOfPropertyChange(propertyName);
        }
    }
}
