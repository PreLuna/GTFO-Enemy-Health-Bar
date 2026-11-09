using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PreLuna.EnemyHealthBar.Control;

static class EnemyHudControlTests
{
    sealed record Result(string Name, bool Passed, string Error);
    static readonly List<Result> Results = new();

    static void Check(bool value, string message = "assertion failed")
    {
        if (!value) throw new Exception(message);
    }

    static void Case(string name, Action test)
    {
        try { test(); Results.Add(new(name, true, null)); }
        catch (Exception error) { Results.Add(new(name, false, error.ToString())); }
    }

    sealed class FakeBackend
    {
        public readonly Dictionary<string, EnemyHudControlValue> Saved = new();
        public int Saves;
        public bool FailSave;

        public FakeBackend()
        {
            Saved["enabled"] = EnemyHudControlValue.Boolean(true);
            Saved["maxDistance"] = EnemyHudControlValue.Number(60);
            Saved["width"] = EnemyHudControlValue.Number(200);
            Saved["height"] = EnemyHudControlValue.Number(5);
            Saved["opacity"] = EnemyHudControlValue.Number(.55);
            Saved["showName"] = EnemyHudControlValue.Boolean(true);
            Saved["showNumbers"] = EnemyHudControlValue.Boolean(true);
            Saved["showStatusSlots"] = EnemyHudControlValue.Boolean(true);
        }

        public EnemyHudControlValue Read(string key) => Saved.TryGetValue(key, out var value) ? value : null;
        public void Write(string key, EnemyHudControlValue value) => Saved[key] = value;

        public void Save()
        {
            Saves++;
            if (FailSave) throw new InvalidOperationException("save failed");
        }

        public EnemyHudControlState State() => new EnemyHudControlState(Read, Write, Save);
    }

    static EnemyHudControlChange Change(
        EnemyHudControlSnapshot snapshot,
        params (string Key, EnemyHudControlValue Value)[] values)
    {
        var map = new Dictionary<string, EnemyHudControlValue>();
        foreach (var pair in values) map[pair.Key] = pair.Value;
        return new EnemyHudControlChange(snapshot.Revision, map);
    }

    static int Main(string[] args)
    {
        Case("initial-snapshot-p-and-r-match", () =>
        {
            var state = new FakeBackend().State();
            var snapshot = state.ReadSnapshot();
            Check(snapshot.Status == EnemyHudControlStatus.Available, "status");
            Check(snapshot.Saved["maxDistance"].ValueEquals(EnemyHudControlValue.Number(60)), "P");
            Check(snapshot.Effective["maxDistance"].ValueEquals(EnemyHudControlValue.Number(60)), "R");
        });

        Case("set-persistent-writes-p-and-r", () =>
        {
            var backend = new FakeBackend();
            var state = backend.State();
            var result = state.SetPersistent(Change(state.ReadSnapshot(), ("maxDistance", EnemyHudControlValue.Number(80))));
            Check(result.Outcome == EnemyHudControlOutcome.Success, result.Message);
            var latest = state.ReadSnapshot();
            Check(latest.Saved["maxDistance"].ValueEquals(EnemyHudControlValue.Number(80)), "P");
            Check(latest.Effective["maxDistance"].ValueEquals(EnemyHudControlValue.Number(80)), "R");
            Check(backend.Saves == 1, "save once");
        });

        Case("validation-rejects-kind-range-and-unknown", () =>
        {
            var state = new FakeBackend().State();
            var snapshot = state.ReadSnapshot();
            var badKind = state.Validate(Change(snapshot, ("enabled", EnemyHudControlValue.Number(1))));
            Check(badKind.Outcome == EnemyHudControlOutcome.ValidationRejected, "kind");
            var badRange = state.Validate(Change(snapshot, ("maxDistance", EnemyHudControlValue.Number(999))));
            Check(badRange.Outcome == EnemyHudControlOutcome.ValidationRejected, "range");
            var unknown = state.Validate(Change(snapshot, ("unknown", EnemyHudControlValue.Boolean(true))));
            Check(unknown.Outcome == EnemyHudControlOutcome.ValidationRejected, "unknown");
        });

        Case("stale-revision-conflict", () =>
        {
            var backend = new FakeBackend();
            var state = backend.State();
            var snapshot = state.ReadSnapshot();
            backend.Saved["maxDistance"] = EnemyHudControlValue.Number(70);
            state.ReadSnapshot();
            var result = state.SetPersistent(Change(snapshot, ("maxDistance", EnemyHudControlValue.Number(80))));
            Check(result.Outcome == EnemyHudControlOutcome.StateConflict, "stale");
        });

        Case("external-saved-change-bumps-revision", () =>
        {
            var backend = new FakeBackend();
            var state = backend.State();
            var first = state.ReadSnapshot();
            backend.Saved["enabled"] = EnemyHudControlValue.Boolean(false);
            var second = state.ReadSnapshot();
            Check(first.Revision != second.Revision, "revision");
            Check(second.Saved["enabled"].ValueEquals(EnemyHudControlValue.Boolean(false)), "P");
        });

        Case("persistent-save-failure-rolls-back-p-and-r", () =>
        {
            var backend = new FakeBackend { FailSave = true };
            var state = backend.State();
            var result = state.SetPersistent(Change(state.ReadSnapshot(), ("maxDistance", EnemyHudControlValue.Number(80))));
            Check(result.Outcome == EnemyHudControlOutcome.SaveFailed, result.Message);
            var latest = state.ReadSnapshot();
            Check(latest.Saved["maxDistance"].ValueEquals(EnemyHudControlValue.Number(60)), "P");
            Check(latest.Effective["maxDistance"].ValueEquals(EnemyHudControlValue.Number(60)), "R");
            Check(backend.Saved["maxDistance"].ValueEquals(EnemyHudControlValue.Number(60)), "backend restored");
        });

        Case("empty-change-is-nochanges", () =>
        {
            var state = new FakeBackend().State();
            var result = state.Validate(new EnemyHudControlChange(state.Revision, new Dictionary<string, EnemyHudControlValue>()));
            Check(result.Outcome == EnemyHudControlOutcome.NoChanges, result.Message);
        });

        var output = args.Length > 0 ? args[0] : "EnemyHudControlTests.json";
        File.WriteAllText(output, JsonSerializer.Serialize(Results, new JsonSerializerOptions { WriteIndented = true }));
        return Results.Exists(result => !result.Passed) ? 1 : 0;
    }
}
