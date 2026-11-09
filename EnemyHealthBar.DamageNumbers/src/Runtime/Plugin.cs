using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using PreLuna.EnemyHealthBar.DamageNumbers.Control;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Runtime
{
    [BepInPlugin(Id, "PreLuna Enemy Health Bar Damage Numbers", Version)]
    [BepInDependency("PreLuna.EnemyHealthBar", "0.4.0")]
    public sealed class Plugin : BasePlugin
    {
        public const string Id = "PreLuna.EnemyHealthBar.DamageNumbers", Version = "0.1.1";
        internal static Plugin Instance;
        internal ManualLogSource LogSource;
        DamageNumberRuntime runtime;
        internal DamageNumberConfig ConfigState;
        internal DamageNumberControlState ControlState;
        readonly object controlGate = new object();

        public override void Load()
        {
            Instance = this;
            LogSource = Log;
            try
            {
                var config = new DamageNumberConfig(Config);
                ConfigState = config;
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

        internal DamageNumberControlState EnsureControlState()
        {
            lock (controlGate)
            {
                if (ControlState == null)
                {
                    ControlState = new DamageNumberControlState(Config.ConfigFilePath, ReadControl, WriteControl, SerializeControl);
                    Config.SaveOnConfigSet = false;
                }
                return ControlState;
            }
        }

        internal static DamageNumberControlValue ReadControl(string key)
        {
            if (Instance?.ConfigState == null) return null;
            var config = Instance.ConfigState;
            switch (key)
            {
                case "enabled": return DamageNumberControlValue.Boolean(config.EnabledEntry.Value);
                case "maxRows": return DamageNumberControlValue.Number(config.MaxRowsEntry.Value);
                case "lifetime": return DamageNumberControlValue.Number(config.LifetimeEntry.Value);
                case "fontSize": return DamageNumberControlValue.Number(config.FontSizeEntry.Value);
                case "maxDistance": return DamageNumberControlValue.Number(config.MaxDistanceEntry.Value);
                case "showArmor": return DamageNumberControlValue.Boolean(config.ShowArmorEntry.Value);
                default: return null;
            }
        }

        internal static void WriteControl(string key, DamageNumberControlValue value)
        {
            if (Instance?.ConfigState == null || value == null) return;
            var config = Instance.ConfigState;
            if (key == "enabled" && value.TryGetBoolean(out var enabled)) { config.EnabledEntry.Value = enabled; return; }
            if (key == "showArmor" && value.TryGetBoolean(out var armor)) { config.ShowArmorEntry.Value = armor; return; }
            if (!value.TryGetNumber(out var number)) return;
            switch (key)
            {
                case "maxRows": config.MaxRowsEntry.Value = (int)number; break;
                case "lifetime": config.LifetimeEntry.Value = (float)number; break;
                case "fontSize": config.FontSizeEntry.Value = (float)number; break;
                case "maxDistance": config.MaxDistanceEntry.Value = (float)number; break;
            }
        }

        internal static string SerializeControl(string key)
        {
            var value = ReadControl(key);
            if (value == null) return null;
            if (value.TryGetBoolean(out var boolean)) return boolean ? "true" : "false";
            if (value.TryGetNumber(out var number)) return number.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            return null;
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
