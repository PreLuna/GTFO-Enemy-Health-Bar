using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PreLuna.EnemyHealthBar.Control;

static class EnemyHudConfigFileStoreTests
{
    sealed record Result(string Name, bool Passed, string Error);
    static readonly List<Result> Results = new();
    static readonly string Root = Path.Combine(Path.GetTempPath(), "enemyhud-config-store-tests-" + Guid.NewGuid().ToString("N"));

    static void Check(bool value, string message = "assertion failed")
    {
        if (!value) throw new Exception(message);
    }

    static void Case(string name, Action test)
    {
        try { test(); Results.Add(new(name, true, null)); }
        catch (Exception error) { Results.Add(new(name, false, error.ToString())); }
    }

    static string WriteFixture(string name, string text)
    {
        Directory.CreateDirectory(Root);
        var path = Path.Combine(Root, name);
        File.WriteAllText(path, text.Replace("\r\n", "\n"));
        return path;
    }

    static int Main(string[] args)
    {
        Case("preserves-comments-unknown-and-other-sections", () =>
        {
            var path = WriteFixture("sample.cfg", "## header\n[General]\n# keep\nEnabled = true\nUnknownKey = 42\n\n[Other]\nShared = \"x\"\n");
            EnemyHudConfigFileStore.Save(path, new[]
            {
                new EnemyHudConfigWrite("General", "Enabled", "false")
            });
            var text = File.ReadAllText(path);
            Check(text.Contains("## header"), "header");
            Check(text.Contains("# keep"), "comment");
            Check(text.Contains("Enabled = false"), "updated");
            Check(text.Contains("UnknownKey = 42"), "unknown");
            Check(text.Contains("[Other]") && text.Contains("Shared = \"x\""), "other section");
        });

        Case("updates-multiple-sections", () =>
        {
            var path = WriteFixture("multi.cfg", "[Display]\nMaxDistance = 60\n[Bar]\nWidth = 200\nHeight = 5\n");
            EnemyHudConfigFileStore.Save(path, new[]
            {
                new EnemyHudConfigWrite("Display", "MaxDistance", "80"),
                new EnemyHudConfigWrite("Bar", "Width", "240"),
                new EnemyHudConfigWrite("Bar", "Opacity", "0.7")
            });
            var text = File.ReadAllText(path);
            Check(text.Contains("MaxDistance = 80"), "distance");
            Check(text.Contains("Width = 240"), "width");
            Check(text.Contains("Height = 5"), "height unchanged");
            Check(text.Contains("Opacity = 0.7"), "inserted opacity");
        });

        Case("adds-missing-section", () =>
        {
            var path = WriteFixture("missing-section.cfg", "[General]\nEnabled = true\n");
            EnemyHudConfigFileStore.Save(path, new[]
            {
                new EnemyHudConfigWrite("Bar", "Width", "200")
            });
            var text = File.ReadAllText(path);
            Check(text.Contains("[General]"), "existing");
            Check(text.Contains("[Bar]") && text.Contains("Width = 200"), "new section");
        });

        Case("leaves-no-temp-or-backup-on-success", () =>
        {
            var path = WriteFixture("cleanup.cfg", "[General]\nEnabled = true\n");
            EnemyHudConfigFileStore.Save(path, new[]
            {
                new EnemyHudConfigWrite("General", "Enabled", "false")
            });
            Check(!File.Exists(path + ".tmp"), "tmp");
            Check(!File.Exists(path + ".bak"), "bak");
        });

        Case("write-failure-keeps-original", () =>
        {
            var path = WriteFixture("readonly.cfg", "[General]\nEnabled = true\n# keep\n");
            var original = File.ReadAllText(path);
            File.SetAttributes(path, FileAttributes.ReadOnly);
            var failed = false;
            try
            {
                EnemyHudConfigFileStore.Save(path, new[]
                {
                    new EnemyHudConfigWrite("General", "Enabled", "false")
                });
            }
            catch
            {
                failed = true;
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }

            Check(failed, "save should fail on read-only destination");
            Check(File.ReadAllText(path) == original, "original content changed");
            Check(!File.Exists(path + ".tmp"), "tmp residue");
        });

        try { if (Directory.Exists(Root)) Directory.Delete(Root, true); } catch { }
        var output = args.Length > 0 ? args[0] : "EnemyHudConfigFileStoreTests.json";
        File.WriteAllText(output, JsonSerializer.Serialize(Results, new JsonSerializerOptions { WriteIndented = true }));
        return Results.Exists(result => !result.Passed) ? 1 : 0;
    }
}
