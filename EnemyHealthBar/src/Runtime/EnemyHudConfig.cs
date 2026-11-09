using System.Collections.Generic;
using BepInEx.Configuration;
using PreLuna.EnemyHealthBar.Control;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Runtime
{
    internal sealed class EnemyHudConfig
    {
        readonly ConfigFile configFile;
        readonly ConfigEntry<bool> enabled;
        readonly ConfigEntry<KeyCode> toggleKey;
        readonly ConfigEntry<float> maxDistance, width, height, opacity, crosshairRadius, margin;
        readonly ConfigEntry<float> minScreenFraction, maxScreenFraction;
        readonly ConfigEntry<bool> showName, showNumbers, showHard, showGlue, showWeakspot, showType, showLevelSlot, showStatusSlots, showEmptyStatusSlots;
        readonly ConfigEntry<int> statusSlots;

        internal EnemyHudConfig(ConfigFile config)
        {
            configFile = config;
            configFile.SaveOnConfigSet = false;
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
            Control = new EnemyHudControlState(ReadControlledSaved, WriteControlledSaved, SaveControlled);
        }

        internal EnemyHudControlState Control { get; }

        static ConfigEntry<float> Number(ConfigFile config, string group, string key, float value, float min, float max, string description)
            => config.Bind(group, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));

        EnemyHudControlValue ReadControlledSaved(string key)
        {
            switch (key)
            {
                case "enabled": return EnemyHudControlValue.Boolean(enabled.Value);
                case "maxDistance": return EnemyHudControlValue.Number(maxDistance.Value);
                case "width": return EnemyHudControlValue.Number(width.Value);
                case "height": return EnemyHudControlValue.Number(height.Value);
                case "opacity": return EnemyHudControlValue.Number(opacity.Value);
                case "showName": return EnemyHudControlValue.Boolean(showName.Value);
                case "showNumbers": return EnemyHudControlValue.Boolean(showNumbers.Value);
                case "showStatusSlots": return EnemyHudControlValue.Boolean(showStatusSlots.Value);
                default: return null;
            }
        }

        void SaveControlled()
        {
            var writes = new List<EnemyHudConfigWrite>
            {
                new EnemyHudConfigWrite("General", "Enabled", enabled.GetSerializedValue()),
                new EnemyHudConfigWrite("Display", "MaxDistance", maxDistance.GetSerializedValue()),
                new EnemyHudConfigWrite("Bar", "Width", width.GetSerializedValue()),
                new EnemyHudConfigWrite("Bar", "Height", height.GetSerializedValue()),
                new EnemyHudConfigWrite("Bar", "Opacity", opacity.GetSerializedValue()),
                new EnemyHudConfigWrite("Display", "ShowName", showName.GetSerializedValue()),
                new EnemyHudConfigWrite("Display", "ShowNumbers", showNumbers.GetSerializedValue()),
                new EnemyHudConfigWrite("Display", "ShowStatusSlots", showStatusSlots.GetSerializedValue())
            };
            EnemyHudConfigFileStore.Save(configFile.ConfigFilePath, writes);
        }

        void WriteControlledSaved(string key, EnemyHudControlValue value)
        {
            switch (key)
            {
                case "enabled":
                    if (value.TryGetBoolean(out var enabledValue)) enabled.Value = enabledValue;
                    break;
                case "maxDistance":
                    if (value.TryGetNumber(out var maxDistanceValue)) maxDistance.Value = (float)maxDistanceValue;
                    break;
                case "width":
                    if (value.TryGetNumber(out var widthValue)) width.Value = (float)widthValue;
                    break;
                case "height":
                    if (value.TryGetNumber(out var heightValue)) height.Value = (float)heightValue;
                    break;
                case "opacity":
                    if (value.TryGetNumber(out var opacityValue)) opacity.Value = (float)opacityValue;
                    break;
                case "showName":
                    if (value.TryGetBoolean(out var showNameValue)) showName.Value = showNameValue;
                    break;
                case "showNumbers":
                    if (value.TryGetBoolean(out var showNumbersValue)) showNumbers.Value = showNumbersValue;
                    break;
                case "showStatusSlots":
                    if (value.TryGetBoolean(out var showStatusSlotsValue)) showStatusSlots.Value = showStatusSlotsValue;
                    break;
            }
        }

        internal bool Enabled => Control.GetEffectiveBoolean("enabled");
        internal KeyCode ToggleKey => toggleKey.Value;
        internal float MaxDistance => Mathf.Clamp((float)Control.GetEffectiveNumber("maxDistance"), 5f, 300f);
        internal float Width => Mathf.Clamp((float)Control.GetEffectiveNumber("width"), 90f, 420f);
        internal float Height => Mathf.Clamp((float)Control.GetEffectiveNumber("height"), 2f, 16f);
        internal float Opacity => Mathf.Clamp((float)Control.GetEffectiveNumber("opacity"), .15f, 1f);
        internal float CrosshairRadius => Mathf.Clamp(crosshairRadius.Value, 0f, 200f);
        internal float ScreenMargin => Mathf.Clamp(margin.Value, 0f, 100f);
        internal float MinScreenFraction => Mathf.Clamp(minScreenFraction.Value, 0f, .45f);
        internal float MaxScreenFraction => Mathf.Clamp(maxScreenFraction.Value, .20f, .95f);
        internal bool ShowName => Control.GetEffectiveBoolean("showName");
        internal bool ShowType => showType.Value;
        internal bool ShowNumbers => Control.GetEffectiveBoolean("showNumbers");
        internal bool ShowHard => showHard.Value;
        internal bool ShowGlue => showGlue.Value;
        internal bool ShowWeakspot => showWeakspot.Value;
        internal bool ShowLevelSlot => showLevelSlot.Value;
        internal bool ShowStatusSlots => Control.GetEffectiveBoolean("showStatusSlots");
        internal bool ShowEmptyStatusSlots => showEmptyStatusSlots.Value;
        internal int StatusSlots => Mathf.Clamp(statusSlots.Value, 0, 6);
    }
}
