using System;
using UnityEngine;
using WarcraftBattle3D.Core;
using UObject = UnityEngine.Object;

namespace WarcraftBattle3D
{
    public class DebugProjectileSpawner : MonoBehaviour
    {
        [SerializeField]
        private GameRuntimeHost runtimeHost;
        [SerializeField]
        private bool autoRun = true;
        [SerializeField]
        private float interval = 1.2f;
        [SerializeField]
        private float radius = 400f;
        [SerializeField]
        private bool spawnProjectile = true;
        [SerializeField]
        private bool spawnEffect = true;
        [SerializeField]
        private string effectKey = "ClickAttack";
        [SerializeField]
        private float effectLife = 0.6f;
        [SerializeField]
        private string projectileKey = "DebugProjectile";
        [SerializeField]
        private UnitType projectileType = UnitType.Ranged;
        [SerializeField]
        private AttackType attackType = AttackType.Normal;
        [SerializeField]
        private float damage = 20f;
        [SerializeField]
        private float impactRadius = 0f;
        [SerializeField]
        private bool hitAllTeams = false;
        [SerializeField]
        private bool spawnOnKey = false;
        [SerializeField]
        private KeyCode spawnKey = KeyCode.T;

        private float _timer;
        private static readonly System.Random Rng = new System.Random();

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
        }

        private void Update()
        {
            var engine = runtimeHost?.Engine;
            if (engine == null)
            {
                return;
            }

            bool trigger = false;
            if (autoRun)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _timer = Mathf.Max(0.1f, interval);
                    trigger = true;
                }
            }

            if (spawnOnKey && Input.GetKeyDown(spawnKey))
            {
                trigger = true;
            }

            if (trigger)
            {
                Spawn(engine);
            }
        }

        private void Spawn(GameEngine engine)
        {
            GetAnchor(engine, out double anchorX, out double anchorY);

            double startX = anchorX + RandomRange(-radius * 0.4f, radius * 0.4f);
            double startY = anchorY + RandomRange(-radius * 0.4f, radius * 0.4f);
            double targetX = anchorX + RandomRange(-radius, radius);
            double targetY = anchorY + RandomRange(-radius, radius);

            startX = Clamp(startX, 0, engine.WorldWidth);
            startY = Clamp(startY, 0, engine.MapDepth);
            targetX = Clamp(targetX, 0, engine.WorldWidth);
            targetY = Clamp(targetY, 0, engine.MapDepth);

            if (spawnProjectile)
            {
                engine.SpawnProjectileAt(
                    startX,
                    startY,
                    targetX,
                    targetY,
                    damage,
                    attackType,
                    projectileType,
                    TeamType.Human,
                    impactRadius,
                    hitAllTeams,
                    projectileKey);
            }

            if (spawnEffect)
            {
                engine.AddVisualEffect(effectKey, targetX, targetY, effectLife);
            }
        }

        private void GetAnchor(GameEngine engine, out double x, out double y)
        {
            var list = engine.Entities;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e != null && e.IsAlive && e.Team == TeamType.Human)
                {
                    x = e.X;
                    y = e.Y;
                    return;
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e != null && e.IsAlive)
                {
                    x = e.X;
                    y = e.Y;
                    return;
                }
            }

            x = engine.WorldWidth * 0.5;
            y = engine.MapDepth * 0.5;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static double RandomRange(float min, float max)
        {
            return min + (max - min) * Rng.NextDouble();
        }
    }
}
