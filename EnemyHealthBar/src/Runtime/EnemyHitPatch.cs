using System;
using System.Collections.Generic;
using System.Reflection;
using Agents;
using Enemies;
using HarmonyLib;
using Player;

namespace PreLuna.EnemyHealthBar.Runtime
{
    // Local-player hits only. Reads health before/after the game's own damage call;
    // never writes health or sends network messages.
    [HarmonyPatch]
    internal static class EnemyHitPatch
    {
        static IntPtr hitBase;
        static float hitBefore;

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.BulletDamage));
            yield return AccessTools.DeclaredMethod(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.MeleeDamage));
        }

        static void Prefix(Dam_EnemyDamageBase __instance, Agent __1)
        {
            try
            {
                hitBase = IntPtr.Zero;
                if (__instance == null || __1 == null) return;
                var local = PlayerManager.GetLocalPlayerAgent();
                if (local == null || local.Pointer != __1.Pointer) return;
                hitBase = __instance.Pointer;
                hitBefore = __instance._Health_k__BackingField;
            }
            catch (Exception error)
            {
                hitBase = IntPtr.Zero;
                Plugin.Instance?.LogSource?.LogWarning("local hit prefix failed: " + error.Message);
            }
        }

        static void Postfix(Dam_EnemyDamageBase __instance)
        {
            try
            {
                if (hitBase == IntPtr.Zero || __instance == null || __instance.Pointer != hitBase) return;
                hitBase = IntPtr.Zero;
                float before = hitBefore;
                float after = __instance._Health_k__BackingField;
                float max = __instance._HealthMax_k__BackingField;
                if (before <= 0f || after >= before) return;
                EnemyHudRuntime.Current?.RegisterHit(__instance, before, after, max);
            }
            catch (Exception error)
            {
                hitBase = IntPtr.Zero;
                Plugin.Instance?.LogSource?.LogWarning("local hit postfix failed: " + error.Message);
            }
        }
    }
}
