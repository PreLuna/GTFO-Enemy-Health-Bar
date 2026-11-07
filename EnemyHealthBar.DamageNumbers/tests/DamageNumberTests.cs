using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PreLuna.EnemyHealthBar.DamageNumbers.Core;

static class DamageNumberTests
{
    sealed record Result(string Name, bool Passed, string Error);
    static readonly List<Result> Results = new();

    static void Check(bool value, string message = "assertion failed")
    {
        if (!value) throw new Exception(message);
    }

    static void Near(float actual, float expected, string name)
        => Check(Math.Abs(actual - expected) < 1e-4f, name + ": " + actual + " != " + expected);

    static void Case(string name, Action test)
    {
        try { test(); Results.Add(new(name, true, null)); }
        catch (Exception error) { Results.Add(new(name, false, error.ToString())); }
    }

    static int Main(string[] args)
    {
        Case("format-plain-and-thousands", () =>
        {
            Check(DamageNumberFormat.Number(0) == "0");
            Check(DamageNumberFormat.Number(1) == "1");
            Check(DamageNumberFormat.Number(999) == "999");
            Check(DamageNumberFormat.Number(1000) == "1,000");
            Check(DamageNumberFormat.Number(1234) == "1,234");
            Check(DamageNumberFormat.Number(1234567) == "1,234,567");
            Check(DamageNumberFormat.Number(int.MaxValue) == "2,147,483,647");
            Check(DamageNumberFormat.Number(-5) == "0");
        });

        Case("from-applied-rounds-and-floors", () =>
        {
            Check(DamageNumberFormat.FromApplied(0f) == 0);
            Check(DamageNumberFormat.FromApplied(-1f) == 0);
            Check(DamageNumberFormat.FromApplied(float.NaN) == 0);
            Check(DamageNumberFormat.FromApplied(.4f) == 1);
            Check(DamageNumberFormat.FromApplied(2.49f) == 2);
            Check(DamageNumberFormat.FromApplied(2.5f) == 3);
            Check(DamageNumberFormat.FromApplied(30f) == 30);
        });

        Case("style-tiers", () =>
        {
            var normal = DamageNumberStyle.For(DamageNumberKind.Normal);
            Near(normal.SizeFactor, 1f, "normal size");
            Check(!normal.Bold);
            var weak = DamageNumberStyle.For(DamageNumberKind.Weakspot);
            Near(weak.SizeFactor, 1.22f, "weak size");
            Check(weak.Bold);
            Near(weak.R, .910f, "weak r");
            var armor = DamageNumberStyle.For(DamageNumberKind.Armor);
            Near(armor.SizeFactor, .85f, "armor size");
            Near(armor.Alpha, .85f, "armor alpha");
            Check(!armor.Bold);
            var immune = DamageNumberStyle.For(DamageNumberKind.Immune);
            Check(DamageNumberFormat.ImmuneText == "IMMUNE");
            Near(immune.G, .980f, "immune g");
            Near(DamageNumberStyle.KillAccent.R, .878f, "kill r");
        });

        Case("fade-hold-and-fade", () =>
        {
            Near(DamageNumberMath.FadeAlpha(0f, 1f), 1f, "spawn");
            Near(DamageNumberMath.FadeAlpha(.5f, 1f), 1f, "hold");
            Near(DamageNumberMath.FadeAlpha(.65f, 1f), 1f, "fade start");
            Near(DamageNumberMath.FadeAlpha(.825f, 1f), .5f, "mid fade");
            Near(DamageNumberMath.FadeAlpha(1f, 1f), 0f, "done");
            Near(DamageNumberMath.FadeAlpha(.5f, 0f), 0f, "zero lifetime");
        });

        Case("row-offset-and-trim", () =>
        {
            Near(DamageNumberMath.RowOffset(0, 30f), 0f, "row0");
            Near(DamageNumberMath.RowOffset(2, 30f), 60f, "row2");
            Near(DamageNumberMath.RowOffset(-1, 30f), 0f, "negative row");
            Check(DamageNumberMath.TrimCount(5, 5) == 0);
            Check(DamageNumberMath.TrimCount(8, 5) == 3);
            Check(DamageNumberMath.TrimCount(0, 5) == 0);
        });

        Case("merge-window", () =>
        {
            Check(!DamageNumberMath.ShouldMerge(1f, .99f, 0f));
            Check(DamageNumberMath.ShouldMerge(1f, .99f, .02f));
            Check(!DamageNumberMath.ShouldMerge(1f, .9f, .05f));
            Check(!DamageNumberMath.ShouldMerge(1f, 1f, -.1f));
        });

        Case("expiry-and-screen-scale", () =>
        {
            Check(DamageNumberMath.IsExpired(.95f, .95f));
            Check(!DamageNumberMath.IsExpired(.94f, .95f));
            Check(DamageNumberMath.IsExpired(1f, 0f));
            Near(DamageNumberMath.ScreenScale(1080), 1f, "1080");
            Near(DamageNumberMath.ScreenScale(2160), 2f, "4k");
            Near(DamageNumberMath.ScreenScale(480), .75f, "small");
            Near(DamageNumberMath.ScreenScale(5000), 2.5f, "clamp");
        });

        bool passed = Results.All(x => x.Passed);
        File.WriteAllText(args[0], JsonSerializer.Serialize(
            new { Passed = passed, Count = Results.Count, Results, GameplayTested = false },
            new JsonSerializerOptions { WriteIndented = true }));
        foreach (var result in Results.Where(x => !x.Passed))
            Console.WriteLine("FAIL " + result.Name + ": " + result.Error);
        Console.WriteLine($"Damage numbers: {Results.Count}; passed={passed}");
        return passed ? 0 : 1;
    }
}
