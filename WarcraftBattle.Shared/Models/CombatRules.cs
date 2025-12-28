using System;
using System.Collections.Generic;
using WarcraftBattle.Shared.Enums;

namespace WarcraftBattle.Shared.Models
{
    public static class CombatRules
    {
        // 使用字典存储伤害系数表：[攻击类型][护甲类型] = 系数
        private static Dictionary<AttackType, Dictionary<ArmorType, double>> _damageTable = new Dictionary<AttackType, Dictionary<ArmorType, double>>();

        static CombatRules()
        {
            // 静态构造函数自动初始化默认规则
            LoadRules();
        }

        /// <summary>
        /// 加载伤害规则。
        /// 目前是硬编码初始化，未来可以改为读取配置文件。
        /// </summary>
        public static void LoadRules()
        {
            _damageTable.Clear();

            // 1. 初始化默认值：所有组合默认为 1.0 (100% 伤害)
            foreach (AttackType atk in Enum.GetValues(typeof(AttackType)))
            {
                if (!_damageTable.ContainsKey(atk))
                    _damageTable[atk] = new Dictionary<ArmorType, double>();

                foreach (ArmorType def in Enum.GetValues(typeof(ArmorType)))
                {
                    _damageTable[atk][def] = 1.0;
                }
            }

            // 2. 应用特定规则 (还原原有的 if-else 逻辑)
            
            // Normal Attack
            SetRule(AttackType.Normal, ArmorType.Medium, 1.5);
            SetRule(AttackType.Normal, ArmorType.Fortified, 0.7);

            // Pierce Attack
            SetRule(AttackType.Pierce, ArmorType.Light, 2.0);
            SetRule(AttackType.Pierce, ArmorType.Heavy, 0.5);
            SetRule(AttackType.Pierce, ArmorType.Fortified, 0.35);

            // Siege Attack
            SetRule(AttackType.Siege, ArmorType.Fortified, 1.5);
            SetRule(AttackType.Siege, ArmorType.Hero, 0.5);

            // Magic Attack
            SetRule(AttackType.Magic, ArmorType.Heavy, 2.0);
            SetRule(AttackType.Magic, ArmorType.Hero, 0.5);

            // Hero Attack 默认为 1.0，无需额外设置
        }

        private static void SetRule(AttackType atk, ArmorType def, double multiplier)
        {
            if (!_damageTable.ContainsKey(atk))
                _damageTable[atk] = new Dictionary<ArmorType, double>();
            
            _damageTable[atk][def] = multiplier;
        }

        /// <summary>
        /// 获取伤害系数
        /// </summary>
        public static double GetMultiplier(AttackType atk, ArmorType def)
        {
            if (_damageTable.TryGetValue(atk, out var defTable))
            {
                if (defTable.TryGetValue(def, out double multiplier))
                {
                    return multiplier;
                }
            }
            // 兜底返回 1.0
            return 1.0;
        }
    }
}
