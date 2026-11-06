using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace PreLuna.EnemyHealthBar.Runtime
{
    [BepInPlugin(Id, "PreLuna Enemy Health Bar", Version)]
    public sealed class Plugin : BasePlugin
    {
        public const string Id = "PreLuna.EnemyHealthBar", Version = "0.2.0";
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
                try
                {
                    harmony = new Harmony(Id);
                    harmony.PatchAll(typeof(EnemyHitPatch));
                    LogSource.LogInfo("P2 local-hit patches applied.");
                }
                catch (System.Exception patchError)
                {
                    LogSource.LogError("P2 local-hit patches failed; focus bar still works: " + patchError);
                }
                LogSource.LogInfo("HUD loaded; ASCII labels; toggle=" + config.ToggleKey + ", maxDistance=" + config.MaxDistance);
            }
            catch (System.Exception error)
            {
                LogSource.LogError("P1 HUD failed to load: " + error);
                throw;
            }
        }

        public override bool Unload()
        {
            try { harmony?.UnpatchSelf(); } catch { }
            harmony = null;
            var current = runtime; runtime = null;
            if (current != null) UnityEngine.Object.Destroy(current);
            if (ReferenceEquals(Instance, this)) Instance = null;
            return true;
        }
    }
}
