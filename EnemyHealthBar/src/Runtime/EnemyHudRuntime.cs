using System;
using Enemies;
using Player;
using PreLuna.EnemyHealthBar.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Runtime
{
    public sealed class EnemyHudRuntime : MonoBehaviour
    {
        internal static EnemyHudRuntime Current;
        EnemyHudConfig config;
        readonly EnemyHudRenderer renderer = new();
        readonly EnemyRecentBars recent = new();
        EnemyHudState state;
        float alpha;
        float lastSeen;
        float focusGhost;
        float lastFocusFraction;
        IntPtr lastTarget;
        bool hidden;
        bool failed;
        bool loggedDraw;
        bool loggedHit;

        public EnemyHudRuntime(IntPtr pointer) : base(pointer) { }

        internal void Initialize(EnemyHudConfig settings)
        {
            Current = this;
            config = settings;
            alpha = 0f;
            lastSeen = float.NegativeInfinity;
            lastTarget = IntPtr.Zero;
            hidden = false;
            failed = false;
        }

        void Update()
        {
            if (config == null || failed) return;
            try
            {
                if (config.ToggleKey != KeyCode.None && Input.GetKeyDown(config.ToggleKey))
                    hidden = !hidden;
                if (hidden || GameStateManager.CurrentStateName != eGameStateName.InLevel)
                {
                    alpha = 0f;
                    state = default;
                    lastTarget = IntPtr.Zero;
                    focusGhost = 0f;
                    lastFocusFraction = 0f;
                    recent.Clear();
                    return;
                }

                float dt = Mathf.Clamp(Time.deltaTime, .001f, .1f);
                bool found = EnemyFocus.TryFind(out _, out var target, out var camera, config.MaxDistance);
                if (found && EnemyVitalsReader.TryRead(target, camera, out var next) && next.Alive)
                {
                    if (lastTarget != target.Pointer)
                    {
                        alpha = Mathf.Min(alpha, .35f);
                        focusGhost = next.HealthFraction;
                        lastFocusFraction = next.HealthFraction;
                    }
                    else
                    {
                        if (next.HealthFraction < lastFocusFraction) focusGhost = Mathf.Max(focusGhost, lastFocusFraction);
                        lastFocusFraction = next.HealthFraction;
                        focusGhost = EnemyBarMath.TrailStep(focusGhost, next.HealthFraction, dt, .35f);
                    }
                    lastTarget = target.Pointer;
                    state = next;
                    lastSeen = Time.time;
                    alpha = Mathf.MoveTowards(alpha, 1f, dt / .10f);
                }
                else
                {
                    if (Time.time - lastSeen > .15f) alpha = Mathf.MoveTowards(alpha, 0f, dt / .20f);
                    if (alpha <= .001f)
                    {
                        alpha = 0f;
                        state = default;
                        lastTarget = IntPtr.Zero;
                        focusGhost = 0f;
                        lastFocusFraction = 0f;
                    }
                }
                if (camera != null) recent.Update(camera);
            }
            catch (Exception error)
            {
                failed = true;
                Plugin.Instance?.LogSource?.LogWarning("HUD update failed: " + error.Message);
            }
        }

        internal void RegisterHit(Dam_EnemyDamageBase damage, float before, float after, float max)
        {
            if (config == null || hidden) return;
            // Register even the focused enemy: it is skipped while focused, then can
            // appear as a recent bar if the crosshair moves away before it expires.
            recent.Register(damage, before, after, max);
            if (!loggedHit)
            {
                loggedHit = true;
                Plugin.Instance?.LogSource?.LogInfo("P2 first local hit: before=" + before + " after=" + after + " max=" + max);
            }
        }

        void OnGUI()
        {
            if (config == null || hidden) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            try
            {
                if (alpha > .001f && state.Alive)
                {
                    if (!loggedDraw)
                    {
                        loggedDraw = true;
                        Plugin.Instance?.LogSource?.LogInfo("P1 first draw: " + state.Name + " type=" + state.TypeLabel + " hp=" + state.Health + "/" + state.HealthMax);
                    }
                    renderer.Draw(state, config, alpha, ghostFraction: focusGhost);
                }
                recent.Draw(renderer, config, lastTarget);
            }
            catch (Exception error)
            {
                failed = true;
                Plugin.Instance?.LogSource?.LogWarning("HUD draw failed: " + error.Message);
            }
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Current, this)) Current = null;
            recent.Clear();
            try { renderer.Dispose(); } catch { }
        }
    }
}
