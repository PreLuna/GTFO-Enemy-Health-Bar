using System;

namespace PreLuna.EnemyHealthBar.Core
{
    public static class EnemyBarMath
    {
        public static float Fraction(float value, float max)
        {
            if (!float.IsFinite(value) || !float.IsFinite(max) || max <= 0f) return 0f;
            return Math.Clamp(value / max, 0f, 1f);
        }

        public static float TrailStep(float ghost, float current, float dt, float seconds)
        {
            if (!float.IsFinite(ghost) || ghost < current) return current;
            if (!float.IsFinite(dt) || dt <= 0f) return ghost;
            if (!float.IsFinite(seconds) || seconds <= 0f) return current;
            float step = dt / seconds;
            if (step >= ghost - current) return current;
            return Math.Max(current, ghost - step);
        }

        public static float FadeAlpha(float now, float lastHit, float hold, float fade)
        {
            if (!float.IsFinite(now) || !float.IsFinite(lastHit)) return 0f;
            float age = now - lastHit;
            if (age <= hold) return 1f;
            if (!float.IsFinite(fade) || fade <= 0f) return 0f;
            return Math.Clamp(1f - (age - hold) / fade, 0f, 1f);
        }
    }
}
