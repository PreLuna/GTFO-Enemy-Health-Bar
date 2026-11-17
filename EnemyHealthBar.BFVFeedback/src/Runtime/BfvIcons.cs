using System;
using System.Collections.Generic;
using System.IO;
using PreLuna.EnemyHealthBar.BFVFeedback.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Runtime
{
    internal sealed class BfvIcons : IDisposable
    {
        readonly Dictionary<string, Texture2D> textures = new(StringComparer.OrdinalIgnoreCase);
        readonly List<Texture2D> owned = new();
        readonly List<string> roots = new();

        internal bool Ready { get; private set; }

        internal void Initialize(BfvConfig config, string pluginRoot)
        {
            BuildRoots(pluginRoot, config.CustomDirectory);
            var files = new[]
            {
                "icons/hitmark_normal.png",
                "icons/hitmark_weakspot.png",
                "icons/hitmark_armor.png",
                "icons/hitmark_kill.png",
                "icons/kill_minor.png",
                "icons/kill_elite.png",
                "icons/kill_boss.png"
            };
            var loaded = 0;
            for (var i = 0; i < files.Length; i++)
                if (Get(files[i]) != null) loaded++;
            Ready = Get("icons/hitmark_normal.png") != null;
            Log("icons ready=" + Ready + "; files=" + loaded + "/" + files.Length + "; roots=" + roots.Count);
        }

        internal Texture2D Marker(BfvMarkerKind kind)
        {
            var specific = Get(BfvPolicy.MarkerIconFile(kind));
            if (specific != null) return specific;
            return Get("icons/hitmark_normal.png");
        }

        internal Texture2D KillIcon(BfvKillTier tier)
            => Get(BfvPolicy.KillIconFile(tier));

        Texture2D Get(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return null;
            if (textures.TryGetValue(relative, out var cached)) return cached;
            for (var i = 0; i < roots.Count; i++)
            {
                var path = Path.Combine(roots[i], relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) continue;
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    if (bytes == null || bytes.Length == 0) continue;
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(bytes))
                    {
                        UnityEngine.Object.Destroy(texture);
                        continue;
                    }
                    texture.hideFlags = HideFlags.HideAndDontSave;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.filterMode = FilterMode.Bilinear;
                    textures[relative] = texture;
                    owned.Add(texture);
                    return texture;
                }
                catch (Exception error)
                {
                    Log("icon read failed (" + relative + "): " + error.GetType().Name);
                }
            }
            textures[relative] = null;
            Log("icon missing: " + relative);
            return null;
        }

        void BuildRoots(string pluginRoot, string customDirectory)
        {
            roots.Clear();
            var custom = (customDirectory ?? "").Trim();
            if (custom.Length > 0)
            {
                var path = Path.IsPathRooted(custom) ? custom : Path.Combine(pluginRoot, custom);
                if (Directory.Exists(path)) roots.Add(path);
            }
            var assets = Path.Combine(pluginRoot, "Assets");
            if (Directory.Exists(assets)) roots.Add(assets);
        }

        public void Dispose()
        {
            try
            {
                for (var i = 0; i < owned.Count; i++)
                    if (owned[i] != null) UnityEngine.Object.Destroy(owned[i]);
            }
            catch
            {
            }
            owned.Clear();
            textures.Clear();
            roots.Clear();
            Ready = false;
        }

        static void Log(string message)
            => Plugin.Instance?.LogSource?.LogInfo("[BFV] " + message);
    }
}
