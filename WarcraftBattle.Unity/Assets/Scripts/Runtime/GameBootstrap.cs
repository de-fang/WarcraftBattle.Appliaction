using UnityEngine;

namespace WarcraftBattle3D
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField]
        private bool autoStart = true;
        [SerializeField]
        private string configFileName = "GameConfig.xml";
        [SerializeField]
        private bool autoStartGame = true;
        [SerializeField]
        private int startStage = 1;
        [SerializeField]
        private bool autoSpawnVfxView = true;
        [SerializeField]
        private bool autoConfigureVfxScale = true;
        [SerializeField]
        private bool vfxAutoScaleFromEngine = true;
        [SerializeField]
        private float vfxUnityTileSize = 1f;
        [SerializeField]
        private float vfxHeightScaleMultiplier = 1.2f;
        [SerializeField]
        private bool autoSpawnEntityView = true;
        [SerializeField]
        private bool autoConfigureEntityScale = true;
        [SerializeField]
        private bool entityAutoScaleFromEngine = true;
        [SerializeField]
        private float entityUnityTileSize = 1f;
        [SerializeField]
        private float entityHeightScaleMultiplier = 1.2f;
        [SerializeField]
        private bool autoSpawnDebugProjectile = true;
        [SerializeField]
        private bool autoSpawnMinimap = true;
        [SerializeField]
        private bool autoConfigureMinimapScale = true;
        [SerializeField]
        private bool minimapAutoScaleFromEngine = true;
        [SerializeField]
        private float minimapUnityTileSize = 1f;

        private void Awake()
        {
            if (!autoStart)
            {
                return;
            }

            var configRuntime = GetComponent<GameConfigRuntime>();
            if (configRuntime == null)
            {
                configRuntime = gameObject.AddComponent<GameConfigRuntime>();
            }
            configRuntime.SetConfigFileName(configFileName);

            var runtimeHost = GetComponent<GameRuntimeHost>();
            if (runtimeHost == null)
            {
                runtimeHost = gameObject.AddComponent<GameRuntimeHost>();
            }
            runtimeHost.SetAutoStart(autoStartGame);
            runtimeHost.SetStartStage(startStage);

            if (autoSpawnVfxView)
            {
                var vfxView = GetComponent<ProjectileVfxView>();
                if (vfxView == null)
                {
                    vfxView = gameObject.AddComponent<ProjectileVfxView>();
                }

                if (autoConfigureVfxScale)
                {
                    vfxView.ConfigureScale(vfxUnityTileSize, vfxHeightScaleMultiplier, vfxAutoScaleFromEngine);
                }
            }

            if (autoSpawnEntityView)
            {
                var entityView = GetComponent<EntityPrimitiveView>();
                if (entityView == null)
                {
                    entityView = gameObject.AddComponent<EntityPrimitiveView>();
                }

                if (autoConfigureEntityScale)
                {
                    entityView.ConfigureScale(entityUnityTileSize, entityHeightScaleMultiplier, entityAutoScaleFromEngine);
                }
            }

            if (autoSpawnDebugProjectile)
            {
                var debugSpawner = GetComponent<DebugProjectileSpawner>();
                if (debugSpawner == null)
                {
                    gameObject.AddComponent<DebugProjectileSpawner>();
                }
            }

            if (autoSpawnMinimap)
            {
                var minimap = GetComponent<MinimapView>();
                if (minimap == null)
                {
                    minimap = gameObject.AddComponent<MinimapView>();
                }

                if (autoConfigureMinimapScale)
                {
                    minimap.ConfigureScale(minimapUnityTileSize, minimapAutoScaleFromEngine);
                }
            }
        }
    }
}
