using Caliburn.Micro;
using System.Windows;
using WarcraftBattle.ViewModels;

namespace WarcraftBattle
{
    public class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper()
        {
            Initialize();

        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewForAsync<ShellViewModel>();
        }
    }
}