using Caliburn.Micro;

namespace WarcraftBattle.Editor
{
    public class InspectorViewModel : Screen
    {
        private MapViewModel? _map;

        public MapViewModel? Map
        {
            get => _map;
            private set
            {
                _map = value;
                NotifyOfPropertyChange(() => Map);
            }
        }

        public void SetMap(MapViewModel? map)
        {
            Map = map;
        }
    }
}
