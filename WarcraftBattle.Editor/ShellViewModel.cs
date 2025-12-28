using Caliburn.Micro;
using System.Linq;

namespace WarcraftBattle.Editor
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private MapViewModel? _activeMap;
        private bool _isToolboxVisible = true;
        private bool _isInspectorVisible = true;
        private bool _isOutputVisible = true;

        public ShellViewModel()
        {
            Toolbox = new ToolboxViewModel();
            Inspector = new InspectorViewModel();
            Output = new OutputViewModel();

            Documents.Add(new MapViewModel(Output));
            ActiveItem = Documents.FirstOrDefault();
        }

        public BindableCollection<MapViewModel> Documents { get; } = new BindableCollection<MapViewModel>();

        public ToolboxViewModel Toolbox { get; }
        public InspectorViewModel Inspector { get; }
        public OutputViewModel Output { get; }

        public MapViewModel? ActiveMap
        {
            get => _activeMap;
            private set
            {
                _activeMap = value;
                NotifyOfPropertyChange(() => ActiveMap);
            }
        }

        public bool IsToolboxVisible
        {
            get => _isToolboxVisible;
            set
            {
                _isToolboxVisible = value;
                NotifyOfPropertyChange(() => IsToolboxVisible);
            }
        }

        public bool IsInspectorVisible
        {
            get => _isInspectorVisible;
            set
            {
                _isInspectorVisible = value;
                NotifyOfPropertyChange(() => IsInspectorVisible);
            }
        }

        public bool IsOutputVisible
        {
            get => _isOutputVisible;
            set
            {
                _isOutputVisible = value;
                NotifyOfPropertyChange(() => IsOutputVisible);
            }
        }

        protected override void OnActiveItemChanged(IScreen? newItem, IScreen? oldItem)
        {
            base.OnActiveItemChanged(newItem, oldItem);
            ActiveMap = newItem as MapViewModel;
            Toolbox.SetMap(ActiveMap);
            Inspector.SetMap(ActiveMap);
        }

        public void ToggleToolbox() => IsToolboxVisible = !IsToolboxVisible;
        public void ToggleInspector() => IsInspectorVisible = !IsInspectorVisible;
        public void ToggleOutput() => IsOutputVisible = !IsOutputVisible;
    }
}
