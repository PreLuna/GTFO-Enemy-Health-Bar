# GTFO-Enemy-Health-Bar

独立敌人血条插件及其全境封锁风格伤害数字子 MOD。不依赖第三人称镜头/HUD、
ForgeUI、ForgeCore 或 DamageSync。

- 父 MOD：**EnemyHealthBar 0.4.0**（P1 焦点 + P2 最近命中 + P3 完整命中事件 API + C3 控制快照）
- 子 MOD：**EnemyHealthBar.DamageNumbers 0.1.1（6字段可写）**（命中点数字、右对齐连击列；硬依赖父 0.4.0）
- 程序集：`PreLuna.EnemyHealthBar`、`PreLuna.EnemyHealthBar.DamageNumbers`
- 游戏目标：父 DLL 放 `BepInEx/plugins/本地定制/PreLuna.EnemyHealthBar/`，
  子 DLL 放 `BepInEx/plugins/本地定制/PreLuna.EnemyHealthBar.DamageNumbers/`；
  不要把子 DLL 再复制进父目录，否则会重复加载。
- 目标游戏：GTFO + BepInEx 6 IL2CPP（本机 6.0.0-be.697）

## 当前功能

### 血条

- 准星焦点单条血条：名称、`EnemyType` 分类、生命、硬直白段、结沫青条、端括号、弱点/燃烧/结沫/标记/不可推动标签、淡入淡出。
- 最近命中副条：最多 3 条，命中后 3s 保持、0.4s 淡出；读取游戏实时生命，不按本地伤害累计。
- 掉血残影：生命下降后保留浅灰残影并收缩。
- `EnemyType` 原生分类：`Weakling/Standard/Special/MiniBoss/Boss`；Boss/MiniBoss **只随焦点/最近命中显示，不做常驻条**。
- 自定义敌人接口：`PreLuna.EnemyHealthBar.Api.EnemyHudApi.RegisterProfile(...)`，可按 `EnemyDataID`/`EnemyDataBlock.name` 覆盖显示名、角色、Tier、Boss 标记、锚点偏移和额外状态槽。
- 完整命中事件：`PreLuna.EnemyHealthBar.Api.EnemyHitApi.HitRegistered` 发布 `EnemyHitInfo`
  （命中点、实际掉血、原始估算、前后生命、弱点/护甲/击杀/无敌、limb）。父 MOD 不渲染数字；
  子 MOD 或其他插件按需订阅。

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

EnemyHitApi.HitRegistered += hit =>
{
    // hit.AppliedDamage / hit.RawDamage / hit.Weakspot / hit.Armor / hit.Kill ...
};
```

### 伤害数字子 MOD

- 全境封锁 2 风格：白色身体、琥珀弱点、青色护甲；命中点附近右对齐、最新在底部、旧数字上移淡出。
- 每一发子弹（每个 `BulletDamage`/`MeleeDamage` 调用）显示**实际掉血** `before-after`；
  霰弹枪每个弹丸一条，默认不合并（`MergeWindowMs=0`）。
- 击杀：数字保持命中类型颜色，追加 0.2s 红色短下划；不把整段数字改红。
- 无敌目标显示 `IMMUNE`；护甲命中可用青色单独开关。
- 系统字体回退：`Bahnschrift SemiCondensed` → `Bahnschrift` → `Arial Narrow` →
  `Segoe UI Semibold` → IMGUI 默认。不包含/不分发商业字体 Borda。
- 旧 `DamageNumbers`（`com.dak.DamageNumbers`）检测：加载时若发现旧 MOD 会写警告；
  两者不要同时启用，否则同一次命中会出现两套数字。

P1 使用 ASCII 标签/数字；中文动态字体（P1.5）尚未接入。

## 文档

- [开发与复用笔记](docs/DEVELOPMENT.md)：架构、字段语义、IL2CPP 坑、Harmony、锚点、构建测试。
- [研究与许可](docs/RESEARCH.md)：上游 MOD 结论、ForgeUI MIT、Division 2 参考。
- [设计说明](docs/DESIGN.md)：线稿、比例、信息层级、状态/等级槽。
- [伤害数字子 MOD](docs/DAMAGE_NUMBERS.md)：样式、数据链、配置、限制、验收项。
- [变更记录](CHANGELOG.md)。

## 安装

1. 退出游戏。
2. 分别放到两个目录：
   - `本地定制/PreLuna.EnemyHealthBar/PreLuna.EnemyHealthBar.dll`
   - `本地定制/PreLuna.EnemyHealthBar.DamageNumbers/PreLuna.EnemyHealthBar.DamageNumbers.dll`
   - **不要**把子 DLL 同时放在父目录；BepInEx 会递归扫描，重复副本会导致版本冲突。
3. 启动游戏；父 MOD 默认 `PageDown` 开关血条/数字显隐。
4. 如果还装着旧 HawChow `GTFOEnemyHealthBar.dll`，先移出/停用，避免双份血条。
5. 如果还装着旧 `DamageNumbers-伤害数字/DamageNumbers.dll`，停用/移出，避免两套伤害数字。
   `FloatingTextAPI` 可以保留不动，新子 MOD 不依赖它。

配置文件：`BepInEx/config/PreLuna.EnemyHealthBar.cfg`、
`BepInEx/config/PreLuna.EnemyHealthBar.DamageNumbers.cfg`（首次启动生成）。

## 主要配置

### 父 MOD

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

### 伤害数字子 MOD

| 分组 | 键 | 默认 | 说明 |
| --- | --- | --- | --- |
| General | Enabled | true | 数字总开关；父 HUD 隐藏时一起隐藏 |
| General | ToggleKey | None | 独立开关；`None` 关闭按键 |
| Display | MaxRows | 12 | 每目标最多行数；1–12，霰弹每弹丸一条建议 8–12 |
| Display | Lifetime | 0.95 | 秒 |
| Display | FontSize | 24 | 1080p 普通基准 |
| Display | LineHeight | 30 | 1080p 行距 |
| Display | RightOffset | 14 | 数字右边缘相对命中点像素 |
| Display | MergeWindowMs | 0 | 0 = 每个弹丸一条；>0 时同目标同类型合并 |
| Display | MaxDistance | 100 | 超过距离不显示；0 = 不限 |
| Display | ShowArmor | true | 青色护甲命中 |
| Display | ShowImmune | true | `IMMUNE` |
| Display | ShowKillFlash | true | 击杀红色短下划 |

## 构建与测试

```powershell
./Build.ps1 -GameRoot 'D:/Program/Game/Steam/Steam/steamapps/common/GTFO' -RuntimeRoot 'C:/Program Files/dotnet/shared/Microsoft.NETCore.App/6.0.16'
./Test.ps1 -RuntimeRoot 'C:/Program Files/dotnet/shared/Microsoft.NETCore.App/6.0.16'
```

当前离线测试：`EnemyTypeMappingTests` 5 项、`EnemyBarMathTests` 5 项、
`EnemyHudControlTests` 7 项（目标控制/保存回滚）、`EnemyHudConfigFileStoreTests` 5 项（安全行级存储）、`DamageNumberTests` 7 项，共 29 项。
构建输出只在 `out/`；不自动写游戏目录。

## C3 控制接口

父 MOD 0.4.0 新增 `PreLuna.EnemyHealthBar.Control`：

- `EnemyHealthBarControlApi.GetSnapshot/Validate/SetPersistent`；
- `EnemyHudControlState` 保存 P／R／T、修订号和设置策略；
- 8 个设置项声明为 `ImmediateAutoSave`：修改后直接更新运行值并写配置；
- 页面级“恢复全部设置”接口已删除（TheArchive 子菜单按钮实测无法稳定触发，C3 不提供恢复默认）；
- 未装 ModControl 时，父 MOD 仍按原配置独立运行。

## C4 安全持久化

父 MOD 0.4.0 的 C4 改动：

- `ConfigFile.SaveOnConfigSet=false`，不再由 BepInEx 自动整文件重写；
- `EnemyHudConfigFileStore` 只更新受管键所在行，保留注释、未知键、未知段和其他行；
- 同目录临时文件＋`File.Replace`，失败保留原文件；
- `SetPersistent` 保存失败时回滚 ConfigEntry 与内存 P／R；
- 测试：安全行级存储 5 项、控制/回滚 7 项；目标仓共 29 项（C4 清理后）。

实机已验证：目标 CFG 值写入/重启读回，哨兵注释与未知键保留；页面控件重启初始化由 ModControl 共享值层处理。

## 验证状态

- P0 探针（已归档）：普通 `Striker_Hibernate` 验证了 `Health/HealthMax`、硬直、结沫、燃烧、弱点、`EnemyType`、相机/准星字段；摘要见[开发笔记](docs/DEVELOPMENT.md)。
- P1：游戏内看到焦点血条（`Shooter_Hibernate [SPEC]`、`Striker_Bullrush`），锚点/字号按用户反馈调整。
- P2：Harmony 本地命中已确认（第一次 `before=30 after=26.229`）；用户 2026-11-06 验收确认焦点条、副条、第三人称焦点条、泡沫结沫/F 状态槽四项正常。
- P3（0.3.0 + 0.1.0）：完整命中捕获、`EnemyHitApi`、伤害数字子 MOD 已完成构建/17 项离线测试；
  用户 2026-11-06 安装后实测反馈 **“测试通过十分满意”**（整体通过，非逐项矩阵）。
  证据见 [docs/evidence/user-acceptance-20261106.txt](docs/evidence/user-acceptance-20261106.txt)。
  仍未单独覆盖：客机、Boss/MiniBoss、中文动态字体、等级虚线框，以及伤害数字的逐项
  极限场景；这些不冒充已验收。

## 当前限制

- 副条已过用户视觉验收，但客机/多目标叠加/Boss 仍需后续覆盖。
- `EnemyType` 是类型，不是数字等级；数字等级只保留 `TierText` 扩展。
- 硬直 `m_damBuildToHitreact` 的重置/同步仍以实测为准；客机读数未验证。
- 结沫“预计团数”没有默认实现；先显示 `AttachedGlueRel`。
- 状态槽/等级虚线框已实现；空槽默认折叠，EMP 等未接字段不伪造。
- 伤害数字只处理本机玩家的 `BulletDamage/MeleeDamage`；爆炸/火焰/持续伤害不覆盖
  （没有可靠的本地玩家归属）。客机是否触发本机伤害回调仍待 P0。
- 数字按实际掉血显示，击杀时显示的是“最后一击扣掉的生命”而不是完整过量伤害；
  原始估算 `RawDamage` 已在 API 中提供，后续可按实测切换。
- 背击颜色暂不启用（方向符号未实测），API 字段保留。
- 不写血量、不发网络包、不修改敌人数据。

## 来源与许可

本项目为 PreLuna 实现。上游研究、ForgeUI MIT、DamageNumbers/FloatingTextAPI MIT
与复用边界见[研究与许可](docs/RESEARCH.md)和[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)；
其他无明确许可证的参考 MOD 不整段复制。
