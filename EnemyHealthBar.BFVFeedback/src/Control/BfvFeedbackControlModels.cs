using System;
using System.Collections.Generic;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Control
{
    public enum BfvFeedbackControlStatus
    {
        Available,
        Uninitialized,
        Missing,
        Incompatible
    }

    [Flags]
    public enum BfvFeedbackControlCapabilities
    {
        None = 0,
        Validate = 1,
        SavePersistent = 2
    }

    public enum BfvFeedbackControlOutcome
    {
        Success,
        NoChanges,
        ValidationRejected,
        StateConflict,
        Unavailable,
        SaveFailed
    }

    public enum BfvFeedbackControlKind
    {
        Boolean,
        Number,
        Integer,
        String
    }

    public sealed class BfvFeedbackControlValue
    {
        BfvFeedbackControlValue(BfvFeedbackControlKind kind, object raw)
        {
            Kind = kind;
            Raw = raw;
        }

        public BfvFeedbackControlKind Kind { get; }
        public object Raw { get; }

        public static BfvFeedbackControlValue Boolean(bool value)
            => new BfvFeedbackControlValue(BfvFeedbackControlKind.Boolean, value);
        public static BfvFeedbackControlValue Number(double value)
            => new BfvFeedbackControlValue(BfvFeedbackControlKind.Number, value);
        public static BfvFeedbackControlValue Integer(int value)
            => new BfvFeedbackControlValue(BfvFeedbackControlKind.Integer, value);
        public static BfvFeedbackControlValue String(string value)
            => new BfvFeedbackControlValue(BfvFeedbackControlKind.String, value ?? string.Empty);

        public bool TryGetBoolean(out bool value)
        {
            if (Kind == BfvFeedbackControlKind.Boolean && Raw is bool typed)
            {
                value = typed;
                return true;
            }
            value = false;
            return false;
        }

        public bool TryGetNumber(out double value)
        {
            if (Kind == BfvFeedbackControlKind.Number && Raw is double typed)
            {
                value = typed;
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryGetInteger(out int value)
        {
            if (Kind == BfvFeedbackControlKind.Integer && Raw is int typed)
            {
                value = typed;
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryGetString(out string value)
        {
            if (Kind == BfvFeedbackControlKind.String && Raw is string typed)
            {
                value = typed;
                return true;
            }
            value = string.Empty;
            return false;
        }

        public bool ValueEquals(BfvFeedbackControlValue other)
        {
            if (other == null || Kind != other.Kind || Raw == null || other.Raw == null) return false;
            if (Kind == BfvFeedbackControlKind.Number)
                return Raw is double left && other.Raw is double right && Math.Abs(left - right) < 1e-9;
            return Raw.Equals(other.Raw);
        }

        public override string ToString() => Raw?.ToString() ?? string.Empty;
    }

    public sealed class BfvFeedbackControlSetting
    {
        public BfvFeedbackControlSetting(
            string key,
            string displayName,
            BfvFeedbackControlKind kind,
            BfvFeedbackControlValue defaultValue,
            double? minimum = null,
            double? maximum = null,
            string section = "",
            string configKey = "")
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            DisplayName = displayName ?? key;
            Kind = kind;
            DefaultValue = defaultValue;
            Minimum = minimum;
            Maximum = maximum;
            Section = section ?? string.Empty;
            ConfigKey = configKey ?? string.Empty;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public BfvFeedbackControlKind Kind { get; }
        public BfvFeedbackControlValue DefaultValue { get; }
        public double? Minimum { get; }
        public double? Maximum { get; }
        public string Section { get; }
        public string ConfigKey { get; }

        public BfvFeedbackControlIssue Validate(BfvFeedbackControlValue value)
        {
            if (value == null) return new BfvFeedbackControlIssue(Key, "缺少值");
            if (value.Kind != Kind) return new BfvFeedbackControlIssue(Key, "值类型不匹配");

            if (Kind == BfvFeedbackControlKind.Boolean)
                return value.TryGetBoolean(out _) ? null : new BfvFeedbackControlIssue(Key, "不是合法布尔值");

            if (Kind == BfvFeedbackControlKind.Integer)
            {
                if (!value.TryGetInteger(out var integer)) return new BfvFeedbackControlIssue(Key, "不是合法整数");
                return CheckRange(integer);
            }

            if (Kind == BfvFeedbackControlKind.String)
                return value.TryGetString(out var text) && !string.IsNullOrWhiteSpace(text)
                    ? null
                    : new BfvFeedbackControlIssue(Key, "不是合法文本");

            if (!value.TryGetNumber(out var number) || double.IsNaN(number) || double.IsInfinity(number))
                return new BfvFeedbackControlIssue(Key, "不是合法数值");
            return CheckRange(number);
        }

        BfvFeedbackControlIssue CheckRange(double value)
        {
            if (Minimum.HasValue && value < Minimum.Value)
                return new BfvFeedbackControlIssue(Key, "小于最小值 " + Minimum.Value);
            if (Maximum.HasValue && value > Maximum.Value)
                return new BfvFeedbackControlIssue(Key, "大于最大值 " + Maximum.Value);
            return null;
        }
    }

    public sealed class BfvFeedbackControlIssue
    {
        public BfvFeedbackControlIssue(string key, string message)
        {
            Key = key ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string Key { get; }
        public string Message { get; }
    }

    public sealed class BfvFeedbackControlSnapshot
    {
        public BfvFeedbackControlSnapshot(
            string targetId,
            string displayName,
            string version,
            BfvFeedbackControlStatus status,
            string revision,
            BfvFeedbackControlCapabilities capabilities,
            IReadOnlyList<BfvFeedbackControlSetting> settings,
            IReadOnlyDictionary<string, BfvFeedbackControlValue> saved,
            IReadOnlyDictionary<string, BfvFeedbackControlValue> effective)
        {
            TargetId = targetId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Version = version ?? string.Empty;
            Status = status;
            Revision = revision ?? string.Empty;
            Capabilities = capabilities;
            Settings = settings ?? Array.Empty<BfvFeedbackControlSetting>();
            Saved = saved ?? new Dictionary<string, BfvFeedbackControlValue>();
            Effective = effective ?? new Dictionary<string, BfvFeedbackControlValue>();
        }

        public string TargetId { get; }
        public string DisplayName { get; }
        public string Version { get; }
        public BfvFeedbackControlStatus Status { get; }
        public string Revision { get; }
        public BfvFeedbackControlCapabilities Capabilities { get; }
        public IReadOnlyList<BfvFeedbackControlSetting> Settings { get; }
        public IReadOnlyDictionary<string, BfvFeedbackControlValue> Saved { get; }
        public IReadOnlyDictionary<string, BfvFeedbackControlValue> Effective { get; }
    }

    public sealed class BfvFeedbackControlChange
    {
        public BfvFeedbackControlChange(string expectedRevision, IDictionary<string, BfvFeedbackControlValue> values)
        {
            ExpectedRevision = expectedRevision ?? string.Empty;
            Values = values ?? new Dictionary<string, BfvFeedbackControlValue>();
        }

        public string ExpectedRevision { get; }
        public IDictionary<string, BfvFeedbackControlValue> Values { get; }
    }

    public sealed class BfvFeedbackControlResult
    {
        public BfvFeedbackControlResult(
            BfvFeedbackControlOutcome outcome,
            string message,
            IReadOnlyList<BfvFeedbackControlIssue> issues = null,
            BfvFeedbackControlSnapshot snapshot = null)
        {
            Outcome = outcome;
            Message = message ?? string.Empty;
            Issues = issues ?? Array.Empty<BfvFeedbackControlIssue>();
            Snapshot = snapshot;
        }

        public BfvFeedbackControlOutcome Outcome { get; }
        public string Message { get; }
        public IReadOnlyList<BfvFeedbackControlIssue> Issues { get; }
        public BfvFeedbackControlSnapshot Snapshot { get; }
    }
}
