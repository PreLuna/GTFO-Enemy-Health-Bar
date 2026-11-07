using System;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Core
{
    public static class DamageNumberMath
    {
        public static float ScreenScale(int screenHeight)
            => Math.Clamp(screenHeight / 1080f, .75f, 2.5f);

        public static bool IsExpired(float age, float lifetime)
            => lifetime <= 0f || age >= lifetime;

        public static float FadeAlpha(float age, float lifetime, float fadeStartFraction = .65f)
        {
            if (lifetime <= 0f || age >= lifetime) return 0f;
            if (age <= 0f) return 1f;
            float start = Math.Clamp(fadeStartFraction, 0f, .99f);
            float fadeStart = lifetime * start;
            if (age <= fadeStart) return 1f;
            return Math.Clamp(1f - (age - fadeStart) / (lifetime - fadeStart), 0f, 1f);
        }

        public static float RowOffset(int row, float lineHeight)
            => Math.Max(0, row) * Math.Max(0f, lineHeight);

        public static int TrimCount(int count, int maxRows)
            => count <= maxRows ? 0 : count - maxRows;

        public static bool ShouldMerge(float now, float lastSpawn, float windowSeconds)
            => windowSeconds > 0f && now - lastSpawn <= windowSeconds;
    }
}
