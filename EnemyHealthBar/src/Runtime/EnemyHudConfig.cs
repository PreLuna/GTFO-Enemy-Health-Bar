using BepInEx.Configuration;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Runtime
{
    internal sealed class EnemyHudConfig
    {
        readonly ConfigEntry<bool> enabled;
        readonly ConfigEntry<KeyCode> toggleKey;
        readonly ConfigEntry<float> maxDistance, width, height, opacity, crosshairRadius, margin;
        readonly ConfigEntry<float> minScreenFraction, maxScreenFraction;
        readonly ConfigEntry<bool> showName, showNumbers, showHard, showGlue, showWeakspot, showType, showLevelSlot, showStatusSlots, showEmptyStatusSlots;
        readonly ConfigEntry<int> statusSlots;

        internal EnemyHudConfig(ConfigFile config)
        {
            enabled = config.Bind("General", "Enabled", true, "P1 敌人血条总开关。");
            toggleKey = config.Bind("General", "ToggleKey", KeyCode.PageDown, "开关血条；避免 F5–F11/Insert/End/Pause。None 关闭按键。");
            maxDistance = Number(config, "Display", "MaxDistance", 60f, 5f, 300f, "焦点血条最大距离，米。");
            width = Number(config, "Bar", "Width", 200f, 90f, 420f, "1080p 基准条宽，像素。");
            height = Number(config, "Bar", "Height", 5f, 2f, 16f, "1080p 基准条高，像素。");
            opacity = Number(config, "Bar", "Opacity", .55f, .15f, 1f, "整体透明度。");
            crosshairRadius = Number(config, "Display", "CrosshairRadius", 72f, 0f, 200f, "准星避让半径，像素；0 关闭。");
            margin = Number(config, "Display", "ScreenMargin", 20f, 0f, 100f, "屏幕边距，像素。");
            minScreenFraction = Number(config, "Display", "MinScreenFraction", .10f, 0f, .45f, "血条允许的最低屏幕高度比例。");
            maxScreenFraction = Number(config, "Display", "MaxScreenFraction", .46f, .20f, .95f, "血条允许的最高屏幕高度比例。");
            showName = config.Bind("Display", "ShowName", true, "显示敌人名称/类型。");
            showType = config.Bind("Display", "ShowType", true, "显示 EnemyType 分类（STD/SPEC/MINI/BOSS）。");
            showNumbers = config.Bind("Display", "ShowNumbers", true, "显示生命/HARD/结沫数值。");
            showHard = config.Bind("Bar", "ShowHard", true, "显示硬直白色小段。");
            showGlue = config.Bind("Bar", "ShowGlue", true, "显示结沫青色细条。");
            showWeakspot = config.Bind("Display", "ShowWeakspot", true, "显示弱点数量与倍率标签。");
            showLevelSlot = config.Bind("Display", "ShowLevelSlot", false, "显示预留等级/威胁槽；默认折叠。只有自定义 profile 有 TierText 才显示文本。");
            showStatusSlots = config.Bind("Display", "ShowStatusSlots", true, "用状态槽显示燃烧/结沫/标记/不可推动等状态。");
            showEmptyStatusSlots = config.Bind("Display", "ShowEmptyStatusSlots", false, "显示空状态槽；默认折叠。");
            statusSlots = config.Bind("Display", "StatusSlots", 4, new ConfigDescription("状态槽上限。", new AcceptableValueRange<int>(0, 6)));
        }

        static ConfigEntry<float> Number(ConfigFile config, string group, string key, float value, float min, float max, string description)
            => config.Bind(group, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));

        internal bool Enabled => enabled.Value;
        internal KeyCode ToggleKey => toggleKey.Value;
        internal float MaxDistance => Mathf.Clamp(maxDistance.Value, 5f, 300f);
        internal float Width => Mathf.Clamp(width.Value, 90f, 420f);
        internal float Height => Mathf.Clamp(height.Value, 2f, 16f);
        internal float Opacity => Mathf.Clamp(opacity.Value, .15f, 1f);
        internal float CrosshairRadius => Mathf.Clamp(crosshairRadius.Value, 0f, 200f);
        internal float ScreenMargin => Mathf.Clamp(margin.Value, 0f, 100f);
        internal float MinScreenFraction => Mathf.Clamp(minScreenFraction.Value, 0f, .45f);
        internal float MaxScreenFraction => Mathf.Clamp(maxScreenFraction.Value, .20f, .95f);
        internal bool ShowName => showName.Value;
        internal bool ShowType => showType.Value;
        internal bool ShowNumbers => showNumbers.Value;
        internal bool ShowHard => showHard.Value;
        internal bool ShowGlue => showGlue.Value;
        internal bool ShowWeakspot => showWeakspot.Value;
        internal bool ShowLevelSlot => showLevelSlot.Value;
        internal bool ShowStatusSlots => showStatusSlots.Value;
        internal bool ShowEmptyStatusSlots => showEmptyStatusSlots.Value;
        internal int StatusSlots => Mathf.Clamp(statusSlots.Value, 0, 6);
    }
}
