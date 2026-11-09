using PreLuna.EnemyHealthBar.Runtime;

namespace PreLuna.EnemyHealthBar.Control
{
    public static class EnemyHealthBarControlApi
    {
        public const string TargetId = EnemyHudControlState.TargetId;

        public static EnemyHudControlSnapshot GetSnapshot()
        {
            var control = Control;
            if (control != null) return control.ReadSnapshot();
            return EnemyHudControlState.Unavailable(
                Plugin.Instance == null ? EnemyHudControlStatus.Missing : EnemyHudControlStatus.Uninitialized);
        }

        public static EnemyHudControlResult Validate(EnemyHudControlChange change)
            => Control?.Validate(change) ?? Unavailable("敌人血条控制状态不可用");

        public static EnemyHudControlResult SetPersistent(EnemyHudControlChange change)
            => Control?.SetPersistent(change) ?? Unavailable("敌人血条控制状态不可用");

        static EnemyHudControlState Control => Plugin.Instance?.HudConfig?.Control;

        static EnemyHudControlResult Unavailable(string message)
            => new EnemyHudControlResult(
                EnemyHudControlOutcome.Unavailable,
                message,
                null,
                EnemyHudControlState.Unavailable(
                    Plugin.Instance == null ? EnemyHudControlStatus.Missing : EnemyHudControlStatus.Uninitialized));
    }
}
