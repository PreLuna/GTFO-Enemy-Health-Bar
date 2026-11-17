# GTFO-Enemy-Health-Bar

独立敌人血条插件、全境封锁风格伤害数字子 MOD，以及 BFV 命中/击杀反馈子 MOD。
不依赖第三人称镜头/HUD、ForgeUI、ForgeCore 或 DamageSync。

- 父 MOD：**EnemyHealthBar 0.4.0**（P1 焦点 + P2 最近命中 + P3 完整命中事件 API + C3 控制快照）
- 子 MOD 1：**EnemyHealthBar.DamageNumbers 0.1.1（6字段可写）**（命中点数字、右对齐连击列；硬依赖父 0.4.0）
- 子 MOD 2：**EnemyHealthBar.BFVFeedback 0.2.0（26 项面板可写）**（BFV 命中音/击杀音、四档命中线、头顶击杀图标；硬依赖父 0.4.0）
- 程序集：`PreLuna.EnemyHealthBar`、`PreLuna.EnemyHealthBar.DamageNumbers`、
  `PreLuna.EnemyHealthBar.BFVFeedback`
- 游戏目标：父 DLL 放 `BepInEx/plugins/本地定制/PreLuna.EnemyHealthBar/`，
  子 DLL 分别放 `BepInEx/plugins/本地定制/PreLuna.EnemyHealthBar.DamageNumbers/`、
  `BepInEx/plugins/本地定制/PreLuna.EnemyHealthBar.BFVFeedback/`；
  不要把子 DLL 再复制进父目录，否则会重复加载。
- 目标游戏：GTFO + BepInEx 6 IL2CPP（本机 6.0.0-be.697）

## 当前功能

### 血条

- 准星焦点单条血条：名称、`EnemyType` 分类、生命、硬直白段（空槽有描边，0 累计也可见；命中累计达到硬直阈值时 4 格短暂全亮 0.22s）、结沫青条、端括号、弱点/燃烧/结沫/标记/不可推动标签、淡入淡出。
- 最近命中副条：最多 3 条，命中后 3s 保持、0.4s 淡出；读取游戏实时生命，不按本地伤害累计。焦点目标在焦点期间不画副条，准星移开后在保持窗口内仍可出现。
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

### BFV 命中/击杀反馈子 MOD

- 事件源：游戏本地 `CrosshairGuiLayer.ShowHitIndicator` / `ShowDeathIndicator` 为主，
  只读 `BulletDamage`/`MeleeDamage` 前缀补齐敌人、头部锚点与 `HealthMax`；只影响本机玩家自己的命中/击杀。
- 命中线：普通白 X、弱点橙 X（大 18%）、真实护甲 limb 青色四角括号、击杀加粗红线；
  跟随世界命中点投影，同一敌人 50ms 内的霰弹命中合并为一条。
- 击杀图标：默认按敌人原生 `EnemyType` 分 MINOR / ELITE / BOSS，分别画白骷髅/橙骷髅/红齿轮；
  Weakling=MINOR，Standard/Special=ELITE，MiniBoss/Boss=BOSS；可切到 `Health` 阈值模式。
  锚点在击杀瞬间冻结，默认 2.2s 淡出，同屏最多 6 个。
- 音效：KillFeed 1.2.19 的 BF5 命中/击杀音（可切 BF1/BF6，缺失类别回落 BF5），
  命中与击杀分开节流；默认静音原版两个本地命中提示事件。
- 素材：DLL 不含音频/图标；`Assets/` 放在插件目录，或用 `Sound/CustomDirectory` 指向外部素材包。
  未放素材时安全回退原版，不静默吞掉反馈。
- 面板：0.2.0 新增 `PreLuna.EnemyHealthBar.BFVFeedback.Control`，由
  `PreLuna.ModControl` 0.3.4 的“已接入 MOD → BFV 反馈”单列页读写 26 项；声音/命中线/图标/
  颜色/档位即时生效，颜色使用 TheArchive 原生 `SColor` 拾色器。`ReplaceVanilla` 现已真正控制原版抑制。
- 与 DamageNumbers 独立：两者都订阅 `EnemyHitApi`/游戏事件，可同时开启。

P1 使用 ASCII 标签/数字；中文动态字体（P1.5）尚未接入。

## 文档

- [开发与复用笔记](docs/DEVELOPMENT.md)：架构、字段语义、IL2CPP 坑、Harmony、锚点、构建测试。
- [研究与许可](docs/RESEARCH.md)：上游 MOD 结论、ForgeUI MIT、Division 2 参考。
- [设计说明](docs/DESIGN.md)：线稿、比例、信息层级、状态/等级槽。
- [伤害数字子 MOD](docs/DAMAGE_NUMBERS.md)：样式、数据链、配置、限制、验收项。
- [BFV 命中/击杀反馈子 MOD](EnemyHealthBar.BFVFeedback/说明.md)：事件流、素材布局、配置、限制。
- [BFV 实现与反馈时序](EnemyHealthBar.BFVFeedback/实现与反馈时序.md)：代码模块、每种音效/UI 的出现条件与时序。
- [BFV 控制面板](EnemyHealthBar.BFVFeedback/控制面板.md)：已接入的 26 项字段、控件类型、颜色拾色器和即时/重启/只读边界。
- [变更记录](CHANGELOG.md)。

## 安装

1. 退出游戏。
2. 分别放到三个目录：
   - `本地定制/PreLuna.EnemyHealthBar/PreLuna.EnemyHealthBar.dll`
   - `本地定制/PreLuna.EnemyHealthBar.DamageNumbers/PreLuna.EnemyHealthBar.DamageNumbers.dll`
   - `本地定制/PreLuna.EnemyHealthBar.BFVFeedback/PreLuna.EnemyHealthBar.BFVFeedback.dll`
     + 同目录 `Assets/audio/...`、`Assets/icons/...`
   - **不要**把子 DLL 同时放在父目录；BepInEx 会递归扫描，重复副本会导致版本冲突。
3. 启动游戏；父 MOD 默认 `PageDown` 开关血条/数字显隐。BFV 反馈默认跟随该显隐。
4. 如果还装着旧 HawChow `GTFOEnemyHealthBar.dll`，先移出/停用，避免双份血条。
5. 如果还装着旧 `DamageNumbers-伤害数字/DamageNumbers.dll`，停用/移出，避免两套伤害数字。
   `FloatingTextAPI` 可以保留不动，新子 MOD 不依赖它。
6. 如果同时装 KillFeed，先在它的 cfg 里关掉 `HitSound`、`HitMarker`、`KillIcon`，避免双份命中反馈。
7. 要用游戏内面板，安装 `PreLuna.ModControl` 0.3.4 到
   `本地定制/PreLuna.ModControl/PreLuna.ModControl.dll`；入口是
   **设置 → 已接入 MOD → BFV 反馈**。不装面板时 BFV 反馈仍按 cfg 运行。

配置文件：`BepInEx/config/PreLuna.EnemyHealthBar.cfg`、
`BepInEx/config/PreLuna.EnemyHealthBar.DamageNumbers.cfg`、
`BepInEx/config/PreLuna.EnemyHealthBar.BFVFeedback.cfg`（首次启动生成）。

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

### BFV 反馈子 MOD

| 分组 | 键 | 默认 | 说明 |
| --- | --- | --- | --- |
| General | Enabled | true | 总开关；关闭后原版标记/提示音恢复 |
| General | ToggleKey | None | 本机临时开关；`None` 关闭按键 |
| General | FollowHudVisibility | true | 跟随血条 PageDown 显隐；隐藏时原版反馈恢复 |
| Sound | Enabled | true | 音效总开关 |
| Sound | Style | BF5 | BF1 / BF5 / BF6；缺失类别回落 BF5 |
| Sound | Volume / HitVolume / KillVolume | 1.0 / 1.0 / 1.0 | 主音量与两通道音量 |
| Sound | Gain | 1.8 | 额外增益（0.5–3.0），tanh 软限幅 |
| Sound | HitGain / KillGain | 1.0 / 1.6 | 命中/击杀再独立乘一次 |
| Sound | KillHighPassHz | 350 | 击杀音高通；0 = 关闭 |
| Sound | KillChimeEnabled / KillChimeVolume | true / 0.9 | 独立短促击杀确认音 |
| Sound | KillPriorityMs | 150 | 击杀后跳过命中音，突出确认音 |
| Sound | HitEnabled / KillEnabled | true / true | 命中/击杀音独立开关 |
| Sound | MinIntervalSeconds | 0.05 | 命中、击杀分别节流 |
| Sound | MuteVanillaIndicator | true | 静音原版两个本地命中提示事件 |
| Sound | CustomDirectory | 空 | 外部素材目录；留空用插件 `Assets/` |
| HitMarker | Enabled | true | 命中线/击杀线总开关 |
| HitMarker | ReplaceVanilla | true | 替换原版标记；关闭则叠加 |
| HitMarker | Size | 64 | 1080p 基准 |
| HitMarker | ColorNormal/Weakspot/Armor/Kill | 白/橙/青/红 | `#RRGGBB` 或 `#RRGGBBAA` |
| HitMarker | ArmorFromNoDamage | false | 仅真实 `eLimbDamageType.Armor` 显示青色 |
| HitMarker | MergeWindowMs | 50 | 同一敌人合并窗口；0 = 不合并 |
| KillIcon | Enabled | true | 头顶击杀图标开关 |
| KillIcon | Size / Opacity / Lifetime / MaxIcons | 46 / 0.85 / 2.2 / 6 | 图标尺寸、透明度、寿命、上限 |
| KillIcon | TierSource | EnemyType | 按原生类型分档；`Health` 用阈值 |
| KillIcon | HeadGapMetres | 0.35 | 头部锚点与图标的额外余量 |
| KillIcon | MinorHealthThreshold / HeavyHealthThreshold | 120 / 1000 | 仅 `TierSource=Health` 时使用 |

## 构建与测试

```powershell
./Build.ps1 -GameRoot 'D:/Program/Game/Steam/Steam/steamapps/common/GTFO' -RuntimeRoot 'C:/Program Files/dotnet/shared/Microsoft.NETCore.App/6.0.36'
./Test.ps1 -RuntimeRoot 'C:/Program Files/dotnet/shared/Microsoft.NETCore.App/6.0.36'
```

当前离线测试：`EnemyTypeMappingTests` 5 项、`EnemyBarMathTests` 6 项、
`EnemyHudControlTests` 7 项（目标控制/保存回滚）、`EnemyHudConfigFileStoreTests` 5 项（安全行级存储）、`DamageNumberTests` 7 项、`BfvFeedbackTests` 10 项（档位/类型/素材回落/标记/颜色），共 40 项。
构建输出只在 `out/`；不自动写游戏目录。BFV 反馈的音频/图标不随 DLL 编译，需要把
`gtfo-bfv-feedback/assets/bfv-feedback-pack/audio`、`icons` 复制到插件目录 `Assets/`，
或由 `Sound/CustomDirectory` 指向外部素材目录。

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
- 2026-11-10：修复焦点期间最近命中副条叠画（副条键改为 `EnemyAgent.Pointer`），硬直空槽改为可见描边；用户实机确认副条与空槽正常。武器攻击时 `HARD` 仍长时间显示 0/5，属游戏 `m_damBuildToHitreact` 累计/采样时机，渲染侧未改。
- 2026-11-10：新增硬直阈值触发闪示（`applied×staggerMulti ≥ DamageUntilHitreact` 时 4 格全亮 0.22s，随后回真实余量）；构建与 30 项离线测试通过，已由用户实机确认正常。
- 2026-11-17：新增 `EnemyHealthBar.BFVFeedback` 0.1.0（BFV 命中/击杀反馈子 MOD）；
  3 程序集构建通过，5 套离线测试共 40 项通过（含 BFV 10 项）；
  用户当日实机反馈 **“测试通过”**（整体通过，非逐项矩阵）。WinMM 音频、击杀确认音、
  高通、命中线、头顶图标与 EnemyType 档位已一起测试；客机/逐敌 Armor 清单仍未单独覆盖。
- 2026-11-17：`EnemyHealthBar.BFVFeedback` 0.2.0 接入 `PreLuna.ModControl` 0.3.4，
  新增单列“BFV 反馈”页 26 项即时配置、`SColor` 拾色器和 `ReplaceVanilla` 修复；
  BFV 5 套 44 项、ModControl 18 套离线测试通过。已安装并确认启动日志
  `BFV control=Available; settings=26`、`PreLuna.ModControl 0.3.4 ready`；
  面板页面已可见；首次发现 26 项全量摘要文字重叠，已改为只显示项数并安装压缩版；
  颜色拾色器/写入/重启读回待用户复测，不冒充已验收。

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
- BFV 反馈以游戏本地命中/击杀指示为主要事件源；客机是否触发 `ShowDeathIndicator`
  以及被游戏设置关闭时的兜底仍未实机验证。`Knife`/爆炸/持续伤害不在 P1 覆盖范围。
- 召唤的 `Striker_Big_Hibernate` 会触发游戏侧 `ES_Hibernate`/`ES_Hitreact` 空引用，
  出现 0 血不死；BFV 反馈与血条都只读，不写血/不控制死亡，需用非 Hibernate 变体对照。
- BFV 反馈的音效素材是 Battlefield 派生样本（来自 KillFeed 1.2.19 提取），
  **不提交进源码仓**；公开分发前需换成 CC0/自制素材。

## 来源与许可

本项目为 PreLuna 实现。上游研究、ForgeUI MIT、DamageNumbers/FloatingTextAPI MIT
与复用边界见[研究与许可](docs/RESEARCH.md)和[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)；
其他无明确许可证的参考 MOD 不整段复制。
BFV 反馈的可选音频/图标素材来自 HawChow/KillFeed 1.2.19（Battlefield 派生，无明确 LICENSE），
只在本机素材包中使用，不进入源码 Git；公开 Release 需替换为可再分发素材。
