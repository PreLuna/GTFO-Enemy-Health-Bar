# GTFO-Enemy-Health-Bar

独立敌人血条插件。不依赖第三人称镜头/HUD、ForgeUI、ForgeCore 或 DamageSync。

- 版本：**0.2.0**（P1 焦点 + P2 最近命中 + 状态槽/自定义接口；用户已验收主要四项）
- 程序集：`PreLuna.EnemyHealthBar`
- 游戏目标：`BepInEx/plugins/PreLuna.EnemyHealthBar/PreLuna.EnemyHealthBar.dll`（本工作区本地化布局为 `本地定制/PreLuna.EnemyHealthBar/`）
- 目标游戏：GTFO + BepInEx 6 IL2CPP（本机 6.0.0-be.697）

## 当前功能

- 准星焦点单条血条：名称、`EnemyType` 分类、生命、硬直白段、结沫青条、端括号、弱点/燃烧/结沫/标记/不可推动标签、淡入淡出。
- 最近命中副条：最多 3 条，命中后 3s 保持、0.4s 淡出；读取游戏实时生命，不按本地伤害累计。
- 掉血残影：生命下降后保留浅灰残影并收缩。
- `EnemyType` 原生分类：`Weakling/Standard/Special/MiniBoss/Boss`；Boss/MiniBoss **只随焦点/最近命中显示，不做常驻条**。
- 自定义敌人接口：`PreLuna.EnemyHealthBar.Api.EnemyHudApi.RegisterProfile(...)`，可按 `EnemyDataID`/`EnemyDataBlock.name` 覆盖显示名、角色、Tier、Boss 标记、锚点偏移和额外状态槽。

```csharp
using PreLuna.EnemyHealthBar.Api;
using PreLuna.EnemyHealthBar.Core;

EnemyHudApi.RegisterProfile(new EnemyHudProfile(
    Key: "123",                 // EnemyDataID 或 EnemyDataBlock.name
    DisplayName: "Custom Boss",
    Role: "BOSS",
    TierText: "LV 10",          // 只有 ShowLevelSlot=true 才显示
    IsBoss: true,
    AnchorOffsetY: .2f,
    ExtraStatusSlots: 0));
```
- 等级/威胁槽默认折叠；只有 `Display/ShowLevelSlot=true` 且 profile 提供 `TierText` 才显示文本。

P1 使用 ASCII 标签/数字；中文动态字体（P1.5）尚未接入。

## 文档

- [开发与复用笔记](docs/DEVELOPMENT.md)：架构、字段语义、IL2CPP 坑、Harmony、锚点、构建测试。
- [研究与许可](docs/RESEARCH.md)：上游 MOD 结论、ForgeUI MIT、Boss/类型数据。
- [设计说明](docs/DESIGN.md)：线稿、比例、信息层级、状态/等级槽。
- [变更记录](CHANGELOG.md)。

## 安装

1. 退出游戏。
2. 把 `PreLuna.EnemyHealthBar.dll` 放到 `BepInEx/plugins/PreLuna.EnemyHealthBar/`（本工作区使用 `本地定制/PreLuna.EnemyHealthBar/`）。
3. 启动游戏；默认 `PageDown` 开关。
4. 如果还装着旧 HawChow `GTFOEnemyHealthBar.dll`，先移出/停用，避免双份血条。

配置文件：`BepInEx/config/PreLuna.EnemyHealthBar.cfg`（首次启动生成）。

## 主要配置

| 分组 | 键 | 默认 | 说明 |
| --- | --- | --- | --- |
| General | Enabled | true | 总开关 |
| General | ToggleKey | PageDown | 开关；`None` 关闭按键 |
| Display | MaxDistance | 60 | 5–300m |
| Display | CrosshairRadius | 72 | 准星避让像素；0 关闭 |
| Display | ShowName/ShowType/ShowNumbers | true | 名称/类型/数值 |
| Display | ShowLevelSlot | false | 预留等级/威胁槽，默认折叠 |
| Display | ShowStatusSlots | true | 燃烧/结沫/标记/不可推动状态槽 |
| Display | StatusSlots | 4 | 0–6；默认不画空槽 |
| Display | ShowEmptyStatusSlots | false | 调试时显示空状态槽 |
| Bar | Width/Height | 200/5 | 1080p 基准；按分辨率缩放 |
| Bar | Opacity | 0.55 | 整体透明度 |
| Bar | ShowHard/ShowGlue | true | 硬直段/结沫条 |
| Display | ShowWeakspot | true | 弱点数量与倍率 |

## 构建与测试

```powershell
./Build.ps1 -GameRoot 'D:/Program/Game/Steam/Steam/steamapps/common/GTFO' -RuntimeRoot 'C:/Program Files/dotnet/shared/Microsoft.NETCore.App/6.0.16'
./Test.ps1 -RuntimeRoot 'C:/Program Files/dotnet/shared/Microsoft.NETCore.App/6.0.16'
```

当前离线测试：`EnemyTypeMappingTests` 5 项、`EnemyBarMathTests` 5 项。构建输出只在 `out/`；不自动写游戏目录。

## 验证状态

- P0 探针（已归档）：普通 `Striker_Hibernate` 验证了 `Health/HealthMax`、硬直、结沫、燃烧、弱点、`EnemyType`、相机/准星字段；摘要见[开发笔记](docs/DEVELOPMENT.md)。
- P1：游戏内看到焦点血条（`Shooter_Hibernate [SPEC]`、`Striker_Bullrush`），锚点/字号按用户反馈调整。
- P2：Harmony 本地命中已确认（第一次 `before=30 after=26.229`）；用户 2026-11-06 验收确认焦点条、副条、第三人称焦点条、泡沫结沫/F 状态槽四项正常。
- 未完成实测：客机、Boss/MiniBoss、燃烧/硬直大幅变化、中文动态字体、等级虚线框；这些不冒充已验收。

## 当前限制

- 副条已过用户视觉验收，但客机/多目标叠加/Boss 仍需后续覆盖。
- `EnemyType` 是类型，不是数字等级；数字等级只保留 `TierText` 扩展。
- 硬直 `m_damBuildToHitreact` 的重置/同步仍以实测为准；客机读数未验证。
- 结沫“预计团数”没有默认实现；先显示 `AttachedGlueRel`。
- 状态槽/等级虚线框已实现；空槽默认折叠，EMP 等未接字段不伪造。
- 不写血量、不发网络包、不修改敌人数据。

## 来源与许可

本项目为 PreLuna 实现。上游研究、ForgeUI MIT 与复用边界见[研究与许可](docs/RESEARCH.md)和[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)；其他无明确许可证的参考 MOD 不整段复制。
