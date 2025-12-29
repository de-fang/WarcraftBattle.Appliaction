using Caliburn.Micro;
using WarcraftBattle.Editor.Properties;

namespace WarcraftBattle.Editor.ViewModels
{
    public class InspectorViewModel : Screen
    {
        private MapViewModel _activeMap;

        public InspectorViewModel()
        {
            DisplayName = StringResources.InspectorTitle;
        }

        public MapViewModel ActiveMap
        {
            get => _activeMap;
            set
            {
                if (_activeMap == value)
                {
                    return;
                }

                _activeMap = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(MapWidth));
                NotifyOfPropertyChange(nameof(MapHeight));
            }
        }

        public int MapWidth
        {
            get => ActiveMap?.TilesWide ?? 0;
            set
            {
                if (ActiveMap == null)
                {
                    return;
                }

                ActiveMap.TilesWide = value;
                NotifyOfPropertyChange();
            }
        }

        public int MapHeight
        {
            get => ActiveMap?.TilesHigh ?? 0;
            set
            {
                if (ActiveMap == null)
                {
                    return;
                }

                ActiveMap.TilesHigh = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
