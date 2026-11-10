using System;
using Enemies;
using PreLuna.EnemyHealthBar.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Runtime
{
    // P2: up to three enemies the local player recently damaged. Health is read
    // from the live game object, not accumulated from local damage.
    internal sealed class EnemyRecentBars
    {
        const int Capacity = 3;
        const float Hold = 3f;
        const float Fade = .4f;
        const float SightInterval = .1f;
        internal const float HardFlashSeconds = .22f;

        sealed class Entry
        {
            internal IntPtr Key;
            internal EnemyAgent Target;
            internal float LastHit;
            internal float NextSight;
            internal float Ghost;
            internal float LastFraction;
            internal float HardFlashUntil;
            internal float HardFlashFraction;
            internal bool Visible;
            internal EnemyHudState State;
        }

        readonly Entry[] entries = new Entry[Capacity];

        internal void Register(Dam_EnemyDamageBase damage, float before, float after, float max, bool hardTrigger)
        {
            if (damage == null || after <= 0f) return;
            EnemyAgent owner;
            try { owner = damage.Owner; } catch { return; }
            if (owner == null || !owner.Alive) return;

            // Focus bars are keyed by EnemyAgent.Pointer; use the same identity for
            // the recent entry so the focused target is actually skipped in Draw.
            var key = owner.Pointer;
            int slot = Find(key);
            if (slot < 0)
            {
                slot = Empty();
                if (slot < 0)
                {
                    slot = 0;
                    for (int i = 1; i < Capacity; i++)
                        if ((entries[i]?.LastHit ?? float.NegativeInfinity) < (entries[slot]?.LastHit ?? float.NegativeInfinity)) slot = i;
                }
                entries[slot] = new Entry { Key = key, Target = owner, Ghost = max > 0f ? Mathf.Clamp01(before / max) : 0f, LastFraction = max > 0f ? Mathf.Clamp01(after / max) : 0f };
            }
            entries[slot].Key = key;
            entries[slot].Target = owner;
            entries[slot].LastHit = Time.time;
            entries[slot].NextSight = 0f;
            entries[slot].Ghost = Mathf.Max(entries[slot].Ghost, max > 0f ? Mathf.Clamp01(before / max) : 0f);
            entries[slot].Visible = true;
            if (hardTrigger)
            {
                entries[slot].HardFlashUntil = Time.time + HardFlashSeconds;
                entries[slot].HardFlashFraction = 1f;
            }
        }

        internal void Update(Camera camera)
        {
            if (camera == null) return;
            float now = Time.time;
            for (int i = 0; i < Capacity; i++)
            {
                var entry = entries[i];
                if (entry == null) continue;
                if (entry.Target == null || !entry.Target.Alive || now - entry.LastHit > Hold + Fade)
                {
                    entries[i] = null;
                    continue;
                }

                if (now >= entry.NextSight)
                {
                    entry.NextSight = now + SightInterval;
                    Vector3 point = entry.Target.MainModelTransform != null
                        ? entry.Target.MainModelTransform.position + Vector3.up * 1.2f
                        : entry.Target.transform.position + Vector3.up * 1.2f;
                    entry.Visible = CanSee(entry.Target, camera, point);
                }

                if (EnemyVitalsReader.TryRead(entry.Target, camera, out var state))
                {
                    entry.State = state;
                    if (state.HasHealth && state.HealthFraction < entry.LastFraction)
                        entry.Ghost = Mathf.Max(entry.Ghost, entry.LastFraction);
                    entry.LastFraction = state.HealthFraction;
                    entry.Ghost = EnemyBarMath.TrailStep(entry.Ghost, state.HealthFraction, Time.deltaTime, .35f);
                }
                else entry.Visible = false;
            }
        }

        internal void Draw(EnemyHudRenderer renderer, EnemyHudConfig config, IntPtr focusKey)
        {
            for (int i = 0; i < Capacity; i++)
            {
                var entry = entries[i];
                if (entry == null || !entry.Visible || !entry.State.Alive || entry.Key == focusKey) continue;
                float alpha = Alpha(Time.time, entry.LastHit);
                if (alpha <= .001f) continue;
                float hardFlash = entry.HardFlashUntil > Time.time ? entry.HardFlashFraction : -1f;
                renderer.Draw(entry.State, config, alpha, compact: true, ghostFraction: entry.Ghost, hardFlash: hardFlash);
            }
        }

        internal void Clear()
        {
            for (int i = 0; i < Capacity; i++) entries[i] = null;
        }

        static float Alpha(float now, float lastHit) => EnemyBarMath.FadeAlpha(now, lastHit, Hold, Fade);

        int Find(IntPtr key)
        {
            for (int i = 0; i < Capacity; i++)
                if (entries[i] != null && entries[i].Key == key) return i;
            return -1;
        }

        int Empty()
        {
            for (int i = 0; i < Capacity; i++) if (entries[i] == null) return i;
            return -1;
        }

        static bool CanSee(EnemyAgent target, Camera camera, Vector3 point)
        {
            try
            {
                Vector3 origin = camera.transform.position;
                if ((point - origin).sqrMagnitude < .0001f) return true;
                int mask = LayerManager.MASK_BULLETWEAPON_RAY | LayerManager.MASK_CAMERA_RAY
                    | LayerManager.MASK_ENEMY | LayerManager.MASK_ENEMY_DAMAGABLE;
                if (!Physics.Linecast(origin, point, out var hit, mask, QueryTriggerInteraction.Ignore)) return true;
                if (hit.collider == null) return true;
                var enemy = hit.collider.GetComponentInParent<EnemyAgent>();
                return enemy != null && enemy.Pointer == target.Pointer;
            }
            catch
            {
                return true;
            }
        }
    }
}
