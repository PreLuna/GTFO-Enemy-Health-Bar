using System;
using Enemies;
using PreLuna.EnemyHealthBar.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Runtime
{
    internal static class EnemyVitalsReader
    {
        static IntPtr anchorPointer;
        static float anchorTopY = float.NegativeInfinity;
        static double anchorNextRefresh;

        internal static bool TryRead(EnemyAgent target, Camera camera, out EnemyHudState state)
        {
            state = default;
            if (target == null || camera == null || target.Damage == null || !target.Alive) return false;
            try
            {
                var damage = target.Damage;
                float health = damage.Health;
                float healthMax = damage.HealthMax;
                if (!float.IsFinite(health) || !float.IsFinite(healthMax) || healthMax <= 0f) return false;

                var data = target.EnemyData;
                var balancing = target.EnemyBalancingData;
                int typeValue = data == null ? -1 : (int)data.EnemyType;
                var type = EnemyTypeMapping.FromNumeric(typeValue);
                string name = data == null ? "?" : (data.name ?? "?");
                string role = "", tier = "";
                bool bossOverride = false;
                float anchorOffsetY = 0f;
                int extraStatusSlots = 0;
                EnemyHudProfile profile;
                bool hasProfile = EnemyHudProfileRegistry.TryGet(target.EnemyDataID.ToString(), out profile)
                    || (data != null && EnemyHudProfileRegistry.TryGet(data.name, out profile));
                if (hasProfile)
                {
                    if (!string.IsNullOrWhiteSpace(profile.DisplayName)) name = profile.DisplayName;
                    role = profile.Role ?? "";
                    tier = profile.TierText ?? "";
                    bossOverride = profile.IsBoss ?? false;
                    anchorOffsetY = profile.AnchorOffsetY;
                    extraStatusSlots = Math.Max(0, profile.ExtraStatusSlots);
                }
                float hard = damage.m_damBuildToHitreact;
                float hardMax = balancing == null ? -1f : balancing.Health.DamageUntilHitreact;
                float glueTolerance = balancing == null ? -1f : balancing.GlueTolerance;
                bool canPush = balancing == null || balancing.CanBePushed;
                float size = Mathf.Max(.5f, target.SizeMultiplier);

                int weakCount = 0;
                float weakMultiplier = 0f;
                var limbs = damage.DamageLimbs;
                if (limbs != null)
                    for (int i = 0; i < limbs.Length; i++)
                    {
                        var limb = limbs[i];
                        if (limb == null) continue;
                        if (limb.m_type == eLimbDamageType.Weakspot)
                        {
                            weakCount++;
                            if (limb.m_weakspotDamageMulti > weakMultiplier) weakMultiplier = limb.m_weakspotDamageMulti;
                        }
                    }

                Vector3 anchor = Anchor(target, size) + Vector3.up * anchorOffsetY;
                Vector3 screen = camera.WorldToScreenPoint(anchor);
                if (!float.IsFinite(screen.x) || !float.IsFinite(screen.y) || screen.z <= 0f) return false;
                var gui = new Vector2(screen.x, Screen.height - screen.y);
                if (!float.IsFinite(gui.x) || !float.IsFinite(gui.y)) return false;

                state = new EnemyHudState(
                    name, type, target.Alive, size,
                    health, healthMax, hard, hardMax,
                    damage.AttachedGlueRel, damage.AttachedGlueVolume, glueTolerance,
                    damage.IsStuckInGlue, glueTolerance >= 1_000_000f,
                    damage.OnFire, damage.m_heatLevel, canPush, target.IsTagged,
                    weakCount, weakMultiplier,
                    Vector3.Distance(camera.transform.position, target.transform.position),
                    anchor, gui, role, tier, bossOverride, extraStatusSlots);
                return true;
            }
            catch
            {
                state = default;
                return false;
            }
        }

        static Vector3 Anchor(EnemyAgent target, float size)
        {
            // Renderer bounds are the reliable top for big enemies whose highest bone
            // can sit in the middle of the visible body. Cache for 0.35s per focus.
            if (anchorPointer != target.Pointer || Time.time >= anchorNextRefresh || !float.IsFinite(anchorTopY))
            {
                anchorPointer = target.Pointer;
                anchorNextRefresh = Time.time + .35;
                anchorTopY = float.NegativeInfinity;
                try
                {
                    var renderers = target.GetComponentsInChildren<Renderer>(true);
                    if (renderers != null)
                        for (int i = 0; i < renderers.Length; i++)
                        {
                            var renderer = renderers[i];
                            if (renderer == null || !renderer.enabled) continue;
                            float top = renderer.bounds.max.y;
                            if (top > anchorTopY) anchorTopY = top;
                        }
                }
                catch { }

                if (!float.IsFinite(anchorTopY))
                {
                    float highest = float.NegativeInfinity;
                    var limbs = target.Damage == null ? null : target.Damage.DamageLimbs;
                    if (limbs != null)
                        for (int i = 0; i < limbs.Length; i++)
                        {
                            var limb = limbs[i];
                            if (limb == null || limb.transform == null) continue;
                            float y = limb.transform.position.y;
                            if (y > highest) highest = y;
                        }
                    if (float.IsFinite(highest)) anchorTopY = highest;
                    else
                    {
                        var model = target.MainModelTransform;
                        anchorTopY = model != null ? model.position.y + 1.55f * size : target.transform.position.y + 1.55f * size;
                    }
                }
            }

            Vector3 basePosition = target.transform.position;
            return new Vector3(basePosition.x, anchorTopY + .22f + .10f * size, basePosition.z);
        }
    }
}
