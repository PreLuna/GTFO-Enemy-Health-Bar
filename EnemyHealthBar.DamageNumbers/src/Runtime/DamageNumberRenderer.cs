using System;
using PreLuna.EnemyHealthBar.DamageNumbers.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Runtime
{
    internal sealed class DamageNumberRenderer : IDisposable
    {
        GUIStyle normal;
        GUIStyle bold;
        bool initialized;

        internal string FontName => DamageNumberFont.UsedName;

        internal bool Ensure()
        {
            if (initialized) return normal != null && bold != null;
            if (GUI.skin == null) return false;
            var font = DamageNumberFont.Get();
            normal = new GUIStyle(GUI.skin.label)
            {
                richText = false,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Normal
            };
            if (font != null) normal.font = font;
            normal.normal.textColor = Color.white;
            bold = new GUIStyle(normal) { fontStyle = FontStyle.Bold };
            initialized = true;
            return true;
        }

        internal void Draw(
            string text,
            DamageNumberStyle style,
            float alpha,
            float fontSize,
            float rightX,
            float centerY,
            float killAlpha,
            float scale)
        {
            if (!Ensure() || string.IsNullOrEmpty(text)) return;
            var gui = style.Bold ? bold : normal;
            gui.fontSize = Mathf.Max(8, Mathf.RoundToInt(fontSize));
            float width = Mathf.Max(80f, 360f * scale);
            float height = gui.fontSize * 1.5f;
            var rect = new Rect(rightX - width, centerY - height * .5f, width, height);
            var old = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha * .65f));
            GUI.Label(new Rect(rect.x + 1.5f * scale, rect.y + 1.5f * scale, rect.width, rect.height), text, gui);

            GUI.color = new Color(style.R, style.G, style.B, Mathf.Clamp01(alpha * style.Alpha));
            GUI.Label(rect, text, gui);

            if (killAlpha > 0f)
            {
                float textWidth = Mathf.Min(width, gui.CalcSize(new GUIContent(text)).x);
                float thickness = Mathf.Max(1f, 2f * scale);
                var line = new Rect(rightX - textWidth, centerY + gui.fontSize * .55f, textWidth, thickness);
                GUI.color = new Color(DamageNumberStyle.KillAccent.R, DamageNumberStyle.KillAccent.G,
                    DamageNumberStyle.KillAccent.B, Mathf.Clamp01(killAlpha));
                GUI.DrawTexture(line, Texture2D.whiteTexture);
            }

            GUI.color = old;
        }

        public void Dispose()
        {
            normal = null;
            bold = null;
            initialized = false;
            DamageNumberFont.Dispose();
        }
    }
}
