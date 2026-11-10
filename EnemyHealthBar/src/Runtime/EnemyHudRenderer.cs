using System;
using PreLuna.EnemyHealthBar.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Runtime
{
    internal sealed class EnemyHudRenderer : IDisposable
    {
        Texture2D pixel;
        GUIStyle style;
        readonly string[] statusLabels = new string[6];
        bool initialized;

        internal void Draw(in EnemyHudState state, EnemyHudConfig config, float alpha, bool compact = false, float ghostFraction = -1f, float hardFlash = -1f)
        {
            Ensure();
            if (!initialized || pixel == null || style == null || alpha <= .001f) return;

            float scale = Mathf.Clamp(Screen.height / 1080f, .75f, 2.5f);
            float width = config.Width * scale * (compact ? .90f : 1f);
            float height = Mathf.Max(2f, config.Height * scale * (compact ? 1.15f : 1f));
            float centerX = state.ScreenAnchor.x;
            float barY = state.ScreenAnchor.y - 22f * scale;
            float minY = Screen.height * config.MinScreenFraction;
            float maxY = Screen.height * config.MaxScreenFraction;
            barY = Mathf.Clamp(barY, minY, Mathf.Max(minY, maxY - height));

            float x = centerX - width * .5f;
            float margin = config.ScreenMargin * scale;
            x = Mathf.Clamp(x, margin, Mathf.Max(margin, Screen.width - margin - width));

            float cx = Screen.width * .5f, cy = Screen.height * .5f;
            float avoid = config.CrosshairRadius * scale;
            if (avoid > 0f && x < cx + avoid && x + width > cx - avoid && barY < cy + avoid && barY + height > cy - avoid)
                barY = cy - avoid - height - 4f * scale;
            barY = Mathf.Clamp(barY, minY, Mathf.Max(minY, maxY - height));

            var ink = state.HealthFraction <= .25f ? new Color(.78f, .27f, .21f) : TypeColor(state.Type, state.BossLike);
            var a = Mathf.Clamp01(alpha) * (compact ? Mathf.Clamp01(config.Opacity + .18f) : config.Opacity);
            var old = GUI.color;

            // hard value pips above the bar
            if (config.ShowHard && state.HasHard)
            {
                int pips = 4;
                float gap = 3f * scale;
                float pw = (width - gap * (pips - 1)) / pips;
                float py = barY - 8f * scale;
                float ph = 4f * scale;
                float hardFraction = hardFlash >= 0f
                    ? Mathf.Clamp01(Mathf.Max(state.HardFraction, hardFlash))
                    : state.HardFraction;
                float fill = hardFraction * pips;
                float edge = Mathf.Max(1f, scale);
                var edgeColor = new Color(.55f, .59f, .62f, .9f * a);
                var trackColor = new Color(.12f, .14f, .16f, .85f * a);
                for (int i = 0; i < pips; i++)
                {
                    var slot = new Rect(x + i * (pw + gap), py, pw, ph);
                    DrawRect(slot, trackColor);
                    float part = Mathf.Clamp01(fill - i);
                    if (part > 0f) DrawRect(new Rect(slot.x, slot.y, pw * part, ph), new Color(.95f, .96f, .98f, a));
                    // Empty pips stay visible at zero build: outline every cell.
                    DrawRect(new Rect(slot.x, slot.y, slot.width, edge), edgeColor);
                    DrawRect(new Rect(slot.x, slot.yMax - edge, slot.width, edge), edgeColor);
                    DrawRect(new Rect(slot.x, slot.y, edge, slot.height), edgeColor);
                    DrawRect(new Rect(slot.xMax - edge, slot.y, edge, slot.height), edgeColor);
                }
            }

            // main bar: lost piece behind the current health, then the fill
            DrawRect(new Rect(x, barY, width, height), new Color(.055f, .07f, .078f, .74f * a));
            if (state.HasHealth)
            {
                float current = state.HealthFraction;
                float ghost = Mathf.Clamp01(ghostFraction);
                if (ghost > current)
                    DrawRect(new Rect(x + width * current, barY, width * (ghost - current), height), new Color(.80f, .82f, .84f, .55f * a));
                DrawRect(new Rect(x, barY, width * current, height), WithAlpha(ink, a));
            }
            // end brackets
            DrawRect(new Rect(x, barY - 3f * scale, 2f * scale, height + 6f * scale), new Color(.95f, .96f, .98f, a));
            DrawRect(new Rect(x + width - 2f * scale, barY - 3f * scale, 2f * scale, height + 6f * scale), new Color(.95f, .96f, .98f, a));

            // glue strip below
            if (config.ShowGlue)
            {
                if (state.GlueImmune)
                {
                    DrawRect(new Rect(x, barY + height + 2f * scale, width, 2f * scale), new Color(.35f, .39f, .42f, .75f * a));
                }
                else if (state.HasGlue)
                {
                    DrawRect(new Rect(x, barY + height + 2f * scale, width, 2f * scale), new Color(.055f, .07f, .078f, .7f * a));
                    DrawRect(new Rect(x, barY + height + 2f * scale, width * state.GlueFraction, 2f * scale), new Color(.35f, .78f, .91f, a));
                }
            }

            // text row above and below
            style.fontSize = Mathf.Max(10, Mathf.RoundToInt(12.5f * scale));
            style.alignment = TextAnchor.MiddleLeft;
            bool showLevel = config.ShowLevelSlot && !compact;
            float levelWidth = showLevel ? 54f * scale : 0f;
            float nameWidth = Mathf.Max(1f, width - (showLevel ? levelWidth + 4f * scale : 0f));
            string head = "";
            if (config.ShowName) head = state.Name;
            if (config.ShowType) head = (head.Length > 0 ? head + "  " : "") + "[" + state.TypeLabel + "]";
            float textY = Mathf.Max(2f, barY - (config.ShowHard ? 31f * scale : 22f * scale));
            if (head.Length > 0)
                DrawText(new Rect(x, textY, nameWidth, 18f * scale), Ellipsize(head, nameWidth, style.fontSize), new Color(.95f, .96f, .97f, a));
            if (showLevel)
                DrawLevelSlot(new Rect(x + width - levelWidth, textY, levelWidth, 18f * scale), state.TierText, scale, a);

            string info = "";
            if (config.ShowNumbers)
            {
                if (state.HasHealth) info = "HP " + Number(state.Health) + "/" + Number(state.HealthMax);
                if (!compact && state.HasHard) info += "  HARD " + Number(state.Hard) + "/" + Number(state.HardMax);
                if (!compact && state.HasGlue) info += "  GLUE " + Mathf.RoundToInt(state.GlueFraction * 100f) + "%";
            }
            if (!compact)
            {
                if (config.ShowWeakspot && state.WeakCount > 0) info += "  WK" + state.WeakCount + "x" + Number(state.WeakMultiplier);
            }
            if (info.Length > 0)
                DrawText(new Rect(x, barY + height + 6f * scale, width, 18f * scale), Ellipsize(info, width, Mathf.Max(9, style.fontSize - 1)),
                    new Color(.82f, .86f, .88f, a));

            if (!compact && config.ShowStatusSlots)
                DrawStatusSlots(state, config, x, barY + height + (info.Length > 0 ? 28f : 12f) * scale, scale, a);

            GUI.color = old;
        }

        void Ensure()
        {
            if (initialized) return;
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, filterMode = FilterMode.Point };
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply(false, true);
            }
            if (style == null && GUI.skin != null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    richText = false,
                    wordWrap = false,
                    clipping = TextClipping.Clip,
                    alignment = TextAnchor.MiddleLeft
                };
                style.normal.textColor = Color.white;
            }
            initialized = pixel != null && style != null;
        }

        void DrawRect(Rect rect, Color color)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = old;
        }

        void DrawLevelSlot(Rect rect, string tier, float scale, float alpha)
        {
            DrawRect(rect, new Color(.055f, .07f, .078f, .62f * alpha));
            Color edge = new(.78f, .80f, .82f, alpha);
            if (!string.IsNullOrWhiteSpace(tier))
            {
                DrawRect(new Rect(rect.x, rect.y, rect.width, 1.5f * scale), edge);
                DrawRect(new Rect(rect.x, rect.yMax - 1.5f * scale, rect.width, 1.5f * scale), edge);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = Mathf.Max(9, Mathf.RoundToInt(10.5f * scale));
                DrawText(rect, tier, new Color(.92f, .93f, .94f, alpha));
                style.alignment = TextAnchor.MiddleLeft;
                style.fontSize = Mathf.Max(10, Mathf.RoundToInt(12.5f * scale));
            }
            else
            {
                DashedBorder(rect, new Color(.55f, .59f, .62f, alpha), scale);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = Mathf.Max(8, Mathf.RoundToInt(9.5f * scale));
                DrawText(rect, "LV", new Color(.62f, .66f, .69f, alpha));
                style.alignment = TextAnchor.MiddleLeft;
                style.fontSize = Mathf.Max(10, Mathf.RoundToInt(12.5f * scale));
            }
        }

        void DrawStatusSlots(in EnemyHudState state, EnemyHudConfig config, float x, float y, float scale, float alpha)
        {
            int count = 0;
            if (state.OnFire) statusLabels[count++] = "F";
            if (state.GlueStuck) statusLabels[count++] = "S";
            if (state.Tagged) statusLabels[count++] = "T";
            if (!state.CanBePushed) statusLabels[count++] = "P";
            if (state.GlueImmune) statusLabels[count++] = "I";

            int slots = config.StatusSlots;
            if (slots <= 0 || (count == 0 && !config.ShowEmptyStatusSlots)) return;
            float size = 18f * scale;
            float gap = 4f * scale;
            int shown = Mathf.Min(slots, Mathf.Max(count, config.ShowEmptyStatusSlots ? slots : count));
            for (int i = 0; i < shown; i++)
            {
                var box = new Rect(x + i * (size + gap), y, size, size);
                DrawRect(box, new Color(.055f, .07f, .078f, .65f * alpha));
                if (i < count)
                {
                    Color edge = statusLabels[i] switch
                    {
                        "F" => new Color(.95f, .55f, .24f, alpha),
                        "S" => new Color(.35f, .78f, .91f, alpha),
                        "T" => new Color(.90f, .82f, .35f, alpha),
                        "P" => new Color(.72f, .76f, .78f, alpha),
                        _ => new Color(.62f, .67f, .70f, alpha)
                    };
                    DrawRect(new Rect(box.x, box.y, box.width, 1.5f * scale), edge);
                    DrawRect(new Rect(box.x, box.yMax - 1.5f * scale, box.width, 1.5f * scale), edge);
                    style.fontSize = Mathf.Max(9, Mathf.RoundToInt(10f * scale));
                    DrawText(new Rect(box.x, box.y - 1f, box.width, box.height + 2f), statusLabels[i], edge);
                }
                else
                {
                    DashedBorder(box, new Color(.35f, .39f, .42f, alpha), scale);
                }
            }
            style.fontSize = Mathf.Max(10, Mathf.RoundToInt(12.5f * scale));
        }

        void DashedBorder(Rect rect, Color color, float scale)
        {
            float dash = 3f * scale, gap = 2f * scale;
            for (float x = rect.x; x < rect.xMax; x += dash + gap)
            {
                float w = Mathf.Min(dash, rect.xMax - x);
                DrawRect(new Rect(x, rect.y, w, 1f * scale), color);
                DrawRect(new Rect(x, rect.yMax - 1f * scale, w, 1f * scale), color);
            }
            for (float y = rect.y; y < rect.yMax; y += dash + gap)
            {
                float h = Mathf.Min(dash, rect.yMax - y);
                DrawRect(new Rect(rect.x, y, 1f * scale, h), color);
                DrawRect(new Rect(rect.xMax - 1f * scale, y, 1f * scale, h), color);
            }
        }

        void DrawText(Rect rect, string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, color.a * .85f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.color = color;
            GUI.Label(rect, text, style);
            GUI.color = old;
        }

        static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, alpha);

        static Color TypeColor(EnemyTypeKind type, bool bossLike)
        {
            if (bossLike) return new Color(.94f, .76f, .30f);
            return type switch
            {
                EnemyTypeKind.Weakling => new Color(.62f, .67f, .70f),
                EnemyTypeKind.Special => new Color(.69f, .42f, .88f),
                EnemyTypeKind.MiniBoss => new Color(.88f, .66f, .24f),
                EnemyTypeKind.Boss => new Color(.94f, .76f, .30f),
                EnemyTypeKind.Unknown => new Color(.62f, .67f, .70f),
                _ => new Color(.85f, .30f, .29f)
            };
        }

        static string Number(float value)
        {
            if (!float.IsFinite(value)) return "?";
            if (Mathf.Abs(value) >= 1_000_000f) return (value / 1_000_000f).ToString() + "M";
            if (Mathf.Abs(value) >= 10_000f) return (value / 1_000f).ToString() + "K";
            if (Mathf.Abs(value - Mathf.Round(value)) < .05f) return Mathf.RoundToInt(value).ToString();
            return value.ToString();
        }

        static string Ellipsize(string text, float width, int fontSize)
        {
            if (string.IsNullOrEmpty(text)) return text;
            float budget = Mathf.Max(1f, (width - 4f) / Mathf.Max(1f, fontSize));
            float used = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                used += text[i] > 127 ? 1f : .58f;
                if (used > budget)
                {
                    int cut = Mathf.Max(0, i - 1);
                    return text.Substring(0, cut) + "…";
                }
            }
            return text;
        }

        public void Dispose()
        {
            if (pixel != null) UnityEngine.Object.Destroy(pixel);
            pixel = null;
            style = null;
            initialized = false;
        }
    }
}
