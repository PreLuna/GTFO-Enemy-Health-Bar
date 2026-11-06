# Research and reuse notes

This is the compact record of the upstream study behind the plugin. Full raw research lives in the workspace `研究资料/敌人血条-20261106/` folder; this file keeps the reusable conclusions inside the repository.

## 1. Upstream material

| Source | What it taught | Reuse decision |
| --- | --- | --- |
| ForgeUI 0.1.0 (`NAinfini`) | `ClipTransform`, `EnemyBarBook` (slot/replace/fade/line-of-sight), `LocalHits` Harmony pattern, pure tests | MIT. Pure ideas/tests reusable with attribution; no ForgeCore/HudCanvas runtime dependency |
| HawChow EnemyHealthBar 1.0.0 | FPSCamera ray, occlusion, head anchor, fade, 60m focus bar | Behavior reference only; no explicit license; no code copied |
| HawChow KillFeed 1.2.8 | weakspot labels, `GlueTolerance`/per-blob formula | Behavior reference only; no explicit license; no code copied |
| Brandonious EnemyBossbars 1.1.1 | Boss alert event, configured boss list | Not adopted; native `EnemyDataBlock.EnemyType` is the classifier |
| randomuserhi DamageSync | host sends `SetHealth`; client receives; client readings can differ | Test-boundary reference only; not a dependency |

ForgeUI MIT license text is in [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md). The project does not ship ForgeUI/ForgeCore.

## 2. Why not a boss list

`GameData.EnemyDataBlock` already has `EnemyType` (`eEnemyType`):

| Value | Enum | Examples from the local dump |
| ---: | --- | --- |
| 0 | `Weakling` | Shadow |
| 1 | `Standard` | Striker, Shooter, Flyer, Pouncer |
| 2 | `Special` | Scout, Shooter_Spread |
| 3 | `MiniBoss` | Tank, Tank_Boss, StrikerBig |
| 4 | `Boss` | Striker_Boss, Birther_Boss, MegaMother, Shooter_Big, SquidBoss_* |

Data is not name-aligned: `Tank_Boss` is `MiniBoss`, `Birther` is `Boss`. Classify by the enum, never by name or `HealthMax`.

Boss/MiniBoss are **not persistent bars**: they show only while focused or recently hit, matching the user's rule. A custom enemy can set `EnemyType` in its data block; otherwise it falls back to `Standard` and can be overridden through the profile API.

## 3. Why no armor/level values

- There is no depletable GTFO armor pool; `ArmorDamageMulti` is only a damage multiplier. The white pips are hitreact (`m_damBuildToHitreact`), never armor.
- There is no numeric player-facing enemy level; `EnemyDataID`/`persistentID` are identity, not level. `TierText` is a custom display field and the level slot is hidden by default.
- Glue is its own resource; use `AttachedGlueRel` and `GlueTolerance`, not accumulated local blob hits.
- The “glue blob count” formula from KillFeed assumes 3 units per blob; it is optional/configurable and not a general GTFO rule.

## 4. Focus vs local-hit models

- HawChow: crosshair focus only; good for a single, low-clutter bar, but lacks multi-target/status.
- ForgeUI: local-hit driven; good for multi-target, needs Harmony.
- This plugin: focus bar + up to 3 recent local-hit bars. The recent bar for the focused target is registered but skipped while focused, so it can reappear when the crosshair moves away.

## 5. Data and synchronization cautions

- Health is read from the live `Dam_EnemyDamageBase`; never inferred from local damage.
- DamageSync proves host/client health can be explicitly synchronized; it does **not** prove hitreact/glue/burn are synchronized. P0 was host-only; client readings remain unverified.
- The P0 probe found `FPSLookCamera` in first person with camera position equal to `FPSCamera`; third person uses the camera-center fallback added later.

## 6. Design conclusions

- Focus bar above the renderer bounds top, not a bone: giant enemies otherwise put the bar inside the body.
- Scale with `Screen.height/1080`, not fixed pixels; 4K needs ~2× width/font.
- ASCII first; CJK dynamic font is a separate hard interop task.
- Empty status/level slots default collapsed; do not reserve screen space for fake data.
- Profile registry is the safe extension seam; avoid hard-coded enemy names.

## 7. Raw research index

Workspace paths (not part of this repository):

- `研究资料/敌人血条-20261106/全境封锁式血条适配设计.md`
- `研究资料/敌人血条-20261106/线稿与对照/`
- `研究资料/敌人血条-20261106/MOD代码研究与复用清单.md`
- `研究资料/敌人血条-20261106/施工方案.md`
- `研究资料/敌人血条-20261106/analysis/` (P0 log, screenshots, probe notes)
- `研究资料/敌人血条-20261106/upstream/` (ForgeUI/HawChow/KillFeed/Bossbars/DamageSync snapshots)
