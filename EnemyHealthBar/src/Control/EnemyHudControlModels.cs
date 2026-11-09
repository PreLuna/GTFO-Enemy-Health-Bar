using System;
using System.Collections.Generic;

namespace PreLuna.EnemyHealthBar.Control
{
    public enum EnemyHudControlStatus
    {
        Available,
        Uninitialized,
        Missing,
        Incompatible
    }

    [Flags]
    public enum EnemyHudControlCapabilities
    {
        None = 0,
        Validate = 1,
        SavePersistent = 2
    }

    public enum EnemyHudControlOutcome
    {
        Success,
        NoChanges,
        ValidationRejected,
        StateConflict,
        Unavailable,
        SaveFailed
    }

    public enum EnemyHudControlKind
    {
        Boolean,
        Number,
        Integer
    }

    public sealed class EnemyHudControlValue
    {
        EnemyHudControlValue(EnemyHudControlKind kind, object raw)
        {
            Kind = kind;
            Raw = raw;
        }

        public EnemyHudControlKind Kind { get; }
        public object Raw { get; }

        public static EnemyHudControlValue Boolean(bool value) => new EnemyHudControlValue(EnemyHudControlKind.Boolean, value);
        public static EnemyHudControlValue Number(double value) => new EnemyHudControlValue(EnemyHudControlKind.Number, value);
        public static EnemyHudControlValue Integer(int value) => new EnemyHudControlValue(EnemyHudControlKind.Integer, value);

        public bool TryGetBoolean(out bool value)
        {
            if (Kind == EnemyHudControlKind.Boolean && Raw is bool typed)
            {
                value = typed;
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetNumber(out double value)
        {
            if (Kind == EnemyHudControlKind.Number && Raw is double typed)
            {
                value = typed;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryGetInteger(out int value)
        {
            if (Kind == EnemyHudControlKind.Integer && Raw is int typed)
            {
                value = typed;
                return true;
            }

            value = 0;
            return false;
        }

        public bool ValueEquals(EnemyHudControlValue other)
        {
            if (other == null || Kind != other.Kind || Raw == null || other.Raw == null) return false;
            if (Kind == EnemyHudControlKind.Number)
                return Raw is double left && other.Raw is double right && Math.Abs(left - right) < 1e-9;
            return Raw.Equals(other.Raw);
        }

        public override string ToString() => Raw?.ToString() ?? string.Empty;
    }

    public sealed class EnemyHudControlSetting
    {
        public EnemyHudControlSetting(
            string key,
            string displayName,
            EnemyHudControlKind kind,
            EnemyHudControlValue defaultValue,
            double? minimum = null,
            double? maximum = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            DisplayName = displayName ?? key;
            Kind = kind;
            DefaultValue = defaultValue;
            Minimum = minimum;
            Maximum = maximum;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public EnemyHudControlKind Kind { get; }
        public EnemyHudControlValue DefaultValue { get; }
        public double? Minimum { get; }
        public double? Maximum { get; }

        public EnemyHudControlIssue Validate(EnemyHudControlValue value)
        {
            if (value == null) return new EnemyHudControlIssue(Key, "缺少值");
            if (value.Kind != Kind) return new EnemyHudControlIssue(Key, "值类型不匹配");

            if (Kind == EnemyHudControlKind.Boolean)
                return value.TryGetBoolean(out _) ? null : new EnemyHudControlIssue(Key, "不是合法布尔值");

            if (Kind == EnemyHudControlKind.Integer)
            {
                if (!value.TryGetInteger(out var integer)) return new EnemyHudControlIssue(Key, "不是合法整数");
                return CheckRange(integer);
            }

            if (!value.TryGetNumber(out var number) || double.IsNaN(number) || double.IsInfinity(number))
                return new EnemyHudControlIssue(Key, "不是合法数值");
            return CheckRange(number);
        }

        EnemyHudControlIssue CheckRange(double value)
        {
            if (Minimum.HasValue && value < Minimum.Value) return new EnemyHudControlIssue(Key, "小于最小值 " + Minimum.Value);
            if (Maximum.HasValue && value > Maximum.Value) return new EnemyHudControlIssue(Key, "大于最大值 " + Maximum.Value);
            return null;
        }
    }

    public sealed class EnemyHudControlIssue
    {
        public EnemyHudControlIssue(string key, string message)
        {
            Key = key ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string Key { get; }
        public string Message { get; }
    }

    public sealed class EnemyHudControlSnapshot
    {
        public EnemyHudControlSnapshot(
            string targetId,
            string displayName,
            string version,
            EnemyHudControlStatus status,
            string revision,
            EnemyHudControlCapabilities capabilities,
            IReadOnlyList<EnemyHudControlSetting> settings,
            IDictionary<string, EnemyHudControlValue> saved,
            IDictionary<string, EnemyHudControlValue> effective)
        {
            TargetId = targetId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Version = version ?? string.Empty;
            Status = status;
            Revision = revision ?? string.Empty;
            Capabilities = capabilities;
            Settings = settings ?? Array.Empty<EnemyHudControlSetting>();
            Saved = Copy(saved);
            Effective = Copy(effective);
        }

        public string TargetId { get; }
        public string DisplayName { get; }
        public string Version { get; }
        public EnemyHudControlStatus Status { get; }
        public string Revision { get; }
        public EnemyHudControlCapabilities Capabilities { get; }
        public IReadOnlyList<EnemyHudControlSetting> Settings { get; }
        public IReadOnlyDictionary<string, EnemyHudControlValue> Saved { get; }
        public IReadOnlyDictionary<string, EnemyHudControlValue> Effective { get; }

        static IReadOnlyDictionary<string, EnemyHudControlValue> Copy(IDictionary<string, EnemyHudControlValue> source)
        {
            var copy = new Dictionary<string, EnemyHudControlValue>(StringComparer.Ordinal);
            if (source != null)
                foreach (var pair in source) copy[pair.Key] = pair.Value;
            return copy;
        }
    }

    public sealed class EnemyHudControlChange
    {
        public EnemyHudControlChange(string expectedRevision, IDictionary<string, EnemyHudControlValue> values)
        {
            ExpectedRevision = expectedRevision ?? string.Empty;
            var copy = new Dictionary<string, EnemyHudControlValue>(StringComparer.Ordinal);
            if (values != null)
                foreach (var pair in values) copy[pair.Key] = pair.Value;
            Values = copy;
        }

        public string ExpectedRevision { get; }
        public IReadOnlyDictionary<string, EnemyHudControlValue> Values { get; }
    }

    public sealed class EnemyHudControlResult
    {
        public EnemyHudControlResult(
            EnemyHudControlOutcome outcome,
            string message,
            IReadOnlyList<EnemyHudControlIssue> issues = null,
            EnemyHudControlSnapshot snapshot = null)
        {
            Outcome = outcome;
            Message = message ?? string.Empty;
            Issues = issues ?? Array.Empty<EnemyHudControlIssue>();
            Snapshot = snapshot;
        }

        public EnemyHudControlOutcome Outcome { get; }
        public string Message { get; }
        public IReadOnlyList<EnemyHudControlIssue> Issues { get; }
        public EnemyHudControlSnapshot Snapshot { get; }
    }
}
