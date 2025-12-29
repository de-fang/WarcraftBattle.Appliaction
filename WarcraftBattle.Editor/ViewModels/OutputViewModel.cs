using System.Collections.ObjectModel;
using Caliburn.Micro;
using WarcraftBattle.Editor.Properties;

namespace WarcraftBattle.Editor.ViewModels
{
    public class OutputViewModel : Screen
    {
        public OutputViewModel()
        {
            DisplayName = StringResources.OutputTitle;
            Messages = new ObservableCollection<string>
            {
                StringResources.OutputPlaceholder
            };
        }

        public ObservableCollection<string> Messages { get; }
    }
}
