using System;
using System.Windows.Media;

namespace WarcraftBattle.Shared.Models
{
    // 通用按钮模型 (用于建造、生产单位)
    public class ActionButtonModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public bool IsUnit { get; set; }
        public bool IsMax { get; set; }
        public ImageSource Icon { get; set; } // 图标
        public string Hotkey { get; set; } // Hotkey display

        public Action<object> OnClick { get; set; }

        public bool CanAfford { get; set; } = true;

        // [修复] 添加 Execute 方法供 XAML 调用
        public void Execute()
        {
            OnClick?.Invoke(this);
        }

        public void UpdateState(double currentGold)
        {
            CanAfford = currentGold >= Cost && !IsMax;
        }
    }

    // 商店物品模型
    public class ShopUnitItem
    {
        public string UnitKey { get; set; }
        public string Name { get; set; }
        public int CurrentLevel { get; set; }
        public bool IsSelected { get; set; }
        public ImageSource Icon { get; set; } // 图标

        public Action<object> OnClick { get; set; }

        public string LevelText => $"Lv.{CurrentLevel}";

        // [修复] 添加 Execute 方法
        public void Execute()
        {
            OnClick?.Invoke(this);
        }
    }

    // 技能按钮模型
    public class SkillButtonModel
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public double MaxCD { get; set; }
        public double CurrentCD { get; set; }
        public string Desc { get; set; }
        public Action<object> OnClick { get; set; }
        public bool IsCoolingDown => CurrentCD > 0;
        public bool CanCast { get; set; } = true;
        public string Hotkey { get; set; }

        // [修复] 添加 Execute 方法
        public void Execute()
        {
            if (CanCast) OnClick?.Invoke(this);
        }

        public void UpdateState(double currentMana)
        {
            CanCast = currentMana >= Cost && !IsCoolingDown;
        }
    }

    // 关卡选择模型
    public class StageItemModel
    {
        public int StageId { get; set; }
        public string Title { get; set; }
        public bool IsLocked { get; set; } // True = 锁定
        public Action<object> OnClick { get; set; }

        // [修复] 添加 Execute 方法
        public void Execute()
        {
            if (!IsLocked) OnClick?.Invoke(this);
        }
    }

    // 简单的浮动文字信息
  
}