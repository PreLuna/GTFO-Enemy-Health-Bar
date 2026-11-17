using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using PreLuna.EnemyHealthBar.BFVFeedback.Runtime;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Control
{
    public static class BfvFeedbackControlApi
    {
        public const string TargetId = BfvFeedbackControlState.TargetId;
        public const string ControlVersion = BfvFeedbackControlState.ControlVersion;

        static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static BfvFeedbackControlSnapshot GetSnapshot()
        {
            var control = Control;
            if (control != null) return control.ReadSnapshot();
            return BfvFeedbackControlState.Unavailable(
                Plugin.Instance == null ? BfvFeedbackControlStatus.Missing : BfvFeedbackControlStatus.Uninitialized);
        }

        public static BfvFeedbackControlResult Validate(BfvFeedbackControlChange change)
            => Control?.Validate(change) ?? Unavailable("BFV 反馈控制状态不可用");

        public static BfvFeedbackControlResult SetPersistent(BfvFeedbackControlChange change)
            => Control?.SetPersistent(change) ?? Unavailable("BFV 反馈控制状态不可用");

        public static string GetSnapshotJson()
            => JsonSerializer.Serialize(ToDto(GetSnapshot()), JsonOptions);

        public static string ValidateJson(string changeJson)
            => JsonSerializer.Serialize(ToDto(ValidateRead(changeJson)), JsonOptions);

        public static string SetPersistentJson(string changeJson)
            => JsonSerializer.Serialize(ToDto(SetPersistentRead(changeJson)), JsonOptions);

        static BfvFeedbackControlState Control => Plugin.Instance?.EnsureControlState();

        static BfvFeedbackControlResult Unavailable(string message)
            => new BfvFeedbackControlResult(
                BfvFeedbackControlOutcome.Unavailable,
                message,
                null,
                BfvFeedbackControlState.Unavailable(
                    Plugin.Instance == null ? BfvFeedbackControlStatus.Missing : BfvFeedbackControlStatus.Uninitialized));

        static BfvFeedbackControlResult ValidateRead(string changeJson)
        {
            if (!TryReadChange(changeJson, out var change, out var error)) return InvalidChange(error);
            return Validate(change);
        }

        static BfvFeedbackControlResult SetPersistentRead(string changeJson)
        {
            if (!TryReadChange(changeJson, out var change, out var error)) return InvalidChange(error);
            return SetPersistent(change);
        }

        static BfvFeedbackControlResult InvalidChange(string message)
            => new BfvFeedbackControlResult(
                BfvFeedbackControlOutcome.ValidationRejected,
                message,
                new[] { new BfvFeedbackControlIssue(string.Empty, message) },
                GetSnapshot());

        static bool TryReadChange(string changeJson, out BfvFeedbackControlChange change, out string error)
        {
            change = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(changeJson))
            {
                error = "变更集为空";
                return false;
            }

            try
            {
                var dto = JsonSerializer.Deserialize<ChangeDto>(changeJson, JsonOptions);
                if (dto == null)
                {
                    error = "变更集无效";
                    return false;
                }

                var values = new Dictionary<string, BfvFeedbackControlValue>(StringComparer.Ordinal);
                if (dto.Values != null)
                {
                    foreach (var pair in dto.Values)
                    {
                        var value = FromDto(pair.Value);
                        if (value == null)
                        {
                            error = "值类型无效：" + pair.Key;
                            return false;
                        }
                        values[pair.Key] = value;
                    }
                }

                change = new BfvFeedbackControlChange(dto.ExpectedRevision ?? string.Empty, values);
                return true;
            }
            catch (Exception ex)
            {
                error = "无法解析变更集：" + ex.Message;
                return false;
            }
        }

        static BfvFeedbackControlValue FromDto(ValueDto dto)
        {
            if (dto == null) return null;
            switch (dto.Kind)
            {
                case 0:
                    return dto.Boolean.HasValue ? BfvFeedbackControlValue.Boolean(dto.Boolean.Value) : null;
                case 1:
                    return dto.Number.HasValue ? BfvFeedbackControlValue.Number(dto.Number.Value) : null;
                case 2:
                    return dto.Integer.HasValue ? BfvFeedbackControlValue.Integer(dto.Integer.Value) : null;
                case 3:
                    return BfvFeedbackControlValue.String(dto.String ?? string.Empty);
                default:
                    return null;
            }
        }

        static SnapshotDto ToDto(BfvFeedbackControlSnapshot snapshot)
        {
            if (snapshot == null) return null;
            var dto = new SnapshotDto
            {
                TargetId = snapshot.TargetId,
                DisplayName = snapshot.DisplayName,
                Version = snapshot.Version,
                Status = (int)snapshot.Status,
                Revision = snapshot.Revision,
                Capabilities = (int)snapshot.Capabilities,
                Settings = new List<SettingDto>(),
                Saved = new Dictionary<string, ValueDto>(StringComparer.Ordinal),
                Effective = new Dictionary<string, ValueDto>(StringComparer.Ordinal)
            };
            foreach (var setting in snapshot.Settings)
            {
                dto.Settings.Add(new SettingDto
                {
                    Key = setting.Key,
                    DisplayName = setting.DisplayName,
                    Kind = (int)setting.Kind,
                    DefaultValue = ToDto(setting.DefaultValue),
                    Minimum = setting.Minimum,
                    Maximum = setting.Maximum
                });
            }
            foreach (var pair in snapshot.Saved) dto.Saved[pair.Key] = ToDto(pair.Value);
            foreach (var pair in snapshot.Effective) dto.Effective[pair.Key] = ToDto(pair.Value);
            return dto;
        }

        static ResultDto ToDto(BfvFeedbackControlResult result)
        {
            if (result == null) return null;
            var dto = new ResultDto
            {
                Outcome = (int)result.Outcome,
                Message = result.Message,
                Issues = new List<IssueDto>(),
                Snapshot = ToDto(result.Snapshot)
            };
            foreach (var issue in result.Issues)
                dto.Issues.Add(new IssueDto { Key = issue.Key, Message = issue.Message });
            return dto;
        }

        static ValueDto ToDto(BfvFeedbackControlValue value)
        {
            if (value == null) return null;
            switch (value.Kind)
            {
                case BfvFeedbackControlKind.Boolean:
                    return value.TryGetBoolean(out var boolean) ? new ValueDto { Kind = 0, Boolean = boolean } : null;
                case BfvFeedbackControlKind.Number:
                    return value.TryGetNumber(out var number) ? new ValueDto { Kind = 1, Number = number } : null;
                case BfvFeedbackControlKind.Integer:
                    return value.TryGetInteger(out var integer) ? new ValueDto { Kind = 2, Integer = integer } : null;
                case BfvFeedbackControlKind.String:
                    return value.TryGetString(out var text) ? new ValueDto { Kind = 3, String = text } : null;
                default:
                    return null;
            }
        }

        sealed class ChangeDto
        {
            public string ExpectedRevision { get; set; }
            public Dictionary<string, ValueDto> Values { get; set; }
        }

        sealed class ValueDto
        {
            public int Kind { get; set; }
            public bool? Boolean { get; set; }
            public double? Number { get; set; }
            public int? Integer { get; set; }
            public string String { get; set; }
        }

        sealed class SnapshotDto
        {
            public string TargetId { get; set; }
            public string DisplayName { get; set; }
            public string Version { get; set; }
            public int Status { get; set; }
            public string Revision { get; set; }
            public int Capabilities { get; set; }
            public List<SettingDto> Settings { get; set; }
            public Dictionary<string, ValueDto> Saved { get; set; }
            public Dictionary<string, ValueDto> Effective { get; set; }
        }

        sealed class SettingDto
        {
            public string Key { get; set; }
            public string DisplayName { get; set; }
            public int Kind { get; set; }
            public ValueDto DefaultValue { get; set; }
            public double? Minimum { get; set; }
            public double? Maximum { get; set; }
        }

        sealed class ResultDto
        {
            public int Outcome { get; set; }
            public string Message { get; set; }
            public List<IssueDto> Issues { get; set; }
            public SnapshotDto Snapshot { get; set; }
        }

        sealed class IssueDto
        {
            public string Key { get; set; }
            public string Message { get; set; }
        }
    }
}
