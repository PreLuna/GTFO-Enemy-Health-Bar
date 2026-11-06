using System;
using System.Collections.Generic;

namespace PreLuna.EnemyHealthBar.Core
{
    // Display-only extension point for custom enemies / integration packs.
    // Key is EnemyDataID (preferred) or EnemyDataBlock.name.
    public readonly record struct EnemyHudProfile(
        string Key,
        string DisplayName,
        string Role,
        string TierText,
        bool? IsBoss,
        float AnchorOffsetY,
        int ExtraStatusSlots)
    {
        public bool IsValid => !string.IsNullOrWhiteSpace(Key);
    }

    public static class EnemyHudProfileRegistry
    {
        static readonly Dictionary<string, EnemyHudProfile> Profiles = new(StringComparer.OrdinalIgnoreCase);

        public static bool Register(in EnemyHudProfile profile)
        {
            if (!profile.IsValid) return false;
            Profiles[profile.Key.Trim()] = profile;
            return true;
        }

        public static bool Unregister(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            return Profiles.Remove(key.Trim());
        }

        public static bool TryGet(string key, out EnemyHudProfile profile)
        {
            profile = default;
            if (string.IsNullOrWhiteSpace(key)) return false;
            return Profiles.TryGetValue(key.Trim(), out profile);
        }

        public static int Count => Profiles.Count;

        public static void Clear() => Profiles.Clear();
    }
}
