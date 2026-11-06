using System;
using PreLuna.EnemyHealthBar.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Runtime
{
    public readonly record struct EnemyHudState(
        string Name,
        EnemyTypeKind Type,
        bool Alive,
        float Size,
        float Health,
        float HealthMax,
        float Hard,
        float HardMax,
        float GlueRel,
        float GlueVolume,
        float GlueTolerance,
        bool GlueStuck,
        bool GlueImmune,
        bool OnFire,
        float Heat,
        bool CanBePushed,
        bool Tagged,
        int WeakCount,
        float WeakMultiplier,
        float Distance,
        Vector3 WorldAnchor,
        Vector2 ScreenAnchor,
        string RoleLabel,
        string TierText,
        bool BossOverride,
        int ExtraStatusSlots)
    {
        public bool HasHealth => float.IsFinite(Health) && float.IsFinite(HealthMax) && HealthMax > 0f;
        public float HealthFraction => EnemyBarMath.Fraction(Health, HealthMax);
        public bool HasHard => float.IsFinite(Hard) && float.IsFinite(HardMax) && HardMax > 0f;
        public float HardFraction => EnemyBarMath.Fraction(Hard, HardMax);
        public bool HasGlue => !GlueImmune && float.IsFinite(GlueRel);
        public float GlueFraction => HasGlue ? Math.Clamp(GlueRel, 0f, 1f) : 0f;
        public bool BossLike => BossOverride || EnemyTypeMapping.IsBossLike(Type);
        public string TypeLabel => !string.IsNullOrWhiteSpace(RoleLabel) ? RoleLabel : Type switch
        {
            EnemyTypeKind.Weakling => "WEAK",
            EnemyTypeKind.Standard => "STD",
            EnemyTypeKind.Special => "SPEC",
            EnemyTypeKind.MiniBoss => "MINI",
            EnemyTypeKind.Boss => "BOSS",
            _ => "?"
        };
    }
}
