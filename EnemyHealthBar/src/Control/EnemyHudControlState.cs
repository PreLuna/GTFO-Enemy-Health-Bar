using System;
using System.Collections.Generic;

namespace PreLuna.EnemyHealthBar.Control
{
    public sealed class EnemyHudControlState
    {
        public const string TargetId = "PreLuna.EnemyHealthBar";
        public const string ControlVersion = "0.4.0";

        public static readonly IReadOnlyList<EnemyHudControlSetting> Definitions = new[]
        {
            new EnemyHudControlSetting("enabled", "总开关", EnemyHudControlKind.Boolean, EnemyHudControlValue.Boolean(true)),
            new EnemyHudControlSetting("maxDistance", "最大距离", EnemyHudControlKind.Number, EnemyHudControlValue.Number(60), 5, 300),
            new EnemyHudControlSetting("width", "条宽", EnemyHudControlKind.Number, EnemyHudControlValue.Number(200), 90, 420),
            new EnemyHudControlSetting("height", "条高", EnemyHudControlKind.Number, EnemyHudControlValue.Number(5), 2, 16),
            new EnemyHudControlSetting("opacity", "透明度", EnemyHudControlKind.Number, EnemyHudControlValue.Number(.55), .15, 1),
            new EnemyHudControlSetting("showName", "显示名称", EnemyHudControlKind.Boolean, EnemyHudControlValue.Boolean(true)),
            new EnemyHudControlSetting("showNumbers", "显示数值", EnemyHudControlKind.Boolean, EnemyHudControlValue.Boolean(true)),
            new EnemyHudControlSetting("showStatusSlots", "状态槽", EnemyHudControlKind.Boolean, EnemyHudControlValue.Boolean(true))
        };

        readonly Func<string, EnemyHudControlValue> readSaved;
        readonly Action<string, EnemyHudControlValue> writeSaved;
        readonly Action save;
        readonly Dictionary<string, EnemyHudControlValue> saved = new Dictionary<string, EnemyHudControlValue>(StringComparer.Ordinal);
        readonly Dictionary<string, EnemyHudControlValue> effective = new Dictionary<string, EnemyHudControlValue>(StringComparer.Ordinal);
        string revision = "1";

        public EnemyHudControlState(
            Func<string, EnemyHudControlValue> readSaved,
            Action<string, EnemyHudControlValue> writeSaved,
            Action save)
        {
            this.readSaved = readSaved ?? throw new ArgumentNullException(nameof(readSaved));
            this.writeSaved = writeSaved ?? throw new ArgumentNullException(nameof(writeSaved));
            this.save = save ?? throw new ArgumentNullException(nameof(save));

            foreach (var definition in Definitions)
            {
                var value = this.readSaved(definition.Key) ?? definition.DefaultValue;
                saved[definition.Key] = value;
                effective[definition.Key] = value;
            }
        }

        public string Revision => revision;

        public EnemyHudControlSnapshot ReadSnapshot()
        {
            RefreshSaved();
            return Snapshot();
        }

        public EnemyHudControlResult Validate(EnemyHudControlChange change)
        {
            var check = Check(change);
            if (check != null) return check;
            return new EnemyHudControlResult(EnemyHudControlOutcome.NoChanges, "没有改动", null, Snapshot());
        }

        public EnemyHudControlResult SetPersistent(EnemyHudControlChange change)
        {
            var check = Check(change, requireChanges: true);
            if (check != null) return check;

            var previous = SnapshotValues(change.Values.Keys);
            try
            {
                foreach (var pair in change.Values) writeSaved(pair.Key, pair.Value);
                save();
            }
            catch (Exception error)
            {
                RestoreValues(previous);
                return new EnemyHudControlResult(EnemyHudControlOutcome.SaveFailed, "保存失败：" + error.Message, null, Snapshot());
            }

            foreach (var pair in change.Values)
            {
                saved[pair.Key] = pair.Value;
                effective[pair.Key] = pair.Value;
            }

            BumpRevision();
            return new EnemyHudControlResult(EnemyHudControlOutcome.Success, "已保存并生效", null, Snapshot());
        }

        public bool GetEffectiveBoolean(string key)
            => effective.TryGetValue(key, out var value) && value.TryGetBoolean(out var boolean) && boolean;

        public double GetEffectiveNumber(string key)
            => effective.TryGetValue(key, out var value) && value.TryGetNumber(out var number) ? number : 0;

        public static EnemyHudControlSnapshot Unavailable(EnemyHudControlStatus status)
            => new EnemyHudControlSnapshot(
                TargetId,
                "PreLuna 敌人血条",
                string.Empty,
                status,
                string.Empty,
                EnemyHudControlCapabilities.None,
                Array.Empty<EnemyHudControlSetting>(),
                new Dictionary<string, EnemyHudControlValue>(),
                new Dictionary<string, EnemyHudControlValue>());

        EnemyHudControlResult Check(EnemyHudControlChange change, bool requireChanges = false)
        {
            if (change == null)
                return new EnemyHudControlResult(EnemyHudControlOutcome.ValidationRejected, "缺少变更集", new[] { new EnemyHudControlIssue(string.Empty, "null change") }, Snapshot());
            if (!string.Equals(change.ExpectedRevision, revision, StringComparison.Ordinal))
                return new EnemyHudControlResult(EnemyHudControlOutcome.StateConflict, "目标状态已变化，请刷新后重试", null, Snapshot());
            if (requireChanges && change.Values.Count == 0)
                return new EnemyHudControlResult(EnemyHudControlOutcome.NoChanges, "没有改动", null, Snapshot());

            var issues = new List<EnemyHudControlIssue>();
            foreach (var pair in change.Values)
            {
                var definition = FindDefinition(pair.Key);
                if (definition == null)
                {
                    issues.Add(new EnemyHudControlIssue(pair.Key, "未注册的设置"));
                    continue;
                }

                var issue = definition.Validate(pair.Value);
                if (issue != null) issues.Add(issue);
            }

            if (issues.Count == 0) return null;
            return new EnemyHudControlResult(EnemyHudControlOutcome.ValidationRejected, "变更未通过校验", issues, Snapshot());
        }

        Dictionary<string, EnemyHudControlValue> SnapshotValues(IEnumerable<string> keys)
        {
            var values = new Dictionary<string, EnemyHudControlValue>(StringComparer.Ordinal);
            foreach (var key in keys)
                if (saved.TryGetValue(key, out var value)) values[key] = value;
            return values;
        }

        void RestoreValues(Dictionary<string, EnemyHudControlValue> values)
        {
            foreach (var pair in values)
            {
                try { writeSaved(pair.Key, pair.Value); }
                catch { }
            }
        }

        void RefreshSaved()
        {
            var changed = false;
            foreach (var definition in Definitions)
            {
                var value = readSaved(definition.Key) ?? definition.DefaultValue;
                if (!saved.TryGetValue(definition.Key, out var previous) || !previous.ValueEquals(value))
                {
                    saved[definition.Key] = value;
                    changed = true;
                }
            }

            if (changed) BumpRevision();
        }

        void BumpRevision()
        {
            var number = 1;
            if (int.TryParse(revision, out var parsed)) number = parsed + 1;
            revision = number.ToString();
        }

        EnemyHudControlSnapshot Snapshot()
            => new EnemyHudControlSnapshot(
                TargetId,
                "PreLuna 敌人血条",
                ControlVersion,
                EnemyHudControlStatus.Available,
                revision,
                EnemyHudControlCapabilities.Validate | EnemyHudControlCapabilities.SavePersistent,
                Definitions,
                saved,
                effective);

        static EnemyHudControlSetting FindDefinition(string key)
        {
            foreach (var definition in Definitions)
                if (string.Equals(definition.Key, key, StringComparison.Ordinal)) return definition;
            return null;
        }
    }
}
