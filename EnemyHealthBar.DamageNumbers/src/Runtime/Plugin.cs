using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Runtime
{
    [BepInPlugin(Id, "PreLuna Enemy Health Bar Damage Numbers", Version)]
    [BepInDependency("PreLuna.EnemyHealthBar", "0.3.0")]
    public sealed class Plugin : BasePlugin
    {
        public const string Id = "PreLuna.EnemyHealthBar.DamageNumbers", Version = "0.1.0";
        internal static Plugin Instance;
        internal ManualLogSource LogSource;
        DamageNumberRuntime runtime;

        public override void Load()
        {
            Instance = this;
            LogSource = Log;
            try
            {
                var config = new DamageNumberConfig(Config);
                ClassInjector.RegisterTypeInIl2Cpp<DamageNumberRuntime>();
                runtime = AddComponent<DamageNumberRuntime>();
                runtime.Initialize(config);
                WarnIfOldDamageNumbersLoaded();
                LogSource.LogInfo("Damage numbers loaded; font=system fallback" +
                    ", maxRows=" + config.MaxRows + ", mergeMs=" + (config.MergeWindowSeconds * 1000f).ToString() +
                    ", lifetime=" + config.Lifetime);
            }
            catch (System.Exception error)
            {
                LogSource.LogError("Damage numbers failed to load: " + error);
                throw;
            }
        }

        void WarnIfOldDamageNumbersLoaded()
        {
            try
            {
                if (IL2CPPChainloader.Instance.Plugins.ContainsKey("com.dak.DamageNumbers"))
                    LogSource.LogWarning("Old DamageNumbers (com.dak.DamageNumbers) is loaded. " +
                        "Disable/remove DamageNumbers.dll to avoid two damage-number layers.");
            }
            catch
            {
                // Old-mod detection is advisory only; never block loading.
            }
        }

        public override bool Unload()
        {
            var current = runtime;
            runtime = null;
            if (current != null) UnityEngine.Object.Destroy(current);
            if (ReferenceEquals(Instance, this)) Instance = null;
            return true;
        }
    }
}
