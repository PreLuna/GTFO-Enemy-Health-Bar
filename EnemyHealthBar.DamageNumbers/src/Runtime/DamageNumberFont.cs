using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Runtime
{
    // System-font fallback only. Borda is a commercial Division 2 typeface and
    // is never shipped; these candidates are narrowed/condensed UI faces already
    // present on common Windows installs. On failure the IMGUI default is used.
    internal static class DamageNumberFont
    {
        static Font cached;
        static bool attempted;

        internal static string UsedName { get; private set; }

        internal static Font Get()
        {
            if (attempted) return cached;
            attempted = true;
            foreach (var candidate in new[] { "Bahnschrift SemiCondensed", "Bahnschrift", "Arial Narrow", "Segoe UI Semibold" })
            {
                Font font = null;
                try
                {
                    font = new Font(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<Font>.NativeClassPtr));
                    Font.Internal_CreateDynamicFont(font, new Il2CppStringArray(new[] { candidate }), 16);
                    if (font == null) continue;
                    font.hideFlags = HideFlags.DontSave;
                    font.RequestCharactersInTexture("0123456789,IMMUNE", 16, FontStyle.Normal);
                    if (!font.HasCharacter('0') || !font.HasCharacter('1'))
                    {
                        UnityEngine.Object.Destroy(font);
                        continue;
                    }
                    cached = font;
                    UsedName = candidate;
                    return cached;
                }
                catch
                {
                    if (font != null) UnityEngine.Object.Destroy(font);
                }
            }
            UsedName = null;
            return null;
        }

        internal static void Dispose()
        {
            if (cached != null) UnityEngine.Object.Destroy(cached);
            cached = null;
            UsedName = null;
            attempted = false;
        }
    }
}
