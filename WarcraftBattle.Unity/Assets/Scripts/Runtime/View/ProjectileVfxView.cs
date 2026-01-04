using System;
using System.Collections.Generic;
using UnityEngine;
using WarcraftBattle3D.Core;
using UObject = UnityEngine.Object;

namespace WarcraftBattle3D
{
    public class ProjectileVfxView : MonoBehaviour
    {
        [Serializable]
        public class NamedPrefab
        {
            public string Key;
            public GameObject Prefab;
        }

        [SerializeField]
        private GameRuntimeHost runtimeHost;
        [SerializeField]
        private Transform projectileRoot;
        [SerializeField]
        private Transform effectRoot;
        [SerializeField]
        private float worldScale = 0.01f;
        [SerializeField]
        private float heightScale = 0.01f;
        [SerializeField]
        private bool autoScaleFromEngine = true;
        [SerializeField]
        private float unityTileSize = 1f;
        [SerializeField]
        private float heightScaleMultiplier = 1.2f;
        [SerializeField]
        private GameObject defaultProjectilePrefab;
        [SerializeField]
        private GameObject defaultEffectPrefab;
        [SerializeField]
        private List<NamedPrefab> projectilePrefabs = new List<NamedPrefab>();
        [SerializeField]
        private List<NamedPrefab> effectPrefabs = new List<NamedPrefab>();

        private readonly Dictionary<Projectile, GameObject> _projectileViews = new Dictionary<Projectile, GameObject>();
        private readonly Dictionary<VisualEffect, GameObject> _effectViews = new Dictionary<VisualEffect, GameObject>();
        private readonly Dictionary<VisualEffect, float> _effectInitialLife = new Dictionary<VisualEffect, float>();
        private readonly Dictionary<VisualEffect, Vector3> _effectBaseScale = new Dictionary<VisualEffect, Vector3>();
        private readonly HashSet<Projectile> _projectileSet = new HashSet<Projectile>();
        private readonly HashSet<VisualEffect> _effectSet = new HashSet<VisualEffect>();
        private readonly List<Projectile> _projectileRemoval = new List<Projectile>();
        private readonly List<VisualEffect> _effectRemoval = new List<VisualEffect>();
        private bool _scaleInitialized;
        private readonly Dictionary<string, GameObject> _projectilePrefabMap =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameObject> _effectPrefabMap =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            if (runtimeHost == null)
            {
                runtimeHost = GetComponent<GameRuntimeHost>();
            }

            if (runtimeHost == null)
            {
                runtimeHost = UObject.FindAnyObjectByType<GameRuntimeHost>();
            }

            BuildPrefabMap(projectilePrefabs, _projectilePrefabMap);
            BuildPrefabMap(effectPrefabs, _effectPrefabMap);

            if (projectileRoot == null)
            {
                projectileRoot = CreateChildRoot("Projectiles");
            }

            if (effectRoot == null)
            {
                effectRoot = CreateChildRoot("Effects");
            }
        }

        private void Update()
        {
            if (runtimeHost == null || runtimeHost.Engine == null)
            {
                return;
            }

            if (autoScaleFromEngine && !_scaleInitialized)
            {
                ApplyScaleFromEngine(runtimeHost.Engine);
            }

            SyncProjectiles(runtimeHost.Engine.Projectiles);
            SyncEffects(runtimeHost.Engine.VisualEffects);
        }

        public void ConfigureScale(float unityTileSizeValue, float heightScaleMultiplierValue, bool enableAutoScale)
        {
            if (unityTileSizeValue > 0f)
            {
                unityTileSize = unityTileSizeValue;
            }

            if (heightScaleMultiplierValue > 0f)
            {
                heightScaleMultiplier = heightScaleMultiplierValue;
            }

            autoScaleFromEngine = enableAutoScale;
            _scaleInitialized = false;
        }

        private Transform CreateChildRoot(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        private static void BuildPrefabMap(List<NamedPrefab> entries, Dictionary<string, GameObject> map)
        {
            map.Clear();
            if (entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Key) || entry.Prefab == null)
                {
                    continue;
                }

                map[entry.Key] = entry.Prefab;
            }
        }

        private void SyncProjectiles(List<Projectile> projectiles)
        {
            _projectileSet.Clear();
            for (int i = 0; i < projectiles.Count; i++)
            {
                var projectile = projectiles[i];
                if (projectile == null)
                {
                    continue;
                }

                _projectileSet.Add(projectile);
                if (!_projectileViews.TryGetValue(projectile, out var view))
                {
                    view = CreateProjectileView(projectile);
                    _projectileViews.Add(projectile, view);
                }

                UpdateProjectileView(projectile, view);
            }

            _projectileRemoval.Clear();
            foreach (var kvp in _projectileViews)
            {
                if (!_projectileSet.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    _projectileRemoval.Add(kvp.Key);
                }
            }

            for (int i = 0; i < _projectileRemoval.Count; i++)
            {
                _projectileViews.Remove(_projectileRemoval[i]);
            }
        }

        private void SyncEffects(List<VisualEffect> effects)
        {
            _effectSet.Clear();
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                _effectSet.Add(effect);
                if (!_effectViews.TryGetValue(effect, out var view))
                {
                    view = CreateEffectView(effect);
                    _effectViews.Add(effect, view);
                    _effectInitialLife[effect] = Mathf.Max(0.01f, (float)effect.Life);
                    _effectBaseScale[effect] = view.transform.localScale;
                }

                UpdateEffectView(effect, view);
            }

            _effectRemoval.Clear();
            foreach (var kvp in _effectViews)
            {
                if (!_effectSet.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    _effectRemoval.Add(kvp.Key);
                }
            }

            for (int i = 0; i < _effectRemoval.Count; i++)
            {
                var key = _effectRemoval[i];
                _effectViews.Remove(key);
                _effectInitialLife.Remove(key);
                _effectBaseScale.Remove(key);
            }
        }

        private void ApplyScaleFromEngine(GameEngine engine)
        {
            if (engine == null)
            {
                return;
            }

            float tileSize = Mathf.Max(1f, engine.TileSize);
            float targetTileSize = Mathf.Max(0.001f, unityTileSize);
            worldScale = targetTileSize / tileSize;
            heightScale = worldScale * Mathf.Max(0.001f, heightScaleMultiplier);
            _scaleInitialized = true;
        }

        private GameObject CreateProjectileView(Projectile projectile)
        {
            var prefab = ResolveProjectilePrefab(projectile);
            GameObject view = prefab != null
                ? Instantiate(prefab, projectileRoot)
                : CreateDefaultProjectile(projectileRoot);
            view.name = BuildProjectileName(projectile);
            return view;
        }

        private GameObject CreateEffectView(VisualEffect effect)
        {
            var prefab = ResolveEffectPrefab(effect);
            GameObject view = prefab != null
                ? Instantiate(prefab, effectRoot)
                : CreateDefaultEffect(effectRoot);
            view.name = BuildEffectName(effect);
            return view;
        }

        private GameObject ResolveProjectilePrefab(Projectile projectile)
        {
            if (projectile != null && !string.IsNullOrEmpty(projectile.VisualKey))
            {
                if (_projectilePrefabMap.TryGetValue(projectile.VisualKey, out var prefab))
                {
                    return prefab;
                }
            }

            return defaultProjectilePrefab;
        }

        private GameObject ResolveEffectPrefab(VisualEffect effect)
        {
            if (effect != null && !string.IsNullOrEmpty(effect.Key))
            {
                if (_effectPrefabMap.TryGetValue(effect.Key, out var prefab))
                {
                    return prefab;
                }
            }

            return defaultEffectPrefab;
        }

        private void UpdateProjectileView(Projectile projectile, GameObject view)
        {
            float x = (float)projectile.X * worldScale;
            float z = (float)projectile.Y * worldScale;
            float y = (float)projectile.Height * heightScale;
            view.transform.position = new Vector3(x, y, z);

            float dx = (float)(projectile.TX - projectile.X);
            float dz = (float)(projectile.TY - projectile.Y);
            var dir = new Vector3(dx, 0f, dz);
            if (dir.sqrMagnitude > 0.0001f)
            {
                view.transform.forward = dir.normalized;
            }
        }

        private void UpdateEffectView(VisualEffect effect, GameObject view)
        {
            float x = (float)effect.X * worldScale;
            float z = (float)effect.Y * worldScale;
            view.transform.position = new Vector3(x, 0f, z);

            if (_effectInitialLife.TryGetValue(effect, out float totalLife)
                && totalLife > 0f
                && _effectBaseScale.TryGetValue(effect, out var baseScale))
            {
                float t = Mathf.Clamp01((float)effect.Life / totalLife);
                float scale = Mathf.Lerp(0.2f, 1f, t);
                view.transform.localScale = baseScale * scale;
            }
        }

        private static string BuildProjectileName(Projectile projectile)
        {
            if (projectile == null)
            {
                return "Projectile";
            }

            if (!string.IsNullOrEmpty(projectile.VisualKey))
            {
                return $"Projectile_{projectile.VisualKey}";
            }

            return $"Projectile_{projectile.SourceType}";
        }

        private static string BuildEffectName(VisualEffect effect)
        {
            if (effect == null)
            {
                return "Effect";
            }

            return string.IsNullOrEmpty(effect.Key) ? "Effect" : $"Effect_{effect.Key}";
        }

        private static GameObject CreateDefaultProjectile(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.3f;
            RemoveCollider(go);
            return go;
        }

        private static GameObject CreateDefaultEffect(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.6f;
            RemoveCollider(go);
            return go;
        }

        private static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
}
