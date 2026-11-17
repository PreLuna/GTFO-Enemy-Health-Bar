using System;
using System.Collections.Generic;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Core
{
    internal static class BfvPolicy
    {
        internal const float DefaultMinorHealthThreshold = 120f;
        internal const float DefaultHeavyHealthThreshold = 1000f;
        internal const float DefaultHitMarkerSize = 64f;
        internal const float DefaultKillIconSize = 46f;
        internal const float DefaultKillIconOpacity = .85f;
        internal const float DefaultKillIconLifetime = 2.2f;
        internal const float DefaultKillIconGap = .35f;
        internal const int DefaultMaxKillIcons = 6;
        internal const float DefaultMergeWindowMs = 50f;
        internal const float DefaultVolume = .8f;
        internal const float DefaultMinIntervalSeconds = .05f;

        internal static BfvKillTier TierFor(float healthMax, float minorThreshold, float heavyThreshold)
        {
            var minor = Threshold(minorThreshold, DefaultMinorHealthThreshold);
            var heavy = Threshold(heavyThreshold, DefaultHeavyHealthThreshold);
            if (!Finite(healthMax) || healthMax <= 0f) return BfvKillTier.Minor;
            if (healthMax >= heavy) return BfvKillTier.Boss;
            if (healthMax >= minor) return BfvKillTier.Elite;
            return BfvKillTier.Minor;
        }

        // GTFO's eEnemyType: Weakling=0, Standard=1, Special=2,
        // MiniBoss=3, Boss=4. The data is often counter-intuitive (Striker_Boss
        // has only 120 HP but is type 4, while Tank is type 3 with 1000 HP), so
        // type-based tiering matches the game's own boss classification better
        // than HealthMax thresholds.
        internal static BfvKillTier TierForEnemyType(int enemyType)
        {
            if (enemyType <= 0) return BfvKillTier.Minor;
            if (enemyType <= 2) return BfvKillTier.Elite;
            return BfvKillTier.Boss;
        }

        internal static BfvTierSource ParseTierSource(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return BfvTierSource.EnemyType;
            var text = value.Trim();
            if (string.Equals(text, "Health", StringComparison.OrdinalIgnoreCase)) return BfvTierSource.Health;
            return BfvTierSource.EnemyType;
        }

        internal static float Threshold(float configured, float fallback)
        {
            if (!Finite(configured) || configured <= 0f) return fallback;
            if (configured < 1f) return 1f;
            if (configured > 1e9f) return 1e9f;
            return configured;
        }

        internal static BfvSoundStyle ParseStyle(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return BfvSoundStyle.Bf5;
            var text = value.Trim();
            if (string.Equals(text, "BF1", StringComparison.OrdinalIgnoreCase)) return BfvSoundStyle.Bf1;
            if (string.Equals(text, "BF6", StringComparison.OrdinalIgnoreCase)) return BfvSoundStyle.Bf6;
            return BfvSoundStyle.Bf5;
        }

        internal static string StyleToken(BfvSoundStyle style)
        {
            switch (style)
            {
                case BfvSoundStyle.Bf1: return "bf1";
                case BfvSoundStyle.Bf6: return "bf6";
                default: return "bf5";
            }
        }

        internal static string TierToken(BfvKillTier tier)
        {
            switch (tier)
            {
                case BfvKillTier.Elite: return "elite";
                case BfvKillTier.Boss: return "boss";
                default: return "minor";
            }
        }

        internal static string FlightToken(BfvFlight flight)
        {
            switch (flight)
            {
                case BfvFlight.Flying: return "flyer";
                case BfvFlight.ArmoredFlying: return "armor_flyer";
                default: return "";
            }
        }

        internal static string[] CueCandidates(
            BfvSoundStyle style, BfvHitCue cue, BfvKillTier tier, BfvFlight flight)
        {
            var list = new List<string>(6);
            if (cue != BfvHitCue.Kill)
            {
                if (cue == BfvHitCue.Weakspot)
                {
                    AddUnique(list, "audio/bf5/weakspot.wav");
                }
                else if (cue == BfvHitCue.Armor)
                {
                    AddUnique(list, "audio/bf5/armor.wav");
                }
                else
                {
                    if (style == BfvSoundStyle.Bf1) AddUnique(list, "audio/bf1/hit.wav");
                    AddUnique(list, "audio/bf5/hit.wav");
                }
                AddUnique(list, "audio/bf5/hit.wav");
                return list.ToArray();
            }

            var suffix = FlightToken(flight);
            var baseName = string.IsNullOrEmpty(suffix)
                ? "kill_" + TierToken(tier)
                : "kill_" + suffix + "_" + TierToken(tier);
            if (style != BfvSoundStyle.Bf5 && tier != BfvKillTier.Boss)
                AddUnique(list, "audio/" + StyleToken(style) + "/" + baseName + ".wav");
            AddUnique(list, "audio/bf5/" + baseName + ".wav");
            if (!string.IsNullOrEmpty(suffix))
                AddUnique(list, "audio/bf5/kill_" + TierToken(tier) + ".wav");
            if (tier != BfvKillTier.Minor)
                AddUnique(list, "audio/bf5/kill_minor.wav");
            AddUnique(list, "audio/bf5/hit.wav");
            return list.ToArray();
        }

        internal static string MarkerIconFile(BfvMarkerKind kind)
        {
            switch (kind)
            {
                case BfvMarkerKind.Normal: return "icons/hitmark_normal.png";
                case BfvMarkerKind.Weakspot: return "icons/hitmark_weakspot.png";
                case BfvMarkerKind.Armor: return "icons/hitmark_armor.png";
                case BfvMarkerKind.Kill: return "icons/hitmark_kill.png";
                default: return "";
            }
        }

        internal static string KillIconFile(BfvKillTier tier)
        {
            switch (tier)
            {
                case BfvKillTier.Elite: return "icons/kill_elite.png";
                case BfvKillTier.Boss: return "icons/kill_boss.png";
                default: return "icons/kill_minor.png";
            }
        }

        internal static BfvMarkerKind MarkerFor(bool weakspot, bool armor, bool kill, bool immune)
        {
            if (kill) return BfvMarkerKind.Kill;
            if (armor || immune) return BfvMarkerKind.Armor;
            if (weakspot) return BfvMarkerKind.Weakspot;
            return BfvMarkerKind.Normal;
        }

        internal static int MarkerPriority(BfvMarkerKind kind)
        {
            switch (kind)
            {
                case BfvMarkerKind.Kill: return 4;
                case BfvMarkerKind.Weakspot: return 3;
                case BfvMarkerKind.Armor: return 2;
                case BfvMarkerKind.Normal: return 1;
                default: return 0;
            }
        }

        internal static float MarkerLifetime(BfvMarkerKind kind)
            => kind == BfvMarkerKind.Kill ? .34f : .18f;

        internal static float MarkerScale(BfvMarkerKind kind)
        {
            switch (kind)
            {
                case BfvMarkerKind.Weakspot: return 1.18f;
                case BfvMarkerKind.Armor: return .92f;
                case BfvMarkerKind.Kill: return 1.35f;
                default: return 1f;
            }
        }

        internal static float MarkerAlpha(float age, float lifetime)
        {
            if (!Finite(age) || !Finite(lifetime) || lifetime <= 0f || age < 0f || age >= lifetime)
                return 0f;
            var value = 1f - age / lifetime;
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        internal static float IconAlpha(float age, float lifetime)
        {
            if (!Finite(age) || !Finite(lifetime) || lifetime <= 0f || age < 0f || age >= lifetime)
                return 0f;
            var fadeIn = Math.Min(.1f, lifetime * .25f);
            var fadeOut = Math.Min(.35f, lifetime * .5f);
            var alpha = 1f;
            if (fadeIn > 0f && age < fadeIn) alpha = age / fadeIn;
            var remaining = lifetime - age;
            if (fadeOut > 0f && remaining < fadeOut)
                alpha = Math.Min(alpha, remaining / fadeOut);
            if (alpha < 0f) return 0f;
            if (alpha > 1f) return 1f;
            return alpha;
        }

        internal static float ScreenScale(float screenHeight)
        {
            if (!Finite(screenHeight) || screenHeight <= 0f) return 1f;
            var value = screenHeight / 1080f;
            if (value < .75f) return .75f;
            if (value > 1.6f) return 1.6f;
            return value;
        }

        internal static float HitMarkerSize(float configured)
            => Clamp(configured, 16f, 200f, DefaultHitMarkerSize);

        internal static float KillIconSize(float configured)
            => Clamp(configured, 26f, 76f, DefaultKillIconSize);

        internal static float KillIconOpacity(float configured)
            => Clamp(configured, 0f, 1f, DefaultKillIconOpacity);

        internal static float KillIconLifetime(float configured)
            => Clamp(configured, .4f, 6f, DefaultKillIconLifetime);

        internal static float KillIconGap(float configured)
            => Clamp(configured, 0f, 2f, DefaultKillIconGap);

        internal static int MaxKillIcons(int configured)
        {
            if (configured <= 0) return DefaultMaxKillIcons;
            if (configured > 12) return 12;
            return configured;
        }

        internal static float MergeWindowSeconds(float configuredMs)
            => Clamp(configuredMs, 0f, 500f, DefaultMergeWindowMs) / 1000f;

        internal static float Volume(float configured)
            => Clamp(configured, 0f, 1f, DefaultVolume);

        internal static float MinIntervalSeconds(float configured)
            => Clamp(configured, .02f, 3f, DefaultMinIntervalSeconds);

        internal static bool ShouldMerge(float now, float lastSpawn, float windowSeconds)
        {
            if (!Finite(now) || !Finite(lastSpawn) || !Finite(windowSeconds) || windowSeconds <= 0f)
                return false;
            var gap = now - lastSpawn;
            return gap >= 0f && gap <= windowSeconds;
        }

        internal static bool ShouldExpire(float age, float lifetime)
            => !Finite(age) || !Finite(lifetime) || lifetime <= 0f || age >= lifetime;

        internal static float Clamp(float value, float min, float max, float fallback)
        {
            if (!Finite(value)) return fallback;
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        internal static bool Finite(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value);

        static void AddUnique(List<string> list, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (!list.Contains(value)) list.Add(value);
        }
    }
}
