using System;
using System.Collections.Generic;
using PreLuna.EnemyHealthBar.Api;
using PreLuna.EnemyHealthBar.DamageNumbers.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.DamageNumbers.Runtime
{
    public sealed class DamageNumberRuntime : MonoBehaviour
    {
        const int MaxTracks = 12;

        sealed class Entry
        {
            internal float SpawnTime;
            internal int Damage;
            internal DamageNumberKind Kind;
            internal bool Kill;
        }

        sealed class Track
        {
            internal long Key;
            internal Vector3 Anchor;
            internal float LastSpawn;
            internal readonly List<Entry> Entries = new();
        }

        readonly List<Track> tracks = new();
        readonly DamageNumberRenderer renderer = new();
        DamageNumberConfig config;
        bool toggledOn = true;
        bool subscribed;
        bool failed;

        public DamageNumberRuntime(IntPtr pointer) : base(pointer) { }

        internal void Initialize(DamageNumberConfig settings)
        {
            config = settings;
            toggledOn = true;
            failed = false;
            if (!subscribed)
            {
                EnemyHitApi.HitRegistered += OnHit;
                subscribed = true;
            }
        }

        void Update()
        {
            if (config == null || failed) return;
            if (config.ToggleKey != KeyCode.None && Input.GetKeyDown(config.ToggleKey))
                toggledOn = !toggledOn;
            if (!config.Enabled || !toggledOn || !EnemyHitApi.HudVisible)
            {
                tracks.Clear();
            }
        }

        void OnHit(EnemyHitInfo info)
        {
            if (config == null || failed || !config.Enabled || !toggledOn || !EnemyHitApi.HudVisible) return;
            try
            {
                DamageNumberKind kind;
                int damage;
                if (info.Immune)
                {
                    if (!config.ShowImmune) return;
                    kind = DamageNumberKind.Immune;
                    damage = 0;
                }
                else
                {
                    if (info.Armor && !config.ShowArmor) return;
                    damage = DamageNumberFormat.FromApplied(info.AppliedDamage);
                    if (damage <= 0) return;
                    kind = info.Armor
                        ? DamageNumberKind.Armor
                        : (info.Weakspot || info.BackBonus ? DamageNumberKind.Weakspot : DamageNumberKind.Normal);
                }

                float now = Time.time;
                long key = info.Enemy.ToInt64();
                var track = FindTrack(key);
                if (track == null)
                {
                    if (tracks.Count >= MaxTracks) RemoveOldestTrack();
                    track = new Track { Key = key };
                    tracks.Add(track);
                }

                Entry last = track.Entries.Count > 0 ? track.Entries[track.Entries.Count - 1] : null;
                if (last != null && config.MergeWindowSeconds > 0f &&
                    DamageNumberMath.ShouldMerge(now, last.SpawnTime, config.MergeWindowSeconds) &&
                    last.Kind == kind && !last.Kill)
                {
                    last.Damage += damage;
                    last.SpawnTime = now;
                    last.Kill = info.Kill;
                }
                else
                {
                    track.Entries.Add(new Entry
                    {
                        SpawnTime = now,
                        Damage = damage,
                        Kind = kind,
                        Kill = info.Kill
                    });
                    int excess = DamageNumberMath.TrimCount(track.Entries.Count, config.MaxRows);
                    if (excess > 0) track.Entries.RemoveRange(0, excess);
                }

                track.Anchor = info.Position;
                track.LastSpawn = now;
            }
            catch (Exception error)
            {
                failed = true;
                Plugin.Instance?.LogSource?.LogWarning("damage number hit failed: " + error.Message);
            }
        }

        void OnGUI()
        {
            if (config == null || failed || !config.Enabled || !toggledOn || !EnemyHitApi.HudVisible) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            var camera = CameraManager.GetCurrentCamera();
            if (camera == null) return;

            int oldDepth = GUI.depth;
            try
            {
                float now = Time.time;
                float scale = DamageNumberMath.ScreenScale(Screen.height);
                float lifetime = config.Lifetime;
                float lineHeight = config.LineHeight * scale;
                GUI.depth = -10;

                for (int t = tracks.Count - 1; t >= 0; t--)
                {
                    var track = tracks[t];
                    while (track.Entries.Count > 0 &&
                           DamageNumberMath.IsExpired(now - track.Entries[0].SpawnTime, lifetime))
                        track.Entries.RemoveAt(0);
                    if (track.Entries.Count == 0)
                    {
                        tracks.RemoveAt(t);
                        continue;
                    }

                    if (config.MaxDistance > 0f &&
                        Vector3.Distance(camera.transform.position, track.Anchor) > config.MaxDistance)
                        continue;

                    Vector3 screen = camera.WorldToScreenPoint(track.Anchor);
                    if (!float.IsFinite(screen.x) || !float.IsFinite(screen.y) || screen.z <= 0f) continue;
                    float x = screen.x;
                    float y = Screen.height - screen.y;
                    if (!float.IsFinite(x) || !float.IsFinite(y)) continue;
                    if (x < 0f || x > Screen.width || y < 0f || y > Screen.height) continue;

                    float rightX = Mathf.Min(x + config.RightOffset * scale, Screen.width - 8f * scale);
                    if (rightX < 120f * scale) continue;

                    int rows = Mathf.Min(track.Entries.Count, config.MaxRows);
                    for (int i = 0; i < rows; i++)
                    {
                        int index = track.Entries.Count - 1 - i;
                        var entry = track.Entries[index];
                        float age = now - entry.SpawnTime;
                        float alpha = DamageNumberMath.FadeAlpha(age, lifetime);
                        if (alpha <= .001f) continue;

                        var style = DamageNumberStyle.For(entry.Kind);
                        float fontSize = config.FontSize * scale * style.SizeFactor;
                        float centerY = y - DamageNumberMath.RowOffset(i, lineHeight) - 6f * scale * (age / lifetime);
                        float killAlpha = entry.Kill && config.ShowKillFlash && age < .25f
                            ? Mathf.Clamp01(1f - age / .25f)
                            : 0f;
                        string text = entry.Kind == DamageNumberKind.Immune
                            ? DamageNumberFormat.ImmuneText
                            : DamageNumberFormat.Number(entry.Damage);
                        renderer.Draw(text, style, alpha, fontSize, rightX, centerY, killAlpha, scale);
                    }
                }

            }
            catch (Exception error)
            {
                failed = true;
                Plugin.Instance?.LogSource?.LogWarning("damage number draw failed: " + error.Message);
            }
            finally
            {
                GUI.depth = oldDepth;
            }
        }

        Track FindTrack(long key)
        {
            for (int i = 0; i < tracks.Count; i++)
                if (tracks[i].Key == key) return tracks[i];
            return null;
        }

        void RemoveOldestTrack()
        {
            if (tracks.Count == 0) return;
            int oldest = 0;
            for (int i = 1; i < tracks.Count; i++)
                if (tracks[i].LastSpawn < tracks[oldest].LastSpawn) oldest = i;
            tracks.RemoveAt(oldest);
        }

        void OnDestroy()
        {
            if (subscribed)
            {
                EnemyHitApi.HitRegistered -= OnHit;
                subscribed = false;
            }
            tracks.Clear();
            try { renderer.Dispose(); } catch { }
        }
    }
}
