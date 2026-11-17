using System;
using System.Globalization;
using BepInEx.Configuration;
using PreLuna.EnemyHealthBar.BFVFeedback.Control;
using PreLuna.EnemyHealthBar.BFVFeedback.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Runtime
{
    internal sealed class BfvConfig
    {
        readonly ConfigEntry<bool> enabled;
        readonly ConfigEntry<KeyCode> toggleKey;
        readonly ConfigEntry<bool> followHudVisibility;

        readonly ConfigEntry<bool> soundEnabled;
        readonly ConfigEntry<string> style;
        readonly ConfigEntry<float> volume;
        readonly ConfigEntry<float> gain;
        readonly ConfigEntry<float> hitGain;
        readonly ConfigEntry<float> killGain;
        readonly ConfigEntry<float> killHighPassHz;
        readonly ConfigEntry<bool> killChimeEnabled;
        readonly ConfigEntry<float> killChimeVolume;
        readonly ConfigEntry<float> killPriorityMs;
        readonly ConfigEntry<bool> hitEnabled;
        readonly ConfigEntry<bool> killEnabled;
        readonly ConfigEntry<float> hitVolume;
        readonly ConfigEntry<float> killVolume;
        readonly ConfigEntry<float> minIntervalSeconds;
        readonly ConfigEntry<bool> muteVanillaIndicator;
        readonly ConfigEntry<string> customDirectory;

        readonly ConfigEntry<bool> hitMarkerEnabled;
        readonly ConfigEntry<bool> replaceVanillaMarker;
        readonly ConfigEntry<float> hitMarkerSize;
        readonly ConfigEntry<string> colorNormal;
        readonly ConfigEntry<string> colorWeakspot;
        readonly ConfigEntry<string> colorArmor;
        readonly ConfigEntry<string> colorKill;
        readonly ConfigEntry<bool> armorFromNoDamage;
        readonly ConfigEntry<float> mergeWindowMs;

        readonly ConfigEntry<bool> killIconEnabled;
        readonly ConfigEntry<float> killIconSize;
        readonly ConfigEntry<float> killIconOpacity;
        readonly ConfigEntry<float> killIconLifetime;
        readonly ConfigEntry<int> killIconMax;
        readonly ConfigEntry<float> killIconGap;
        readonly ConfigEntry<float> minorHealthThreshold;
        readonly ConfigEntry<float> heavyHealthThreshold;
        readonly ConfigEntry<string> tierSource;

        internal BfvConfig(ConfigFile config)
        {
            enabled = config.Bind("General", "Enabled", true,
                "BFV 命中/击杀反馈总开关。关闭后原版命中标记与提示音完全恢复。");
            toggleKey = config.Bind("General", "ToggleKey", KeyCode.None,
                "本机临时开关；None 表示不占用按键。与 General/Enabled 不同，这个只影响当前会话。");
            followHudVisibility = config.Bind("General", "FollowHudVisibility", true,
                "跟随敌人血条 HUD 的显隐：PageDown 隐藏血条时，命中线/击杀图标/音效也一起关闭，原版反馈恢复。");

            soundEnabled = config.Bind("Sound", "Enabled", true,
                "命中/击杀音效总开关。关闭后仍可保留命中线与击杀图标。");
            style = config.Bind("Sound", "Style", "BF5",
                "音效风格：BF1、BF5（默认）或 BF6。缺失的类别自动回落到 BF5。");
            volume = config.Bind("Sound", "Volume", 1f,
                new ConfigDescription("音效主音量（0–1），与命中/击杀通道音量相乘。",
                    new AcceptableValueRange<float>(0f, 1f)));
            gain = config.Bind("Sound", "Gain", 1.8f,
                new ConfigDescription("额外增益（0.5–3.0），用于压过枪声；内部用 tanh 软限幅，避免硬削波。",
                    new AcceptableValueRange<float>(.5f, 3f)));
            hitGain = config.Bind("Sound", "HitGain", 1f,
                new ConfigDescription("命中音独立增益（0.5–3.0），与 Gain 相乘。",
                    new AcceptableValueRange<float>(.5f, 3f)));
            killGain = config.Bind("Sound", "KillGain", 1.6f,
                new ConfigDescription("击杀音独立增益（0.5–3.0），与 Gain 相乘；觉得击杀叮声弱就加这里。",
                    new AcceptableValueRange<float>(.5f, 3f)));
            killHighPassHz = config.Bind("Sound", "KillHighPassHz", 350f,
                new ConfigDescription("击杀音高通截止频率（Hz），削掉被枪声掩蔽的低频；0 = 关闭。",
                    new AcceptableValueRange<float>(0f, 2000f)));
            killChimeEnabled = config.Bind("Sound", "KillChimeEnabled", true,
                "击杀时额外叠一个短促的高频确认音；即使命中音/霰弹音很多也能听出来。");
            killChimeVolume = config.Bind("Sound", "KillChimeVolume", .9f,
                new ConfigDescription("独立击杀确认音的音量（0–1）。",
                    new AcceptableValueRange<float>(0f, 1f)));
            killPriorityMs = config.Bind("Sound", "KillPriorityMs", 150f,
                new ConfigDescription("击杀后多少毫秒内压低/跳过命中音，让击杀确认音不被盖住；0 = 不跳过。",
                    new AcceptableValueRange<float>(0f, 500f)));
            hitEnabled = config.Bind("Sound", "HitEnabled", true,
                "命中音（普通/弱点/护甲）独立开关。");
            killEnabled = config.Bind("Sound", "KillEnabled", true,
                "击杀音（MINOR/ELITE/BOSS）独立开关。");
            hitVolume = config.Bind("Sound", "HitVolume", 1f,
                new ConfigDescription("命中音通道音量（0–1）。",
                    new AcceptableValueRange<float>(0f, 1f)));
            killVolume = config.Bind("Sound", "KillVolume", 1f,
                new ConfigDescription("击杀音通道音量（0–1）。",
                    new AcceptableValueRange<float>(0f, 1f)));
            minIntervalSeconds = config.Bind("Sound", "MinIntervalSeconds", .05f,
                new ConfigDescription("两次命中音之间、两次击杀音之间的最小间隔（秒）。",
                    new AcceptableValueRange<float>(.02f, 3f)));
            muteVanillaIndicator = config.Bind("Sound", "MuteVanillaIndicator", true,
                "静音原版命中确认提示层（两个本地 *LOCALINDICATOR* 事件）。只影响提示音，不碰子弹冲击音与敌人语音。");
            customDirectory = config.Bind("Sound", "CustomDirectory", "",
                "可选的自定义素材目录（绝对路径，或相对插件目录）。目录内按 audio/bf5、icons 等同构放置；留空则使用插件目录下的 Assets。");

            hitMarkerEnabled = config.Bind("HitMarker", "Enabled", true,
                "BFV 命中线/击杀线开关。关闭后保留原版标记。");
            replaceVanillaMarker = config.Bind("HitMarker", "ReplaceVanilla", true,
                "用 BFV 命中线替换原版标记。关闭时原版标记保留，BFV 线条叠加在上层。");
            hitMarkerSize = config.Bind("HitMarker", "Size", 64f,
                new ConfigDescription("命中线基准尺寸（1080p 像素）。",
                    new AcceptableValueRange<float>(16f, 200f)));
            colorNormal = config.Bind("HitMarker", "ColorNormal", "#FFFFFF",
                "普通命中颜色，格式 #RRGGBB 或 #RRGGBBAA。");
            colorWeakspot = config.Bind("HitMarker", "ColorWeakspot", "#FF8C00",
                "弱点命中颜色，格式 #RRGGBB 或 #RRGGBBAA。");
            colorArmor = config.Bind("HitMarker", "ColorArmor", "#49ADC9",
                "护甲/免伤命中颜色，格式 #RRGGBB 或 #RRGGBBAA。");
            colorKill = config.Bind("HitMarker", "ColorKill", "#B92828",
                "击杀线颜色，格式 #RRGGBB 或 #RRGGBBAA。");
            armorFromNoDamage = config.Bind("HitMarker", "ArmorFromNoDamage", false,
                "没有 limb 上下文时，是否把游戏 ShowHitIndicator 的 noDamage 当作护甲。默认 false：只有真实 eLimbDamageType.Armor 才显示青色护甲。");
            mergeWindowMs = config.Bind("HitMarker", "MergeWindowMs", BfvPolicy.DefaultMergeWindowMs,
                new ConfigDescription("同一敌人多次命中合并为一条线的窗口（毫秒）；0 = 不合并。",
                    new AcceptableValueRange<float>(0f, 500f)));

            killIconEnabled = config.Bind("KillIcon", "Enabled", true,
                "头顶 BFV 击杀图标开关。");
            killIconSize = config.Bind("KillIcon", "Size", BfvPolicy.DefaultKillIconSize,
                new ConfigDescription("头顶击杀图标基准尺寸（1080p 像素）。",
                    new AcceptableValueRange<float>(26f, 76f)));
            killIconOpacity = config.Bind("KillIcon", "Opacity", BfvPolicy.DefaultKillIconOpacity,
                new ConfigDescription("头顶击杀图标整体不透明度（0–1）。",
                    new AcceptableValueRange<float>(0f, 1f)));
            killIconLifetime = config.Bind("KillIcon", "Lifetime", BfvPolicy.DefaultKillIconLifetime,
                new ConfigDescription("头顶击杀图标可见时间（秒）。",
                    new AcceptableValueRange<float>(.4f, 6f)));
            killIconMax = config.Bind("KillIcon", "MaxIcons", BfvPolicy.DefaultMaxKillIcons,
                new ConfigDescription("同屏击杀图标上限。",
                    new AcceptableValueRange<int>(1, 12)));
            killIconGap = config.Bind("KillIcon", "HeadGapMetres", BfvPolicy.DefaultKillIconGap,
                new ConfigDescription("敌人头部锚点与图标之间的额外世界余量（米）。",
                    new AcceptableValueRange<float>(0f, 2f)));
            minorHealthThreshold = config.Bind("KillIcon", "MinorHealthThreshold", BfvPolicy.DefaultMinorHealthThreshold,
                new ConfigDescription("MINOR 与 ELITE 的分界：HealthMax 低于该值为 MINOR。",
                    new AcceptableValueRange<float>(1f, 1e9f)));
            heavyHealthThreshold = config.Bind("KillIcon", "HeavyHealthThreshold", BfvPolicy.DefaultHeavyHealthThreshold,
                new ConfigDescription("Health 档位模式下 ELITE 与 BOSS 的分界：HealthMax 大于等于该值为 BOSS。",
                    new AcceptableValueRange<float>(1f, 1e9f)));
            tierSource = config.Bind("KillIcon", "TierSource", "EnemyType",
                "档位来源：EnemyType（默认）按敌人原生类型，Weakling=MINOR、Standard/Special=ELITE、MiniBoss/Boss=BOSS；Health 按 HealthMax 阈值。");
        }

        internal bool Enabled => enabled.Value;
        internal KeyCode ToggleKey => toggleKey.Value;
        internal bool FollowHudVisibility => followHudVisibility.Value;

        internal bool SoundEnabled => soundEnabled.Value;
        internal BfvSoundStyle SoundStyle => BfvPolicy.ParseStyle(style.Value);
        internal float Volume => BfvPolicy.Volume(volume.Value);
        internal float Gain => BfvPolicy.Clamp(gain.Value, .5f, 3f, 1.8f);
        internal float HitGain => BfvPolicy.Clamp(hitGain.Value, .5f, 3f, 1f);
        internal float KillGain => BfvPolicy.Clamp(killGain.Value, .5f, 3f, 1.6f);
        internal float KillHighPassHz => BfvPolicy.Clamp(killHighPassHz.Value, 0f, 2000f, 350f);
        internal bool KillChimeEnabled => killChimeEnabled.Value;
        internal float KillChimeVolume => BfvPolicy.Clamp(killChimeVolume.Value, 0f, 1f, .9f);
        internal float KillPrioritySeconds => BfvPolicy.Clamp(killPriorityMs.Value, 0f, 500f, 150f) / 1000f;
        internal bool HitEnabled => hitEnabled.Value;
        internal bool KillEnabled => killEnabled.Value;
        internal float HitVolume => BfvPolicy.Volume(hitVolume.Value);
        internal float KillVolume => BfvPolicy.Volume(killVolume.Value);
        internal float MinIntervalSeconds => BfvPolicy.MinIntervalSeconds(minIntervalSeconds.Value);
        internal bool MuteVanillaIndicator => muteVanillaIndicator.Value;
        internal string CustomDirectory => customDirectory.Value ?? "";
        internal bool SoundActive => SoundEnabled && (HitEnabled || KillEnabled);

        internal bool HitMarkerEnabled => hitMarkerEnabled.Value;
        internal bool ReplaceVanillaMarker => replaceVanillaMarker.Value;
        internal float HitMarkerSize => BfvPolicy.HitMarkerSize(hitMarkerSize.Value);
        internal string ColorNormal => colorNormal.Value;
        internal string ColorWeakspot => colorWeakspot.Value;
        internal string ColorArmor => colorArmor.Value;
        internal string ColorKill => colorKill.Value;
        internal bool ArmorFromNoDamage => armorFromNoDamage.Value;
        internal float MergeWindowSeconds => BfvPolicy.MergeWindowSeconds(mergeWindowMs.Value);

        internal bool KillIconEnabled => killIconEnabled.Value;
        internal float KillIconSize => BfvPolicy.KillIconSize(killIconSize.Value);
        internal float KillIconOpacity => BfvPolicy.KillIconOpacity(killIconOpacity.Value);
        internal float KillIconLifetime => BfvPolicy.KillIconLifetime(killIconLifetime.Value);
        internal int KillIconMax => BfvPolicy.MaxKillIcons(killIconMax.Value);
        internal float KillIconGap => BfvPolicy.KillIconGap(killIconGap.Value);
        internal float MinorHealthThreshold => BfvPolicy.Threshold(minorHealthThreshold.Value, BfvPolicy.DefaultMinorHealthThreshold);
        internal float HeavyHealthThreshold => BfvPolicy.Threshold(heavyHealthThreshold.Value, BfvPolicy.DefaultHeavyHealthThreshold);
        internal BfvTierSource TierSource => BfvPolicy.ParseTierSource(tierSource.Value);

        internal bool HasAnyFeedback => Enabled && (SoundActive || HitMarkerEnabled || KillIconEnabled);

        internal BfvFeedbackControlValue ReadControl(string key)
        {
            switch (key)
            {
                case "enabled": return BfvFeedbackControlValue.Boolean(Enabled);
                case "followHudVisibility": return BfvFeedbackControlValue.Boolean(FollowHudVisibility);
                case "soundEnabled": return BfvFeedbackControlValue.Boolean(SoundEnabled);
                case "styleIndex": return BfvFeedbackControlValue.Number(StyleIndex());
                case "volume": return BfvFeedbackControlValue.Number(Volume);
                case "gain": return BfvFeedbackControlValue.Number(Gain);
                case "killGain": return BfvFeedbackControlValue.Number(KillGain);
                case "killHighPassHz": return BfvFeedbackControlValue.Number(KillHighPassHz);
                case "killChimeEnabled": return BfvFeedbackControlValue.Boolean(KillChimeEnabled);
                case "killChimeVolume": return BfvFeedbackControlValue.Number(KillChimeVolume);
                case "killPriorityMs": return BfvFeedbackControlValue.Number(KillPrioritySeconds * 1000f);
                case "muteVanillaIndicator": return BfvFeedbackControlValue.Boolean(MuteVanillaIndicator);
                case "hitMarkerEnabled": return BfvFeedbackControlValue.Boolean(HitMarkerEnabled);
                case "replaceVanilla": return BfvFeedbackControlValue.Boolean(ReplaceVanillaMarker);
                case "hitMarkerSize": return BfvFeedbackControlValue.Number(HitMarkerSize);
                case "armorFromNoDamage": return BfvFeedbackControlValue.Boolean(ArmorFromNoDamage);
                case "colorNormal": return BfvFeedbackControlValue.String(ColorNormal);
                case "colorWeakspot": return BfvFeedbackControlValue.String(ColorWeakspot);
                case "colorArmor": return BfvFeedbackControlValue.String(ColorArmor);
                case "colorKill": return BfvFeedbackControlValue.String(ColorKill);
                case "killIconEnabled": return BfvFeedbackControlValue.Boolean(KillIconEnabled);
                case "killIconSize": return BfvFeedbackControlValue.Number(KillIconSize);
                case "killIconOpacity": return BfvFeedbackControlValue.Number(KillIconOpacity);
                case "killIconLifetime": return BfvFeedbackControlValue.Number(KillIconLifetime);
                case "healthTiering": return BfvFeedbackControlValue.Boolean(TierSource == BfvTierSource.Health);
                case "killIconGap": return BfvFeedbackControlValue.Number(KillIconGap);
                default: return null;
            }
        }

        internal void WriteControl(string key, BfvFeedbackControlValue value)
        {
            if (value == null) return;
            switch (key)
            {
                case "enabled": if (value.TryGetBoolean(out var enabledValue)) enabled.Value = enabledValue; break;
                case "followHudVisibility": if (value.TryGetBoolean(out var followValue)) followHudVisibility.Value = followValue; break;
                case "soundEnabled": if (value.TryGetBoolean(out var soundValue)) soundEnabled.Value = soundValue; break;
                case "styleIndex":
                    if (value.TryGetNumber(out var styleIndex))
                    {
                        var rounded = (int)Math.Round(styleIndex);
                        style.Value = rounded <= 0 ? "BF1" : rounded >= 2 ? "BF6" : "BF5";
                    }
                    break;
                case "volume": if (value.TryGetNumber(out var volumeValue)) volume.Value = (float)volumeValue; break;
                case "gain": if (value.TryGetNumber(out var gainValue)) gain.Value = (float)gainValue; break;
                case "killGain": if (value.TryGetNumber(out var killGainValue)) killGain.Value = (float)killGainValue; break;
                case "killHighPassHz": if (value.TryGetNumber(out var highPassValue)) killHighPassHz.Value = (float)highPassValue; break;
                case "killChimeEnabled": if (value.TryGetBoolean(out var chimeValue)) killChimeEnabled.Value = chimeValue; break;
                case "killChimeVolume": if (value.TryGetNumber(out var chimeVolumeValue)) killChimeVolume.Value = (float)chimeVolumeValue; break;
                case "killPriorityMs": if (value.TryGetNumber(out var priorityValue)) killPriorityMs.Value = (float)priorityValue; break;
                case "muteVanillaIndicator": if (value.TryGetBoolean(out var muteValue)) muteVanillaIndicator.Value = muteValue; break;
                case "hitMarkerEnabled": if (value.TryGetBoolean(out var markerValue)) hitMarkerEnabled.Value = markerValue; break;
                case "replaceVanilla": if (value.TryGetBoolean(out var replaceValue)) replaceVanillaMarker.Value = replaceValue; break;
                case "hitMarkerSize": if (value.TryGetNumber(out var sizeValue)) hitMarkerSize.Value = (float)sizeValue; break;
                case "armorFromNoDamage": if (value.TryGetBoolean(out var armorValue)) armorFromNoDamage.Value = armorValue; break;
                case "colorNormal": if (value.TryGetString(out var normalValue)) colorNormal.Value = normalValue; break;
                case "colorWeakspot": if (value.TryGetString(out var weakValue)) colorWeakspot.Value = weakValue; break;
                case "colorArmor": if (value.TryGetString(out var armorColorValue)) colorArmor.Value = armorColorValue; break;
                case "colorKill": if (value.TryGetString(out var killColorValue)) colorKill.Value = killColorValue; break;
                case "killIconEnabled": if (value.TryGetBoolean(out var iconValue)) killIconEnabled.Value = iconValue; break;
                case "killIconSize": if (value.TryGetNumber(out var iconSizeValue)) killIconSize.Value = (float)iconSizeValue; break;
                case "killIconOpacity": if (value.TryGetNumber(out var opacityValue)) killIconOpacity.Value = (float)opacityValue; break;
                case "killIconLifetime": if (value.TryGetNumber(out var lifetimeValue)) killIconLifetime.Value = (float)lifetimeValue; break;
                case "healthTiering": if (value.TryGetBoolean(out var healthTieringValue)) tierSource.Value = healthTieringValue ? "Health" : "EnemyType"; break;
                case "killIconGap": if (value.TryGetNumber(out var gapValue)) killIconGap.Value = (float)gapValue; break;
            }
        }

        internal string SerializeControl(string key)
        {
            switch (key)
            {
                case "enabled": return Enabled ? "true" : "false";
                case "followHudVisibility": return FollowHudVisibility ? "true" : "false";
                case "soundEnabled": return SoundEnabled ? "true" : "false";
                case "styleIndex": return StyleIndex() == 0 ? "BF1" : StyleIndex() == 2 ? "BF6" : "BF5";
                case "volume": return Volume.ToString("0.###", CultureInfo.InvariantCulture);
                case "gain": return Gain.ToString("0.###", CultureInfo.InvariantCulture);
                case "killGain": return KillGain.ToString("0.###", CultureInfo.InvariantCulture);
                case "killHighPassHz": return KillHighPassHz.ToString("0.###", CultureInfo.InvariantCulture);
                case "killChimeEnabled": return KillChimeEnabled ? "true" : "false";
                case "killChimeVolume": return KillChimeVolume.ToString("0.###", CultureInfo.InvariantCulture);
                case "killPriorityMs": return (KillPrioritySeconds * 1000f).ToString("0.###", CultureInfo.InvariantCulture);
                case "muteVanillaIndicator": return MuteVanillaIndicator ? "true" : "false";
                case "hitMarkerEnabled": return HitMarkerEnabled ? "true" : "false";
                case "replaceVanilla": return ReplaceVanillaMarker ? "true" : "false";
                case "hitMarkerSize": return HitMarkerSize.ToString("0.###", CultureInfo.InvariantCulture);
                case "armorFromNoDamage": return ArmorFromNoDamage ? "true" : "false";
                case "colorNormal": return ColorNormal;
                case "colorWeakspot": return ColorWeakspot;
                case "colorArmor": return ColorArmor;
                case "colorKill": return ColorKill;
                case "killIconEnabled": return KillIconEnabled ? "true" : "false";
                case "killIconSize": return KillIconSize.ToString("0.###", CultureInfo.InvariantCulture);
                case "killIconOpacity": return KillIconOpacity.ToString("0.###", CultureInfo.InvariantCulture);
                case "killIconLifetime": return KillIconLifetime.ToString("0.###", CultureInfo.InvariantCulture);
                case "healthTiering": return TierSource == BfvTierSource.Health ? "Health" : "EnemyType";
                case "killIconGap": return KillIconGap.ToString("0.###", CultureInfo.InvariantCulture);
                default: return null;
            }
        }

        int StyleIndex()
        {
            switch (BfvPolicy.ParseStyle(style.Value))
            {
                case BfvSoundStyle.Bf1: return 0;
                case BfvSoundStyle.Bf6: return 2;
                default: return 1;
            }
        }
    }
}
