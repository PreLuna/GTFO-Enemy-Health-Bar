using System;
using System.Collections.Generic;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Control
{
    internal sealed class DamageNumberControlState
    {
        internal const string TargetId = "PreLuna.EnemyHealthBar.DamageNumbers";
        internal const string ControlVersion = "0.1.1";

        internal static readonly IReadOnlyList<DamageNumberControlSetting> Definitions = new[]
        {
            new DamageNumberControlSetting("enabled", "启用", DamageNumberControlKind.Boolean, DamageNumberControlValue.Boolean(true), "General", "Enabled"),
            new DamageNumberControlSetting("maxRows", "最大行数", DamageNumberControlKind.Number, DamageNumberControlValue.Number(12), "Display", "MaxRows", 1, 12),
            new DamageNumberControlSetting("lifetime", "生命周期", DamageNumberControlKind.Number, DamageNumberControlValue.Number(.95), "Display", "Lifetime", .3, 3),
            new DamageNumberControlSetting("fontSize", "字号", DamageNumberControlKind.Number, DamageNumberControlValue.Number(24), "Display", "FontSize", 12, 48),
            new DamageNumberControlSetting("maxDistance", "最大距离", DamageNumberControlKind.Number, DamageNumberControlValue.Number(100), "Display", "MaxDistance", 0, 300),
            new DamageNumberControlSetting("showArmor", "显示护甲命中", DamageNumberControlKind.Boolean, DamageNumberControlValue.Boolean(true), "Display", "ShowArmor")
        };

        readonly string path;
        readonly Func<string, DamageNumberControlValue> readSaved;
        readonly Action<string, DamageNumberControlValue> writeSaved;
        readonly Func<string, string> serialize;
        readonly Func<bool> getAutoSave;
        readonly Action<bool> setAutoSave;
        readonly Dictionary<string, DamageNumberControlValue> saved = new(StringComparer.Ordinal);
        readonly Dictionary<string, DamageNumberControlValue> effective = new(StringComparer.Ordinal);
        string revision = "1";
        string fileHash;

        internal DamageNumberControlState(
            string path,
            Func<string, DamageNumberControlValue> readSaved,
            Action<string, DamageNumberControlValue> writeSaved,
            Func<string, string> serialize,
            Func<bool> getAutoSave = null,
            Action<bool> setAutoSave = null)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.readSaved = readSaved ?? throw new ArgumentNullException(nameof(readSaved));
            this.writeSaved = writeSaved ?? throw new ArgumentNullException(nameof(writeSaved));
            this.serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
            this.getAutoSave = getAutoSave;
            this.setAutoSave = setAutoSave;
            fileHash = DamageNumberControlFileStore.Capture(path);

            foreach (var definition in Definitions)
            {
                var value = this.readSaved(definition.Key) ?? definition.DefaultValue;
                saved[definition.Key] = value;
                effective[definition.Key] = value;
            }
        }

        internal string Revision => revision;

        internal DamageNumberControlSnapshot ReadSnapshot()
        {
            RefreshSaved();
            return Snapshot();
        }

        internal DamageNumberControlResult Validate(DamageNumberControlChange change)
        {
            var check = Check(change);
            if (check != null) return check;
            return new DamageNumberControlResult(DamageNumberControlOutcome.NoChanges, "没有改动", null, Snapshot());
        }

        internal DamageNumberControlResult SetPersistent(DamageNumberControlChange change)
        {
            var check = Check(change, requireChanges: true);
            if (check != null) return check;

            var previous = SnapshotValues(change.Values.Keys);
            var previousAutoSave = getAutoSave?.Invoke();
            var restoreAutoSave = setAutoSave != null && previousAutoSave.HasValue;
            if (restoreAutoSave) setAutoSave(false);

            try
            {
                try
                {
                    foreach (var pair in change.Values) writeSaved(pair.Key, pair.Value);
                    var writes = new List<DamageNumberControlWrite>();
                    foreach (var pair in change.Values)
                    {
                        var definition = FindDefinition(pair.Key);
                        var text = serialize(pair.Key);
                        if (definition == null || text == null)
                            throw new InvalidOperationException("缺少配置映射：" + pair.Key);
                        writes.Add(new DamageNumberControlWrite(definition.Section, definition.ConfigKey, text));
                    }

                    fileHash = DamageNumberControlFileStore.Save(path, fileHash, writes);
                }
                catch (DamageNumberControlConflictException error)
                {
                    RestoreValues(previous);
                    RefreshSaved(false);
                    return new DamageNumberControlResult(DamageNumberControlOutcome.StateConflict, "配置文件已被外部修改：" + error.Message, null, Snapshot());
                }
                catch (Exception error)
                {
                    RestoreValues(previous);
                    RefreshSaved(false);
                    return new DamageNumberControlResult(DamageNumberControlOutcome.SaveFailed, "保存失败：" + error.Message, null, Snapshot());
                }

                foreach (var pair in change.Values)
                {
                    saved[pair.Key] = pair.Value;
                    effective[pair.Key] = pair.Value;
                }

                BumpRevision();
                return new DamageNumberControlResult(DamageNumberControlOutcome.Success, "已保存并生效", null, Snapshot());
            }
            finally
            {
                if (restoreAutoSave) setAutoSave(previousAutoSave.Value);
            }
        }

        DamageNumberControlResult Check(DamageNumberControlChange change, bool requireChanges = false)
        {
            if (change == null)
                return new DamageNumberControlResult(DamageNumberControlOutcome.ValidationRejected, "缺少变更集", new[] { new DamageNumberControlIssue(string.Empty, "null change") }, Snapshot());
            if (!string.Equals(change.ExpectedRevision, revision, StringComparison.Ordinal))
                return new DamageNumberControlResult(DamageNumberControlOutcome.StateConflict, "目标状态已变化，请刷新后重试", null, Snapshot());
            if (requireChanges && change.Values.Count == 0)
                return new DamageNumberControlResult(DamageNumberControlOutcome.NoChanges, "没有改动", null, Snapshot());

            var issues = new List<DamageNumberControlIssue>();
            foreach (var pair in change.Values)
            {
                var definition = FindDefinition(pair.Key);
                if (definition == null)
                {
                    issues.Add(new DamageNumberControlIssue(pair.Key, "未注册的设置"));
                    continue;
                }

                var issue = definition.Validate(pair.Value);
                if (issue != null) issues.Add(issue);
            }

            if (issues.Count == 0) return null;
            return new DamageNumberControlResult(DamageNumberControlOutcome.ValidationRejected, "变更未通过校验", issues, Snapshot());
        }

        Dictionary<string, DamageNumberControlValue> SnapshotValues(IEnumerable<string> keys)
        {
            var values = new Dictionary<string, DamageNumberControlValue>(StringComparer.Ordinal);
            foreach (var key in keys)
                if (saved.TryGetValue(key, out var value)) values[key] = value;
            return values;
        }

        void RestoreValues(Dictionary<string, DamageNumberControlValue> values)
        {
            foreach (var pair in values)
            {
                try { writeSaved(pair.Key, pair.Value); }
                catch { }
            }
        }

        void RefreshSaved(bool adoptFile = true)
        {
            var changed = false;
            foreach (var definition in Definitions)
            {
                var value = readSaved(definition.Key) ?? definition.DefaultValue;
                if (!saved.TryGetValue(definition.Key, out var previous) || !previous.ValueEquals(value))
                {
                    saved[definition.Key] = value;
                    effective[definition.Key] = value;
                    changed = true;
                }
            }

            if (changed)
            {
                // Hotkeys (F9/F10 etc.) update ConfigEntry values and let BepInEx save the file.
                // Adopt that internal file state so a subsequent ModControl write is not a false conflict.
                if (adoptFile)
                {
                    var current = DamageNumberControlFileStore.Capture(path);
                    if (!string.IsNullOrEmpty(current)) fileHash = current;
                }

                BumpRevision();
            }
        }

        void BumpRevision()
        {
            var number = 1;
            if (int.TryParse(revision, out var parsed)) number = parsed + 1;
            revision = number.ToString();
        }

        DamageNumberControlSnapshot Snapshot()
            => new DamageNumberControlSnapshot(
                TargetId,
                "PreLuna 伤害数字",
                ControlVersion,
                DamageNumberControlStatus.Available,
                revision,
                DamageNumberControlCapabilities.Validate | DamageNumberControlCapabilities.SavePersistent,
                Definitions,
                saved,
                effective);

        internal static DamageNumberControlSnapshot Unavailable(DamageNumberControlStatus status)
            => new DamageNumberControlSnapshot(
                TargetId,
                "PreLuna 伤害数字",
                string.Empty,
                status,
                string.Empty,
                DamageNumberControlCapabilities.None,
                Array.Empty<DamageNumberControlSetting>(),
                new Dictionary<string, DamageNumberControlValue>(),
                new Dictionary<string, DamageNumberControlValue>());

        static DamageNumberControlSetting FindDefinition(string key)
        {
            foreach (var definition in Definitions)
                if (string.Equals(definition.Key, key, StringComparison.Ordinal)) return definition;
            return null;
        }
    }
}
