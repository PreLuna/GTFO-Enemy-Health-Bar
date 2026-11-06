namespace PreLuna.EnemyHealthBar.Api
{
    using PreLuna.EnemyHealthBar.Core;

    // Soft API for other mods: call RegisterProfile at load time if a custom
    // enemy needs a display name/tier/color/extra status slots.
    public static class EnemyHudApi
    {
        public static bool RegisterProfile(in EnemyHudProfile profile) => EnemyHudProfileRegistry.Register(profile);

        public static bool UnregisterProfile(string key) => EnemyHudProfileRegistry.Unregister(key);

        public static int ProfileCount => EnemyHudProfileRegistry.Count;
    }
}
