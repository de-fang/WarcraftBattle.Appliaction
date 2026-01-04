using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WarcraftBattle3D.Core;
using UObject = UnityEngine.Object;

namespace WarcraftBattle3D
{
    public class MinimapView : MonoBehaviour
    {
        [SerializeField]
        private GameRuntimeHost runtimeHost;
        [SerializeField]
        private int textureSize = 256;
        [SerializeField]
        private float cameraHeight = 40f;
        [SerializeField]
        private bool autoHeight = true;
        [SerializeField]
        private float heightScale = 0.9f;
        [SerializeField]
        private float padding = 1f;
        [SerializeField]
        private Vector2 uiSize = new Vector2(220f, 220f);
        [SerializeField]
        private Vector2 uiOffset = new Vector2(-12f, 12f);
        [SerializeField]
        private bool anchorTopRight = true;
        [SerializeField]
        private bool autoScaleFromEngine = true;
        [SerializeField]
        private float unityTileSize = 1f;
        [SerializeField]
        private bool showIcons = true;
        [SerializeField]
        private bool hideDeadIcons = true;
        [SerializeField]
        private bool invertY = false;
        [SerializeField]
        private float unitIconSize = 6f;
        [SerializeField]
        private float buildingIconSize = 10f;
        [SerializeField]
        private float obstacleIconSize = 5f;
        [SerializeField]
        private bool showViewRect = true;
        [SerializeField]
        private Color viewRectColor = new Color(0f, 1f, 0.3f, 0.2f);
        [SerializeField]
        private Color viewRectOutline = new Color(0f, 1f, 0.3f, 0.8f);
        [SerializeField]
        private float viewRectOutlineSize = 1f;
        [SerializeField]
        private bool enableClickMove = true;
        [SerializeField]
        private bool smoothCameraMove = true;
        [SerializeField]
        private float cameraMoveSpeed = 6f;
        [SerializeField]
        private float clickMoveHeightOverride = 0f;
        [SerializeField]
        private bool tintIconsByTeam = true;
        [SerializeField]
        private Sprite defaultUnitSprite;
        [SerializeField]
        private Sprite defaultBuildingSprite;
        [SerializeField]
        private Sprite defaultObstacleSprite;
        [SerializeField]
        private Sprite defaultUnknownSprite;
        [SerializeField]
        private List<IconSpriteMapping> iconOverrides = new List<IconSpriteMapping>();

        private Camera _camera;
        private RenderTexture _texture;
        private RawImage _rawImage;
        private RectTransform _rect;
        private RectTransform _overlayRoot;
        private RectTransform _iconRoot;
        private RectTransform _viewRoot;
        private MinimapViewportQuad _viewQuad;
        private MinimapViewportOutline _viewOutline;
        private bool _scaleInitialized;
        private double _lastWidth;
        private double _lastDepth;
        private float _worldScale = 0.01f;
        private Sprite _iconSprite;
        private readonly Dictionary<Entity, Image> _icons = new Dictionary<Entity, Image>();
        private readonly HashSet<Entity> _entitySet = new HashSet<Entity>();
        private readonly List<Entity> _iconRemoval = new List<Entity>();
        private readonly Dictionary<string, IconSpriteMapping> _iconOverrideMap =
            new Dictionary<string, IconSpriteMapping>(StringComparer.OrdinalIgnoreCase);
        private Vector3? _cameraTarget;

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

            EnsureCamera();
            EnsureUI();
            EnsureRenderTexture();
            BuildIconOverrideMap();
        }

        private void Update()
        {
            var engine = runtimeHost?.Engine;
            if (engine == null)
            {
                return;
            }

            if (autoScaleFromEngine && !_scaleInitialized)
            {
                ApplyScaleFromEngine(engine);
            }

            if (_lastWidth != engine.WorldWidth || _lastDepth != engine.MapDepth)
            {
                ApplyMapSize(engine);
            }

            if (showIcons)
            {
                UpdateIcons(engine);
            }
            else if (_iconRoot != null)
            {
                _iconRoot.gameObject.SetActive(false);
            }

            if (showViewRect)
            {
                UpdateViewRect(engine);
            }
            else if (_viewRoot != null)
            {
                _viewRoot.gameObject.SetActive(false);
            }

            if (enableClickMove)
            {
                HandleClickMove(engine);
            }

            UpdateCameraMove();
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }

        public void ConfigureScale(float unityTileSizeValue, bool enableAutoScale)
        {
            if (unityTileSizeValue > 0f)
            {
                unityTileSize = unityTileSizeValue;
            }

            autoScaleFromEngine = enableAutoScale;
            _scaleInitialized = false;
        }

        private void OnValidate()
        {
            BuildIconOverrideMap();
        }

        private void EnsureCamera()
        {
            if (_camera != null)
            {
                return;
            }

            var camGo = new GameObject("MinimapCamera");
            camGo.transform.SetParent(transform, false);
            _camera = camGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 1000f;
            _camera.cullingMask = ~0;
            _camera.depth = 20;
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void EnsureUI()
        {
            if (_rawImage != null)
            {
                return;
            }

            var canvasGo = new GameObject("MinimapCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("Minimap");
            imageGo.transform.SetParent(canvasGo.transform, false);
            _rawImage = imageGo.AddComponent<RawImage>();
            _rawImage.color = Color.white;

            _rect = imageGo.GetComponent<RectTransform>();
            _rect.sizeDelta = uiSize;
            var anchor = anchorTopRight ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
            _rect.anchorMin = anchor;
            _rect.anchorMax = anchor;
            _rect.pivot = anchor;
            _rect.anchoredPosition = uiOffset;

            EnsureOverlay();
        }

        private void EnsureRenderTexture()
        {
            if (_texture == null || _texture.width != textureSize || _texture.height != textureSize)
            {
                if (_texture != null)
                {
                    _texture.Release();
                    Destroy(_texture);
                }

                _texture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
                _texture.filterMode = FilterMode.Point;
                _texture.Create();
            }

            if (_camera != null)
            {
                _camera.targetTexture = _texture;
            }

            if (_rawImage != null)
            {
                _rawImage.texture = _texture;
            }
        }

        private void EnsureOverlay()
        {
            if (_overlayRoot != null || _rawImage == null)
            {
                return;
            }

            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(_rawImage.transform, false);
            _overlayRoot = overlayGo.AddComponent<RectTransform>();
            _overlayRoot.anchorMin = Vector2.zero;
            _overlayRoot.anchorMax = Vector2.one;
            _overlayRoot.pivot = new Vector2(0.5f, 0.5f);
            _overlayRoot.offsetMin = Vector2.zero;
            _overlayRoot.offsetMax = Vector2.zero;

            var iconGo = new GameObject("Icons");
            iconGo.transform.SetParent(_overlayRoot, false);
            _iconRoot = iconGo.AddComponent<RectTransform>();
            _iconRoot.anchorMin = Vector2.zero;
            _iconRoot.anchorMax = Vector2.one;
            _iconRoot.pivot = new Vector2(0.5f, 0.5f);
            _iconRoot.offsetMin = Vector2.zero;
            _iconRoot.offsetMax = Vector2.zero;

            var viewRoot = new GameObject("ViewRect");
            viewRoot.transform.SetParent(_overlayRoot, false);
            _viewRoot = viewRoot.AddComponent<RectTransform>();
            _viewRoot.anchorMin = Vector2.zero;
            _viewRoot.anchorMax = Vector2.one;
            _viewRoot.pivot = new Vector2(0.5f, 0.5f);
            _viewRoot.offsetMin = Vector2.zero;
            _viewRoot.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_viewRoot, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            _viewQuad = fillGo.AddComponent<MinimapViewportQuad>();
            _viewQuad.color = viewRectColor;
            _viewQuad.raycastTarget = false;

            var outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(_viewRoot, false);
            var outlineRect = outlineGo.AddComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.pivot = new Vector2(0.5f, 0.5f);
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            _viewOutline = outlineGo.AddComponent<MinimapViewportOutline>();
            _viewOutline.color = viewRectOutline;
            _viewOutline.Thickness = viewRectOutlineSize;
            _viewOutline.raycastTarget = false;

            _iconSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            if (_iconSprite == null)
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                _iconSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
        }

        private void ApplyScaleFromEngine(GameEngine engine)
        {
            float tileSize = Mathf.Max(1f, engine.TileSize);
            float targetTileSize = Mathf.Max(0.001f, unityTileSize);
            _worldScale = targetTileSize / tileSize;
            _scaleInitialized = true;
            ApplyMapSize(engine);
        }

        private void ApplyMapSize(GameEngine engine)
        {
            _lastWidth = engine.WorldWidth;
            _lastDepth = engine.MapDepth;

            float mapWidth = (float)engine.WorldWidth * _worldScale;
            float mapDepth = (float)engine.MapDepth * _worldScale;
            float halfSize = Mathf.Max(mapWidth, mapDepth) * 0.5f + padding;

            if (_camera == null)
            {
                return;
            }

            if (autoHeight)
            {
                cameraHeight = Mathf.Max(5f, halfSize * heightScale);
            }

            _camera.orthographicSize = halfSize;
            _camera.transform.position = new Vector3(mapWidth * 0.5f, cameraHeight, mapDepth * 0.5f);
        }

        private void BuildIconOverrideMap()
        {
            _iconOverrideMap.Clear();
            if (iconOverrides == null)
            {
                return;
            }

            foreach (var entry in iconOverrides)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Key) || entry.Sprite == null)
                {
                    continue;
                }

                _iconOverrideMap[entry.Key] = entry;
            }
        }

        private void UpdateIcons(GameEngine engine)
        {
            if (_iconRoot == null)
            {
                return;
            }

            if (!_iconRoot.gameObject.activeSelf)
            {
                _iconRoot.gameObject.SetActive(true);
            }

            _entitySet.Clear();
            var entities = engine.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity == null)
                {
                    continue;
                }

                _entitySet.Add(entity);
                if (!_icons.TryGetValue(entity, out var icon))
                {
                    icon = CreateIcon(entity);
                    _icons.Add(entity, icon);
                }

                UpdateIcon(entity, icon, engine);
            }

            _iconRemoval.Clear();
            foreach (var kvp in _icons)
            {
                if (!_entitySet.Contains(kvp.Key))
                {
                    Destroy(kvp.Value.gameObject);
                    _iconRemoval.Add(kvp.Key);
                }
            }

            for (int i = 0; i < _iconRemoval.Count; i++)
            {
                _icons.Remove(_iconRemoval[i]);
            }
        }

        private Image CreateIcon(Entity entity)
        {
            var go = new GameObject($"Icon_{entity.Id}");
            go.transform.SetParent(_iconRoot, false);
            var image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.sprite = ResolveIconSprite(entity, out _);
            return image;
        }

        private void UpdateIcon(Entity entity, Image icon, GameEngine engine)
        {
            if (hideDeadIcons)
            {
                icon.gameObject.SetActive(entity.IsAlive);
                if (!entity.IsAlive)
                {
                    return;
                }
            }

            var rect = icon.rectTransform;
            rect.anchoredPosition = WorldToLocal(entity.X, entity.Y, engine);

            var sprite = ResolveIconSprite(entity, out float sizeOverride);
            if (icon.sprite != sprite)
            {
                icon.sprite = sprite;
            }

            float size = sizeOverride > 0 ? sizeOverride : ResolveIconSize(entity);
            rect.sizeDelta = new Vector2(size, size);

            icon.color = tintIconsByTeam ? ResolveIconColor(entity.Team) : Color.white;
        }

        private Sprite ResolveIconSprite(Entity entity, out float sizeOverride)
        {
            sizeOverride = 0f;
            string key = GetIconKey(entity);
            if (!string.IsNullOrEmpty(key) && _iconOverrideMap.TryGetValue(key, out var overrideEntry))
            {
                if (overrideEntry.Sprite != null)
                {
                    sizeOverride = overrideEntry.Size;
                    return overrideEntry.Sprite;
                }
            }

            if (entity is Building)
            {
                return defaultBuildingSprite ?? defaultUnitSprite ?? _iconSprite;
            }

            if (entity is Obstacle)
            {
                return defaultObstacleSprite ?? defaultUnitSprite ?? _iconSprite;
            }

            if (entity is Unit)
            {
                return defaultUnitSprite ?? _iconSprite;
            }

            return defaultUnknownSprite ?? _iconSprite;
        }

        private static string GetIconKey(Entity entity)
        {
            if (entity is Unit unit)
            {
                return unit.Key;
            }

            if (entity is Building building)
            {
                if (!string.IsNullOrEmpty(building.Info?.Id))
                {
                    return building.Info.Id;
                }

                return building.Info?.Name;
            }

            if (entity is Obstacle obstacle)
            {
                return obstacle.Type;
            }

            return null;
        }

        private float ResolveIconSize(Entity entity)
        {
            if (entity is Building)
            {
                return buildingIconSize;
            }

            if (entity is Obstacle)
            {
                return obstacleIconSize;
            }

            return unitIconSize;
        }

        private static Color ResolveIconColor(TeamType team)
        {
            if (team == TeamType.Human)
            {
                return new Color(0.25f, 0.6f, 1f, 1f);
            }

            if (team == TeamType.Orc)
            {
                return new Color(0.9f, 0.2f, 0.2f, 1f);
            }

            return new Color(0.6f, 0.6f, 0.6f, 1f);
        }

        private void UpdateViewRect(GameEngine engine)
        {
            if (_viewRoot == null || _viewQuad == null || _viewOutline == null)
            {
                return;
            }

            var cam = GetMainCamera();
            if (cam == null)
            {
                _viewRoot.gameObject.SetActive(false);
                return;
            }

            if (!TryGetViewportGroundPoints(cam, engine, out var quad))
            {
                _viewRoot.gameObject.SetActive(false);
                return;
            }

            _viewRoot.gameObject.SetActive(true);
            _viewQuad.color = viewRectColor;
            _viewOutline.color = viewRectOutline;
            _viewOutline.Thickness = viewRectOutlineSize;

            var a = WorldToLocal(quad[0].x, quad[0].y, engine);
            var b = WorldToLocal(quad[1].x, quad[1].y, engine);
            var c = WorldToLocal(quad[2].x, quad[2].y, engine);
            var d = WorldToLocal(quad[3].x, quad[3].y, engine);
            _viewQuad.SetPoints(a, b, c, d);
            _viewOutline.SetPoints(a, b, c, d);
        }

        private void HandleClickMove(GameEngine engine)
        {
            if (_rect == null)
            {
                return;
            }

            Vector2 screenPos;
            bool clicked = false;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPos = touch.position;
                    clicked = true;
                }
                else
                {
                    return;
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                clicked = true;
            }
            else
            {
                return;
            }

            if (!clicked)
            {
                return;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPos, null))
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPos, null, out var local))
            {
                return;
            }

            var rect = _rect.rect;
            if (rect.width <= 0 || rect.height <= 0)
            {
                return;
            }

            float x01 = (local.x / rect.width) + 0.5f;
            float y01 = (local.y / rect.height) + 0.5f;
            x01 = Mathf.Clamp01(x01);
            y01 = Mathf.Clamp01(y01);
            if (invertY)
            {
                y01 = 1f - y01;
            }

            double worldX = x01 * engine.WorldWidth;
            double worldY = y01 * engine.MapDepth;

            float unityX = (float)worldX * _worldScale;
            float unityZ = (float)worldY * _worldScale;
            float mapWidth = (float)engine.WorldWidth * _worldScale;
            float mapDepth = (float)engine.MapDepth * _worldScale;
            unityX = Mathf.Clamp(unityX, 0f, mapWidth);
            unityZ = Mathf.Clamp(unityZ, 0f, mapDepth);

            var cam = GetMainCamera();
            if (cam == null)
            {
                return;
            }

            float targetY = clickMoveHeightOverride > 0f ? clickMoveHeightOverride : cam.transform.position.y;
            var target = new Vector3(unityX, targetY, unityZ);

            if (smoothCameraMove)
            {
                _cameraTarget = target;
            }
            else
            {
                cam.transform.position = target;
            }
        }

        private void UpdateCameraMove()
        {
            if (!_cameraTarget.HasValue)
            {
                return;
            }

            var cam = GetMainCamera();
            if (cam == null)
            {
                _cameraTarget = null;
                return;
            }

            var target = _cameraTarget.Value;
            cam.transform.position = Vector3.Lerp(
                cam.transform.position,
                target,
                Mathf.Clamp01(Time.deltaTime * cameraMoveSpeed));

            if ((cam.transform.position - target).sqrMagnitude < 0.01f)
            {
                cam.transform.position = target;
                _cameraTarget = null;
            }
        }

        private Camera GetMainCamera()
        {
            var cam = Camera.main;
            if (cam != null && cam != _camera)
            {
                return cam;
            }

            var any = UObject.FindAnyObjectByType<Camera>();
            if (any != null && any != _camera)
            {
                return any;
            }

            return null;
        }

        private bool TryGetViewportGroundPoints(Camera cam, GameEngine engine, out Vector2[] points)
        {
            points = new Vector2[4];
            var ground = new Plane(Vector3.up, Vector3.zero);

            return TryGetViewportPoint(cam, engine, ground, new Vector2(0f, 0f), out points[0])
                && TryGetViewportPoint(cam, engine, ground, new Vector2(1f, 0f), out points[1])
                && TryGetViewportPoint(cam, engine, ground, new Vector2(1f, 1f), out points[2])
                && TryGetViewportPoint(cam, engine, ground, new Vector2(0f, 1f), out points[3]);
        }

        private bool TryGetViewportPoint(
            Camera cam,
            GameEngine engine,
            Plane ground,
            Vector2 viewport,
            out Vector2 point)
        {
            point = Vector2.zero;
            var ray = cam.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
            if (!ground.Raycast(ray, out float enter))
            {
                return false;
            }

            var point3d = ray.GetPoint(enter);
            if (Mathf.Abs(_worldScale) < 0.0001f)
            {
                return false;
            }

            float worldX = point3d.x / _worldScale;
            float worldY = point3d.z / _worldScale;

            worldX = Mathf.Clamp(worldX, 0f, (float)engine.WorldWidth);
            worldY = Mathf.Clamp(worldY, 0f, (float)engine.MapDepth);

            point = new Vector2(worldX, worldY);
            return true;
        }

        private Vector2 WorldToLocal(double worldX, double worldY, GameEngine engine)
        {
            if (_rect == null)
            {
                return Vector2.zero;
            }

            float width = (float)engine.WorldWidth;
            float depth = (float)engine.MapDepth;
            float x01 = width > 0 ? (float)(worldX / width) : 0f;
            float y01 = depth > 0 ? (float)(worldY / depth) : 0f;
            x01 = Mathf.Clamp01(x01);
            y01 = Mathf.Clamp01(y01);
            if (invertY)
            {
                y01 = 1f - y01;
            }

            var rect = _rect.rect;
            float x = (x01 - 0.5f) * rect.width;
            float y = (y01 - 0.5f) * rect.height;
            return new Vector2(x, y);
        }

        [Serializable]
        public class IconSpriteMapping
        {
            public string Key;
            public Sprite Sprite;
            public float Size;
        }
    }
}
