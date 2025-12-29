using Caliburn.Micro;
using WarcraftBattle.Editor.Properties;

namespace WarcraftBattle.Editor.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private bool _isToolboxVisible = true;
        private bool _isInspectorVisible = true;
        private bool _isOutputVisible = true;
        private string _statusMessage = StringResources.StatusReady;
        private readonly EditorSessionViewModel _session;

        public ShellViewModel()
        {
            DisplayName = StringResources.AppTitle;
            _session = new EditorSessionViewModel();
            Documents = new BindableCollection<MapViewModel>
            {
                new MapViewModel(StringResources.DocumentDefaultTitle, _session)
            };
            ActivateItemAsync(Documents[0]);
            Toolbox = new ToolboxViewModel(_session);
            Inspector = new InspectorViewModel();
            Output = new OutputViewModel();
            Inspector.ActiveMap = Documents[0];
        }

        public BindableCollection<MapViewModel> Documents { get; }

        public ToolboxViewModel Toolbox { get; }

        public InspectorViewModel Inspector { get; }

        public OutputViewModel Output { get; }

        public bool IsToolboxVisible
        {
            get => _isToolboxVisible;
            set
            {
                if (_isToolboxVisible == value)
                {
                    return;
                }

                _isToolboxVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsInspectorVisible
        {
            get => _isInspectorVisible;
            set
            {
                if (_isInspectorVisible == value)
                {
                    return;
                }

                _isInspectorVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsOutputVisible
        {
            get => _isOutputVisible;
            set
            {
                if (_isOutputVisible == value)
                {
                    return;
                }

                _isOutputVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value)
                {
                    return;
                }

                _statusMessage = value;
                NotifyOfPropertyChange();
            }
        }

        protected override void OnActiveItemChanged(object oldItem, object newItem)
        {
            base.OnActiveItemChanged(oldItem, newItem);
            Inspector.ActiveMap = newItem as MapViewModel;
        }

        public void NewMap()
        {
            var map = new MapViewModel(StringResources.DocumentUntitled, _session);
            Documents.Add(map);
            ActivateItemAsync(map);
            StatusMessage = StringResources.StatusNewMap;
        }

        public void Open()
        {
            StatusMessage = StringResources.StatusOpenNotImplemented;
        }

        public void Save()
        {
            StatusMessage = StringResources.StatusSaveNotImplemented;
        }

        public void SaveAs()
        {
            StatusMessage = StringResources.StatusSaveAsNotImplemented;
        }

        public void Export()
        {
            StatusMessage = StringResources.StatusExportNotImplemented;
        }

        public void Undo()
        {
            StatusMessage = StringResources.StatusUndoNotImplemented;
        }

        public void Redo()
        {
            StatusMessage = StringResources.StatusRedoNotImplemented;
        }

        public void Cut()
        {
            StatusMessage = StringResources.StatusCutNotImplemented;
        }

        public void Copy()
        {
            StatusMessage = StringResources.StatusCopyNotImplemented;
        }

        public void Paste()
        {
            StatusMessage = StringResources.StatusPasteNotImplemented;
        }

        public void ToggleGrid()
        {
            StatusMessage = StringResources.StatusToggleGrid;
        }

        public void ToggleFog()
        {
            StatusMessage = StringResources.StatusToggleFog;
        }

        public void ZoomIn()
        {
            StatusMessage = StringResources.StatusZoomIn;
        }

        public void ZoomOut()
        {
            StatusMessage = StringResources.StatusZoomOut;
        }

        public void OpenObjectEditor()
        {
            StatusMessage = StringResources.StatusObjectEditor;
        }

        public void OpenTriggerEditor()
        {
            StatusMessage = StringResources.StatusTriggerEditor;
        }

        public void OpenEnvironmentSettings()
        {
            StatusMessage = StringResources.StatusEnvironmentSettings;
        }
    }
}
