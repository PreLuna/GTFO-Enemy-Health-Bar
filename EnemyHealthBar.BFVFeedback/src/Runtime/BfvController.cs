using System;
using System.Collections.Generic;
using System.Reflection;
using Enemies;
using HarmonyLib;
using PreLuna.EnemyHealthBar.Api;
using PreLuna.EnemyHealthBar.BFVFeedback.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Runtime
{
    internal sealed class BfvController : IDisposable
    {
        struct MarkerEntry
        {
            internal Vector3 World;
            internal BfvMarkerKind Kind;
            internal float At;
            internal IntPtr Enemy;
        }

        struct KillIconEntry
        {
            internal Vector3 World;
            internal BfvKillTier Tier;
            internal float At;
        }

        const int MaxMarkers = 24;

        BfvConfig config;
        BfvAudio audio;
        BfvIcons icons;
        readonly List<MarkerEntry> markers = new();
        readonly List<KillIconEntry> killIcons = new();
        readonly List<uint> mutedIds = new();
        Color normalColor;
        Color weakspotColor;
        Color armorColor;
        Color killColor;
        string normalColorText;
        string weakspotColorText;
        string armorColorText;
        string killColorText;
        float lastHitSoundAt = float.NegativeInfinity;
        float lastKillSoundAt = float.NegativeInfinity;
        float lastKillPriorityAt = float.NegativeInfinity;
        bool hidden;
        bool failed;
        bool subscribed;
        bool loggedParentHit;
        bool mutedResolved;
        bool mutedWarned;

        internal void Initialize(BfvConfig settings, string pluginRoot)
        {
            config = settings;
            hidden = false;
            failed = false;
            markers.Clear();
            killIcons.Clear();
            normalColorText = settings.ColorNormal;
            weakspotColorText = settings.ColorWeakspot;
            armorColorText = settings.ColorArmor;
            killColorText = settings.ColorKill;
            RefreshColors(true);
            audio = new BfvAudio();
            audio.Initialize(settings, pluginRoot);
            icons = new BfvIcons();
            icons.Initialize(settings, pluginRoot);
            ResolveMutedEvents();
            if (!subscribed)
            {
                EnemyHitApi.HitRegistered += OnParentHit;
                subscribed = true;
            }
        }

        internal bool FeedbackActive
        {
            get
            {
                if (config == null || failed || !config.HasAnyFeedback || hidden) return false;
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return false;
                if (!config.FollowHudVisibility) return true;
                return EnemyHitApi.HudVisible;
            }
        }

        internal void Update()
        {
            if (config == null || failed) return;
            RefreshColors();
            if (config.ToggleKey != KeyCode.None && Input.GetKeyDown(config.ToggleKey))
            {
                hidden = !hidden;
                ClearTransient();
            }
            if (!FeedbackActive)
            {
                ClearTransient();
                return;
            }
            if (config.SoundActive && audio != null && !audio.Warmed) audio.Warmup();
            var now = Time.time;
            for (var i = markers.Count - 1; i >= 0; i--)
                if (BfvPolicy.ShouldExpire(now - markers[i].At, BfvPolicy.MarkerLifetime(markers[i].Kind)))
                    markers.RemoveAt(i);
            if (!config.HitMarkerEnabled) markers.Clear();
            for (var i = killIcons.Count - 1; i >= 0; i--)
                if (BfvPolicy.ShouldExpire(now - killIcons[i].At, config.KillIconLifetime))
                    killIcons.RemoveAt(i);
            if (!config.KillIconEnabled) killIcons.Clear();
        }

        // Called from the CrosshairGuiLayer prefix. Returns true when the vanilla
        // marker should be suppressed; false keeps the vanilla marker.
        internal bool OnHitIndicator(Vector3 world, bool isWeakspot, bool isFriendly, bool noDamage)
        {
            if (!FeedbackActive || isFriendly || config == null || !Finite(world)) return false;
            var hasContext = BfvImpactContext.TryFind(world, Time.frameCount, out var impact);
            var armor = (hasContext && impact.Armor) || (config.ArmorFromNoDamage && noDamage);
            var kind = BfvPolicy.MarkerFor(
                isWeakspot || (hasContext && impact.Weakspot),
                armor,
                false, false);
            var handled = false;
            if (config.HitMarkerEnabled && icons != null && icons.Ready)
            {
                var texture = icons.Marker(kind);
                if (texture != null)
                {
                    AddMarker(world, kind, hasContext ? impact.Damage : IntPtr.Zero);
                    handled = true;
                }
            }
            if (config.SoundActive && config.HitEnabled)
                PlayHitSound(kind);
            return handled && config.ReplaceVanillaMarker;
        }

        internal bool OnDeathIndicator(Vector3 world)
        {
            if (!FeedbackActive || config == null || !Finite(world)) return false;
            var hasContext = BfvImpactContext.TryFind(world, Time.frameCount, out var impact);
            if (!hasContext) hasContext = TryResolveEnemy(world, out impact);
            var tier = hasContext
                ? BfvPolicy.TierFor(impact.HealthMax, config.MinorHealthThreshold, config.HeavyHealthThreshold)
                : BfvKillTier.Minor;
            if (hasContext && config.TierSource == BfvTierSource.EnemyType && impact.EnemyType >= 0)
                tier = BfvPolicy.TierForEnemyType(impact.EnemyType);
            var flight = hasContext && impact.Flyer ? BfvFlight.Flying : BfvFlight.None;
            var handled = false;

            if (config.HitMarkerEnabled && icons != null && icons.Ready)
            {
                var texture = icons.Marker(BfvMarkerKind.Kill);
                if (texture != null)
                {
                    AddMarker(world, BfvMarkerKind.Kill, hasContext ? impact.Damage : IntPtr.Zero);
                    handled = true;
                }
            }
            if (config.KillIconEnabled && icons != null)
            {
                var texture = icons.KillIcon(tier);
                if (texture != null)
                {
                    var anchor = hasContext && Finite(impact.Eye)
                        ? impact.Eye + Vector3.up * config.KillIconGap
                        : world + Vector3.up * .6f;
                    if (AddKillIcon(anchor, tier)) handled = true;
                }
            }
            if (config.SoundActive && config.KillEnabled)
                PlayKillSound(tier, flight);
            return handled && config.ReplaceVanillaMarker;
        }

        internal bool ShouldMuteIndicator(uint eventId)
        {
            if (!FeedbackActive || config == null || !config.MuteVanillaIndicator) return false;
            if (!mutedResolved) ResolveMutedEvents();
            for (var i = 0; i < mutedIds.Count; i++)
                if (mutedIds[i] == eventId) return true;
            return false;
        }

        void OnParentHit(EnemyHitInfo info)
        {
            if (config == null || failed || loggedParentHit) return;
            loggedParentHit = true;
            Log("parent hit linked; applied=" + info.AppliedDamage.ToString() +
                "; weakspot=" + info.Weakspot + "; armor=" + info.Armor +
                "; kill=" + info.Kill + "; healthMax=" + info.HealthMax.ToString());
        }

        void PlayHitSound(BfvMarkerKind kind)
        {
            if (audio == null || !audio.Ready) return;
            var now = Time.time;
            if (now - lastKillPriorityAt < config.KillPrioritySeconds) return;
            if (now - lastHitSoundAt < config.MinIntervalSeconds) return;
            lastHitSoundAt = now;
            var cue = kind == BfvMarkerKind.Armor
                ? BfvHitCue.Armor
                : kind == BfvMarkerKind.Weakspot ? BfvHitCue.Weakspot : BfvHitCue.Normal;
            audio.PlayHit(cue, config.HitVolume);
        }

        void PlayKillSound(BfvKillTier tier, BfvFlight flight)
        {
            if (audio == null || !audio.Ready) return;
            var now = Time.time;
            if (now - lastKillSoundAt < config.MinIntervalSeconds) return;
            lastKillSoundAt = now;
            lastKillPriorityAt = now;
            audio.PlayKill(tier, flight, config.KillVolume);
        }

        void AddMarker(Vector3 world, BfvMarkerKind kind, IntPtr enemy)
        {
            var now = Time.time;
            var window = config.MergeWindowSeconds;
            if (window > 0f)
            {
                for (var i = markers.Count - 1; i >= 0; i--)
                {
                    var marker = markers[i];
                    if (marker.Enemy != enemy) continue;
                    if (!BfvPolicy.ShouldMerge(now, marker.At, window)) continue;
                    if (BfvPolicy.MarkerPriority(kind) >= BfvPolicy.MarkerPriority(marker.Kind))
                    {
                        marker.World = world;
                        marker.Kind = kind;
                    }
                    marker.At = now;
                    markers[i] = marker;
                    return;
                }
            }
            if (markers.Count >= MaxMarkers) RemoveOldestMarker();
            markers.Add(new MarkerEntry { World = world, Kind = kind, At = now, Enemy = enemy });
        }

        void RemoveOldestMarker()
        {
            if (markers.Count == 0) return;
            var oldest = 0;
            for (var i = 1; i < markers.Count; i++)
                if (markers[i].At < markers[oldest].At) oldest = i;
            markers.RemoveAt(oldest);
        }

        bool AddKillIcon(Vector3 world, BfvKillTier tier)
        {
            if (!config.KillIconEnabled || !Finite(world)) return false;
            var max = config.KillIconMax;
            while (killIcons.Count >= max && killIcons.Count > 0)
            {
                var oldest = 0;
                for (var i = 1; i < killIcons.Count; i++)
                    if (killIcons[i].At < killIcons[oldest].At) oldest = i;
                killIcons.RemoveAt(oldest);
            }
            killIcons.Add(new KillIconEntry { World = world, Tier = tier, At = Time.time });
            return true;
        }

        internal void OnGUI()
        {
            if (config == null || failed || !FeedbackActive) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            var camera = CameraManager.GetCurrentCamera();
            if (camera == null) return;
            var scale = BfvPolicy.ScreenScale(Screen.height);
            var oldDepth = GUI.depth;
            var oldColor = GUI.color;
            try
            {
                GUI.depth = -10;
                if (config.HitMarkerEnabled)
                {
                    for (var i = 0; i < markers.Count; i++)
                    {
                        var marker = markers[i];
                        var alpha = BfvPolicy.MarkerAlpha(Time.time - marker.At, BfvPolicy.MarkerLifetime(marker.Kind));
                        if (alpha <= .001f) continue;
                        if (!TryProject(camera, marker.World, out var gui)) continue;
                        var texture = icons == null ? null : icons.Marker(marker.Kind);
                        if (texture == null) continue;
                        var size = config.HitMarkerSize * scale * BfvPolicy.MarkerScale(marker.Kind);
                        var color = ColorFor(marker.Kind);
                        GUI.color = new Color(color.r, color.g, color.b, alpha);
                        GUI.DrawTexture(new Rect(gui.x - size * .5f, gui.y - size * .5f, size, size),
                            texture, ScaleMode.ScaleToFit, true);
                    }
                }
                if (config.KillIconEnabled)
                {
                    for (var i = 0; i < killIcons.Count; i++)
                    {
                        var icon = killIcons[i];
                        var alpha = BfvPolicy.IconAlpha(Time.time - icon.At, config.KillIconLifetime) * config.KillIconOpacity;
                        if (alpha <= .001f) continue;
                        if (!TryProject(camera, icon.World, out var gui)) continue;
                        var texture = icons == null ? null : icons.KillIcon(icon.Tier);
                        if (texture == null) continue;
                        var size = config.KillIconSize * scale;
                        GUI.color = new Color(1f, 1f, 1f, alpha);
                        GUI.DrawTexture(new Rect(gui.x - size * .5f, gui.y - size * .5f, size, size),
                            texture, ScaleMode.ScaleToFit, true);
                    }
                }
            }
            catch (Exception error)
            {
                failed = true;
                Log("draw failed: " + error.Message);
            }
            finally
            {
                GUI.depth = oldDepth;
                GUI.color = oldColor;
            }
        }

        void RefreshColors(bool force = false)
        {
            if (config == null) return;
            var normal = config.ColorNormal;
            var weakspot = config.ColorWeakspot;
            var armor = config.ColorArmor;
            var kill = config.ColorKill;
            if (!force && normal == normalColorText && weakspot == weakspotColorText &&
                armor == armorColorText && kill == killColorText) return;
            normalColorText = normal;
            weakspotColorText = weakspot;
            armorColorText = armor;
            killColorText = kill;
            normalColor = ToColor(BfvColor.ParseOrDefault(normal, BfvColor.White));
            weakspotColor = ToColor(BfvColor.ParseOrDefault(weakspot, BfvColor.Weakspot));
            armorColor = ToColor(BfvColor.ParseOrDefault(armor, BfvColor.Armor));
            killColor = ToColor(BfvColor.ParseOrDefault(kill, BfvColor.Kill));
        }

        Color ColorFor(BfvMarkerKind kind)
        {
            switch (kind)
            {
                case BfvMarkerKind.Weakspot: return weakspotColor;
                case BfvMarkerKind.Armor: return armorColor;
                case BfvMarkerKind.Kill: return killColor;
                default: return normalColor;
            }
        }

        static bool TryProject(Camera camera, Vector3 world, out Vector2 gui)
        {
            gui = default;
            if (camera == null || !Finite(world)) return false;
            Vector3 screen;
            try { screen = camera.WorldToScreenPoint(world); }
            catch { return false; }
            if (!float.IsFinite(screen.x) || !float.IsFinite(screen.y) || screen.z <= 0f) return false;
            var x = screen.x;
            var y = Screen.height - screen.y;
            if (!float.IsFinite(x) || !float.IsFinite(y)) return false;
            if (x < 0f || x > Screen.width || y < 0f || y > Screen.height) return false;
            gui = new Vector2(x, y);
            return true;
        }

        bool TryResolveEnemy(Vector3 world, out BfvImpactContext.Impact impact)
        {
            impact = default;
            try
            {
                var mask = LayerManager.MASK_ENEMY | LayerManager.MASK_ENEMY_DAMAGABLE;
                var colliders = Physics.OverlapSphere(world, .8f, mask, QueryTriggerInteraction.Ignore);
                if (colliders == null) return false;
                for (var i = 0; i < colliders.Length; i++)
                {
                    var collider = colliders[i];
                    if (collider == null) continue;
                    EnemyAgent agent;
                    try { agent = collider.GetComponentInParent<EnemyAgent>(); }
                    catch { agent = null; }
                    if (agent == null || agent.Damage == null) continue;
                    impact.Valid = true;
                    impact.Position = world;
                    impact.Frame = Time.frameCount;
                    try
                    {
                        impact.Owner = agent.Pointer;
                        impact.Eye = agent.EyePosition;
                        var behavior = agent.EnemyBehaviorData;
                        impact.Flyer = behavior != null && behavior.IsFlyer;
                        var data = agent.EnemyData;
                        impact.EnemyType = data == null ? -1 : (int)data.EnemyType;
                        impact.HealthMax = agent.Damage.HealthMax;
                    }
                    catch
                    {
                    }
                    if (!Finite(impact.Eye)) impact.Eye = world + Vector3.up * 1.2f;
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        void ResolveMutedEvents()
        {
            if (mutedResolved) return;
            mutedIds.Clear();
            try
            {
                var type = AccessTools.TypeByName("AK.EVENTS");
                if (type == null)
                {
                    if (!mutedWarned)
                    {
                        mutedWarned = true;
                        Log("AK.EVENTS not ready; will retry on the first indicator sound");
                    }
                    return;
                }
                AddEvent(type, "BULLETHITENEMYLOCALINDICATOR");
                AddEvent(type, "BULLETHITENEMYWEAKSPOTLOCALINDICATOR");
                mutedResolved = true;
                Log("muted indicator ids=" + mutedIds.Count);
            }
            catch (Exception error)
            {
                if (!mutedWarned)
                {
                    mutedWarned = true;
                    Log("indicator id resolve failed: " + error.GetType().Name);
                }
            }
        }

        void AddEvent(Type type, string name)
        {
            try
            {
                var property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public);
                if (property != null && property.PropertyType == typeof(uint))
                {
                    mutedIds.Add((uint)property.GetValue(null));
                    return;
                }
                var field = type.GetField(name, BindingFlags.Static | BindingFlags.Public);
                if (field != null && field.FieldType == typeof(uint))
                    mutedIds.Add((uint)field.GetValue(null));
            }
            catch
            {
            }
        }

        static Color ToColor(BfvColor color)
            => new Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

        static bool Finite(Vector3 value)
            => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        void ClearTransient()
        {
            markers.Clear();
            killIcons.Clear();
        }

        public void Dispose()
        {
            if (subscribed)
            {
                EnemyHitApi.HitRegistered -= OnParentHit;
                subscribed = false;
            }
            ClearTransient();
            try { audio?.Dispose(); } catch { }
            try { icons?.Dispose(); } catch { }
            audio = null;
            icons = null;
            config = null;
        }

        static void Log(string message)
            => Plugin.Instance?.LogSource?.LogInfo("[BFV] " + message);
    }
}
