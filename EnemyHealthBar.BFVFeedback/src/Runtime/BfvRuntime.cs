using System;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Runtime
{
    // The only IL2CPP-injected MonoBehaviour. Keep its method signatures to
    // primitives / Unity structs: ClassInjector walks every method and fails on
    // arbitrary managed parameter types. All feedback logic lives in BfvController.
    public sealed class BfvRuntime : MonoBehaviour
    {
        internal static BfvRuntime Current;
        BfvController controller;

        public BfvRuntime(IntPtr pointer) : base(pointer) { }

        internal void Initialize()
        {
            Current = this;
            try
            {
                var config = BfvBootstrap.Config;
                var root = BfvBootstrap.PluginRoot;
                if (config == null || string.IsNullOrEmpty(root))
                {
                    Plugin.Instance?.LogSource?.LogError("BFV bootstrap missing; feedback disabled");
                    return;
                }
                controller = new BfvController();
                controller.Initialize(config, root);
            }
            catch (Exception error)
            {
                Plugin.Instance?.LogSource?.LogError("BFV controller init failed: " + error);
            }
        }

        internal bool FeedbackActive => controller != null && controller.FeedbackActive;

        internal bool OnHitIndicator(Vector3 world, bool isWeakspot, bool isFriendly, bool noDamage)
            => controller != null && controller.OnHitIndicator(world, isWeakspot, isFriendly, noDamage);

        internal bool OnDeathIndicator(Vector3 world)
            => controller != null && controller.OnDeathIndicator(world);

        internal bool ShouldMuteIndicator(uint eventId)
            => controller != null && controller.ShouldMuteIndicator(eventId);

        void Update() { controller?.Update(); }

        void OnGUI() { controller?.OnGUI(); }

        void OnDestroy()
        {
            var current = controller;
            controller = null;
            try { current?.Dispose(); } catch { }
            if (ReferenceEquals(Current, this)) Current = null;
        }
    }

    internal static class BfvBootstrap
    {
        internal static BfvConfig Config;
        internal static string PluginRoot;

        internal static void Set(BfvConfig config, string pluginRoot)
        {
            Config = config;
            PluginRoot = pluginRoot;
        }

        internal static void Clear()
        {
            Config = null;
            PluginRoot = null;
        }
    }
}
