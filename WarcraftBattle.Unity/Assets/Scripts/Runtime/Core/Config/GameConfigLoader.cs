using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace WarcraftBattle3D.Core.Config
{
    public class GameConfigBundle
    {
        public GlobalSettingsConfig GlobalSettings { get; set; } = new GlobalSettingsConfig();
        public AIProfile AIProfile { get; } = new AIProfile();
        public EnvironmentConfig Environment { get; } = new EnvironmentConfig();
        public Dictionary<string, ObstacleDef> ObstacleInfo { get; } = new Dictionary<string, ObstacleDef>();
        public Dictionary<string, EffectConfig> EffectConfigs { get; } = new Dictionary<string, EffectConfig>();
        public Dictionary<string, UnitStats> BaseUnitStats { get; } = new Dictionary<string, UnitStats>();
        public Dictionary<string, List<UpgradeLevelDef>> UnitUpgradeDefs { get; } =
            new Dictionary<string, List<UpgradeLevelDef>>();
        public Dictionary<string, BuildingInfo> BuildingRegistry { get; } = new Dictionary<string, BuildingInfo>();
        public List<BuildingInfo> BuildingBlueprints { get; } = new List<BuildingInfo>();
        public List<UpgradeInfo> PermanentUpgrades { get; } = new List<UpgradeInfo>();
        public Dictionary<int, StageInfo> Stages { get; } = new Dictionary<int, StageInfo>();
    }

    public class GameConfigLoader
    {
        public GameConfigBundle LoadFromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new ArgumentException("Xml content is empty.", nameof(xml));
            }

            var serializer = new XmlSerializer(typeof(GameConfigData));
            using (var reader = new StringReader(xml))
            {
                var config = (GameConfigData)serializer.Deserialize(reader);
                return LoadFromConfigs(new List<GameConfigData> { config });
            }
        }

        public GameConfigBundle LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Config file not found.", path);
            }

            var serializer = new XmlSerializer(typeof(GameConfigData));
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var config = (GameConfigData)serializer.Deserialize(stream);
                return LoadFromConfigs(new List<GameConfigData> { config });
            }
        }

        public GameConfigBundle LoadFromConfigs(IEnumerable<GameConfigData> configs)
        {
            if (configs == null)
            {
                throw new ArgumentNullException(nameof(configs));
            }

            var configList = configs.Where(c => c != null).ToList();
            if (configList.Count == 0)
            {
                throw new ArgumentException("No configs provided.", nameof(configs));
            }

            var bundle = new GameConfigBundle();

            var globalsSource = configList.FirstOrDefault(c => c.GlobalSettings != null) ?? configList[0];
            if (globalsSource.GlobalSettings != null)
            {
                bundle.GlobalSettings = globalsSource.GlobalSettings;
            }

            var aiSource = configList.LastOrDefault(c => c.AIProfile != null);
            if (aiSource?.AIProfile != null)
            {
                bundle.AIProfile.Aggressiveness = aiSource.AIProfile.Aggressiveness;
                bundle.AIProfile.WaveInterval = aiSource.AIProfile.WaveInterval;
                bundle.AIProfile.BuildOrder = new List<string>(aiSource.AIProfile.BuildOrder);
            }

            var envSource = configList.LastOrDefault(c => c.Environment != null);
            if (envSource?.Environment != null)
            {
                bundle.Environment.BackgroundHex = envSource.Environment.BackgroundHex;
                bundle.Environment.TerrainTextures.Clear();
                foreach (var t in envSource.Environment.TerrainTextures)
                {
                    bundle.Environment.TerrainTextures.Add(new TerrainDef { Id = t.Id, Path = t.Path, Weight = t.Weight });
                }
            }

            bundle.ObstacleInfo.Clear();
            foreach (var cfg in configList)
            {
                foreach (var obs in cfg.ObstacleDefinitions)
                {
                    var def = new ObstacleDef
                    {
                        Key = obs.Key,
                        Name = obs.Name,
                        Image = obs.Image,
                        Folder = obs.Folder,
                        Width = obs.Width,
                        Height = obs.Height,
                        OffsetY = obs.OffsetY,
                        HasCollision = obs.HasCollision
                    };

                    if (obs.SpriteSheet != null)
                    {
                        def.SpriteConfig = new SpriteSheetConfig
                        {
                            Image = obs.SpriteSheet.Image,
                            FrameW = obs.SpriteSheet.FrameW,
                            FrameH = obs.SpriteSheet.FrameH,
                            Count = obs.SpriteSheet.Count,
                            Scale = obs.SpriteSheet.Scale
                        };
                    }

                    if (!string.IsNullOrEmpty(def.Key))
                    {
                        bundle.ObstacleInfo[def.Key] = def;
                    }
                }
            }

            bundle.EffectConfigs.Clear();
            foreach (var cfg in configList)
            {
                foreach (var eff in cfg.Effects)
                {
                    var ec = new EffectConfig
                    {
                        Key = eff.Key,
                        Image = eff.Image,
                        FrameW = eff.FrameW,
                        FrameH = eff.FrameH,
                        Count = eff.Count,
                        Speed = eff.Speed,
                        Scale = eff.Scale
                    };

                    if (eff.Light != null)
                    {
                        ec.Light = new LightSourceDef
                        {
                            Color = eff.Light.Color,
                            Radius = eff.Light.Radius,
                            FlickerIntensity = eff.Light.FlickerIntensity,
                            FlickerFrequency = eff.Light.FlickerFrequency
                        };
                    }

                    if (!string.IsNullOrEmpty(ec.Key))
                    {
                        bundle.EffectConfigs[ec.Key] = ec;
                    }
                }
            }

            bundle.BaseUnitStats.Clear();
            var rawUnits = new Dictionary<string, UnitConfig>();
            foreach (var cfg in configList)
            {
                foreach (var u in cfg.Units)
                {
                    if (!string.IsNullOrEmpty(u.Key))
                    {
                        rawUnits[u.Key] = u;
                    }
                }
            }

            foreach (var key in rawUnits.Keys)
            {
                GetOrLoadUnitStats(key, rawUnits, new HashSet<string>(), bundle.BaseUnitStats);
            }

            bundle.UnitUpgradeDefs.Clear();
            foreach (var cfg in configList)
            {
                foreach (var tree in cfg.UnitUpgrades)
                {
                    if (string.IsNullOrEmpty(tree.Key))
                    {
                        continue;
                    }

                    var list = new List<UpgradeLevelDef>();
                    foreach (var lvl in tree.Levels)
                    {
                        list.Add(new UpgradeLevelDef
                        {
                            Cost = lvl.Cost,
                            Type = lvl.Type,
                            Value = lvl.Value,
                            Desc = lvl.Desc
                        });
                    }

                    bundle.UnitUpgradeDefs[tree.Key] = list;
                }
            }

            bundle.BuildingBlueprints.Clear();
            bundle.BuildingRegistry.Clear();
            foreach (var cfg in configList)
            {
                foreach (var b in cfg.Buildings)
                {
                    var info = new BuildingInfo
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Cost = b.Cost,
                        Max = b.Max,
                        Width = b.Width,
                        Height = b.Height,
                        Damage = b.Damage,
                        Range = b.Range,
                        CD = b.CD,
                        Hp = b.Hp,
                        Image = b.Image,
                        Scale = b.Scale,
                        OffsetY = b.OffsetY
                    };

                    if (!string.IsNullOrEmpty(b.Produces))
                    {
                        info.Produces.AddRange(b.Produces.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }

                    if (b.Light != null)
                    {
                        info.Light = new LightSourceDef
                        {
                            Color = b.Light.Color,
                            Radius = b.Light.Radius,
                            FlickerIntensity = b.Light.FlickerIntensity,
                            FlickerFrequency = b.Light.FlickerFrequency
                        };
                    }

                    if (b.SpriteSheet != null)
                    {
                        info.SpriteConfig = new SpriteSheetConfig
                        {
                            Image = b.SpriteSheet.Image,
                            FrameW = b.SpriteSheet.FrameW,
                            FrameH = b.SpriteSheet.FrameH,
                            Count = b.SpriteSheet.Count,
                            Scale = b.SpriteSheet.Scale
                        };

                        foreach (var st in b.SpriteSheet.States)
                        {
                            info.SpriteConfig.States.Add(new SpriteAnimDef
                            {
                                Name = st.Name,
                                Row = st.Row,
                                Count = st.Count,
                                Speed = st.Speed,
                                Loop = st.Loop
                            });
                        }
                    }

                    if (b.ProjectileSprite != null)
                    {
                        info.ProjectileConfig = new ProjectileSpriteConfig
                        {
                            Image = b.ProjectileSprite.Image,
                            FrameW = b.ProjectileSprite.FrameW,
                            FrameH = b.ProjectileSprite.FrameH,
                            Count = b.ProjectileSprite.Count,
                            Speed = b.ProjectileSprite.Speed,
                            Scale = b.ProjectileSprite.Scale
                        };
                    }

                    if (!string.IsNullOrEmpty(info.Id))
                    {
                        bundle.BuildingRegistry[info.Id] = info;
                    }

                    if (info.Id != "castle" && info.Id != "stronghold" && !info.Id.StartsWith("orc", StringComparison.OrdinalIgnoreCase))
                    {
                        bundle.BuildingBlueprints.Add(info);
                    }
                }
            }

            bundle.PermanentUpgrades.Clear();
            foreach (var cfg in configList)
            {
                foreach (var upg in cfg.Upgrades)
                {
                    bundle.PermanentUpgrades.Add(new UpgradeInfo
                    {
                        Id = upg.Id,
                        Name = upg.Name,
                        Desc = upg.Desc,
                        Cost = upg.Cost
                    });
                }
            }

            bundle.Stages.Clear();
            foreach (var cfg in configList)
            {
                foreach (var s in cfg.Stages)
                {
                    var stage = new StageInfo
                    {
                        Id = s.Id,
                        Title = s.Title,
                        EnemyBaseHp = s.Hp,
                        MapWidth = s.Width,
                        MapHeight = s.Height,
                        RandomObstacleCount = s.ObstaclesCount,
                        TileMapData = s.TileMapData
                    };

                    foreach (var t in s.Terrain)
                    {
                        stage.TerrainRules.Add(new StageTerrainDef { TileId = t.Id, Weight = t.Weight });
                    }

                    foreach (var r in s.MapTiles)
                    {
                        stage.TileOverrides.Add(new TileRectDef { X = r.X, Y = r.Y, W = r.W, H = r.H, TileId = r.Id });
                    }

                    foreach (var p in s.Placements)
                    {
                        stage.Placements.Add(new EntityPlacement
                        {
                            Team = p.Team,
                            Type = p.Type,
                            Key = p.Key,
                            X = p.X,
                            Y = p.Y,
                            Rotation = p.Rotation,
                            Width = p.Width,
                            Height = p.Height
                        });
                    }

                    foreach (var o in s.Obstacles)
                    {
                        stage.FixedObstacles.Add(new LevelObstacleDef
                        {
                            X = o.X,
                            Y = o.Y,
                            Type = o.Type,
                            Rotation = o.Rotation,
                            Width = o.Width,
                            Height = o.Height
                        });
                    }

                    for (int x = 0; x <= stage.MapWidth; x += 50)
                    {
                        double y = stage.MapHeight / 2 + Math.Sin(x / 400.0) * (stage.MapHeight / 6);
                        stage.VisualRoad.Add(new PointD(x, y));
                    }

                    bundle.Stages[stage.Id] = stage;
                }
            }

            ValidateConfig(bundle);
            return bundle;
        }

        private static UnitStats GetOrLoadUnitStats(
            string key,
            Dictionary<string, UnitConfig> rawUnits,
            HashSet<string> visiting,
            Dictionary<string, UnitStats> baseUnitStats)
        {
            if (baseUnitStats.ContainsKey(key))
            {
                return baseUnitStats[key];
            }

            if (!rawUnits.ContainsKey(key))
            {
                return null;
            }

            if (visiting.Contains(key))
            {
                return new UnitStats { Key = key };
            }

            visiting.Add(key);
            var config = rawUnits[key];
            UnitStats stats = new UnitStats();

            if (!string.IsNullOrEmpty(config.Inherits))
            {
                var parent = GetOrLoadUnitStats(config.Inherits, rawUnits, visiting, baseUnitStats);
                if (parent != null)
                {
                    stats = parent.Clone();
                }
            }

            ApplyUnitConfig(stats, config);
            baseUnitStats[key] = stats;
            visiting.Remove(key);
            return stats;
        }

        private static void ApplyUnitConfig(UnitStats stats, UnitConfig config)
        {
            stats.Key = config.Key;
            if (config.Name != null) stats.Name = config.Name;
            if (config.Faction != null) stats.Faction = config.Faction;
            if (config.Cost.HasValue) stats.Cost = config.Cost.Value;
            if (config.HP.HasValue) stats.HP = config.HP.Value;
            if (config.Dmg.HasValue) stats.Dmg = config.Dmg.Value;
            if (config.Range.HasValue) stats.Range = config.Range.Value;
            if (config.Speed.HasValue) stats.Speed = config.Speed.Value;
            if (config.CD.HasValue) stats.CD = (int)config.CD.Value;

            if (config.Type.HasValue) stats.Type = config.Type.Value;
            if (config.AtkType.HasValue) stats.AtkType = config.AtkType.Value;
            if (config.DefType.HasValue) stats.DefType = config.DefType.Value;

            if (config.IsHero.HasValue) stats.IsHero = config.IsHero.Value;
            if (config.IsMounted.HasValue) stats.IsMounted = config.IsMounted.Value;
            if (config.MaxMana.HasValue) stats.MaxMana = config.MaxMana.Value;
            if (config.ManaRegen.HasValue) stats.ManaRegen = config.ManaRegen.Value;
            if (config.BlockChance.HasValue) stats.BlockChance = config.BlockChance.Value;
            if (config.CritChance.HasValue) stats.CritChance = config.CritChance.Value;
            if (config.SightRadius.HasValue) stats.SightRadius = config.SightRadius.Value;
            if (config.Width.HasValue) stats.Width = config.Width.Value;
            if (config.Height.HasValue) stats.Height = config.Height.Value;
            if (config.SkillName != null) stats.SkillName = config.SkillName;

            if (config.SpriteSheet != null)
            {
                stats.SpriteConfig = new SpriteSheetConfig
                {
                    Image = config.SpriteSheet.Image,
                    FrameW = config.SpriteSheet.FrameW,
                    FrameH = config.SpriteSheet.FrameH,
                    Scale = config.SpriteSheet.Scale
                };

                foreach (var st in config.SpriteSheet.States)
                {
                    stats.SpriteConfig.States.Add(new SpriteAnimDef
                    {
                        Name = st.Name,
                        Row = st.Row,
                        Count = st.Count,
                        Speed = st.Speed,
                        Loop = st.Loop
                    });
                }
            }

            if (config.ProjectileSprite != null)
            {
                stats.ProjectileConfig = new ProjectileSpriteConfig
                {
                    Image = config.ProjectileSprite.Image,
                    FrameW = config.ProjectileSprite.FrameW,
                    FrameH = config.ProjectileSprite.FrameH,
                    Count = config.ProjectileSprite.Count,
                    Speed = config.ProjectileSprite.Speed,
                    Scale = config.ProjectileSprite.Scale
                };

                if (config.ProjectileSprite.Light != null)
                {
                    stats.ProjectileConfig.Light = new LightSourceDef
                    {
                        Color = config.ProjectileSprite.Light.Color,
                        Radius = config.ProjectileSprite.Light.Radius,
                        FlickerIntensity = config.ProjectileSprite.Light.FlickerIntensity,
                        FlickerFrequency = config.ProjectileSprite.Light.FlickerFrequency
                    };
                }
            }

            if (config.Skills != null && config.Skills.Count > 0)
            {
                stats.SkillDefs.Clear();
                foreach (var s in config.Skills)
                {
                    var skillDef = new SkillDef
                    {
                        Key = s.Key,
                        Name = s.Name,
                        Cost = s.Cost,
                        CD = s.CD,
                        Desc = s.Desc,
                        Target = s.Target,
                        Range = s.Range,
                        Radius = s.Radius,
                        CastTime = s.CastTime
                    };

                    foreach (var e in s.Effects)
                    {
                        skillDef.Effects.Add(new SkillEffectDef
                        {
                            Type = e.Type,
                            TargetType = e.TargetType,
                            Value = e.Value,
                            Radius = e.Radius,
                            Duration = e.Duration,
                            VisualKey = e.VisualKey,
                            ProjectileId = e.ProjectileId
                        });
                    }

                    stats.SkillDefs.Add(skillDef);
                }
            }
        }

        private static void ValidateConfig(GameConfigBundle bundle)
        {
            foreach (var kvp in bundle.BuildingRegistry)
            {
                var building = kvp.Value;
                foreach (var unitKey in building.Produces)
                {
                    if (!string.IsNullOrEmpty(unitKey) && !bundle.BaseUnitStats.ContainsKey(unitKey))
                    {
                        Debug.WriteLine(
                            $"[Config] Building '{building.Name}' ({building.Id}) produces unknown unit '{unitKey}'."
                        );
                    }
                }
            }

            foreach (var kvp in bundle.BaseUnitStats)
            {
                var unit = kvp.Value;
                if (!string.IsNullOrEmpty(unit.SkillName))
                {
                    bool skillExists = unit.SkillDefs.Any(s => s.Key == unit.SkillName);
                    if (!skillExists)
                    {
                        Debug.WriteLine(
                            $"[Config] Unit '{unit.Name}' ({unit.Key}) references missing skill '{unit.SkillName}'."
                        );
                    }
                }
            }
        }
    }
}
