using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PreLuna.EnemyHealthBar.Core;

static class EnemyTypeMappingTests
{
    sealed record Result(string Name, bool Passed, string Error);
    static readonly List<Result> Results = new();
    static void Check(bool value, string message = "assertion failed") { if (!value) throw new Exception(message); }
    static void Case(string name, Action test)
    {
        try { test(); Results.Add(new(name, true, null)); }
        catch (Exception error) { Results.Add(new(name, false, error.ToString())); }
    }

    static int Main(string[] args)
    {
        Case("numeric-values-match-game-enum", () =>
        {
            Check((int)EnemyTypeKind.Weakling == 0);
            Check((int)EnemyTypeKind.Standard == 1);
            Check((int)EnemyTypeKind.Special == 2);
            Check((int)EnemyTypeKind.MiniBoss == 3);
            Check((int)EnemyTypeKind.Boss == 4);
        });
        Case("from-numeric-maps-all-known-values", () =>
        {
            Check(EnemyTypeMapping.FromNumeric(0) == EnemyTypeKind.Weakling);
            Check(EnemyTypeMapping.FromNumeric(1) == EnemyTypeKind.Standard);
            Check(EnemyTypeMapping.FromNumeric(2) == EnemyTypeKind.Special);
            Check(EnemyTypeMapping.FromNumeric(3) == EnemyTypeKind.MiniBoss);
            Check(EnemyTypeMapping.FromNumeric(4) == EnemyTypeKind.Boss);
        });
        Case("unknown-and-out-of-range-are-unknown", () =>
        {
            Check(EnemyTypeMapping.FromNumeric(-1) == EnemyTypeKind.Unknown);
            Check(EnemyTypeMapping.FromNumeric(5) == EnemyTypeKind.Unknown);
            Check(EnemyTypeMapping.FromNumeric(99) == EnemyTypeKind.Unknown);
        });
        Case("boss-like-only-boss-and-miniboss", () =>
        {
            Check(EnemyTypeMapping.IsBossLike(EnemyTypeKind.Boss));
            Check(EnemyTypeMapping.IsBossLike(EnemyTypeKind.MiniBoss));
            Check(!EnemyTypeMapping.IsBossLike(EnemyTypeKind.Special));
            Check(!EnemyTypeMapping.IsBossLike(EnemyTypeKind.Standard));
            Check(!EnemyTypeMapping.IsBossLike(EnemyTypeKind.Unknown));
        });
        Case("labels-and-role-keys-are-stable", () =>
        {
            Check(EnemyTypeMapping.Label(EnemyTypeKind.Boss) == "Boss");
            Check(EnemyTypeMapping.RoleKey(EnemyTypeKind.MiniBoss) == "miniboss");
            Check(EnemyTypeMapping.Label(EnemyTypeKind.Unknown) == "Unknown");
        });

        bool passed = Results.All(r => r.Passed);
        File.WriteAllText(args[0], JsonSerializer.Serialize(new { Passed = passed, Count = Results.Count, Results, GameplayTested = false },
            new JsonSerializerOptions { WriteIndented = true }));
        foreach (var result in Results.Where(r => !r.Passed)) Console.WriteLine("FAIL " + result.Name + ": " + result.Error);
        Console.WriteLine($"Enemy type mapping: {Results.Count}; passed={passed}");
        return passed ? 0 : 1;
    }
}
