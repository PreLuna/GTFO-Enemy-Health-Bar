using System;
using System.Collections.Generic;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Control
{
    public enum DamageNumberControlStatus
    {
        Available,
        Uninitialized,
        Missing,
        Incompatible
    }

    [Flags]
    public enum DamageNumberControlCapabilities
    {
        None = 0,
        Validate = 1,
        SavePersistent = 2
    }

    public enum DamageNumberControlOutcome
    {
        Success,
        NoChanges,
        ValidationRejected,
        StateConflict,
        Unavailable,
        SaveFailed
    }

    public enum DamageNumberControlKind
    {
        Boolean,
        Number,
        Integer
    }

    public sealed class DamageNumberControlValue
    {
        DamageNumberControlValue(DamageNumberControlKind kind, object raw)
        {
            Kind = kind;
            Raw = raw;
        }

        public DamageNumberControlKind Kind { get; }
        public object Raw { get; }

        public static DamageNumberControlValue Boolean(bool value) => new DamageNumberControlValue(DamageNumberControlKind.Boolean, value);
        public static DamageNumberControlValue Number(double value) => new DamageNumberControlValue(DamageNumberControlKind.Number, value);
        public static DamageNumberControlValue Integer(int value) => new DamageNumberControlValue(DamageNumberControlKind.Integer, value);

        public bool TryGetBoolean(out bool value)
        {
            if (Kind == DamageNumberControlKind.Boolean && Raw is bool typed)
            {
                value = typed;
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetNumber(out double value)
        {
            if (Kind == DamageNumberControlKind.Number && Raw is double typed)
            {
                value = typed;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryGetInteger(out int value)
        {
            if (Kind == DamageNumberControlKind.Integer && Raw is int typed)
            {
                value = typed;
                return true;
            }

            value = 0;
            return false;
        }

        public bool ValueEquals(DamageNumberControlValue other)
        {
            if (other == null || Kind != other.Kind || Raw == null || other.Raw == null) return false;
            if (Kind == DamageNumberControlKind.Number)
                return Raw is double left && other.Raw is double right && Math.Abs(left - right) < 1e-9;
            return Raw.Equals(other.Raw);
        }

        public override string ToString() => Raw?.ToString() ?? string.Empty;
    }

    public sealed class DamageNumberControlSetting
    {
        public DamageNumberControlSetting(
            string key,
            string displayName,
            DamageNumberControlKind kind,
            DamageNumberControlValue defaultValue,
            string section,
            string configKey,
            double? minimum = null,
            double? maximum = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            DisplayName = displayName ?? key;
            Kind = kind;
            DefaultValue = defaultValue;
            Section = section ?? string.Empty;
            ConfigKey = configKey ?? string.Empty;
            Minimum = minimum;
            Maximum = maximum;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public DamageNumberControlKind Kind { get; }
        public DamageNumberControlValue DefaultValue { get; }
        public string Section { get; }
        public string ConfigKey { get; }
        public double? Minimum { get; }
        public double? Maximum { get; }

        public DamageNumberControlIssue Validate(DamageNumberControlValue value)
        {
            if (value == null) return new DamageNumberControlIssue(Key, "缺少值");
            if (value.Kind != Kind) return new DamageNumberControlIssue(Key, "值类型不匹配");

            if (Kind == DamageNumberControlKind.Boolean)
                return value.TryGetBoolean(out _) ? null : new DamageNumberControlIssue(Key, "不是合法布尔值");

            if (Kind == DamageNumberControlKind.Integer)
            {
                if (!value.TryGetInteger(out var integer)) return new DamageNumberControlIssue(Key, "不是合法整数");
                return CheckRange(integer);
            }

            if (!value.TryGetNumber(out var number) || double.IsNaN(number) || double.IsInfinity(number))
                return new DamageNumberControlIssue(Key, "不是合法数值");
            return CheckRange(number);
        }

        DamageNumberControlIssue CheckRange(double value)
        {
            if (Minimum.HasValue && value < Minimum.Value) return new DamageNumberControlIssue(Key, "小于最小值 " + Minimum.Value);
            if (Maximum.HasValue && value > Maximum.Value) return new DamageNumberControlIssue(Key, "大于最大值 " + Maximum.Value);
            return null;
        }
    }

    public sealed class DamageNumberControlIssue
    {
        public DamageNumberControlIssue(string key, string message)
        {
            Key = key ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string Key { get; }
        public string Message { get; }
    }

    public sealed class DamageNumberControlSnapshot
    {
        public DamageNumberControlSnapshot(
            string targetId,
            string displayName,
            string version,
            DamageNumberControlStatus status,
            string revision,
            DamageNumberControlCapabilities capabilities,
            IReadOnlyList<DamageNumberControlSetting> settings,
            IDictionary<string, DamageNumberControlValue> saved,
            IDictionary<string, DamageNumberControlValue> effective)
        {
            TargetId = targetId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Version = version ?? string.Empty;
            Status = status;
            Revision = revision ?? string.Empty;
            Capabilities = capabilities;
            Settings = settings ?? Array.Empty<DamageNumberControlSetting>();
            Saved = Copy(saved);
            Effective = Copy(effective);
        }

        public string TargetId { get; }
        public string DisplayName { get; }
        public string Version { get; }
        public DamageNumberControlStatus Status { get; }
        public string Revision { get; }
        public DamageNumberControlCapabilities Capabilities { get; }
        public IReadOnlyList<DamageNumberControlSetting> Settings { get; }
        public IReadOnlyDictionary<string, DamageNumberControlValue> Saved { get; }
        public IReadOnlyDictionary<string, DamageNumberControlValue> Effective { get; }

        static IReadOnlyDictionary<string, DamageNumberControlValue> Copy(IDictionary<string, DamageNumberControlValue> source)
        {
            var copy = new Dictionary<string, DamageNumberControlValue>(StringComparer.Ordinal);
            if (source != null)
                foreach (var pair in source) copy[pair.Key] = pair.Value;
            return copy;
        }
    }

    public sealed class DamageNumberControlChange
    {
        public DamageNumberControlChange(string expectedRevision, IDictionary<string, DamageNumberControlValue> values)
        {
            ExpectedRevision = expectedRevision ?? string.Empty;
            var copy = new Dictionary<string, DamageNumberControlValue>(StringComparer.Ordinal);
            if (values != null)
                foreach (var pair in values) copy[pair.Key] = pair.Value;
            Values = copy;
        }

        public string ExpectedRevision { get; }
        public IReadOnlyDictionary<string, DamageNumberControlValue> Values { get; }
    }

    public sealed class DamageNumberControlResult
    {
        public DamageNumberControlResult(
            DamageNumberControlOutcome outcome,
            string message,
            IReadOnlyList<DamageNumberControlIssue> issues = null,
            DamageNumberControlSnapshot snapshot = null)
        {
            Outcome = outcome;
            Message = message ?? string.Empty;
            Issues = issues ?? Array.Empty<DamageNumberControlIssue>();
            Snapshot = snapshot;
        }

        public DamageNumberControlOutcome Outcome { get; }
        public string Message { get; }
        public IReadOnlyList<DamageNumberControlIssue> Issues { get; }
        public DamageNumberControlSnapshot Snapshot { get; }
    }
}
