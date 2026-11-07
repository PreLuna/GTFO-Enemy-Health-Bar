using System;
using PreLuna.EnemyHealthBar.Runtime;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Api
{
    // Read-only fact published after a local-player damage call. The health bar
    // does not render anything from this struct; sub-mods can subscribe.
    public readonly struct EnemyHitInfo
    {
        public EnemyHitInfo(
            IntPtr enemy,
            Vector3 position,
            Vector3 direction,
            float appliedDamage,
            float rawDamage,
            float healthBefore,
            float healthAfter,
            float healthMax,
            bool weakspot,
            bool armor,
            bool backBonus,
            bool kill,
            bool immune,
            int limbId)
        {
            Enemy = enemy;
            Position = position;
            Direction = direction;
            AppliedDamage = appliedDamage;
            RawDamage = rawDamage;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
            HealthMax = healthMax;
            Weakspot = weakspot;
            Armor = armor;
            BackBonus = backBonus;
            Kill = kill;
            Immune = immune;
            LimbId = limbId;
        }

        public IntPtr Enemy { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float AppliedDamage { get; }
        public float RawDamage { get; }
        public float HealthBefore { get; }
        public float HealthAfter { get; }
        public float HealthMax { get; }
        public bool Weakspot { get; }
        public bool Armor { get; }
        public bool BackBonus { get; }
        public bool Kill { get; }
        public bool Immune { get; }
        public int LimbId { get; }
    }

    public static class EnemyHitApi
    {
        public static event Action<EnemyHitInfo> HitRegistered;

        // Mirrors the parent HUD's PageDown/Enabled state. Sub-mods should hide
        // their overlays when the health bar itself is hidden.
        public static bool HudVisible { get; internal set; }

        internal static void Publish(in EnemyHitInfo info)
        {
            var handlers = HitRegistered;
            if (handlers == null) return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<EnemyHitInfo>)handler)(info);
                }
                catch (Exception error)
                {
                    Plugin.Instance?.LogSource?.LogWarning(
                        "enemy hit listener failed: " + error.Message);
                }
            }
        }

        internal static void Clear()
        {
            HitRegistered = null;
            HudVisible = false;
        }
    }
}
