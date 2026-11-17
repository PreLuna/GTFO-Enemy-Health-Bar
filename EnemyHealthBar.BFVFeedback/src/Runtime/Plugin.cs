using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using PreLuna.EnemyHealthBar.BFVFeedback.Control;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Runtime
{
    [BepInPlugin(Id, "PreLuna Enemy Health Bar BFV Feedback", Version)]
    [BepInDependency("PreLuna.EnemyHealthBar", "0.4.0")]
    public sealed class Plugin : BasePlugin
    {
        public const string Id = "PreLuna.EnemyHealthBar.BFVFeedback";
        public const string Version = "0.2.0";

        internal static Plugin Instance;
        internal ManualLogSource LogSource;
        internal BfvConfig ConfigState;
        BfvRuntime runtime;
        Harmony harmony;
        BfvFeedbackControlState controlState;
        readonly object controlGate = new object();

        public override void Load()
        {
            Instance = this;
            LogSource = Log;
            try
            {
                ConfigState = new BfvConfig(Config);
                Config.SaveOnConfigSet = false;
                var controlSnapshot = BfvFeedbackControlApi.GetSnapshot();
                LogSource.LogInfo("BFV control=" + controlSnapshot.Status + "; settings=" +
                    controlSnapshot.Settings.Count + "; revision=" + controlSnapshot.Revision);
                var pluginRoot = ResolvePluginRoot();
                BfvBootstrap.Set(ConfigState, pluginRoot);
                ClassInjector.RegisterTypeInIl2Cpp<BfvRuntime>();
                runtime = AddComponent<BfvRuntime>();
                runtime.Initialize();
                harmony = new Harmony(Id);
                Patch(typeof(BfvBulletImpactPatch), "bullet-context");
                Patch(typeof(BfvMeleeImpactPatch), "melee-context");
                Patch(typeof(BfvHitIndicatorPatch), "hit-indicator");
                Patch(typeof(BfvDeathIndicatorPatch), "death-indicator");
                Patch(typeof(BfvCellSoundPostPatch), "cell-sound");
                Patch(typeof(BfvCellSoundPostPositionPatch), "cell-sound-position");
                LogSource.LogInfo("BFV feedback loaded; muteVanilla=" + ConfigState.MuteVanillaIndicator +
                    "; replaceMarker=" + ConfigState.ReplaceVanillaMarker +
                    "; style=" + ConfigState.SoundStyle + "; root=" + pluginRoot);
            }
            catch (Exception error)
            {
                LogSource.LogError("BFV feedback failed to load: " + error);
                throw;
            }
        }

        void Patch(Type type, string label)
        {
            try
            {
                harmony.PatchAll(type);
                LogSource.LogInfo("BFV " + label + " patch applied.");
            }
            catch (Exception error)
            {
                LogSource.LogWarning("BFV " + label + " patch failed; that layer keeps vanilla: " + error.Message);
            }
        }

        internal BfvFeedbackControlState EnsureControlState()
        {
            lock (controlGate)
            {
                if (controlState == null)
                {
                    controlState = new BfvFeedbackControlState(
                        Config.ConfigFilePath,
                        key => ConfigState.ReadControl(key),
                        (key, value) => ConfigState.WriteControl(key, value),
                        key => ConfigState.SerializeControl(key),
                        () => Config.SaveOnConfigSet,
                        value => Config.SaveOnConfigSet = value);
                }
                return controlState;
            }
        }

        string ResolvePluginRoot()
        {
            var localized = Path.Combine(Paths.PluginPath, "本地定制", "PreLuna.EnemyHealthBar.BFVFeedback");
            if (Directory.Exists(localized)) return localized;
            return Path.Combine(Paths.PluginPath, "PreLuna.EnemyHealthBar.BFVFeedback");
        }

        public override bool Unload()
        {
            try { harmony?.UnpatchSelf(); } catch { }
            harmony = null;
            BfvImpactContext.Clear();
            var current = runtime;
            runtime = null;
            if (current != null) UnityEngine.Object.Destroy(current);
            BfvBootstrap.Clear();
            ConfigState = null;
            controlState = null;
            if (ReferenceEquals(Instance, this)) Instance = null;
            return true;
        }
    }
}
