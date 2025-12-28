using System.Collections.Generic;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine.AI
{
    // === 1. 基础框架 ===
    public enum NodeState { Running, Success, Failure }

    public abstract class Node<T>
    {
        public abstract NodeState Evaluate(T agent, GameEngine engine, double dt);
    }

    public class Selector<T> : Node<T>
    {
        private List<Node<T>> _children = new List<Node<T>>();
        public Selector(params Node<T>[] nodes) { _children.AddRange(nodes); }
        public override NodeState Evaluate(T agent, GameEngine engine, double dt)
        {
            foreach (var node in _children)
            {
                var result = node.Evaluate(agent, engine, dt);
                if (result != NodeState.Failure) return result;
            }
            return NodeState.Failure;
        }
    }

    public class Sequence<T> : Node<T>
    {
        private List<Node<T>> _children = new List<Node<T>>();
        public Sequence(params Node<T>[] nodes) { _children.AddRange(nodes); }
        public override NodeState Evaluate(T agent, GameEngine engine, double dt)
        {
            bool anyChildRunning = false;
            foreach (var node in _children)
            {
                var result = node.Evaluate(agent, engine, dt);
                if (result == NodeState.Failure) return NodeState.Failure;
                if (result == NodeState.Running) anyChildRunning = true;
            }
            return anyChildRunning ? NodeState.Running : NodeState.Success;
        }
    }

    public class Inverter<T> : Node<T>
    {
        private Node<T> _node;
        public Inverter(Node<T> node) { _node = node; }
        public override NodeState Evaluate(T agent, GameEngine engine, double dt)
        {
            var result = _node.Evaluate(agent, engine, dt);
            if (result == NodeState.Failure) return NodeState.Success;
            if (result == NodeState.Success) return NodeState.Failure;
            return NodeState.Running;
        }
    }

    // === 2. 具体逻辑节点 ===

    // [条件] 检查是否有强制命令
    public class CheckHasCommand : Node<Unit>
    {
        public override NodeState Evaluate(Unit unit, GameEngine engine, double dt)
        {
            return unit.HasCommand ? NodeState.Success : NodeState.Failure;
        }
    }

    // [条件] 检查是否被手动停止 (S键)
    public class CheckIsStopped : Node<Unit>
    {
        public override NodeState Evaluate(Unit unit, GameEngine engine, double dt)
        {
            return unit.IsManuallyStopped ? NodeState.Success : NodeState.Failure;
        }
    }

    // [动作] 执行强制移动命令 (核心修复)
    public class ExecuteCommandMove : Node<Unit>
    {
        public override NodeState Evaluate(Unit unit, GameEngine engine, double dt)
        {
            // 如果是攻击移动，先检查有没有敌人
            // 如果 unit.CheckScanForEnemy 返回 true，说明发现了敌人，
            // 此时必须返回 Failure，让 Selector 跳出当前分支，进入下方的“战斗分支”
            if (unit.IsAttackMove)
            {
                if (unit.CheckScanForEnemy(engine, dt))
                {
                    // 发现敌人 -> 中断移动 -> 失败
                    return NodeState.Failure;
                }
            }

            // 执行移动逻辑
            // MoveToCommandTarget 内部已经包含了到达判定
            bool finished = unit.MoveToCommandTarget(engine, dt);

            // 如果走完了，返回 Success；还在走，返回 Running
            return finished ? NodeState.Success : NodeState.Running;
        }
    }

    // [动作] 扫描敌人
    public class FindTargetNode : Node<Unit>
    {
        public override NodeState Evaluate(Unit unit, GameEngine engine, double dt)
        {
            // 尝试扫描敌人
            bool found = unit.CheckScanForEnemy(engine, dt, force: false);
            return found ? NodeState.Success : NodeState.Failure;
        }
    }

    // [条件] 检查射程
    public class CheckTargetInRange : Node<Unit>
    {
        public override NodeState Evaluate(Unit unit, GameEngine engine, double dt)
        {
            if (unit.Target == null || unit.Target.HP <= 0) return NodeState.Failure;
            double distSq = (unit.Target.X - unit.X) * (unit.Target.X - unit.X) + (unit.Target.Y - unit.Y) * (unit.Target.Y - unit.Y);

            // 判定距离：射程 + 两人体积的一半 (边缘对边缘)
            double r = unit.Range + (unit.Width + unit.Target.Width) * 0.45;

            if (distSq <= r * r) return NodeState.Success;
            return NodeState.Failure;
        }
    }

    // [动作] 攻击
    public class AttackTargetNode : Node<Unit>
    {
        public override NodeState Evaluate(Unit unit, GameEngine engine, double dt)
        {
            if (unit.Target == null || unit.Target.HP <= 0) return NodeState.Failure;

            // 如果已经在攻击状态，更新攻击逻辑
            if (unit.State == UnitState.Attack)
            {
                unit.UpdateAttackLogic(engine, dt);
                return NodeState.Running;
            }

            // 冷却就绪 -> 攻击
            if (unit.CurrentCD <= 0)
            {
                unit.StartAttack(engine);
                return NodeState.Running;
            }
            else
            {
                // 冷却中 -> 面向敌人待机
                unit.FaceTarget();
                if (unit.Animator?.CurrentAnimName != "Idle") unit.Animator?.Play("Idle");
                return NodeState.Running;
            }
        }
    }

    // [动作] 追击
    public class ChaseTargetNode : Node<Unit>
    {
        public override NodeState Evaluate(Unit unit, GameEngine engine, double dt)
        {
            if (unit.Target == null || unit.Target.HP <= 0) return NodeState.Failure;

            unit.State = UnitState.Move;
            unit.Animator?.Play("Move");

            // 追击目标位置
            unit.InvokeMoveWithSteering(unit.Target.X, unit.Target.Y, dt, engine);
            return NodeState.Running;
        }
    }

    // [动作] 自动推线 (Lane Push)
    public class LanePushNode : Node<Unit>
    {
        public override NodeState Evaluate(Unit unit, GameEngine engine, double dt)
        {
            // 这里的逻辑是：如果没事做，也没被S停，就往敌方基地走
            double targetX = unit.Team == TeamType.Human ? engine.WorldWidth - 100 : 100;
            double laneY = engine.MapDepth / 2;

            // [Fix] Give each unit a unique, stable Y-target to prevent clumping into a line.
            // Use the unit's hash code to generate a deterministic "slot" offset.
            int hashCode = unit.GetHashCode();
            double yOffset = (hashCode % 601) - 300; // Creates a spread of +/- 300 pixels
            double targetY = laneY + yOffset;

            unit.State = UnitState.Move;
            unit.Animator?.Play("Move");
            unit.InvokeMoveWithSteering(targetX, targetY, dt, engine);

            return NodeState.Running;
        }
    }

    // [动作] 待机
    public class IdleNode : Node<Unit>
    {
        public override NodeState Evaluate(Unit unit, GameEngine engine, double dt)
        {
            unit.State = UnitState.Idle;
            unit.Animator?.Play("Idle");
            unit.Velocity = new PointD(0, 0); // 彻底刹车
            return NodeState.Running;
        }
    }
}