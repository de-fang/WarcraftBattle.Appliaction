using System.Collections.Generic;

namespace WarcraftBattle.Engine.AI
{
    /// <summary>
    /// Implements the Flyweight pattern for Behavior Trees.
    /// Ensures that all units of the same type share a single, stateless behavior tree instance
    /// to significantly reduce memory allocations.
    /// </summary>
    public static class BehaviorTreeFactory
    {
        private static readonly Dictionary<string, Node<Unit>> _treeCache = new Dictionary<string, Node<Unit>>();
        private static readonly object _lock = new object();

        public static Node<Unit> GetTreeForUnit(string unitKey)
        {
            // Currently, all units share the same generic AI logic.
            // If different units (e.g., "grunt" vs "shaman") needed different trees,
            // we would use the 'unitKey' to cache and retrieve specific trees.
            const string genericTreeKey = "default_unit_tree";

            lock (_lock)
            {
                if (_treeCache.TryGetValue(genericTreeKey, out var cachedTree))
                {
                    return cachedTree;
                }

                // This is the exact same logic that was previously in the Unit constructor.
                var newTree = new Selector<Unit>(
                    new Sequence<Unit>(new CheckHasCommand(), new ExecuteCommandMove()),
                    new Sequence<Unit>(new FindTargetNode(), new Selector<Unit>(new Sequence<Unit>(new CheckTargetInRange(), new AttackTargetNode()), new ChaseTargetNode())),
                    new Sequence<Unit>(new Inverter<Unit>(new CheckIsStopped()), new LanePushNode()),
                    new IdleNode()
                );

                _treeCache[genericTreeKey] = newTree;
                return newTree;
            }
        }
    }
}