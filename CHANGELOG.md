# Changelog

## EnemyHealthBar 0.3.0 / EnemyHealthBar.DamageNumbers 0.1.0 — 2026-11-06

- Parent: full local-hit capture (`BulletDamage`/`MeleeDamage`): hit point/direction,
  `dam`, `precisionMulti`, limb id/type, health before/after/max, kill/immune.
- Parent: public `EnemyHitApi.HitRegistered` (`EnemyHitInfo`) and `HudVisible`;
  the health bar does not render damage numbers and does not depend on the sub-mod.
- Parent: honor `General/Enabled`; `PageDown` hides the bar and sets `HudVisible=false`.
- Sub-mod: `PreLuna.EnemyHealthBar.DamageNumbers` 0.1.0, hard dependency on parent 0.3.0.
- Sub-mod: Division-2-inspired right-aligned stacks at the hit point; white body,
  amber weakspot, cyan armor, `IMMUNE`; kill red short underline.
- Sub-mod: displays actual health loss per damage call; shotgun pellets stay separate
  (`MergeWindowMs=0` default); `MaxRows=12`.
- Sub-mod: system-font fallback (Bahnschrift SemiCondensed → Bahnschrift → Arial Narrow
  → Segoe UI Semibold → IMGUI default); no Borda/asset bundle shipped.
- Sub-mod: warns when old `com.dak.DamageNumbers` is loaded; disable it to avoid duplicates.
- Build/test: 2 assemblies, 17 offline checks (5 + 5 + 7).
- User-confirmed 2026-11-06: installed 0.3.0 + 0.1.0, reported
  “测试通过十分满意” (overall pass; client/Boss/Chinese font/level slot and
  exhaustive edge cases remain unverified).

## 0.2.0 — 2026-11-06

- P1 focus bar: name/type/health, hard pips, glue strip, ghost, fade, ASCII labels.
- P2 local hits: Harmony `BulletDamage`/`MeleeDamage`, up to 3 recent bars, 3s/0.4s, line of sight.
- `EnemyType` role/Boss classification; Boss/MiniBoss only focus/recent, no permanent list.
- Status slots (fire/stuck/tagged/no-push/glue-immune), empty slots collapsed.
- `EnemyHudProfileRegistry` and `EnemyHudApi.RegisterProfile` for custom enemies.
- Third-person camera-center fallback; renderer-bounds anchor cache; 4K scaling.
- Build/test: 1 assembly, 10 offline checks.
- User-confirmed: focus bar, recent bar, third-person focus, foam/F status slot.

## 0.1.0 — 2026-11-06

- P0 probe: read health, hitreact, glue, burn, weakspot, `EnemyType`, camera/aim identity; logged to `BepInEx/PreLuna.EnemyHealthBar-P0.log`.
- P0 findings: normal `Striker_Hibernate`; host-only; first-person camera `FPSLookCamera`.
