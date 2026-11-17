using System;
using System.Collections.Generic;
using PreLuna.EnemyHealthBar.BFVFeedback.Core;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Control
{
    internal sealed class BfvFeedbackControlState
    {
        internal const string TargetId = "PreLuna.EnemyHealthBar.BFVFeedback";
        internal const string ControlVersion = "0.2.0";

        internal static readonly IReadOnlyList<BfvFeedbackControlSetting> Definitions = new[]
        {
            new BfvFeedbackControlSetting("enabled", "启用 BFV 反馈", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(true), section: "General", configKey: "Enabled"),
            new BfvFeedbackControlSetting("followHudVisibility", "跟随血条显隐", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(true), section: "General", configKey: "FollowHudVisibility"),

            new BfvFeedbackControlSetting("soundEnabled", "音效总开关", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(true), section: "Sound", configKey: "Enabled"),
            new BfvFeedbackControlSetting("styleIndex", "音效风格（0=BF1 1=BF5 2=BF6）", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(1), 0, 2, "Sound", "Style"),
            new BfvFeedbackControlSetting("volume", "主音量", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(1), 0, 1, "Sound", "Volume"),
            new BfvFeedbackControlSetting("gain", "总增益", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(1.8), .5, 3, "Sound", "Gain"),
            new BfvFeedbackControlSetting("killGain", "击杀增益", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(1.6), .5, 3, "Sound", "KillGain"),
            new BfvFeedbackControlSetting("killHighPassHz", "击杀高通（Hz）", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(350), 0, 2000, "Sound", "KillHighPassHz"),
            new BfvFeedbackControlSetting("killChimeEnabled", "击杀确认音", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(true), section: "Sound", configKey: "KillChimeEnabled"),
            new BfvFeedbackControlSetting("killChimeVolume", "确认音音量", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(.9), 0, 1, "Sound", "KillChimeVolume"),
            new BfvFeedbackControlSetting("killPriorityMs", "击杀优先级（ms）", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(150), 0, 500, "Sound", "KillPriorityMs"),
            new BfvFeedbackControlSetting("muteVanillaIndicator", "静音原版提示", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(true), section: "Sound", configKey: "MuteVanillaIndicator"),

            new BfvFeedbackControlSetting("hitMarkerEnabled", "命中线开关", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(true), section: "HitMarker", configKey: "Enabled"),
            new BfvFeedbackControlSetting("replaceVanilla", "替换原版标记", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(true), section: "HitMarker", configKey: "ReplaceVanilla"),
            new BfvFeedbackControlSetting("hitMarkerSize", "命中线尺寸", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(64), 16, 200, "HitMarker", "Size"),
            new BfvFeedbackControlSetting("armorFromNoDamage", "把 noDamage 当护甲", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(false), section: "HitMarker", configKey: "ArmorFromNoDamage"),
            new BfvFeedbackControlSetting("colorNormal", "普通命中颜色", BfvFeedbackControlKind.String, BfvFeedbackControlValue.String("#FFFFFF"), section: "HitMarker", configKey: "ColorNormal"),
            new BfvFeedbackControlSetting("colorWeakspot", "弱点命中颜色", BfvFeedbackControlKind.String, BfvFeedbackControlValue.String("#FF8C00"), section: "HitMarker", configKey: "ColorWeakspot"),
            new BfvFeedbackControlSetting("colorArmor", "护甲颜色", BfvFeedbackControlKind.String, BfvFeedbackControlValue.String("#49ADC9"), section: "HitMarker", configKey: "ColorArmor"),
            new BfvFeedbackControlSetting("colorKill", "击杀线颜色", BfvFeedbackControlKind.String, BfvFeedbackControlValue.String("#B92828"), section: "HitMarker", configKey: "ColorKill"),

            new BfvFeedbackControlSetting("killIconEnabled", "击杀图标开关", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(true), section: "KillIcon", configKey: "Enabled"),
            new BfvFeedbackControlSetting("killIconSize", "图标尺寸", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(46), 26, 76, "KillIcon", "Size"),
            new BfvFeedbackControlSetting("killIconOpacity", "图标透明度", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(.85), 0, 1, "KillIcon", "Opacity"),
            new BfvFeedbackControlSetting("killIconLifetime", "图标寿命（秒）", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(2.2), .4, 6, "KillIcon", "Lifetime"),
            new BfvFeedbackControlSetting("healthTiering", "按血量分档（关=按敌人类型）", BfvFeedbackControlKind.Boolean, BfvFeedbackControlValue.Boolean(false), section: "KillIcon", configKey: "TierSource"),
            new BfvFeedbackControlSetting("killIconGap", "头部间距（米）", BfvFeedbackControlKind.Number, BfvFeedbackControlValue.Number(.35), 0, 2, "KillIcon", "HeadGapMetres")
        };

        readonly string path;
        readonly Func<string, BfvFeedbackControlValue> readSaved;
        readonly Action<string, BfvFeedbackControlValue> writeSaved;
        readonly Func<string, string> serialize;
        readonly Func<bool> getAutoSave;
        readonly Action<bool> setAutoSave;
        readonly Dictionary<string, BfvFeedbackControlValue> saved = new(StringComparer.Ordinal);
        readonly Dictionary<string, BfvFeedbackControlValue> effective = new(StringComparer.Ordinal);
        string revision = "1";
        string fileHash;

        internal BfvFeedbackControlState(
            string path,
            Func<string, BfvFeedbackControlValue> readSaved,
            Action<string, BfvFeedbackControlValue> writeSaved,
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
            fileHash = BfvFeedbackControlFileStore.Capture(path);

            foreach (var definition in Definitions)
            {
                var value = this.readSaved(definition.Key) ?? definition.DefaultValue;
                saved[definition.Key] = value;
                effective[definition.Key] = value;
            }
        }

        internal string Revision => revision;

        internal BfvFeedbackControlSnapshot ReadSnapshot()
        {
            RefreshSaved();
            return Snapshot();
        }

        internal BfvFeedbackControlResult Validate(BfvFeedbackControlChange change)
        {
            var check = Check(change);
            if (check != null) return check;
            return new BfvFeedbackControlResult(BfvFeedbackControlOutcome.NoChanges, "没有改动", null, Snapshot());
        }

        internal BfvFeedbackControlResult SetPersistent(BfvFeedbackControlChange change)
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
                    var writes = new List<BfvFeedbackControlWrite>();
                    foreach (var pair in change.Values)
                    {
                        var definition = FindDefinition(pair.Key);
                        var text = serialize(pair.Key);
                        if (definition == null || text == null)
                            throw new InvalidOperationException("缺少配置映射：" + pair.Key);
                        writes.Add(new BfvFeedbackControlWrite(definition.Section, definition.ConfigKey, text));
                    }

                    fileHash = BfvFeedbackControlFileStore.Save(path, fileHash, writes);
                }
                catch (BfvFeedbackControlConflictException error)
                {
                    RestoreValues(previous);
                    RefreshSaved(false);
                    return new BfvFeedbackControlResult(BfvFeedbackControlOutcome.StateConflict,
                        "配置文件已被外部修改：" + error.Message, null, Snapshot());
                }
                catch (Exception error)
                {
                    RestoreValues(previous);
                    RefreshSaved(false);
                    return new BfvFeedbackControlResult(BfvFeedbackControlOutcome.SaveFailed,
                        "保存失败：" + error.Message, null, Snapshot());
                }

                foreach (var pair in change.Values)
                {
                    saved[pair.Key] = pair.Value;
                    effective[pair.Key] = pair.Value;
                }

                BumpRevision();
                return new BfvFeedbackControlResult(BfvFeedbackControlOutcome.Success,
                    "已保存并生效", null, Snapshot());
            }
            finally
            {
                if (restoreAutoSave) setAutoSave(previousAutoSave.Value);
            }
        }

        BfvFeedbackControlResult Check(BfvFeedbackControlChange change, bool requireChanges = false)
        {
            if (change == null)
                return new BfvFeedbackControlResult(BfvFeedbackControlOutcome.ValidationRejected,
                    "缺少变更集", new[] { new BfvFeedbackControlIssue(string.Empty, "null change") }, Snapshot());
            if (!string.Equals(change.ExpectedRevision, revision, StringComparison.Ordinal))
                return new BfvFeedbackControlResult(BfvFeedbackControlOutcome.StateConflict,
                    "目标状态已变化，请刷新后重试", null, Snapshot());
            if (requireChanges && change.Values.Count == 0)
                return new BfvFeedbackControlResult(BfvFeedbackControlOutcome.NoChanges,
                    "没有改动", null, Snapshot());

            var issues = new List<BfvFeedbackControlIssue>();
            foreach (var pair in change.Values)
            {
                var definition = FindDefinition(pair.Key);
                if (definition == null)
                {
                    issues.Add(new BfvFeedbackControlIssue(pair.Key, "未注册的设置"));
                    continue;
                }

                var issue = definition.Validate(pair.Value);
                if (issue != null)
                {
                    issues.Add(issue);
                    continue;
                }

                if (definition.Kind == BfvFeedbackControlKind.String &&
                    definition.Key.StartsWith("color", StringComparison.Ordinal) &&
                    pair.Value.TryGetString(out var color) &&
                    !BfvColor.TryParse(color, out _))
                {
                    issues.Add(new BfvFeedbackControlIssue(pair.Key, "颜色格式应为 #RRGGBB 或 #RRGGBBAA"));
                }
            }

            if (issues.Count == 0) return null;
            return new BfvFeedbackControlResult(BfvFeedbackControlOutcome.ValidationRejected,
                "变更未通过校验", issues, Snapshot());
        }

        Dictionary<string, BfvFeedbackControlValue> SnapshotValues(IEnumerable<string> keys)
        {
            var values = new Dictionary<string, BfvFeedbackControlValue>(StringComparer.Ordinal);
            foreach (var key in keys)
                if (saved.TryGetValue(key, out var value)) values[key] = value;
            return values;
        }

        void RestoreValues(Dictionary<string, BfvFeedbackControlValue> values)
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
                if (adoptFile)
                {
                    var current = BfvFeedbackControlFileStore.Capture(path);
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

        BfvFeedbackControlSnapshot Snapshot()
            => new BfvFeedbackControlSnapshot(
                TargetId,
                "PreLuna BFV 命中/击杀反馈",
                ControlVersion,
                BfvFeedbackControlStatus.Available,
                revision,
                BfvFeedbackControlCapabilities.Validate | BfvFeedbackControlCapabilities.SavePersistent,
                Definitions,
                saved,
                effective);

        internal static BfvFeedbackControlSnapshot Unavailable(BfvFeedbackControlStatus status)
            => new BfvFeedbackControlSnapshot(
                TargetId,
                "PreLuna BFV 命中/击杀反馈",
                string.Empty,
                status,
                string.Empty,
                BfvFeedbackControlCapabilities.None,
                Array.Empty<BfvFeedbackControlSetting>(),
                new Dictionary<string, BfvFeedbackControlValue>(),
                new Dictionary<string, BfvFeedbackControlValue>());

        static BfvFeedbackControlSetting FindDefinition(string key)
        {
            foreach (var definition in Definitions)
                if (string.Equals(definition.Key, key, StringComparison.Ordinal)) return definition;
            return null;
        }
    }
}
