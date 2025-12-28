using System;
using WarcraftBattle.Engine;

namespace WarcraftBattle.Engine
{
    public abstract class UnitCommand
    {
        public abstract void Execute(Unit unit, GameEngine engine);
    }

    public class MoveCommand : UnitCommand
    {
        public double TargetX { get; }
        public double TargetY { get; }

        public MoveCommand(double x, double y)
        {
            TargetX = x;
            TargetY = y;
        }

        public override void Execute(Unit unit, GameEngine engine)
        {
            unit.OrderMove(TargetX, TargetY, engine);
            EventBus.Publish(new UnitCommandEvent(unit, this));
        }
    }

    public class AttackCommand : UnitCommand
    {
        public Entity TargetEntity { get; }

        public AttackCommand(Entity target)
        {
            TargetEntity = target;
        }

        public override void Execute(Unit unit, GameEngine engine)
        {
            unit.OrderAttack(TargetEntity);
            EventBus.Publish(new UnitCommandEvent(unit, this));
        }
    }

    public class StopCommand : UnitCommand
    {
        public override void Execute(Unit unit, GameEngine engine)
        {
            unit.OrderStop();
        }
    }

    public class AttackMoveCommand : UnitCommand
    {
        public double TargetX { get; }
        public double TargetY { get; }

        public AttackMoveCommand(double x, double y)
        {
            TargetX = x;
            TargetY = y;
        }

        public override void Execute(Unit unit, GameEngine engine)
        {
            unit.OrderAttackMove(TargetX, TargetY, engine);
        }
    }
}
