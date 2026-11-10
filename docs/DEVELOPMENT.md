# Development notes

This document is the handoff/reuse map for the independent GTFO enemy health bar. It records the parts that were non-obvious in practice, so a later change does not have to rediscover them.

## 1. Scope and status

Version 0.3.0 implements:

- focus bar at the crosshair;
- up to three recent-hit bars (local player only);
- health, hard-value pips, glue strip, ghost/trail, weakspot tag, status slots;
- `EnemyType`-based role/Boss classification;
- profile registry/API for custom enemies;
- full local-hit capture and the public `EnemyHitApi.HitRegistered` event;
- the separate `PreLuna.EnemyHealthBar.DamageNumbers` 0.1.0 sub-mod (IMGUI
  Division-style stacks built on that event).

User-confirmed on 2026-11-06: 0.2.0 focus bar, recent bar, third-person focus bar,
foam/F status slot; 0.3.0 + 0.1.0 installed and reported “测试通过十分满意”
(overall pass, not a per-feature matrix). Not yet verified: client, Boss/MiniBoss
in-game, large hitreact/burn changes, Chinese font, level slot in-game, and
exhaustive damage-number edge cases.

## 2. Architecture

```text
BepInEx plugin (Plugin.cs)
  ├─ EnemyHudConfig                 config entries
  ├─ Harmony BulletDamagePatch      full local BulletDamage args
  ├─ Harmony MeleeDamagePatch       full local MeleeDamage args
  ├─ EnemyHitCapture                before/after, limb type, flags -> EnemyHitApi
  └─ EnemyHudRuntime : MonoBehaviour
      ├─ EnemyFocus                 FPSCamera ray + camera-center fallback
      ├─ EnemyVitalsReader          game fields -> EnemyHudState
      ├─ EnemyRecentBars            local-hit slots + line of sight
      ├─ EnemyHudRenderer           IMGUI, bars, pips, status slots
      └─ EnemyBarMath               pure fraction/trail/fade math

PreLuna.EnemyHealthBar.DamageNumbers (separate assembly)
  ├─ Plugin                         BepInDependency on 0.3.0
  ├─ DamageNumberRuntime            event -> per-target stack -> OnGUI
  ├─ DamageNumberRenderer           IMGUI rows/shadow/kill underline
  ├─ DamageNumberFont               system-font fallback
  └─ Core (Format/Style/Math)       pure C#; offline tests
```

All Unity reads and drawing happen on the main thread. The pure parts (`EnemyTypeMapping`, `EnemyBarMath`, `EnemyHudProfile`) have no Unity dependency and are covered by offline tests.

## 3. File map

| Path | Responsibility |
| --- | --- |
| `src/Runtime/Plugin.cs` | BepInPlugin identity, config, IL2CPP type registration, Harmony setup/unpatch |
| `src/Runtime/EnemyHitApi.cs` | Public `EnemyHitInfo` struct, `HitRegistered` event, `HudVisible` |
| `src/Runtime/EnemyHitPatch.cs` | `EnemyHitCapture`, `BulletDamagePatch`, `MeleeDamagePatch` |
| `src/Runtime/EnemyHudRuntime.cs` | focus state, fade/ghost, recent bars, OnGUI, `HudVisible` |
| `src/Runtime/EnemyFocus.cs` | ray selection, local-player collider skip, third-person fallback |
| `src/Runtime/EnemyVitalsReader.cs` | reads health/hard/glue/weakspot/type and projects the anchor |
| `src/Runtime/EnemyHudState.cs` | immutable display state and validity/fraction helpers |
| `src/Runtime/EnemyHudRenderer.cs` | IMGUI rectangles/text, type colors, status/level slots |
| `src/Runtime/EnemyRecentBars.cs` | 3 recent targets, 3s hold/0.4s fade, visibility check |
| `src/Runtime/EnemyHudConfig.cs` | BepInEx config and bounds |
| `src/Core/EnemyTypeMapping.cs` | mirrors `eEnemyType`; no Unity reference |
| `src/Core/EnemyBarMath.cs` | fraction/trail/fade math; no Unity reference |
| `src/Core/EnemyHudProfile.cs` | display-only custom-enemy profile + registry |
| `src/Core/EnemyHudApi.cs` | public `RegisterProfile`/`UnregisterProfile` API |
| `EnemyHealthBar.DamageNumbers/src/Core/` | format/style/fade/row math; no Unity reference |
| `EnemyHealthBar.DamageNumbers/src/Runtime/` | plugin/config/runtime/renderer/system-font fallback |
| `EnemyHealthBar.DamageNumbers/tests/` | pure damage-number tests |
| `tests/` | pure tests run by `Test.ps1` |

## 4. Game data used

Read-only fields:

| Purpose | Field |
| --- | --- |
| Health | `EnemyAgent.Damage.Health`, `.HealthMax` |
| Hard value | `Damage.m_damBuildToHitreact`; `EnemyBalancingData.Health.DamageUntilHitreact` |
| Glue | `Damage.AttachedGlueRel`, `.AttachedGlueVolume`, `.IsStuckInGlue`, `.GlueMoveSpeedMulti`; `EnemyBalancingData.GlueTolerance` |
| Burn | `Damage.OnFire`, `.m_heatLevel` |
| Weakspot | `Damage.DamageLimbs[i].m_type == eLimbDamageType.Weakspot`, `.m_weakspotDamageMulti` |
| Identity/type | `EnemyAgent.EnemyData.EnemyType`, `.EnemyDataID`, `.EnemyData.name`, `.SizeMultiplier`, `.IsScout`, `.Alive`, `.IsTagged` |
| Push | `EnemyBalancingData.CanBePushed` |

Semantics that matter:

- GTFO has **no depletable armor pool**; `ArmorDamageMulti` is only a multiplier. The white pips are hitreact, never fake armor.
- GTFO has **no numeric enemy level**; `EnemyType` is a category, not a level.
- `EnemyDataBlock.EnemyType` is the native classifier. Values: `Weakling=0`, `Standard=1`, `Special=2`, `MiniBoss=3`, `Boss=4`. Data is not name-aligned: `Tank_Boss` is `MiniBoss`, `Birther` is `Boss`; classify by enum.
- Glue is independent of health. Use `AttachedGlueRel`; do not accumulate blob counts from local hits.
- `Health` on a client may not match the host for every sub-resource; hide unknown sub-bars rather than estimating.

## 5. Focus and projection

Selection:

1. Read `PlayerAgent.FPSCamera.CameraRayPos/CameraRayDir` (fall back to `Position/Forward`).
2. `Physics.RaycastNonAlloc` with bullet/camera/enemy/enemy-damageable masks.
3. Skip colliders under the local player (third-person body/weapon).
4. Take the nearest remaining hit; only an `EnemyAgent` parent is valid.
5. If that fails, try `Camera.ViewportPointToRay(0.5, 0.5)` for third person.
6. Do not show through walls; max 60m by default.

Projection uses Unity `Camera.WorldToScreenPoint`; GUI Y is flipped (`Screen.height - screen.y`). The renderer scales by `Screen.height / 1080` (0.75–2.5) so 4K is not a tiny 1080p-sized bar.

Anchor:

- Compute the highest `Renderer.bounds.max.y` in the target hierarchy, cached 0.35s; this handles giant enemies whose highest bone is inside the body.
- Fallback: highest limb transform, then `MainModelTransform` + 1.55×size.
- Add 0.22m + 0.10×size; projection is clamped to 10–46% screen height.

## 6. Harmony local hit flow

Patch:

- `Dam_EnemyDamageBase.BulletDamage` (`BulletDamagePatch`)
- `Dam_EnemyDamageBase.MeleeDamage` (`MeleeDamagePatch`)

Each Prefix captures the full call only when the source `Agent` is the local player:
`dam`, `precisionMulti`, `staggerMulti`, `limbID`, `position`, `direction`, the
pre-hit `m_damBuildToHitreact`, and `_Health_k__BackingField`. Postfix reads
`_Health/_HealthMax`, resolves the limb type/multiplier and `DamageUntilHitreact`,
computes applied/raw damage and kill/immune flags, publishes `EnemyHitInfo` through
`EnemyHitApi.HitRegistered`, and only calls `EnemyHudRuntime.RegisterHit` when health
actually dropped.

Rules:

- Never write health or send network packets.
- Use `AccessTools.DeclaredMethod` (the base class has overloads); patch each
  method type explicitly so one missing target does not block the other.
- `Harmony.UnpatchAll(id)` is obsolete in this HarmonyX build; use `harmony.UnpatchSelf()`.
- Recent bars read live health every frame; local damage is only used to choose targets, not to synthesize health.
- A hit on the currently focused enemy is still registered; the recent bar is skipped while focused, then can appear when the crosshair moves away.
- The hard-pip trigger flash is `applied * staggerMulti >= DamageUntilHitreact` (with the pre-hit value) via `EnemyBarMath.HardTrigger`. It is display-only; the game may consume the live value inside the same damage call, so this flash is not a persistent meter and does not estimate health damage.
- Displayed damage is `before - after`; the raw estimate
  `dam * precisionMulti * limbMulti` is published for later calibration.
- Back-bonus color is reserved/off until the direction sign is measured.
- `limbID` uses `0 <= limbID < DamageLimbs.Length`; upstream's `> 0` check is not copied.
- Event listeners are isolated with try/catch, so a broken sub-mod cannot break the bar.
- `EnemyHitApi.Clear()` runs on parent unload; the sub-mod unsubscribes in `OnDestroy`.

## 7. Recent bars / ghost

- Capacity 3, hold 3s, fade 0.4s.
- Entry keys use `EnemyAgent.Pointer` (the focus key). Do not key by `Dam_EnemyDamageBase.Pointer`: that value never matches the focus key, so the focused target would draw both its focus bar and a compact recent bar.
- Each entry also stores a 0.22 s hard-flash timestamp; the focus bar keeps the same flash in `EnemyHudRuntime` and clears it when the target changes or the HUD hides.
- Visibility: `Physics.Linecast` from the camera to a body point; only the target itself (or no hit) is visible. Checked at 0.1s intervals.
- Health is read from the live object; recent entries are cleared when the target dies or the level ends.
- Ghost is a light-gray segment between current health and the previous fraction, shrinking over 0.35s. It is visual only.

## 8. Rendering and profiles

- 1×1 texture plus `GUI.skin.label` GUI styles; no external assets.
- Type colors: Weakling gray, Standard red, Special purple, MiniBoss/Boss gold; low health red.
- Status slots (default 4): `F` fire, `S` stuck/glue, `T` tagged, `P` cannot be pushed, `I` glue-immune. Empty slots are hidden unless `ShowEmptyStatusSlots=true`.
- Level slot is hidden by default (`ShowLevelSlot=false`); when enabled it is a dashed `LV` box, or the profile's `TierText`.
- Custom profile (`EnemyHudApi.RegisterProfile`) is display-only and keyed by `EnemyDataID` first, then `EnemyDataBlock.name`. It can override display name, role text, tier, boss flag, anchor offset and extra status slots.

## 9. IL2CPP / BepInEx pitfalls (important)

These were observed on the actual GTFO/BepInEx 6 build:

1. `Single.ToString(string, IFormatProvider)` is missing. `FormattableString.Invariant`, interpolated format specifiers and `float.ToString("F2")` can throw `Method not found`.
   - Use parameterless `value.ToString()` or integer conversions; avoid format providers.
2. `Vector3.ToString("F2")` also triggered the same underlying float formatting path.
   - Build component strings from `x/y/z` parameterless conversions.
3. `CameraManager.GetCurrentCamera()` is the output camera; `FPSCamera` is the game aim source. They can differ in third person; use the FPSCamera ray first and camera-center fallback.
4. `ClassInjector.RegisterTypeInIl2Cpp<T>()` must run before `AddComponent<T>()`.
5. Renderer bounds (`GetComponentsInChildren<Renderer>`) should be cached; don't do it every frame.
6. Use `Harmony.UnpatchSelf()`, not `UnpatchAll(id)`, with this HarmonyX version.
7. ASCII avoids the unresolved CJK dynamic-font work. If adding Chinese, reuse the known-good `HolographicHud.PrepareFont` native path from `PreLuna.ThirdPersonHUD`; the two `CreateDynamicFontFromOSFont` factories were not reliable here.

## 10. Build, test, install

```powershell
./Build.ps1 -GameRoot 'D:/Program/Game/Steam/Steam/steamapps/common/GTFO' -RuntimeRoot 'C:/Program Files/dotnet/shared/Microsoft.NETCore.App/6.0.16'
./Test.ps1 -RuntimeRoot 'C:/Program Files/dotnet/shared/Microsoft.NETCore.App/6.0.16'
```

- Build: `out/package/BepInEx/plugins/PreLuna.EnemyHealthBar/PreLuna.EnemyHealthBar.dll` and
  `out/package/BepInEx/plugins/PreLuna.EnemyHealthBar.DamageNumbers/PreLuna.EnemyHealthBar.DamageNumbers.dll`.
- Install: both DLLs in `BepInEx/plugins/本地定制/PreLuna.EnemyHealthBar/`.
- Config: `BepInEx/config/PreLuna.EnemyHealthBar.cfg`,
  `BepInEx/config/PreLuna.EnemyHealthBar.DamageNumbers.cfg`.
- Parent has no runtime dependencies; do not load a second enemy-bar mod simultaneously.
  Sub-mod hard-depends on parent 0.3.0; disable old `com.dak.DamageNumbers`.

## 11. Test matrix

Automated (17 checks):

- `EnemyTypeMappingTests`: enum values, unknown mapping, boss-like predicate, labels (5).
- `EnemyBarMathTests`: fraction bounds/invalid, ghost trail, fade alpha, profile registry (5).
- `DamageNumberTests`: formatting, rounding, style tiers, fade, row/trim, merge window,
  expiry, screen scale (7).

Gameplay evidence:

- P0: `Striker_Hibernate`, health 20/20, hard 0/5, glue tolerance 4, glue rel 0→1, stuck true, weakspot ×3, camera `FPSLookCamera`.
- P1: focus bar on `Striker_Bullrush` and `Shooter_Hibernate [SPEC]`; anchor/scale accepted.
- P2: first local hit `before=30 after=26.229343 max=30`; user confirmed focus, recent, third-person focus, foam/F status slot.
- P3: user installed 0.3.0 + 0.1.0 on 2026-11-06 and reported
  “测试通过十分满意” (overall pass; not a per-feature matrix). The patched
  fields are logged once as `P3 first local hit: applied=... raw=... limb=...
  type=... weakspot=... armor=... kill=... immune=...`.
- Still open: client, Boss/MiniBoss in-game, large hitreact/burn, Chinese font,
  level slot in-game, multi-target clutter, exhaustive damage-number edge cases.

## 12. Safe extension points

- Add a display override: `EnemyHudApi.RegisterProfile`.
- Consume damage facts: `EnemyHitApi.HitRegistered` (`EnemyHitInfo`); do not patch the
  same methods again unless you need data the parent does not publish.
- Add a data provider/status: extend `EnemyHudState` and `EnemyVitalsReader`, keep rendering in `EnemyHudRenderer`.
- Add a new status: append a slot label in `DrawStatusSlots` and a color mapping.
- Add a layout: keep the state reader unchanged; add a renderer path or a second renderer.
- Add a custom boss: set `EnemyType` in its `EnemyDataBlock`, or register `IsBoss=true`; do not add a name list.
- Tune the damage-number stack in `EnemyHealthBar.DamageNumbers/src/Core/` first; the
  runtime only projects positions and draws.

## 13. References

- ForgeUI 0.1.0 (MIT): `EnemyBarBook`, `ClipTransform`, `LocalHits` patterns; upstream source is kept in the workspace research folder.
- HawChow EnemyHealthBar / KillFeed: focus/occlusion, head anchor, weakspot/glue-tag behavior reference (no explicit license found; no code copied).
- EnemyBossbars: boss event/config reference (not adopted; native `EnemyType` is used).
- DamageSync: host/client health-sync caveat reference (not a dependency).
- GTFO-Modding/DamageNumbers + FloatingTextAPI (MIT, Copyright (c) 2022 Dak): local-hit
  hook/parameter flow and armor/weakspot classification reference; no FloatingTextAPI
  dependency, TMP asset bundle, or Random-3D-float presentation is shipped.
