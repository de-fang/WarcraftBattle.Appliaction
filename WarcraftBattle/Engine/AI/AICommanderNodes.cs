using System;
using System.Linq;

namespace WarcraftBattle.Engine.AI
{
    // =================================================
    // AI Commander Specific Behavior Tree Nodes
    // =================================================

    #region Conditions

    /// <summary>
    /// Checks if the AI can afford a certain cost.
    /// </summary>
    public class CheckEconomy : Node<AICommander>
    {
        private readonly int _requiredGold;
        public CheckEconomy(int requiredGold) { _requiredGold = requiredGold; }

        public override NodeState Evaluate(AICommander commander, GameEngine engine, double dt)
        {
            return engine.Gold >= _requiredGold ? NodeState.Success : NodeState.Failure;
        }
    }

    /// <summary>
    /// Checks if the build order queue is finished.
    /// </summary>
    public class IsBuildOrderComplete : Node<AICommander>
    {
        public override NodeState Evaluate(AICommander commander, GameEngine engine, double dt)
        {
            return commander.IsBuildOrderFinished() ? NodeState.Success : NodeState.Failure;
        }
    }

    /// <summary>
    /// Checks if the army is large enough to launch an attack.
    /// </summary>
    public class CheckArmySize : Node<AICommander>
    {
        private readonly int _requiredSize;
        public CheckArmySize(int requiredSize) { _requiredSize = requiredSize; }

        public override NodeState Evaluate(AICommander commander, GameEngine engine, double dt)
        {
            int armyCount = engine.Entities.Count(e => e is Unit u && u.Team == Shared.Enums.TeamType.Orc && u.HP > 0);
            return armyCount >= _requiredSize ? NodeState.Success : NodeState.Failure;
        }
    }

    #endregion

    #region Actions

    /// <summary>
    /// Tries to build the next building in the AI's build order.
    /// </summary>
    public class BuildFromOrder : Node<AICommander>
    {
        public override NodeState Evaluate(AICommander commander, GameEngine engine, double dt)
        {
            string buildingKey = commander.GetNextBuildingInOrder();
            if (string.IsNullOrEmpty(buildingKey)) return NodeState.Failure;

            if (!engine.BuildingRegistry.TryGetValue(buildingKey, out var blueprint)) return NodeState.Failure;

            // Check cost
            if (engine.Gold < blueprint.Cost) return NodeState.Running; // Not enough gold, wait

            // Check max count
            int currentCount = engine.Entities.Count(e => e is Building b && b.Team == Shared.Enums.TeamType.Orc && b.Id == buildingKey);
            if (currentCount >= blueprint.Max)
            {
                commander.AdvanceBuildOrder(); // Skip this, it's already maxed out
                return NodeState.Success;
            }

            // Find a location and build
            if (commander.TryBuildBuilding(blueprint))
            {
                commander.AdvanceBuildOrder();
                return NodeState.Success;
            }

            // Can't find a spot, maybe wait and try again
            return NodeState.Running;
        }
    }

    /// <summary>
    /// Trains a specific unit if conditions are met.
    /// </summary>
    public class TrainUnit : Node<AICommander>
    {
        private readonly string _unitKey;
        public TrainUnit(string unitKey) { _unitKey = unitKey; }

        public override NodeState Evaluate(AICommander commander, GameEngine engine, double dt)
        {
            // Check pop cap
            if (engine.GetUnitCount() >= engine.MaxPop) return NodeState.Failure;

            // Check cost
            if (!engine.BaseUnitStats.TryGetValue(_unitKey, out var stats)) return NodeState.Failure;
            if (engine.Gold < stats.Cost) return NodeState.Failure; // Can't afford, fail for now

            engine.SpawnUnit(_unitKey, Shared.Enums.TeamType.Orc);
            return NodeState.Success;
        }
    }

    /// <summary>
    /// Sends all idle military units to attack the player's base.
    /// </summary>
    public class LaunchAttack : Node<AICommander>
    {
        public override NodeState Evaluate(AICommander commander, GameEngine engine, double dt)
        {
            var playerBase = engine.Entities.FirstOrDefault(e => e.Team == Shared.Enums.TeamType.Human && e is Building b && b.Id == "castle");
            if (playerBase == null) return NodeState.Failure;

            var idleAttackers = engine.Entities.OfType<Unit>()
                .Where(u => u.Team == Shared.Enums.TeamType.Orc && u.HP > 0 && u.State == Shared.Enums.UnitState.Idle)
                .ToList();

            if (idleAttackers.Count == 0) return NodeState.Failure; // No one to send

            foreach (var unit in idleAttackers)
            {
                unit.OrderAttackMove(playerBase.X, playerBase.Y, engine);
            }

            return NodeState.Success;
        }
    }

    /// <summary>
    /// A simple node that waits for a certain amount of time.
    /// </summary>
    public class Wait : Node<AICommander>
    {
        private double _duration;
        private double _timer;
        public Wait(double duration) { _duration = duration; }

        public override NodeState Evaluate(AICommander commander, GameEngine engine, double dt)
        {
            _timer += dt;
            if (_timer >= _duration) { _timer = 0; return NodeState.Success; }
            return NodeState.Running;
        }
    }

    #endregion
}