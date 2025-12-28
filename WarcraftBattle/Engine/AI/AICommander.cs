using System;
using System.Linq;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine.AI
{
    public class AICommander
    {
        private readonly GameEngine _engine;
        private readonly Node<AICommander> _root;
        private int _buildOrderIndex = 0;
        private double _trainUnitTimer = 0;

        public AICommander(GameEngine engine)
        {
            _engine = engine;

            // Define the AI's behavior using a behavior tree
            _root = new Selector<AICommander>(
                // Priority 1: Follow the build order
                new Sequence<AICommander>(
                    new Inverter<AICommander>(new IsBuildOrderComplete()), // If build order is NOT complete...
                    new BuildFromOrder()                                 // ...then build the next item.
                ),

                // Priority 2: Train units periodically
                new Sequence<AICommander>(
                    new TrainUnit("grunt") // Simple logic: always try to train grunts
                ),

                // Priority 3: Launch a major attack when army is large enough
                new Sequence<AICommander>(
                    new CheckArmySize(10), // If army has at least 10 units...
                    new LaunchAttack()     // ...send them to attack.
                ),

                // Fallback: Wait
                new Wait(1.0)
            );
        }

        public void Update(double dt)
        {
            // The entire AI logic is now driven by the behavior tree
            _root.Evaluate(this, _engine, dt);

            // The old wave logic can be adapted or replaced.
            // For now, let's keep the difficulty scaling.
            if (_engine.AiWaveLevel < 1 + _engine.AIProfile.Aggressiveness)
            {
                _engine.AiWaveLevel = 1 + _engine.AIProfile.Aggressiveness;
            }
        }

        #region Behavior Tree Helper Methods

        public bool IsBuildOrderFinished() => _buildOrderIndex >= _engine.AIProfile.BuildOrder.Count;
        public string GetNextBuildingInOrder() => IsBuildOrderFinished() ? null : _engine.AIProfile.BuildOrder[_buildOrderIndex];
        public void AdvanceBuildOrder() => _buildOrderIndex++;

        public bool TryBuildBuilding(BuildingInfo blueprint)
        {
            // Find a valid, unoccupied position near an existing AI building
            var aiBases = _engine.Entities.Where(e => e.Team == TeamType.Orc && e is Building).ToList();
            if (aiBases.Count == 0) return false; // Should not happen if stronghold exists

            var anchor = aiBases[_buildOrderIndex % aiBases.Count]; // Build near different buildings

            for (int i = 0; i < 20; i++) // Try 20 random positions
            {
                double angle = new Random().NextDouble() * 2 * Math.PI;
                double dist = 150 + new Random().NextDouble() * 200;
                double x = anchor.X + Math.Cos(angle) * dist;
                double y = anchor.Y + Math.Sin(angle) * dist;

                // Clamp to map bounds
                x = Math.Clamp(x, blueprint.Width, _engine.WorldWidth - blueprint.Width);
                y = Math.Clamp(y, blueprint.Height, _engine.MapDepth - blueprint.Height);

                if (_engine.CheckBuildValidity(x, y, blueprint.Width, blueprint.Height))
                {
                    _engine.Gold -= blueprint.Cost;
                    var newBuilding = new Building(x, y, TeamType.Orc, blueprint);
                    _engine.Entities.Add(newBuilding);
                    _engine.InvalidatePathfinding();
                    return true;
                }
            }

            return false; // Failed to find a spot
        }

        #endregion
    }
}