using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PreLuna.EnemyHealthBar.BFVFeedback.Control;

static class BfvFeedbackControlTests
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

    static BfvFeedbackControlState CreateState(string path, Dictionary<string, BfvFeedbackControlValue> values)
    {
        return new BfvFeedbackControlState(
            path,
            key => values.TryGetValue(key, out var value) ? value : null,
            (key, value) => values[key] = value,
            key => values[key].ToString(),
            () => false,
            _ => { });
    }

    static int Main(string[] args)
    {
        Case("definitions-cover-26-p1-settings", () =>
        {
            Check(BfvFeedbackControlState.Definitions.Count == 26, "count");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var setting in BfvFeedbackControlState.Definitions)
                Check(keys.Add(setting.Key), "duplicate " + setting.Key);
            Check(keys.Contains("styleIndex"), "style");
            Check(keys.Contains("healthTiering"), "tier");
            Check(keys.Contains("colorNormal") && keys.Contains("colorKill"), "colors");
        });

        Case("validates-style-range-and-color-format", () =>
        {
            var values = new Dictionary<string, BfvFeedbackControlValue>(StringComparer.Ordinal);
            var state = CreateState(Path.Combine(Path.GetTempPath(), "bfv-control-validate.cfg"), values);
            var snapshot = state.ReadSnapshot();

            var badStyle = new BfvFeedbackControlChange(snapshot.Revision,
                new Dictionary<string, BfvFeedbackControlValue> { ["styleIndex"] = BfvFeedbackControlValue.Number(3) });
            Check(state.Validate(badStyle).Outcome == BfvFeedbackControlOutcome.ValidationRejected, "style range");

            var badColor = new BfvFeedbackControlChange(snapshot.Revision,
                new Dictionary<string, BfvFeedbackControlValue> { ["colorNormal"] = BfvFeedbackControlValue.String("#GGGGGG") });
            Check(state.Validate(badColor).Outcome == BfvFeedbackControlOutcome.ValidationRejected, "color format");

            var good = new BfvFeedbackControlChange(snapshot.Revision,
                new Dictionary<string, BfvFeedbackControlValue>
                {
                    ["styleIndex"] = BfvFeedbackControlValue.Number(2),
                    ["healthTiering"] = BfvFeedbackControlValue.Boolean(true),
                    ["colorNormal"] = BfvFeedbackControlValue.String("#FF0000")
                });
            Check(state.Validate(good).Outcome == BfvFeedbackControlOutcome.NoChanges, "valid");
        });

        Case("set-persistent-bumps-revision", () =>
        {
            var path = Path.Combine(Path.GetTempPath(), "bfv-control-set-" + Guid.NewGuid().ToString("N") + ".cfg");
            File.WriteAllText(path, "[HitMarker]\nColorNormal = #FFFFFF\n");
            var values = new Dictionary<string, BfvFeedbackControlValue>(StringComparer.Ordinal);
            var state = CreateState(path, values);
            var snapshot = state.ReadSnapshot();
            var change = new BfvFeedbackControlChange(snapshot.Revision,
                new Dictionary<string, BfvFeedbackControlValue>
                {
                    ["colorNormal"] = BfvFeedbackControlValue.String("#FF0000"),
                    ["styleIndex"] = BfvFeedbackControlValue.Number(2)
                });
            var result = state.SetPersistent(change);
            Check(result.Outcome == BfvFeedbackControlOutcome.Success, "success");
            Check(result.Snapshot.Revision != snapshot.Revision, "revision");
            Check(result.Snapshot.Effective["colorNormal"].TryGetString(out var color) && color == "#FF0000", "color");
            Check(File.ReadAllText(path).Contains("ColorNormal = #FF0000"), "file color");
            try { File.Delete(path); } catch { }
        });

        Case("file-store-preserves-comments-and-detects-conflict", () =>
        {
            var path = Path.Combine(Path.GetTempPath(), "bfv-store-" + Guid.NewGuid().ToString("N") + ".cfg");
            File.WriteAllText(path, "# keep me\n[HitMarker]\nColorNormal = #FFFFFF\n");
            var hash = BfvFeedbackControlFileStore.Capture(path);
            var next = BfvFeedbackControlFileStore.Save(path, hash, new[]
            {
                new BfvFeedbackControlWrite("HitMarker", "ColorNormal", "#FF0000"),
                new BfvFeedbackControlWrite("KillIcon", "TierSource", "Health")
            });
            var text = File.ReadAllText(path);
            Check(text.Contains("# keep me"), "comment");
            Check(text.Contains("ColorNormal = #FF0000"), "color");
            Check(text.Contains("[KillIcon]") && text.Contains("TierSource = Health"), "section");
            Check(next != hash, "hash changed");

            var conflict = false;
            try
            {
                BfvFeedbackControlFileStore.Save(path, hash, new[] { new BfvFeedbackControlWrite("HitMarker", "ColorNormal", "#00FF00") });
            }
            catch (BfvFeedbackControlConflictException) { conflict = true; }
            Check(conflict, "conflict");
            try { File.Delete(path); } catch { }
        });

        var output = args.Length > 0 ? args[0] : "BfvFeedbackControlTests.json";
        File.WriteAllText(output, JsonSerializer.Serialize(Results, new JsonSerializerOptions { WriteIndented = true }));
        return Results.Exists(result => !result.Passed) ? 1 : 0;
    }
}
