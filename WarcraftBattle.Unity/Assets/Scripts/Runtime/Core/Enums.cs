namespace WarcraftBattle3D.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        Over
    }

    public enum UnitType
    {
        Melee,
        Ranged,
        Siege,
        Support
    }

    public enum UnitState
    {
        Move,
        Attack,
        Idle,
        Die,
        AttackMove
    }

    public enum ArmorType
    {
        Light,
        Medium,
        Heavy,
        Fortified,
        Hero,
        Unarmored
    }

    public enum AttackType
    {
        Normal,
        Pierce,
        Siege,
        Magic,
        Hero
    }

    public enum TeamType
    {
        Neutral,
        Human,
        Orc
    }
}
