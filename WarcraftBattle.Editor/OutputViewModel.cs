using Caliburn.Micro;
using System.Collections.ObjectModel;

namespace WarcraftBattle.Editor
{
    public class OutputViewModel : Screen
    {
        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();

        public void AddMessage(string message)
        {
            Messages.Add(message);
        }
    }
}
