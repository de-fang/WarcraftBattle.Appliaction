using System.Windows.Media;

namespace WarcraftBattle.Views.Controls
{
    internal static class ThemePalette
    {
        public static readonly Color AccentColor = Color.FromRgb(74, 163, 255);
        public static readonly Color AccentSoftColor = Color.FromRgb(122, 199, 255);
        public static readonly Color HighlightColor = Color.FromRgb(246, 193, 92);
        public static readonly Color SuccessColor = Color.FromRgb(107, 209, 118);
        public static readonly Color DangerColor = Color.FromRgb(255, 98, 98);
        public static readonly Color WarningColor = Color.FromRgb(255, 179, 71);
        public static readonly Color InfoColor = Color.FromRgb(112, 204, 255);
        public static readonly Color NeutralColor = Color.FromRgb(160, 167, 176);

        public static readonly Color TeamHumanColor = Color.FromRgb(92, 176, 255);
        public static readonly Color TeamOrcColor = Color.FromRgb(236, 96, 84);
        public static readonly Color BuildingHumanColor = Color.FromRgb(140, 210, 255);
        public static readonly Color BuildingOrcColor = Color.FromRgb(255, 156, 107);
        public static readonly Color LootColor = Color.FromRgb(246, 193, 92);

        public static readonly Brush AccentBrush = new SolidColorBrush(AccentColor);
        public static readonly Brush AccentSoftBrush = new SolidColorBrush(AccentSoftColor);
        public static readonly Brush HighlightBrush = new SolidColorBrush(HighlightColor);
        public static readonly Brush SuccessBrush = new SolidColorBrush(SuccessColor);
        public static readonly Brush DangerBrush = new SolidColorBrush(DangerColor);
        public static readonly Brush WarningBrush = new SolidColorBrush(WarningColor);
        public static readonly Brush InfoBrush = new SolidColorBrush(InfoColor);
        public static readonly Brush NeutralBrush = new SolidColorBrush(NeutralColor);

        public static readonly Brush TeamHumanBrush = new SolidColorBrush(TeamHumanColor);
        public static readonly Brush TeamOrcBrush = new SolidColorBrush(TeamOrcColor);
        public static readonly Brush BuildingHumanBrush = new SolidColorBrush(BuildingHumanColor);
        public static readonly Brush BuildingOrcBrush = new SolidColorBrush(BuildingOrcColor);
        public static readonly Brush LootBrush = new SolidColorBrush(LootColor);

        public static readonly Brush HpFriendlyBrush = new SolidColorBrush(SuccessColor);
        public static readonly Brush HpEnemyBrush = new SolidColorBrush(DangerColor);
        public static readonly Brush BloodBrush = new SolidColorBrush(Color.FromArgb(220, 200, 40, 40));
        public static readonly Brush SparkBrush = new SolidColorBrush(Color.FromArgb(220, 255, 170, 80));

        public static readonly Pen SelectionPen = new Pen(SuccessBrush, 2);
        public static readonly Pen MinimapViewportPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 230, 230, 230)), 1);

        static ThemePalette()
        {
            AccentBrush.Freeze();
            AccentSoftBrush.Freeze();
            HighlightBrush.Freeze();
            SuccessBrush.Freeze();
            DangerBrush.Freeze();
            WarningBrush.Freeze();
            InfoBrush.Freeze();
            NeutralBrush.Freeze();
            TeamHumanBrush.Freeze();
            TeamOrcBrush.Freeze();
            BuildingHumanBrush.Freeze();
            BuildingOrcBrush.Freeze();
            LootBrush.Freeze();
            HpFriendlyBrush.Freeze();
            HpEnemyBrush.Freeze();
            BloodBrush.Freeze();
            SparkBrush.Freeze();
            SelectionPen.Freeze();
            MinimapViewportPen.Freeze();
        }
    }
}
