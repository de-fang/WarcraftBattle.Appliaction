using System;
using System.Collections.Generic;

namespace WarcraftBattle3D.Core
{
    public static class CombatRules
    {
        private static readonly Dictionary<AttackType, Dictionary<ArmorType, double>> DamageTable =
            new Dictionary<AttackType, Dictionary<ArmorType, double>>();

        static CombatRules()
        {
            LoadRules();
        }

        public static void LoadRules()
        {
            DamageTable.Clear();

            foreach (AttackType atk in Enum.GetValues(typeof(AttackType)))
            {
                if (!DamageTable.ContainsKey(atk))
                {
                    DamageTable[atk] = new Dictionary<ArmorType, double>();
                }

                foreach (ArmorType def in Enum.GetValues(typeof(ArmorType)))
                {
                    DamageTable[atk][def] = 1.0;
                }
            }

            SetRule(AttackType.Normal, ArmorType.Medium, 1.5);
            SetRule(AttackType.Normal, ArmorType.Fortified, 0.7);

            SetRule(AttackType.Pierce, ArmorType.Light, 2.0);
            SetRule(AttackType.Pierce, ArmorType.Heavy, 0.5);
            SetRule(AttackType.Pierce, ArmorType.Fortified, 0.35);

            SetRule(AttackType.Siege, ArmorType.Fortified, 1.5);
            SetRule(AttackType.Siege, ArmorType.Hero, 0.5);

            SetRule(AttackType.Magic, ArmorType.Heavy, 2.0);
            SetRule(AttackType.Magic, ArmorType.Hero, 0.5);
        }

        private static void SetRule(AttackType atk, ArmorType def, double multiplier)
        {
            if (!DamageTable.ContainsKey(atk))
            {
                DamageTable[atk] = new Dictionary<ArmorType, double>();
            }

            DamageTable[atk][def] = multiplier;
        }

        public static double GetMultiplier(AttackType atk, ArmorType def)
        {
            if (DamageTable.TryGetValue(atk, out var defTable))
            {
                if (defTable.TryGetValue(def, out double multiplier))
                {
                    return multiplier;
                }
            }

            return 1.0;
        }
    }
}
