# Gameplay evidence (0.2.0)

Local debug evidence, not a formal release acceptance. Known gaps are listed at the end.

## P0

`p0-striker-hibernate.txt` is the final probe log (34 data lines). It records:

- target `Striker_Hibernate`, `EnemyType=1` (Standard), ID 24;
- health `20/20`, hard `0/5`;
- glue tolerance 4, `AttachedGlueRel` 0→0.75→1, `IsStuckInGlue=True`, `GlueMoveSpeedMulti` 1→0.25→0.12→0;
- one weakspot, multiplier 3; `CanBePushed=True`; `OnFire=False`;
- camera `FPSLookCamera`, FOV 55; host (`master=host`); camera and `FPSCamera` positions matched in this first-person sample.

Earlier header-only probe runs were discarded; the cause was the IL2CPP formatting path documented in [../DEVELOPMENT.md](../DEVELOPMENT.md).

## P1/P2

- Focus-bar screenshot: [../media/p1-focus.jpg](../media/p1-focus.jpg) (`Shooter_Hibernate [SPEC]`).
- `game-log-excerpt.txt`: 0.2.0 load/patch lines.
- A separate run recorded `P2 first local hit: before=30 after=26.229343 max=30`; that `LogOutput.log` was overwritten by the next launch, so it is not presented as a complete log.

## Conclusion

Confirmed: normal-enemy fields, focus bar, `EnemyType` classification, Harmony local hit; user confirmed focus bar, recent bar, third-person focus, foam/F status slot on 2026-11-06.

Not confirmed: client, Boss/MiniBoss gameplay, large hitreact/burn changes, Chinese font, level slot in-game.

Offline: `EnemyTypeMappingTests` 5 + `EnemyBarMathTests` 5 = 10 checks. See [offline-tests.json](offline-tests.json), [build-output.txt](build-output.txt), [build-manifest.json](build-manifest.json).
