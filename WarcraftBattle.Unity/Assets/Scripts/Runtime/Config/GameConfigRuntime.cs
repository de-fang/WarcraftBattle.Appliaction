using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using WarcraftBattle3D.Core.Config;

namespace WarcraftBattle3D
{
    public class GameConfigRuntime : MonoBehaviour
    {
        [SerializeField]
        private string configFileName = "GameConfig.xml";

        public GameConfigBundle Bundle { get; private set; }
        public bool IsLoaded => Bundle != null;
        public bool HasError => _hasError;
        public string ErrorMessage => _errorMessage;

        private bool _hasError;
        private string _errorMessage;

        private void Start()
        {
            StartCoroutine(LoadConfigCoroutine());
        }

        public void SetConfigFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            configFileName = fileName;
        }

        private IEnumerator LoadConfigCoroutine()
        {
            var path = Path.Combine(Application.streamingAssetsPath, configFileName);
            string xml = null;

#if UNITY_ANDROID && !UNITY_EDITOR
            xml = ReadStreamingAssetsAndroid(configFileName);
            if (string.IsNullOrEmpty(xml))
            {
                SetError($"Config not found: {configFileName}");
                yield break;
            }
            yield return null;
#else
            if (!File.Exists(path))
            {
                SetError($"Config not found: {path}");
                yield break;
            }

            xml = File.ReadAllText(path);
#endif

            try
            {
                var loader = new GameConfigLoader();
                Bundle = loader.LoadFromXml(xml);
                Debug.Log($"Config loaded: {Bundle.BaseUnitStats.Count} units, {Bundle.Stages.Count} stages.");
            }
            catch (Exception ex)
            {
                SetError($"Config parse failed: {ex.Message}");
            }
        }

        private void SetError(string message)
        {
            _hasError = true;
            _errorMessage = message;
            Debug.LogError(message);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private string ReadStreamingAssetsAndroid(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var assetManager = activity.Call<AndroidJavaObject>("getAssets"))
                using (var inputStream = assetManager.Call<AndroidJavaObject>("open", fileName))
                {
                    byte[] data = ReadAllBytes(inputStream);
                    return data != null ? Encoding.UTF8.GetString(data) : null;
                }
            }
            catch (Exception ex)
            {
                SetError($"Config load failed: {ex.Message}");
                return null;
            }
        }

        private static byte[] ReadAllBytes(AndroidJavaObject inputStream)
        {
            if (inputStream == null)
            {
                return null;
            }

            using (var outputStream = new AndroidJavaObject("java.io.ByteArrayOutputStream"))
            {
                var buffer = new byte[4096];
                int read;
                while ((read = inputStream.Call<int>("read", buffer)) > 0)
                {
                    outputStream.Call("write", buffer, 0, read);
                }

                return outputStream.Call<byte[]>("toByteArray");
            }
        }
#endif
    }
}
