using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PreLuna.EnemyHealthBar.BFVFeedback.Core;

static class BfvFeedbackTests
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
        Case("tier-boundaries", () =>
        {
            Check(BfvPolicy.TierFor(119.9f, 120f, 1000f) == BfvKillTier.Minor);
            Check(BfvPolicy.TierFor(120f, 120f, 1000f) == BfvKillTier.Elite);
            Check(BfvPolicy.TierFor(999.9f, 120f, 1000f) == BfvKillTier.Elite);
            Check(BfvPolicy.TierFor(1000f, 120f, 1000f) == BfvKillTier.Boss);
            Check(BfvPolicy.TierFor(float.NaN, 120f, 1000f) == BfvKillTier.Minor);
            Check(BfvPolicy.TierFor(-1f, 120f, 1000f) == BfvKillTier.Minor);
            // Heavy <= minor leaves the elite band empty, matching the deterministic
            // KillFeed rule: below heavy is minor, at/above heavy is boss.
            Check(BfvPolicy.TierFor(79f, 120f, 80f) == BfvKillTier.Minor);
            Check(BfvPolicy.TierFor(80f, 120f, 80f) == BfvKillTier.Boss);
            Check(BfvPolicy.TierFor(120f, 120f, 80f) == BfvKillTier.Boss);
        });

        Case("tier-enemy-type", () =>
        {
            Check(BfvPolicy.TierForEnemyType(0) == BfvKillTier.Minor);
            Check(BfvPolicy.TierForEnemyType(1) == BfvKillTier.Elite);
            Check(BfvPolicy.TierForEnemyType(2) == BfvKillTier.Elite);
            Check(BfvPolicy.TierForEnemyType(3) == BfvKillTier.Boss);
            Check(BfvPolicy.TierForEnemyType(4) == BfvKillTier.Boss);
            Check(BfvPolicy.ParseTierSource("EnemyType") == BfvTierSource.EnemyType);
            Check(BfvPolicy.ParseTierSource("health") == BfvTierSource.Health);
            Check(BfvPolicy.ParseTierSource("nope") == BfvTierSource.EnemyType);
        });

        Case("threshold-clamps", () =>
        {
            Near(BfvPolicy.Threshold(0f, 120f), 120f, "zero");
            Near(BfvPolicy.Threshold(-5f, 120f), 120f, "negative");
            Near(BfvPolicy.Threshold(float.NaN, 120f), 120f, "nan");
            Near(BfvPolicy.Threshold(.5f, 120f), 1f, "below one");
            Near(BfvPolicy.Threshold(2e9f, 120f), 1e9f, "huge");
            Near(BfvPolicy.Threshold(80f, 120f), 80f, "valid");
        });

        Case("style-parse", () =>
        {
            Check(BfvPolicy.ParseStyle("BF1") == BfvSoundStyle.Bf1);
            Check(BfvPolicy.ParseStyle("bf6") == BfvSoundStyle.Bf6);
            Check(BfvPolicy.ParseStyle("BF5") == BfvSoundStyle.Bf5);
            Check(BfvPolicy.ParseStyle("nope") == BfvSoundStyle.Bf5);
            Check(BfvPolicy.ParseStyle("") == BfvSoundStyle.Bf5);
            Check(BfvPolicy.StyleToken(BfvSoundStyle.Bf1) == "bf1");
            Check(BfvPolicy.TierToken(BfvKillTier.Elite) == "elite");
            Check(BfvPolicy.FlightToken(BfvFlight.ArmoredFlying) == "armor_flyer");
        });

        Case("cue-hit-candidates", () =>
        {
            var bf5 = BfvPolicy.CueCandidates(BfvSoundStyle.Bf5, BfvHitCue.Normal, BfvKillTier.Minor, BfvFlight.None);
            Check(bf5.Length == 1 && bf5[0] == "audio/bf5/hit.wav");
            var bf1 = BfvPolicy.CueCandidates(BfvSoundStyle.Bf1, BfvHitCue.Normal, BfvKillTier.Minor, BfvFlight.None);
            Check(bf1[0] == "audio/bf1/hit.wav");
            Check(bf1[1] == "audio/bf5/hit.wav");
            var bf6 = BfvPolicy.CueCandidates(BfvSoundStyle.Bf6, BfvHitCue.Normal, BfvKillTier.Minor, BfvFlight.None);
            Check(bf6.Length == 1 && bf6[0] == "audio/bf5/hit.wav");
            var weak = BfvPolicy.CueCandidates(BfvSoundStyle.Bf1, BfvHitCue.Weakspot, BfvKillTier.Minor, BfvFlight.None);
            Check(weak[0] == "audio/bf5/weakspot.wav");
            Check(weak.Contains("audio/bf5/hit.wav"));
            var armor = BfvPolicy.CueCandidates(BfvSoundStyle.Bf1, BfvHitCue.Armor, BfvKillTier.Minor, BfvFlight.None);
            Check(armor[0] == "audio/bf5/armor.wav");
        });

        Case("cue-kill-candidates", () =>
        {
            var bf5 = BfvPolicy.CueCandidates(BfvSoundStyle.Bf5, BfvHitCue.Kill, BfvKillTier.Minor, BfvFlight.None);
            Check(bf5[0] == "audio/bf5/kill_minor.wav");
            var bf1 = BfvPolicy.CueCandidates(BfvSoundStyle.Bf1, BfvHitCue.Kill, BfvKillTier.Elite, BfvFlight.None);
            Check(bf1[0] == "audio/bf1/kill_elite.wav");
            Check(bf1[1] == "audio/bf5/kill_elite.wav");
            var boss = BfvPolicy.CueCandidates(BfvSoundStyle.Bf1, BfvHitCue.Kill, BfvKillTier.Boss, BfvFlight.None);
            Check(boss[0] == "audio/bf5/kill_boss.wav");
            var flight = BfvPolicy.CueCandidates(BfvSoundStyle.Bf6, BfvHitCue.Kill, BfvKillTier.Elite, BfvFlight.ArmoredFlying);
            Check(flight[0] == "audio/bf6/kill_armor_flyer_elite.wav");
            Check(flight[1] == "audio/bf5/kill_armor_flyer_elite.wav");
            Check(flight[2] == "audio/bf5/kill_elite.wav");
            Check(flight.Contains("audio/bf5/hit.wav"));
        });

        Case("marker-kind-and-scale", () =>
        {
            Check(BfvPolicy.MarkerFor(true, false, false, false) == BfvMarkerKind.Weakspot);
            Check(BfvPolicy.MarkerFor(false, true, false, false) == BfvMarkerKind.Armor);
            Check(BfvPolicy.MarkerFor(true, true, false, false) == BfvMarkerKind.Armor);
            Check(BfvPolicy.MarkerFor(false, false, true, false) == BfvMarkerKind.Kill);
            Check(BfvPolicy.MarkerFor(false, false, false, true) == BfvMarkerKind.Armor);
            Check(BfvPolicy.MarkerPriority(BfvMarkerKind.Kill) > BfvPolicy.MarkerPriority(BfvMarkerKind.Weakspot));
            Near(BfvPolicy.MarkerScale(BfvMarkerKind.Weakspot), 1.18f, "weak scale");
            Near(BfvPolicy.MarkerScale(BfvMarkerKind.Armor), .92f, "armor scale");
            Near(BfvPolicy.MarkerScale(BfvMarkerKind.Kill), 1.35f, "kill scale");
            Near(BfvPolicy.MarkerLifetime(BfvMarkerKind.Kill), .34f, "kill lifetime");
            Near(BfvPolicy.MarkerLifetime(BfvMarkerKind.Normal), .18f, "hit lifetime");
        });

        Case("alpha-fades", () =>
        {
            Near(BfvPolicy.MarkerAlpha(0f, .18f), 1f, "marker spawn");
            Near(BfvPolicy.MarkerAlpha(.09f, .18f), .5f, "marker half");
            Near(BfvPolicy.MarkerAlpha(.18f, .18f), 0f, "marker end");
            Near(BfvPolicy.MarkerAlpha(-.1f, .18f), 0f, "marker negative");
            Near(BfvPolicy.IconAlpha(0f, 2.2f), 0f, "icon spawn");
            Near(BfvPolicy.IconAlpha(.05f, 2.2f), .5f, "icon fade in");
            Near(BfvPolicy.IconAlpha(1f, 2.2f), 1f, "icon hold");
            Near(BfvPolicy.IconAlpha(2.2f, 2.2f), 0f, "icon end");
        });

        Case("screen-and-clamps", () =>
        {
            Near(BfvPolicy.ScreenScale(1080), 1f, "1080");
            Near(BfvPolicy.ScreenScale(2160), 1.6f, "4k clamp");
            Near(BfvPolicy.ScreenScale(480), .75f, "small clamp");
            Near(BfvPolicy.HitMarkerSize(0f), 16f, "marker min");
            Near(BfvPolicy.HitMarkerSize(999f), 200f, "marker max");
            Near(BfvPolicy.KillIconSize(10f), 26f, "icon min");
            Near(BfvPolicy.KillIconLifetime(9f), 6f, "icon lifetime max");
            Check(BfvPolicy.MaxKillIcons(0) == 6);
            Check(BfvPolicy.MaxKillIcons(99) == 12);
            Near(BfvPolicy.Volume(2f), 1f, "volume max");
            Near(BfvPolicy.MinIntervalSeconds(0f), .02f, "interval min");
            Near(BfvPolicy.MergeWindowSeconds(700f), .5f, "merge max");
            Check(BfvPolicy.ShouldMerge(1f, .99f, .02f));
            Check(!BfvPolicy.ShouldMerge(1f, .9f, .05f));
            Check(BfvPolicy.ShouldExpire(.95f, .95f));
            Check(!BfvPolicy.ShouldExpire(.94f, .95f));
        });

        Case("color-parse", () =>
        {
            Check(BfvColor.TryParse("#FFFFFF", out var white));
            Check(white.R == 255 && white.G == 255 && white.B == 255 && white.A == 255);
            Check(BfvColor.TryParse("#ff8c00", out var weak));
            Check(weak.R == 255 && weak.G == 140 && weak.B == 0);
            Check(BfvColor.TryParse("#49ADC9", out var armor));
            Check(armor.R == 73 && armor.G == 173 && armor.B == 201);
            Check(BfvColor.TryParse("#abc", out var shortColor));
            Check(shortColor.R == 170 && shortColor.G == 187 && shortColor.B == 204);
            Check(BfvColor.TryParse("#11223344", out var withAlpha));
            Check(withAlpha.R == 17 && withAlpha.G == 34 && withAlpha.B == 51 && withAlpha.A == 68);
            Check(!BfvColor.TryParse("#12", out _));
            Check(!BfvColor.TryParse("nope", out _));
            var fallback = BfvColor.ParseOrDefault("zzz", BfvColor.Kill);
            Check(fallback.R == BfvColor.Kill.R && fallback.G == BfvColor.Kill.G);
        });

        bool passed = Results.All(x => x.Passed);
        File.WriteAllText(args[0], JsonSerializer.Serialize(
            new { Passed = passed, Count = Results.Count, Results, GameplayTested = false },
            new JsonSerializerOptions { WriteIndented = true }));
        foreach (var result in Results.Where(x => !x.Passed))
            Console.WriteLine("FAIL " + result.Name + ": " + result.Error);
        Console.WriteLine($"BFV feedback: {Results.Count}; passed={passed}");
        return passed ? 0 : 1;
    }
}
