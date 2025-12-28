namespace WarcraftBattle.Shared.Models
{
    public class SkillEffectDef
    {
        public string Type;        // 效果类型: "Damage", "Heal", "Visual", "Projectile", "Buff"
        public string TargetType;  // 目标类型: "Self", "Target", "AreaEnemy", "AreaAlly"
        public double Value;       // 数值 (伤害量/治疗量)
        public double Range;       // 施法距离或判定距离
        public double Radius;      // 范围半径 (AOE用)
        public double Duration;    // 持续时间 (Buff/眩晕用)
        public string VisualKey;   // 播放的特效名称 (如 "Explosion")
        public string ProjectileId;// 如果是投射物，投射物的配置ID
    }
}