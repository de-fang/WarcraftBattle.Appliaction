using WarcraftBattle.Shared.Models;
using WarcraftBattle.Shared.Models.Config;

namespace WarcraftBattle.Editor.ViewModels
{
    public class StageEntryViewModel
    {
        public StageEntryViewModel(StageConfig config)
        {
            Config = config;
            StageInfo = StageMapper.ToStageInfo(config);
        }

        public StageConfig Config { get; }

        public StageInfo StageInfo { get; }

        public string DisplayName => $"{Config.Id} - {Config.Title}";

        public void SyncToConfig()
        {
            StageMapper.ApplyStageInfo(StageInfo, Config);
        }
    }
}
