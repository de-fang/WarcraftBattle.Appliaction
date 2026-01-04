using System;
using System.Collections.Generic;
using UnityEngine;
using WarcraftBattle3D.Core;
using UObject = UnityEngine.Object;

namespace WarcraftBattle3D
{
    public class EntityPrimitiveView : MonoBehaviour
    {
        [SerializeField]
        private GameRuntimeHost runtimeHost;
        [SerializeField]
        private Transform unitRoot;
        [SerializeField]
        private Transform buildingRoot;
        [SerializeField]
        private Transform obstacleRoot;
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
        private bool hideDead = true;
        [SerializeField]
        private Material humanMaterial;
        [SerializeField]
        private Material orcMaterial;
        [SerializeField]
        private Material neutralMaterial;

        public float WorldScale => worldScale;

        private bool _scaleInitialized;
        private readonly Dictionary<Entity, GameObject> _views = new Dictionary<Entity, GameObject>();
        private readonly HashSet<Entity> _entitySet = new HashSet<Entity>();
        private readonly List<Entity> _removal = new List<Entity>();

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

            if (unitRoot == null)
            {
                unitRoot = CreateChildRoot("Units");
            }

            if (buildingRoot == null)
            {
                buildingRoot = CreateChildRoot("Buildings");
            }

            if (obstacleRoot == null)
            {
                obstacleRoot = CreateChildRoot("Obstacles");
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

            SyncEntities(runtimeHost.Engine.Entities);
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

        private void ApplyScaleFromEngine(GameEngine engine)
        {
            float tileSize = Mathf.Max(1f, engine.TileSize);
            float targetTileSize = Mathf.Max(0.001f, unityTileSize);
            worldScale = targetTileSize / tileSize;
            heightScale = worldScale * Mathf.Max(0.001f, heightScaleMultiplier);
            _scaleInitialized = true;
        }

        private void SyncEntities(List<Entity> entities)
        {
            _entitySet.Clear();
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity == null)
                {
                    continue;
                }

                _entitySet.Add(entity);
                if (!_views.TryGetValue(entity, out var view))
                {
                    view = CreateEntityView(entity);
                    _views.Add(entity, view);
                }

                UpdateEntityView(entity, view);
            }

            _removal.Clear();
            foreach (var kvp in _views)
            {
                if (!_entitySet.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    _removal.Add(kvp.Key);
                }
            }

            for (int i = 0; i < _removal.Count; i++)
            {
                _views.Remove(_removal[i]);
            }
        }

        private GameObject CreateEntityView(Entity entity)
        {
            var kind = ResolveVisualKind(entity, out var parent);

            GameObject go;
            if (ModelFactory.Instance != null)
            {
                go = ModelFactory.Instance.CreateModel(entity, parent);
                go.name = BuildEntityName(entity, kind);
            }
            else
            {
                var primitive = ResolvePrimitiveType(kind);
                go = GameObject.CreatePrimitive(primitive);
                go.transform.SetParent(parent, false);
                go.name = BuildEntityName(entity, kind);
                RemoveCollider(go);
                ApplyTeamMaterial(entity, go);
            }

            return go;
        }

        private void UpdateEntityView(Entity entity, GameObject view)
        {
            if (hideDead)
            {
                view.SetActive(entity.IsAlive);
                if (!entity.IsAlive)
                {
                    return;
                }
            }

            float x = (float)entity.X * worldScale;
            float z = (float)entity.Y * worldScale;

            // Note: If using ModelFactory, scaling logic might differ,
            // but for now we apply uniform scaling to fit the logical size.
            // ModelFactory models are expected to be roughly 1x1x1 unit size unscaled (except height offset).

            float scaleX = Mathf.Max(0.05f, (float)entity.Width * worldScale);
            float scaleZ = Mathf.Max(0.05f, (float)entity.Height * worldScale);
            float scaleY = Mathf.Max(0.05f, (float)entity.Height * heightScale);

            // If using ModelFactory, we might not want to distort the model too much with non-uniform scaling if it's a complex mesh.
            // But for now let's keep it consistent.

            view.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            view.transform.position = new Vector3(x, scaleY * 0.5f, z); // Center Y is half height
            view.transform.rotation = Quaternion.Euler(0f, (float)entity.Rotation, 0f);
        }

        private void ApplyTeamMaterial(Entity entity, GameObject view)
        {
            var renderer = view.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var material = ResolveMaterial(entity.Team);
            if (material != null)
            {
                renderer.sharedMaterial = material;
                return;
            }

            renderer.material.color = ResolveColor(entity.Team);
        }

        private Material ResolveMaterial(TeamType team)
        {
            if (team == TeamType.Human)
            {
                return humanMaterial;
            }

            if (team == TeamType.Orc)
            {
                return orcMaterial;
            }

            return neutralMaterial;
        }

        private static Color ResolveColor(TeamType team)
        {
            if (team == TeamType.Human)
            {
                return new Color(0.25f, 0.6f, 1f);
            }

            if (team == TeamType.Orc)
            {
                return new Color(0.9f, 0.2f, 0.2f);
            }

            return new Color(0.6f, 0.6f, 0.6f);
        }

        private VisualKind ResolveVisualKind(Entity entity, out Transform parent)
        {
            if (entity is Unit)
            {
                parent = unitRoot;
                return VisualKind.Unit;
            }

            if (entity is Building)
            {
                parent = buildingRoot;
                return VisualKind.Building;
            }

            if (entity is Obstacle)
            {
                parent = obstacleRoot;
                return VisualKind.Obstacle;
            }

            parent = unitRoot;
            return VisualKind.Unknown;
        }

        private static PrimitiveType ResolvePrimitiveType(VisualKind kind)
        {
            switch (kind)
            {
                case VisualKind.Building:
                    return PrimitiveType.Cube;
                case VisualKind.Obstacle:
                    return PrimitiveType.Cylinder;
                case VisualKind.Unit:
                    return PrimitiveType.Capsule;
                default:
                    return PrimitiveType.Sphere;
            }
        }

        private static string BuildEntityName(Entity entity, VisualKind kind)
        {
            if (entity == null)
            {
                return "Entity";
            }

            return $"{kind}_{entity.Id}";
        }

        private static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private enum VisualKind
        {
            Unknown,
            Unit,
            Building,
            Obstacle
        }
    }
}
