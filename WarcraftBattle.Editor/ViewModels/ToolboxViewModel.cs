using Caliburn.Micro;
using WarcraftBattle.Editor.Properties;

namespace WarcraftBattle.Editor.ViewModels
{
    public class ToolboxViewModel : Screen
    {
        public ToolboxViewModel(EditorSessionViewModel session)
        {
            DisplayName = StringResources.ToolboxTitle;
            Session = session;
        }

        public EditorSessionViewModel Session { get; }
    }
}
