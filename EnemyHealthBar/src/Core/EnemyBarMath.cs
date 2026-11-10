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

        // True when a local hit's stagger contribution reaches the enemy's
        // hitreact threshold. The game may consume the value in the same damage
        // call, so this is computed from the pre-hit value and the damage call.
        public static bool HardTrigger(float hardBefore, float applied, float staggerMulti, float hardMax)
        {
            if (!float.IsFinite(hardBefore) || !float.IsFinite(applied)
                || !float.IsFinite(staggerMulti) || !float.IsFinite(hardMax)) return false;
            if (hardBefore < 0f || applied <= 0f || staggerMulti <= 0f || hardMax <= 0f) return false;
            float added = applied * staggerMulti;
            if (!float.IsFinite(added)) return false;
            return hardBefore + added >= hardMax;
        }
    }
}
