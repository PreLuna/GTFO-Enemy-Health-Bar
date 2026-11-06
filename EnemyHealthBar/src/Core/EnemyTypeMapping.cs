using System;

namespace PreLuna.EnemyHealthBar.Core
{
    // Mirrors GameData.eEnemyType in GTFO. The game enum is global, so the runtime
    // converts it to its numeric value before this pure mapping is used.
    public enum EnemyTypeKind
    {
        Unknown = -1,
        Weakling = 0,
        Standard = 1,
        Special = 2,
        MiniBoss = 3,
        Boss = 4
    }

    public static class EnemyTypeMapping
    {
        public static EnemyTypeKind FromNumeric(int value) => value switch
        {
            0 => EnemyTypeKind.Weakling,
            1 => EnemyTypeKind.Standard,
            2 => EnemyTypeKind.Special,
            3 => EnemyTypeKind.MiniBoss,
            4 => EnemyTypeKind.Boss,
            _ => EnemyTypeKind.Unknown
        };

        public static bool IsBossLike(EnemyTypeKind kind) => kind is EnemyTypeKind.MiniBoss or EnemyTypeKind.Boss;

        public static string Label(EnemyTypeKind kind) => kind switch
        {
            EnemyTypeKind.Weakling => "Weakling",
            EnemyTypeKind.Standard => "Standard",
            EnemyTypeKind.Special => "Special",
            EnemyTypeKind.MiniBoss => "MiniBoss",
            EnemyTypeKind.Boss => "Boss",
            _ => "Unknown"
        };

        public static string RoleKey(EnemyTypeKind kind) => kind switch
        {
            EnemyTypeKind.Weakling => "weakling",
            EnemyTypeKind.Standard => "standard",
            EnemyTypeKind.Special => "special",
            EnemyTypeKind.MiniBoss => "miniboss",
            EnemyTypeKind.Boss => "boss",
            _ => "unknown"
        };
    }
}
