# Damage numbers sub-mod

This is the implementation note for `PreLuna.EnemyHealthBar.DamageNumbers` 0.1.0.
Visual sources and measured colors are in the workspace
`研究截图/伤害数字_参考/`; the design rationale is in
`研究资料/伤害数字-20261106/研究结论.md` (workspace paths, not shipped here).

## 1. What it is

The parent health bar ships the facts, the sub-mod draws the numbers:

```text
Dam_EnemyDamageBase.BulletDamage / MeleeDamage
  -> parent BulletDamagePatch / MeleeDamagePatch
  -> EnemyHitCapture (local player only, before/after health, limb type)
  -> EnemyHitApi.HitRegistered (public read-only EnemyHitInfo)
  -> DamageNumberRuntime: per-target stack (actual health loss, no merge by default)
  -> IMGUI: projected hit point, right-aligned rows, white/amber/cyan, fade
```

The parent does not render damage numbers and does not depend on the sub-mod.
The sub-mod compiles against `PreLuna.EnemyHealthBar` and declares a hard
`BepInDependency("PreLuna.EnemyHealthBar", "0.3.0")`.

## 2. Style

Reference: Division 2 damage numbers (Interface In Game 4K screenshots +
Bilibili 1080P combat frame). The adaptation keeps the structure, not the
commercial Borda font.

| Kind | Size | Color | Notes |
| --- | --- | --- | --- |
| Normal | 24px @1080p, regular | `#F2F0EA` | body/health |
| Weakspot | 1.22×, bold | `#E8A33D` | weakspot, precision, back-bonus (flag reserved) |
| Armor | 0.85× | `#4FB8E8`, 85% alpha | `eLimbDamageType.Armor` |
| Immune | 0.92× | `#96FAE8` | text `IMMUNE` |
| Kill | keep hit color | red `#E05252` underline | 0.2s short underline, not red text |

- Numbers are right-aligned at the projected hit point + `RightOffset`.
- The newest number is at the bottom; older rows move up by `LineHeight`.
- `Lifetime` 0.95s; last 35% fades out; a small 6px rise accompanies the fade.
- No thick outline: a 1.5px black shadow keeps the digits readable in dark rooms.
- Thousands separators are inserted manually, avoiding IL2CPP format-provider
  problems; applied damage is rounded to an integer, with a minimum of 1.

## 3. Data rules

- `AppliedDamage = HealthBefore - HealthAfter` is the displayed value. This is
  what the player sees drop in the health bar.
- Shotgun pellets are separate `BulletDamage` calls and therefore separate rows.
  `MergeWindowMs=0` by default. Setting it to a positive value merges same-target
  same-kind rows within that window (kept for future tuning).
- `Kill` is `after <= 0`; it only adds the short red underline.
- `Immune` is `Dam_EnemyDamageBase.IsImortal`; it shows `IMMUNE`, no number.
- `Weakspot` comes from `eLimbDamageType.Weakspot` or (non-melee)
  `precisionMulti > 1.01`. `Armor` comes from `eLimbDamageType.Armor`.
- `RawDamage` is `dam * precisionMulti * limbMulti` and is exposed in the API for
  later calibration; it is **not** displayed by default.
- Back-bonus color is disabled until the direction sign convention is measured;
  `EnemyHitInfo.BackBonus` remains in the API as a reserved field.
- Only local-player `BulletDamage`/`MeleeDamage` is covered. Explosions and
  damage-over-time have no reliable local-player attribution and are skipped.

## 4. Fonts

The Division 2 UI uses Borda (commercial, not redistributable). The sub-mod uses
system fallback in this order:

`Bahnschrift SemiCondensed` → `Bahnschrift` → `Arial Narrow` →
`Segoe UI Semibold` → IMGUI default.

The dynamic-font creation uses the same verified IL2CPP path as
`PreLuna.ThirdPersonHUD` (`Font.Internal_CreateDynamicFont`), with a
`HasCharacter('0'/'1')` check before accepting a candidate. No asset bundle,
no font file shipped.

## 5. Configuration

See the README table. Defaults chosen for the confirmed behavior:

- actual health loss, per pellet (`MergeWindowMs=0`);
- `MaxRows=12` so a full shotgun blast is not cut off;
- kill red underline on;
- system font fallback;
- parent `PageDown` hides both bar and numbers (`HudVisible`).

## 6. Test / acceptance

Offline (`DamageNumberTests` 7):

- manual thousands formatting including `int.MaxValue`;
- applied-damage rounding/clamp;
- style tiers and kill accent;
- fade hold/fade boundaries, row offset, trim;
- merge-window and expiry; screen scale clamp.

User acceptance (2026-11-06): the user installed 0.3.0 + 0.1.0 and reported
**“测试通过十分满意”**. This is recorded as an overall pass, not a per-feature
matrix; the items below remain useful regression coverage.

Remaining regression items:

1. Normal body shot: white number equals actual health drop.
2. Weakspot: amber, larger/bold; verify `precisionMulti`/limb type.
3. Armor limb: cyan; verify `eLimbDamageType.Armor` exists on a live target.
4. Shotgun: each pellet one row; no merge; 12-row limit not clipped.
5. Kill: red underline on the killing pellet only.
6. Immune: `IMMUNE` without a number.
7. Third person / 4K / dark room / edge of screen.
8. Client: whether the local client receives `BulletDamage`; if not, numbers are
   host-only and the README must say so.
9. Old `DamageNumbers` loaded: warning appears; disabling it removes duplicates.
   The current install disables it via `.dll.disabled`.

Evidence lives in `docs/evidence/` (user acceptance + build/test snapshot).

## 7. Installation

Both DLLs go to the same plugin directory:

```text
BepInEx/plugins/PreLuna.EnemyHealthBar/
├─ PreLuna.EnemyHealthBar.dll
└─ PreLuna.EnemyHealthBar.DamageNumbers.dll
```

Configs:

```text
BepInEx/config/PreLuna.EnemyHealthBar.cfg
BepInEx/config/PreLuna.EnemyHealthBar.DamageNumbers.cfg
```

Disable the old `DamageNumbers.dll`; `FloatingTextAPI` is not a dependency of the
new sub-mod and can stay if another mod uses it.
