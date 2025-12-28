using Caliburn.Micro;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Editor.ViewModels
{
    public class PlacementViewModel : PropertyChangedBase, IEditablePlacement
    {
        private readonly StageInfo _stage;
        private readonly int _index;
        private readonly string _displayName;

        public PlacementViewModel(StageInfo stage, int index, string displayName)
        {
            _stage = stage;
            _index = index;
            _displayName = displayName;
        }

        public string DisplayName => _displayName;

        public string Kind => "Building";

        public double X
        {
            get => _stage.Placements[_index].X;
            set => UpdateModel(model => model.X = value, nameof(X));
        }

        public double Y
        {
            get => _stage.Placements[_index].Y;
            set => UpdateModel(model => model.Y = value, nameof(Y));
        }

        public double Width
        {
            get => _stage.Placements[_index].Width;
            set => UpdateModel(model => model.Width = value, nameof(Width));
        }

        public double Height
        {
            get => _stage.Placements[_index].Height;
            set => UpdateModel(model => model.Height = value, nameof(Height));
        }

        public double Rotation
        {
            get => _stage.Placements[_index].Rotation;
            set => UpdateModel(model => model.Rotation = value, nameof(Rotation));
        }

        private void UpdateModel(System.Action<EntityPlacement> update, string propertyName)
        {
            var model = _stage.Placements[_index];
            update(model);
            _stage.Placements[_index] = model;
            NotifyOfPropertyChange(propertyName);
        }
    }
}
