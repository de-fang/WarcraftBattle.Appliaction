using UnityEngine;

namespace WarcraftBattle3D
{
    public static class EntryPoint
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            EnsureCamera();
            EnsureLight();

            var existing = Object.FindAnyObjectByType<GameBootstrap>();
            if (existing != null)
            {
                Object.DontDestroyOnLoad(existing.gameObject);
                EnsureComponents(existing.gameObject);
                return;
            }

            var go = new GameObject("GameBootstrap");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameBootstrap>();
            EnsureComponents(go);
        }

        private static void EnsureComponents(GameObject go)
        {
            if (go.GetComponent<ModelFactory>() == null)
            {
                go.AddComponent<ModelFactory>();
            }

            if (go.GetComponent<PlayerController>() == null)
            {
                go.AddComponent<PlayerController>();
            }
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                var cam = Camera.main;
                if (cam.GetComponent<RTSCameraController>() == null)
                {
                    cam.gameObject.AddComponent<RTSCameraController>();
                }
                return;
            }

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.transform.position = new Vector3(0f, 12f, -12f);
            camGo.transform.rotation = Quaternion.Euler(35f, 0f, 0f);

            camGo.AddComponent<RTSCameraController>();
        }

        private static void EnsureLight()
        {
            if (Object.FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
