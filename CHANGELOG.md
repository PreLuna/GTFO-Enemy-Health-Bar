# Changelog

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
