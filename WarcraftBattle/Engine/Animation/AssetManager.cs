using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine.Animation
{
    public static class AssetManager
    {
        // ==========================================
        // WPF ��Դ���� (���� UI ��)
        // ==========================================
        private static Dictionary<string, BitmapSource> _textureCache = new Dictionary<string, BitmapSource>();
        private static Dictionary<string, AnimationClip> _animationCache = new Dictionary<string, AnimationClip>();
        public static Dictionary<string, ImageSource> StaticSprites = new Dictionary<string, ImageSource>();
        public static Brush BackgroundBrush { get; private set; } = Brushes.Black;



        // ���������Ա� Skia ��Ⱦʱʵʱ����֡����
        private static Dictionary<string, UnitStats> _unitStatsCache = new Dictionary<string, UnitStats>();
        private static Dictionary<string, BuildingInfo> _buildingInfoCache = new Dictionary<string, BuildingInfo>();
        private static Dictionary<string, SpriteSheetConfig> _effectConfigCache = new Dictionary<string, SpriteSheetConfig>();

        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static string _baseDirOverride;




        // ==========================================
        // WPF ���� API
        // ==========================================
        public static bool HasAnimation(string unitKey, string animName)
        {
            string cacheKey = $"{unitKey}_{animName}";
            return _animationCache.ContainsKey(cacheKey);
        }

        public static void EnsureImage(string key, string path)
        {
            if (StaticSprites.ContainsKey(key)) return;
            var img = LoadTexture(path);
            if (img != null)
            {
                StaticSprites[key] = img;
                var clip = new AnimationClip { Name = "Idle", Loop = true, Scale = 1.0 };
                var frame = new SpriteFrame
                {
                    Sheet = img,
                    SourceRect = new Int32Rect(0, 0, img.PixelWidth, img.PixelHeight)
                };
                clip.Frames.Add(frame);
                _animationCache[$"{key}_Idle"] = clip;

                // [Skia] ͬʱҲ����Ԥ���� Skia ����
                LoadTexture(path);
            }
        }

        public static Animator CreateAnimator(string unitKey)
        {
            var animator = new Animator();
            string prefix = unitKey + "_";
            bool found = false;
            foreach (var kvp in _animationCache)
            {
                if (kvp.Key.StartsWith(prefix))
                {
                    animator.AddClip(kvp.Value);
                    found = true;
                }
            }
            if (!found && StaticSprites.ContainsKey(unitKey))
            {
                var clip = new AnimationClip { Name = "Idle", Loop = true, Scale = 1.0 };
                if (StaticSprites[unitKey] is BitmapSource bmp)
                {
                    var frame = new SpriteFrame
                    {
                        Sheet = bmp,
                        SourceRect = new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)
                    };
                    clip.Frames.Add(frame);
                    animator.AddClip(clip);
                }
            }

            // [Optimization] Register animation groups if any
            if (_unitAnimGroups.TryGetValue(unitKey, out var groups))
            {
                foreach (var kvp in groups)
                {
                    animator.RegisterGroup(kvp.Key, kvp.Value);
                }
            }
            return animator;
        }

        public static Animator CreateBuildingAnimator(string buildingId) => CreateAnimator(buildingId);
        public static Animator CreateEffectAnimator(string effectKey) => CreateAnimator(effectKey);

        public static Animator CreateProjectileAnimator(string sourceKey)
        {
            string unitProjKey = $"{sourceKey}_Projectile";
            if (_animationCache.ContainsKey(unitProjKey))
            {
                var a = new Animator(); a.AddClip(_animationCache[unitProjKey]); return a;
            }
            if (_animationCache.ContainsKey(sourceKey)) // Fallback
            {
                var a = new Animator(); a.AddClip(_animationCache[sourceKey]); return a;
            }
            return null;
        }

        public static ImageSource GetIcon(string key)
        {
            var icon = LoadTexture($"{key}/icon.png");
            if (icon != null) return icon;
            string idleKey = $"{key}_Idle";
            if (_animationCache.TryGetValue(idleKey, out var clip) && clip.Frames.Count > 0)
            {
                var frame = clip.Frames[0];
                return new CroppedBitmap(frame.Sheet, frame.SourceRect);
            }
            if (StaticSprites.ContainsKey(key)) return StaticSprites[key];
            return null;
        }

        public static BitmapSource TryGetCachedTexture(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            return _textureCache.TryGetValue(relativePath, out var cached) ? cached : null;
        }

        public static ImageSource GetTerrainImage(int tileId)
        {
            string key = $"Terrain_{tileId}";
            return StaticSprites.ContainsKey(key) ? StaticSprites[key] : null;
        }

        public static UnitStats GetUnitStats(string unitKey)
        {
            return _unitStatsCache.ContainsKey(unitKey) ? _unitStatsCache[unitKey] : null;
        }

        // ==========================================
        // ��ʼ�������
        // ==========================================
        public static void Init(Dictionary<string, UnitStats> unitStats, Dictionary<string, EffectConfig> effects)
        {
            // [Skia] ������������
            _unitStatsCache = unitStats;
            if (effects != null)
            {
                foreach (var kvp in effects)
                {
                    var cfg = kvp.Value;
                    if (cfg == null || string.IsNullOrEmpty(cfg.Key)) continue;

                    // ת��Ϊͨ�� SpriteConfig ����
                    _effectConfigCache[cfg.Key] = new SpriteSheetConfig
                    {
                        Image = cfg.Image ?? $"Effects/{cfg.Key}.png",
                        FrameW = cfg.FrameW,
                        FrameH = cfg.FrameH,
                        Count = cfg.Count,
                        Scale = cfg.Scale
                    };
                }
            }

            // WPF ��Դ����
            foreach (var kvp in unitStats) LoadUnitAssets(kvp.Key, kvp.Value);
            if (effects != null) foreach (var kvp in effects) LoadEffectAssets(kvp.Value);
        }

        public static void LoadEnvironment(EnvironmentConfig config)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(config.BackgroundHex);
                BackgroundBrush = new SolidColorBrush(color);
                BackgroundBrush.Freeze();
            }
            catch { BackgroundBrush = Brushes.Black; }

            foreach (var tex in config.TerrainTextures)
            {
                // WPF
                var img = LoadTexture(tex.Path);
                if (img != null) StaticSprites[$"Terrain_{tex.Id}"] = img;

                // Skia
                LoadTexture(tex.Path);
            }
        }

        public static void LoadBuildingAssets(Dictionary<string, BuildingInfo> buildings)
        {
            // [Skia] ��������
            _buildingInfoCache = buildings;

            foreach (var kvp in buildings)
            {
                var info = kvp.Value;
                // [Skia] Ԥ������ͼ
                if (!string.IsNullOrEmpty(info.Image)) LoadTexture(info.Image);
                if (info.SpriteConfig != null && !string.IsNullOrEmpty(info.SpriteConfig.Image))
                    LoadTexture(info.SpriteConfig.Image);

                // WPF �����߼� (���ֲ���)
                if (info.SpriteConfig != null && !string.IsNullOrEmpty(info.SpriteConfig.Image))
                {
                    var clip = LoadAnimationClip(info.Id, "Idle", info.SpriteConfig);
                    if (clip != null && clip.Frames.Count > 0)
                    {
                        // For static sprites, we can still use a CroppedBitmap for simplicity in the UI if needed, but the game renderer will use the frame.
                        StaticSprites[info.Id] = new CroppedBitmap(clip.Frames[0].Sheet, clip.Frames[0].SourceRect);
                    }
                }
                else if (!string.IsNullOrEmpty(info.Image))
                {
                    var img = LoadTexture(info.Image);
                    if (img != null)
                    {
                        StaticSprites[info.Id] = img;
                        var clip = new AnimationClip { Name = "Idle", Loop = true, Scale = info.Scale };
                        var frame = new SpriteFrame
                        {
                            Sheet = img,
                            SourceRect = new Int32Rect(0, 0, img.PixelWidth, img.PixelHeight)
                        };
                        clip.Frames.Add(frame);
                        _animationCache[$"{info.Id}_Idle"] = clip;
                    }
                }
            }
        }

        public static void LoadObstacleImages(Dictionary<string, ObstacleDef> obstacles)
        {
            foreach (var kvp in obstacles)
            {
                var def = kvp.Value;
                // [Skia] Ԥ����
                if (!string.IsNullOrEmpty(def.Image)) LoadTexture(def.Image);

                // WPF Logic
                BitmapSource img = null;
                if (!string.IsNullOrEmpty(def.Image)) img = LoadTexture(def.Image);
                if (def.SpriteConfig != null && !string.IsNullOrEmpty(def.SpriteConfig.Image))
                {
                    var clip = LoadAnimationClip(def.Key, "Idle", def.SpriteConfig);
                    if (clip != null && clip.Frames.Count > 0)
                    {
                        img = new CroppedBitmap(clip.Frames[0].Sheet, clip.Frames[0].SourceRect);
                    }
                }
                if (img != null)
                {
                    StaticSprites[def.Key] = img;
                    var clip = new AnimationClip { Name = "Idle", Loop = true, Scale = def.SpriteConfig?.Scale ?? 1.0 };
                    var frame = new SpriteFrame
                    {
                        Sheet = img,
                        SourceRect = new Int32Rect(0, 0, img.PixelWidth, img.PixelHeight)
                    };
                    clip.Frames.Add(frame);
                    _animationCache[$"{def.Key}_Idle"] = clip;
                }
            }
        }



        // ==========================================
        // ���ļ����߼� (WPF)
        // ==========================================
        private static BitmapSource LoadTexture(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            if (_textureCache.ContainsKey(relativePath)) return _textureCache[relativePath];

            var baseDir = GetBaseDir();
            string fullPath = Path.Combine(baseDir, relativePath);
            if (!File.Exists(fullPath)) fullPath = Path.Combine(baseDir, "Assets", relativePath);
            if (!File.Exists(fullPath)) fullPath = Path.Combine(baseDir, "Assets", Path.GetFileName(relativePath));

            if (!File.Exists(fullPath)) return null;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                _textureCache[relativePath] = bmp;
                return bmp;
            }
            catch { return null; }
        }

        private static void LoadUnitAssets(string unitKey, UnitStats stats)
        {
            // [Skia] Ԥ����
            if (stats.SpriteConfig != null && !string.IsNullOrEmpty(stats.SpriteConfig.Image))
            {
                LoadTexture(stats.SpriteConfig.Image);
            }

            // WPF Load Logic (Unchanged)
            if (stats.SpriteConfig != null && !string.IsNullOrEmpty(stats.SpriteConfig.Image))
            {
                BitmapSource sheet = LoadTexture(stats.SpriteConfig.Image);
                if (sheet == null) return;

                // [Optimization] Group variants from SpriteConfig
                var stateGroups = new Dictionary<string, List<string>>();

                foreach (var stateDef in stats.SpriteConfig.States)
                {
                    string cacheKey = $"{unitKey}_{stateDef.Name}";
                    int fw = stats.SpriteConfig.FrameW; int fh = stats.SpriteConfig.FrameH;
                    if (fw <= 0 || fh <= 0) continue;
                    var clip = new AnimationClip { Name = stateDef.Name, FrameRate = stateDef.Speed, Loop = stateDef.Loop, Scale = stats.SpriteConfig.Scale };
                    for (int i = 0; i < stateDef.Count; i++)
                    {
                        int x = i * fw; int y = stateDef.Row * fh;
                        if (x + fw > sheet.PixelWidth || y + fh > sheet.PixelHeight) break;
                        clip.Frames.Add(new SpriteFrame
                        {
                            Sheet = sheet,
                            SourceRect = new Int32Rect(x, y, fw, fh)
                        });
                    }
                    _animationCache[cacheKey] = clip;

                    // Check for variants (e.g. "Attack_2")
                    // We assume the base name is the part before the last underscore if it ends with a number
                    // Or simpler: just check if the name matches "BaseName_N" pattern
                    // Actually, the request is to register "Attack" -> ["Attack", "Attack_2"]
                    // So we need to parse the stateDef.Name

                    string baseName = stateDef.Name;
                    int underscoreIndex = baseName.LastIndexOf('_');
                    if (underscoreIndex > 0 && int.TryParse(baseName.Substring(underscoreIndex + 1), out _))
                    {
                        baseName = baseName.Substring(0, underscoreIndex);
                    }

                    if (!stateGroups.ContainsKey(baseName)) stateGroups[baseName] = new List<string>();
                    stateGroups[baseName].Add(stateDef.Name);
                }

                // Register groups to a temporary animator just to store the metadata? 
                // No, Animator is created per unit instance. We need to store this group info somewhere static 
                // or register it when creating the Animator.
                // Since Animator.RegisterGroup is an instance method, we can't call it here directly on a static object.
                // However, CreateAnimator uses _animationCache. We need a way to persist group info.
                // Let's add a static dictionary for animation groups in AssetManager.
                // Wait, the prompt says "Modify LoadUnitAssets logic... call animator.RegisterGroup".
                // But LoadUnitAssets is static and runs once at startup. Animator is created later.
                // We should store the group definitions in AssetManager and apply them in CreateAnimator.

                // Let's add a static cache for groups:
                if (!_unitAnimGroups.ContainsKey(unitKey)) _unitAnimGroups[unitKey] = new Dictionary<string, List<string>>();
                foreach (var kvp in stateGroups)
                {
                    if (kvp.Value.Count > 1)
                    {
                        _unitAnimGroups[unitKey][kvp.Key] = kvp.Value;
                    }
                }

                if (stats.ProjectileConfig != null) LoadProjectileAnim(unitKey, stats.ProjectileConfig);
            }
            else
            {
                if (!_unitAnimGroups.ContainsKey(unitKey)) _unitAnimGroups[unitKey] = new Dictionary<string, List<string>>();

                foreach (UnitState state in Enum.GetValues(typeof(UnitState)))
                {
                    string stateName = state.ToString();
                    var variants = new List<string>();

                    // 1. Load base state (e.g. "Attack")
                    if (TryLoadAndCacheClip(unitKey, stateName, state)) variants.Add(stateName);

                    // 2. Try load variants (e.g. "Attack_2", "Attack_3"...)
                    int index = 2;
                    while (true)
                    {
                        string variantName = $"{stateName}_{index}";
                        if (TryLoadAndCacheClip(unitKey, variantName, state))
                        {
                            variants.Add(variantName);
                            index++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (variants.Count > 1)
                    {
                        _unitAnimGroups[unitKey][stateName] = variants;
                    }
                }
            }
        }

        // Helper to load clip from disk/folder and cache it
        private static bool TryLoadAndCacheClip(string unitKey, string animName, UnitState state)
        {
            var clip = LoadStripFromDisk(unitKey, animName) ?? LoadSequenceFromFolder(unitKey, animName);
            if (clip != null)
            {
                // Loop logic: Attack and Die usually don't loop
                // For variants like "Attack_2", we should apply the same rule as "Attack"
                clip.Loop = (state != UnitState.Attack && state != UnitState.Die);
                _animationCache[$"{unitKey}_{animName}"] = clip;
                return true;
            }
            return false;
        }

        // Store animation groups metadata: UnitKey -> { StateName -> [Variant1, Variant2...] }
        private static Dictionary<string, Dictionary<string, List<string>>> _unitAnimGroups = new Dictionary<string, Dictionary<string, List<string>>>();

        private static void LoadEffectAssets(EffectConfig cfg)
        {
            if (string.IsNullOrEmpty(cfg.Key)) return;
            // [Skia] Preload
            if (cfg.Image != null) LoadTexture(cfg.Image);

            var clip = LoadAnimationClip(cfg.Key, cfg.Key, new SpriteSheetConfig
            {
                Image = cfg.Image ?? $"Effects/{cfg.Key}.png",
                FrameW = cfg.FrameW,
                FrameH = cfg.FrameH,
                Count = cfg.Count,
                Scale = cfg.Scale
            });
            if (clip != null)
            {
                clip.FrameRate = cfg.Speed; clip.Loop = (cfg.Key == "DivineShield");
                _animationCache[cfg.Key] = clip;
            }
        }

        // ==========================================
        // ��������
        // ==========================================
        private static AnimationClip LoadAnimationClip(string ownerKey, string animName, SpriteSheetConfig config)
        {
            var sheet = LoadTexture(config.Image);
            if (sheet == null) return null;
            var clip = new AnimationClip { Name = animName, Scale = config.Scale };
            int fw = config.FrameW > 0 ? config.FrameW : sheet.PixelHeight;
            int fh = config.FrameH > 0 ? config.FrameH : sheet.PixelHeight;
            int count = config.Count > 0 ? config.Count : (sheet.PixelWidth / fw);
            for (int i = 0; i < count; i++)
            {
                clip.Frames.Add(new SpriteFrame
                {
                    Sheet = sheet,
                    SourceRect = new Int32Rect(i * fw, 0, fw, fh)
                });
            }
            return clip;
        }

        private static AnimationClip LoadStripFromDisk(string folder, string filenameWithoutExt)
        {
            var sheet = LoadTexture($"{folder}/{filenameWithoutExt}.png");
            if (sheet == null) return null;
            int h = sheet.PixelHeight; int w = h; int count = sheet.PixelWidth / w;
            var clip = new AnimationClip { Name = filenameWithoutExt, FrameRate = 10, Scale = 1.0 };
            for (int i = 0; i < count; i++)
            {
                clip.Frames.Add(new SpriteFrame
                {
                    Sheet = sheet,
                    SourceRect = new Int32Rect(i * w, 0, w, h)
                });
            }
            return clip;
        }

        private static AnimationClip LoadSequenceFromFolder(string parentFolder, string subFolder)
        {
            string dirPath = Path.Combine(GetBaseDir(), "Assets", parentFolder, subFolder);
            if (!Directory.Exists(dirPath)) return null;
            var files = Directory.GetFiles(dirPath, "*.png").OrderBy(f => f).ToArray();
            if (files.Length == 0) return null;
            var clip = new AnimationClip { Name = subFolder, FrameRate = 15, Scale = 1.0 };
            foreach (var f in files)
            {
                var img = LoadTexture($"{parentFolder}/{subFolder}/{Path.GetFileName(f)}");
                if (img != null) clip.Frames.Add(new SpriteFrame
                {
                    Sheet = img,
                    SourceRect = new Int32Rect(0, 0, img.PixelWidth, img.PixelHeight)
                });
            }
            return clip;
        }

        private static void LoadProjectileAnim(string unitKey, ProjectileSpriteConfig config)
        {
            // [Skia] Preload
            if (config.Image != null) LoadTexture(config.Image);

            var clip = LoadAnimationClip(unitKey, "Projectile", new SpriteSheetConfig { Image = config.Image, FrameW = config.FrameW, FrameH = config.FrameH, Count = config.Count, Scale = config.Scale });
            if (clip != null) { clip.Loop = true; clip.FrameRate = config.Speed; _animationCache[$"{unitKey}_Projectile"] = clip; }
        }

        public static void SetBaseDirectory(string baseDir)
        {
            if (!string.IsNullOrWhiteSpace(baseDir) && Directory.Exists(baseDir))
            {
                _baseDirOverride = baseDir;
            }
        }

        private static string GetBaseDir()
        {
            return string.IsNullOrWhiteSpace(_baseDirOverride) ? BaseDir : _baseDirOverride;
        }
    }
}
