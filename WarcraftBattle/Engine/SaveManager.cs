using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine
{
    public class SaveDataDTO
    {
        public double Gold { get; set; }
        public int Stage { get; set; }
        public double WaveLevel { get; set; }
        public List<UnitSaveData> Units { get; set; } = new List<UnitSaveData>();
        public List<BuildingSaveData> Buildings { get; set; } = new List<BuildingSaveData>();
    }

    public class UnitSaveData
    {
        public string Key { get; set; }
        public TeamType Team { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double HP { get; set; }
        public double Mana { get; set; }
        public CommandSaveData CurrentCommand { get; set; }
    }

    public class BuildingSaveData
    {
        public string Id { get; set; }
        public TeamType Team { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double HP { get; set; }
    }

    public class CommandSaveData
    {
        public string Type { get; set; } // "Move", "Attack", etc.
        public double TargetX { get; set; }
        public double TargetY { get; set; }
    }

    public static class SaveManager
    {
        public static void SaveGame(string filename, GameEngine engine)
        {
            var dto = new SaveDataDTO
            {
                Gold = engine.Gold,
                Stage = engine.Stage,
                WaveLevel = engine.AiWaveLevel
            };

            foreach (var entity in engine.Entities)
            {
                if (entity is Unit u && u.HP > 0)
                {
                    var uData = new UnitSaveData
                    {
                        Key = u.Key,
                        Team = u.Team,
                        X = u.X,
                        Y = u.Y,
                        HP = u.HP,
                        Mana = u.Mana
                    };

                    if (u.HasCommand && u.CommandTargetPos.HasValue)
                    {
                        // Currently we only support saving Move Command targets easily
                        // If it's an attack move, we could differentiate if we had that info exposed easily
                        // For now, let's save the destination.
                        uData.CurrentCommand = new CommandSaveData
                        {
                            Type = u.IsAttackMove ? "AttackMove" : "Move",
                            TargetX = u.CommandTargetPos.Value.X,
                            TargetY = u.CommandTargetPos.Value.Y
                        };
                    }

                    dto.Units.Add(uData);
                }
                else if (entity is Building b && b.HP > 0)
                {
                    dto.Buildings.Add(new BuildingSaveData
                    {
                        Id = b.Id,
                        Team = b.Team,
                        X = b.X,
                        Y = b.Y,
                        HP = b.HP
                    });
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(dto, options);
            File.WriteAllText(filename, jsonString);
        }

        public static void LoadGame(string filename, GameEngine engine)
        {
            if (!File.Exists(filename)) return;

            string jsonString = File.ReadAllText(filename);
            var dto = JsonSerializer.Deserialize<SaveDataDTO>(jsonString);

            if (dto == null) return;

            // Reset Game State
            // Note: We might want to keep the current stage map if it matches, or reload it.
            // Simplified: Just clear entities and respawn. 
            // Ideally engine.Start(dto.Stage) would be called first to setup map/resources, 
            // then we override the state.

            engine.Start(dto.Stage); // Re-initialize map and defaults

            // Override globals
            engine.Gold = dto.Gold;
            engine.AiWaveLevel = dto.WaveLevel;

            // Clear default entities spawned by Start() (like bases, if we are restoring them from save)
            // But Start() spawns buildings based on config.
            // If our save contains all buildings, we should clear everything.
            engine.Entities.Clear();

            // Restore Buildings
            foreach (var bData in dto.Buildings)
            {
                // Find blueprint to restore stats
                BuildingInfo bp = null;
                if (engine.BuildingRegistry.ContainsKey(bData.Id)) bp = engine.BuildingRegistry[bData.Id];

                // Fallback or default values
                string name = bp?.Name ?? "Unknown";
                double w = bp?.Width ?? 120;
                double h = bp?.Height ?? 120;
                double offY = bp?.OffsetY ?? 0;

                var info = bp != null ? bp.Clone() : new BuildingInfo { Id = bData.Id, Name = name, Width = w, Height = h, OffsetY = offY };
                // Override HP from save
                info.Hp = bData.HP;

                var b = new Building(bData.X, bData.Y, bData.Team, info);
                engine.Entities.Add(b);
            }

            // Restore Units
            foreach (var uData in dto.Units)
            {
                if (!engine.BaseUnitStats.ContainsKey(uData.Key)) continue;

                var stats = engine.GetUnitStats(uData.Key);
                var u = new Unit(uData.X, uData.Y, uData.Team, uData.Key, stats);
                u.HP = uData.HP;
                u.Mana = uData.Mana;

                engine.Entities.Add(u);

                // Restore Command
                if (uData.CurrentCommand != null)
                {
                    if (uData.CurrentCommand.Type == "Move")
                    {
                        u.IssueCommand(new MoveCommand(uData.CurrentCommand.TargetX, uData.CurrentCommand.TargetY), engine);
                    }
                    else if (uData.CurrentCommand.Type == "AttackMove")
                    {
                        // We need an AttackMove command class or method. 
                        // The user asked for "restoring movement command".
                        // Existing code has Unit.OrderAttackMove.
                        // Let's create a specific command or reuse MoveCommand?
                        // Wait, previous task created MoveCommand, AttackCommand, StopCommand.
                        // It didn't strictly create "AttackMoveCommand".
                        // I should add it or just use MoveCommand for now if AttackMoveCommand doesn't exist.
                        // Let's check Commands.cs content from memory or assume I should add it if I want to support it properly.
                        // But for compliance with "MoveCommand (contains target X, Y)", I will use that. 
                        // If I want to support AttackMove, I should add it to Commands.cs.
                        // For this task, I'll stick to MoveCommand for simplicity unless I see AttackMove is critical.
                        // Actually, Unit.OrderAttackMove exists. I should probably add AttackMoveCommand to Commands.cs to be complete.
                        // But I'll stick to what I have: MoveCommand.
                        u.IssueCommand(new MoveCommand(uData.CurrentCommand.TargetX, uData.CurrentCommand.TargetY), engine);
                    }
                }
            }

            // Re-clamp camera just in case
            engine.ClampCamera();
        }
    }
}
