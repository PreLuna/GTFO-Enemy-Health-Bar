# Gameplay evidence (0.2.0 → 0.3.0)

Local debug evidence and user acceptance, not a formal release certification.
Known gaps are listed at the end.

## Files

| File | Content |
| --- | --- |
| `p0-striker-hibernate.txt` | 0.2.0 P0 field probe on `Striker_Hibernate` |
| `game-log-excerpt-0.2.0.txt` | 0.2.0 load/patch log excerpt |
| `build-manifest.json` | Final 0.3.0/0.1.0 build snapshot (source lock + DLL SHA256) |
| `build-output.txt` | Final compile result |
| `offline-tests.json` | 17 offline checks (5 + 5 + 7) |
| `user-acceptance-20261106.txt` | User-reported overall pass and scope |

The final build snapshot was taken before the docs-only cleanup. The build used
source lock `C04BF28D1431ECC0FB32F491CB8FB5E45F89F5982579D98BD1E4FAD6A6002677`;
the current `maintenance.lock.json` is the final docs lock and differs only in
documentation files. Non-document inputs were not changed and the build was not
rerun after this cleanup. `out/` was removed after the snapshot.

## P0

`p0-striker-hibernate.txt` is the final probe log (34 data lines). It records:

- target `Striker_Hibernate`, `EnemyType=1` (Standard), ID 24;
- health `20/20`, hard `0/5`;
- glue tolerance 4, `AttachedGlueRel` 0→0.75→1, `IsStuckInGlue=True`, `GlueMoveSpeedMulti` 1→0.25→0.12→0;
- one weakspot, multiplier 3; `CanBePushed=True`; `OnFire=False`;
- camera `FPSLookCamera`, FOV 55; host (`master=host`); camera and `FPSCamera`
  positions matched in this first-person sample.

Earlier header-only probe runs were discarded; the cause was the IL2CPP
formatting path documented in [../DEVELOPMENT.md](../DEVELOPMENT.md).

## P1/P2

- Focus-bar screenshot: [../media/p1-focus.jpg](../media/p1-focus.jpg)
  (`Shooter_Hibernate [SPEC]`).
- `game-log-excerpt-0.2.0.txt`: 0.2.0 load/patch lines.
- A separate run recorded `P2 first local hit: before=30 after=26.229343 max=30`;
  that `LogOutput.log` was overwritten by the next launch, so it is not presented
  as a complete log.
- User confirmed 0.2.0 focus bar, recent bar, third-person focus, foam/F status
  slot on 2026-11-06.

## P3 (0.3.0 / damage numbers 0.1.0)

- Build: 2 assemblies, final hashes and source lock in
  [build-manifest.json](build-manifest.json):
  - parent `431132ECBBC7200228DB1C2BC596E7473CC5DD45C0F972D414B9A9589F2AC3FA`
  - sub-mod `C23DF9205B226F02AAB5062FE3D735F8766727415EA40AEEAA75BCE85FA6E745`
- Offline: 17 checks (5 + 5 + 7) in [offline-tests.json](offline-tests.json).
- The parent writes one `P3 first local hit: applied=... raw=... limb=... type=...`
  line per session; it is a probe, not proof of the visual result.
- The user installed this build and reported **“测试通过十分满意”**
  (2026-11-06). See [user-acceptance-20261106.txt](user-acceptance-20261106.txt).
  This is recorded as a user-reported overall pass, not as a per-feature matrix.
- Still not claimed by this evidence: client/non-host behavior, Boss/MiniBoss
  gameplay, Chinese dynamic font, level slot, or exhaustive weakspot/armor/kill/
  4K/edge-case coverage. The in-game matrix remains in
  [../DAMAGE_NUMBERS.md](../DAMAGE_NUMBERS.md).

## Conclusion

Confirmed by earlier local runs: P0 fields, focus bar, recent bar, `EnemyType`
classification, Harmony local hit. Confirmed by user on 2026-11-06: 0.2.0 four
main bar features; 0.3.0 + 0.1.0 overall test pass and satisfaction.

Not confirmed: client, Boss/MiniBoss gameplay, large hitreact/burn changes,
Chinese font, level slot in-game, exhaustive damage-number matrix.

Offline: `EnemyTypeMappingTests` 5 + `EnemyBarMathTests` 5 + `DamageNumberTests` 7
= 17 checks. See [offline-tests.json](offline-tests.json),
[build-output.txt](build-output.txt), [build-manifest.json](build-manifest.json).
