using System;
using System.Reflection;
using Agents;
using Enemies;
using HarmonyLib;
using Player;
using PreLuna.EnemyHealthBar.Api;
using PreLuna.EnemyHealthBar.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Runtime
{
    // Local-player hits only. Captures the full damage call, publishes the fact
    // for sub-mods, and keeps the existing recent-bar registration unchanged.
    internal static class EnemyHitCapture
    {
        static IntPtr pointer;
        static bool active;
        static bool melee;
        static float before;
        static float dam;
        static float precisionMulti;
        static float staggerMulti;
        static float hardBefore;
        static int limbId;
        static Vector3 position;
        static Vector3 direction;
        static bool logged;

        internal static void Begin(
            Dam_EnemyDamageBase damage,
            Agent source,
            float damageAmount,
            float precision,
            float stagger,
            int limb,
            Vector3 hitPosition,
            Vector3 hitDirection,
            bool isMelee)
        {
            Reset();
            if (damage == null || source == null) return;
            try
            {
                var local = PlayerManager.GetLocalPlayerAgent();
                if (local == null || local.Pointer != source.Pointer) return;
                float health = damage._Health_k__BackingField;
                if (!float.IsFinite(health) || health <= 0f) return;
                pointer = damage.Pointer;
                before = health;
                dam = damageAmount;
                precisionMulti = precision;
                staggerMulti = stagger;
                try { hardBefore = damage.m_damBuildToHitreact; } catch { hardBefore = -1f; }
                limbId = limb;
                position = hitPosition;
                direction = hitDirection;
                melee = isMelee;
                active = true;
            }
            catch (Exception error)
            {
                Reset();
                Warn("prefix", error);
            }
        }

        internal static void End(Dam_EnemyDamageBase damage)
        {
            if (!active) return;
            if (damage == null || damage.Pointer != pointer)
            {
                Reset();
                return;
            }
            active = false;
            try
            {
                float after = damage._Health_k__BackingField;
                float max = damage._HealthMax_k__BackingField;
                if (!float.IsFinite(after) || after > before) return;

                var owner = damage.Owner;
                if (owner == null) return;

                bool immune = damage.IsImortal;
                bool kill = after <= 0f;
                var limbs = damage.DamageLimbs;
                var type = eLimbDamageType.Normal;
                float limbMulti = 1f;
                if (limbs != null && limbId >= 0 && limbId < limbs.Length)
                {
                    var limb = limbs[limbId];
                    if (limb != null)
                    {
                        type = limb.m_type;
                        if (type == eLimbDamageType.Weakspot)
                            limbMulti = Mathf.Max(.01f, limb.m_weakspotDamageMulti);
                        else if (type == eLimbDamageType.Armor)
                            limbMulti = Mathf.Max(.01f, limb.m_armorDamageMulti);
                    }
                }

                bool weakspot = type == eLimbDamageType.Weakspot || (!melee && precisionMulti > 1.01f);
                bool armor = type == eLimbDamageType.Armor;
                float applied = Mathf.Max(0f, before - after);
                float raw = Mathf.Max(0f, dam * precisionMulti * limbMulti);
                if (!float.IsFinite(raw)) raw = applied;

                float hardMax = -1f;
                try
                {
                    var balancing = owner.EnemyBalancingData;
                    if (balancing != null) hardMax = balancing.Health.DamageUntilHitreact;
                }
                catch { }
                bool hardTrigger = EnemyBarMath.HardTrigger(hardBefore, applied, staggerMulti, hardMax);

                // Back-bonus coloring is intentionally off until P0 verifies the
                // direction sign convention. The field stays in the API for later.
                bool back = false;

                Vector3 hit = position;
                if (!IsFinite(hit) || hit.sqrMagnitude < .000001f) hit = SafeEye(owner);
                if (!IsFinite(hit)) return;

                var info = new EnemyHitInfo(
                    damage.Pointer, hit, direction, applied, raw,
                    before, after, max, weakspot, armor, back, kill, immune, limbId);
                EnemyHitApi.Publish(info);

                if (applied > 0f)
                    EnemyHudRuntime.Current?.RegisterHit(damage, before, after, max, hardTrigger);

                if (!logged)
                {
                    logged = true;
                    Plugin.Instance?.LogSource?.LogInfo(
                        "P3 first local hit: applied=" + applied + " raw=" + raw +
                        " before=" + before + " after=" + after + " max=" + max +
                        " limb=" + limbId + " type=" + type + " weakspot=" + weakspot +
                        " armor=" + armor + " kill=" + kill + " immune=" + immune);
                }
            }
            catch (Exception error)
            {
                Warn("postfix", error);
            }
            finally
            {
                Reset();
            }
        }

        static Vector3 SafeEye(EnemyAgent owner)
        {
            try { return owner.EyePosition; }
            catch { return Vector3.zero; }
        }

        static bool IsFinite(Vector3 value)
            => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static void Reset()
        {
            active = false;
            pointer = IntPtr.Zero;
            before = 0f;
            dam = 0f;
            precisionMulti = 1f;
            staggerMulti = 0f;
            hardBefore = 0f;
            limbId = -1;
            position = default;
            direction = default;
            melee = false;
        }

        static void Warn(string phase, Exception error)
            => Plugin.Instance?.LogSource?.LogWarning("local hit " + phase + " failed: " + error.Message);
    }

    [HarmonyPatch]
    internal static class BulletDamagePatch
    {
        static MethodBase TargetMethod()
            => AccessTools.DeclaredMethod(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.BulletDamage));

        [HarmonyPrefix]
        static void Prefix(
            Dam_EnemyDamageBase __instance,
            float dam,
            Agent sourceAgent,
            Vector3 position,
            Vector3 direction,
            Vector3 normal,
            bool allowDirectionalBonus,
            int limbID,
            float staggerMulti,
            float precisionMulti,
            uint gearCategoryId)
            => EnemyHitCapture.Begin(__instance, sourceAgent, dam, precisionMulti, staggerMulti, limbID, position, direction, false);

        [HarmonyPostfix]
        static void Postfix(Dam_EnemyDamageBase __instance)
            => EnemyHitCapture.End(__instance);
    }

    [HarmonyPatch]
    internal static class MeleeDamagePatch
    {
        static MethodBase TargetMethod()
            => AccessTools.DeclaredMethod(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.MeleeDamage));

        [HarmonyPrefix]
        static void Prefix(
            Dam_EnemyDamageBase __instance,
            float dam,
            Agent sourceAgent,
            Vector3 position,
            Vector3 direction,
            int limbID,
            float staggerMulti,
            float precisionMulti,
            float backstabberMulti,
            float sleeperMulti,
            bool skipLimbDestruction,
            DamageNoiseLevel damageNoiseLevel,
            uint gearCategoryId)
            => EnemyHitCapture.Begin(__instance, sourceAgent, dam, precisionMulti, staggerMulti, limbID, position, direction, true);

        [HarmonyPostfix]
        static void Postfix(Dam_EnemyDamageBase __instance)
            => EnemyHitCapture.End(__instance);
    }
}
