using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace WarcraftBattle.Models
{
    public class UnitInfoModel
    {
        public Engine.Entity Entity { get; set; }
        public ImageSource Icon { get; set; }
        public bool IsSelected { get; set; }
        public double HpPct { get; set; }
        public Brush HpColor { get; set; }
        public Action<object> OnClick { get; set; }

        public void Execute()
        {
            OnClick?.Invoke(this);
        }
    }
}
