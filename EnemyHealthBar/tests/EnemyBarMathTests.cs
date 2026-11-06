using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PreLuna.EnemyHealthBar.Core;

static class EnemyBarMathTests
{
    sealed record Result(string Name, bool Passed, string Error);
    static readonly List<Result> Results = new();
    static void Check(bool value, string message = "assertion failed") { if (!value) throw new Exception(message); }
    static void Near(float actual, float expected, string name) => Check(Math.Abs(actual - expected) < 1e-4f, name + ": " + actual + " != " + expected);
    static void Case(string name, Action test)
    {
        try { test(); Results.Add(new(name, true, null)); }
        catch (Exception error) { Results.Add(new(name, false, error.ToString())); }
    }

    static int Main(string[] args)
    {
        Case("fraction-clamps-and-rejects-invalid", () =>
        {
            Near(EnemyBarMath.Fraction(5f, 10f), .5f, "half");
            Near(EnemyBarMath.Fraction(20f, 10f), 1f, "high");
            Near(EnemyBarMath.Fraction(-1f, 10f), 0f, "low");
            Near(EnemyBarMath.Fraction(float.NaN, 10f), 0f, "nan");
            Near(EnemyBarMath.Fraction(1f, 0f), 0f, "zero max");
        });
        Case("trail-steps-down-and-snaps", () =>
        {
            Near(EnemyBarMath.TrailStep(.8f, .5f, .1f, .4f), .55f, "step");
            Near(EnemyBarMath.TrailStep(.52f, .5f, .1f, .4f), .5f, "snap");
            Near(EnemyBarMath.TrailStep(.4f, .5f, .1f, .4f), .5f, "below current");
        });
        Case("fade-alpha-hold-and-fade", () =>
        {
            Near(EnemyBarMath.FadeAlpha(2f, 1f, 3f, .4f), 1f, "hold");
            Near(EnemyBarMath.FadeAlpha(4.2f, 1f, 3f, .4f), .5f, "fade");
            Near(EnemyBarMath.FadeAlpha(5f, 1f, 3f, .4f), 0f, "done");
        });
        Case("profile-registry-by-id-and-name", () =>
        {
            EnemyHudProfileRegistry.Clear();
            Check(EnemyHudProfileRegistry.Register(new EnemyHudProfile("123", "Custom", "BOSS", "LV 5", true, .2f, 4)));
            Check(EnemyHudProfileRegistry.Register(new EnemyHudProfile("Custom_Name", "Name", "SPEC", "", null, 0f, 0)));
            Check(EnemyHudProfileRegistry.TryGet("123", out var id) && id.DisplayName == "Custom" && id.TierText == "LV 5");
            Check(EnemyHudProfileRegistry.TryGet("custom_name", out var name) && name.DisplayName == "Name");
            Check(EnemyHudProfileRegistry.Unregister("123") && !EnemyHudProfileRegistry.TryGet("123", out _));
            EnemyHudProfileRegistry.Clear();
        });
        Case("invalid-profile-key-is-rejected", () =>
        {
            EnemyHudProfileRegistry.Clear();
            Check(!EnemyHudProfileRegistry.Register(new EnemyHudProfile("", "x", "", "", null, 0f, 0)));
            Check(EnemyHudProfileRegistry.Count == 0);
        });

        bool passed = Results.All(x => x.Passed);
        File.WriteAllText(args[0], JsonSerializer.Serialize(new { Passed = passed, Count = Results.Count, Results, GameplayTested = false },
            new JsonSerializerOptions { WriteIndented = true }));
        foreach (var result in Results.Where(x => !x.Passed)) Console.WriteLine("FAIL " + result.Name + ": " + result.Error);
        Console.WriteLine($"Enemy bar math/profile: {Results.Count}; passed={passed}");
        return passed ? 0 : 1;
    }
}
