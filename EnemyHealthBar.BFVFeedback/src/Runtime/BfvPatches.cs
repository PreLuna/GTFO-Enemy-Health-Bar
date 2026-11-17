using System.Reflection;
using Agents;
using HarmonyLib;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Runtime
{
    [HarmonyPatch]
    internal static class BfvBulletImpactPatch
    {
        static MethodBase TargetMethod()
            => AccessTools.DeclaredMethod(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.BulletDamage));

        [HarmonyPrefix]
        static void Prefix(
            Dam_EnemyDamageBase __instance,
            Agent sourceAgent,
            Vector3 position,
            int limbID,
            float precisionMulti)
            => BfvImpactContext.Record(__instance, sourceAgent, position, limbID, precisionMulti, false);
    }

    [HarmonyPatch]
    internal static class BfvMeleeImpactPatch
    {
        static MethodBase TargetMethod()
            => AccessTools.DeclaredMethod(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.MeleeDamage));

        [HarmonyPrefix]
        static void Prefix(
            Dam_EnemyDamageBase __instance,
            Agent sourceAgent,
            Vector3 position,
            int limbID,
            float precisionMulti)
            => BfvImpactContext.Record(__instance, sourceAgent, position, limbID, precisionMulti, true);
    }

    [HarmonyPatch]
    internal static class BfvHitIndicatorPatch
    {
        static MethodBase TargetMethod()
            => AccessTools.Method(
                AccessTools.TypeByName("CrosshairGuiLayer"),
                "ShowHitIndicator",
                new[] { typeof(Vector3), typeof(bool), typeof(bool), typeof(bool) });

        [HarmonyPrefix]
        static bool Prefix(Vector3 worldPos, bool isWeakspot, bool isFriendly, bool noDamage)
        {
            try
            {
                var runtime = BfvRuntime.Current;
                return runtime == null || !runtime.OnHitIndicator(worldPos, isWeakspot, isFriendly, noDamage);
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch]
    internal static class BfvDeathIndicatorPatch
    {
        static MethodBase TargetMethod()
            => AccessTools.Method(
                AccessTools.TypeByName("CrosshairGuiLayer"),
                "ShowDeathIndicator",
                new[] { typeof(Vector3) });

        [HarmonyPrefix]
        static bool Prefix(Vector3 worldPos)
        {
            try
            {
                var runtime = BfvRuntime.Current;
                return runtime == null || !runtime.OnDeathIndicator(worldPos);
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch]
    internal static class BfvCellSoundPostPatch
    {
        static MethodBase TargetMethod()
            => AccessTools.Method(typeof(CellSound), "Post", new[] { typeof(uint) });

        [HarmonyPrefix]
        static bool Prefix(uint eventID)
        {
            try
            {
                var runtime = BfvRuntime.Current;
                return runtime == null || !runtime.ShouldMuteIndicator(eventID);
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch]
    internal static class BfvCellSoundPostPositionPatch
    {
        static MethodBase TargetMethod()
            => AccessTools.Method(typeof(CellSound), "Post", new[] { typeof(uint), typeof(Vector3) });

        [HarmonyPrefix]
        static bool Prefix(uint eventID)
        {
            try
            {
                var runtime = BfvRuntime.Current;
                return runtime == null || !runtime.ShouldMuteIndicator(eventID);
            }
            catch
            {
                return true;
            }
        }
    }
}
