using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using PreLuna.EnemyHealthBar.Api;

namespace PreLuna.EnemyHealthBar.Runtime
{
    [BepInPlugin(Id, "PreLuna Enemy Health Bar", Version)]
    public sealed class Plugin : BasePlugin
    {
        public const string Id = "PreLuna.EnemyHealthBar", Version = "0.3.0";
        internal static Plugin Instance;
        internal ManualLogSource LogSource;
        EnemyHudRuntime runtime;
        Harmony harmony;

        public override void Load()
        {
            Instance = this;
            LogSource = Log;
            try
            {
                var config = new EnemyHudConfig(Config);
                ClassInjector.RegisterTypeInIl2Cpp<EnemyHudRuntime>();
                runtime = AddComponent<EnemyHudRuntime>();
                runtime.Initialize(config);
                harmony = new Harmony(Id);
                Patch(typeof(BulletDamagePatch), "bullet");
                Patch(typeof(MeleeDamagePatch), "melee");
                LogSource.LogInfo("HUD loaded; ASCII labels; toggle=" + config.ToggleKey + ", maxDistance=" + config.MaxDistance);
            }
            catch (System.Exception error)
            {
                LogSource.LogError("HUD failed to load: " + error);
                throw;
            }
        }

        void Patch(System.Type type, string label)
        {
            try
            {
                harmony.PatchAll(type);
                LogSource.LogInfo("local-hit " + label + " patch applied.");
            }
            catch (System.Exception error)
            {
                LogSource.LogError("local-hit " + label + " patch failed; focus bar still works: " + error);
            }
        }

        public override bool Unload()
        {
            try { harmony?.UnpatchSelf(); } catch { }
            harmony = null;
            EnemyHitApi.Clear();
            var current = runtime; runtime = null;
            if (current != null) UnityEngine.Object.Destroy(current);
            if (ReferenceEquals(Instance, this)) Instance = null;
            return true;
        }
    }
}
