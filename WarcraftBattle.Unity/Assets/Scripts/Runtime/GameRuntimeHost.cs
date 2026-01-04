using System.Collections;
using UnityEngine;
using WarcraftBattle3D.Core;

namespace WarcraftBattle3D
{
    public class GameRuntimeHost : MonoBehaviour
    {
        [SerializeField]
        private int startStage = 1;
        [SerializeField]
        private bool autoStart = true;

        public GameEngine Engine { get; private set; }

        private GameConfigRuntime _configRuntime;

        private IEnumerator Start()
        {
            _configRuntime = GetComponent<GameConfigRuntime>();
            if (_configRuntime == null)
            {
                _configRuntime = gameObject.AddComponent<GameConfigRuntime>();
            }

            while (!_configRuntime.IsLoaded && !_configRuntime.HasError)
            {
                yield return null;
            }

            if (_configRuntime.HasError)
            {
                yield break;
            }

            Engine = new GameEngine(_configRuntime.Bundle);

            if (autoStart)
            {
                Engine.StartGame(startStage);
            }
        }

        private void Update()
        {
            if (Engine != null)
            {
                Engine.Update(Time.deltaTime);
            }
        }

        public void SetStartStage(int stageId)
        {
            if (stageId > 0)
            {
                startStage = stageId;
            }
        }

        public void SetAutoStart(bool enabled)
        {
            autoStart = enabled;
        }
    }
}
