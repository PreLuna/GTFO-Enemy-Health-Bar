using BepInEx.Configuration;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Runtime
{
    internal sealed class DamageNumberConfig
    {
        readonly ConfigEntry<bool> enabled;
        readonly ConfigEntry<KeyCode> toggleKey;
        readonly ConfigEntry<int> maxRows;
        readonly ConfigEntry<float> lifetime;
        readonly ConfigEntry<float> fontSize;
        readonly ConfigEntry<float> lineHeight;
        readonly ConfigEntry<float> rightOffset;
        readonly ConfigEntry<float> mergeWindowMs;
        readonly ConfigEntry<float> maxDistance;
        readonly ConfigEntry<bool> showArmor;
        readonly ConfigEntry<bool> showImmune;
        readonly ConfigEntry<bool> showKillFlash;

        internal DamageNumberConfig(ConfigFile config)
        {
            enabled = config.Bind("General", "Enabled", true, "伤害数字总开关。父血条 HUD 隐藏时也会一起隐藏。");
            toggleKey = config.Bind("General", "ToggleKey", KeyCode.None, "独立开关；None 关闭按键。父血条 PageDown 仍然控制整体显隐。");
            maxRows = config.Bind("Display", "MaxRows", 12,
                new ConfigDescription("每个目标最多显示的行数。霰弹枪每弹丸一条时建议 8–12。", new AcceptableValueRange<int>(1, 12)));
            lifetime = config.Bind("Display", "Lifetime", .95f,
                new ConfigDescription("每条数字的生命周期（秒）。", new AcceptableValueRange<float>(.3f, 3f)));
            fontSize = config.Bind("Display", "FontSize", 24f,
                new ConfigDescription("1080p 普通数字基准字号。", new AcceptableValueRange<float>(12f, 48f)));
            lineHeight = config.Bind("Display", "LineHeight", 30f,
                new ConfigDescription("1080p 行距。", new AcceptableValueRange<float>(14f, 60f)));
            rightOffset = config.Bind("Display", "RightOffset", 14f,
                new ConfigDescription("数字右边缘相对命中点的像素偏移。", new AcceptableValueRange<float>(0f, 80f)));
            mergeWindowMs = config.Bind("Display", "MergeWindowMs", 0f,
                new ConfigDescription("同目标合并窗口（毫秒）。0 = 不合并，霰弹每个弹丸一条。", new AcceptableValueRange<float>(0f, 500f)));
            maxDistance = config.Bind("Display", "MaxDistance", 100f,
                new ConfigDescription("超过该距离不再显示，米；0 = 不限。", new AcceptableValueRange<float>(0f, 300f)));
            showArmor = config.Bind("Display", "ShowArmor", true, "显示护甲肢体命中（青色）。");
            showImmune = config.Bind("Display", "ShowImmune", true, "显示 IMMUNE。");
            showKillFlash = config.Bind("Display", "ShowKillFlash", true, "击杀时显示红色短下划。");
        }

        internal ConfigEntry<bool> EnabledEntry => enabled;
        internal ConfigEntry<int> MaxRowsEntry => maxRows;
        internal ConfigEntry<float> LifetimeEntry => lifetime;
        internal ConfigEntry<float> FontSizeEntry => fontSize;
        internal ConfigEntry<float> MaxDistanceEntry => maxDistance;
        internal ConfigEntry<bool> ShowArmorEntry => showArmor;

        internal bool Enabled => enabled.Value;
        internal KeyCode ToggleKey => toggleKey.Value;
        internal int MaxRows => Mathf.Clamp(maxRows.Value, 1, 12);
        internal float Lifetime => Mathf.Clamp(lifetime.Value, .3f, 3f);
        internal float FontSize => Mathf.Clamp(fontSize.Value, 12f, 48f);
        internal float LineHeight => Mathf.Clamp(lineHeight.Value, 14f, 60f);
        internal float RightOffset => Mathf.Clamp(rightOffset.Value, 0f, 80f);
        internal float MergeWindowSeconds => Mathf.Clamp(mergeWindowMs.Value, 0f, 500f) / 1000f;
        internal float MaxDistance => Mathf.Clamp(maxDistance.Value, 0f, 300f);
        internal bool ShowArmor => showArmor.Value;
        internal bool ShowImmune => showImmune.Value;
        internal bool ShowKillFlash => showKillFlash.Value;
    }
}
