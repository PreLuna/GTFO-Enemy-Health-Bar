using System;
using Agents;
using Player;
using PreLuna.EnemyHealthBar.BFVFeedback.Core;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.BFVFeedback.Runtime
{
    // Read-only context captured in the damage prefix. It exists only to give the
    // game's hit/death indicator events the enemy, head anchor and health cap that
    // the indicator events themselves do not carry. No damage is calculated here.
    internal static class BfvImpactContext
    {
        const int Capacity = 16;
        const float MatchDistance = 1f;
        const int FrameWindow = 2;

        internal struct Impact
        {
            internal IntPtr Damage;
            internal IntPtr Owner;
            internal Vector3 Position;
            internal Vector3 Eye;
            internal float HealthMax;
            internal int EnemyType;
            internal bool Weakspot;
            internal bool Armor;
            internal bool Flyer;
            internal int Frame;
            internal bool Valid;
        }

        static readonly Impact[] Impacts = new Impact[Capacity];
        static int next;

        internal static void Record(
            Dam_EnemyDamageBase damage, Agent source, Vector3 position,
            int limbId, float precisionMulti, bool melee)
        {
            if (damage == null || source == null) return;
            var runtime = BfvRuntime.Current;
            if (runtime == null || !runtime.FeedbackActive) return;

            try
            {
                var local = PlayerManager.GetLocalPlayerAgent();
                if (local == null || local.Pointer != source.Pointer) return;
                var health = damage._Health_k__BackingField;
                if (!BfvPolicy.Finite(health) || health <= 0f) return;

                var impact = new Impact
                {
                    Damage = damage.Pointer,
                    Position = position,
                    Frame = Time.frameCount,
                    Valid = true
                };
                try
                {
                    var owner = damage.Owner;
                    if (owner != null)
                    {
                        impact.Owner = owner.Pointer;
                        impact.Eye = owner.EyePosition;
                        var behavior = owner.EnemyBehaviorData;
                        impact.Flyer = behavior != null && behavior.IsFlyer;
                        var data = owner.EnemyData;
                        impact.EnemyType = data == null ? -1 : (int)data.EnemyType;
                    }
                }
                catch
                {
                }
                if (!IsFinite(impact.Eye))
                    impact.Eye = position + Vector3.up * 1.2f;

                try
                {
                    var limbs = damage.DamageLimbs;
                    if (limbs != null && limbId >= 0 && limbId < limbs.Length)
                    {
                        var limb = limbs[limbId];
                        if (limb != null)
                        {
                            var type = limb.m_type;
                            impact.Weakspot = type == eLimbDamageType.Weakspot
                                || (!melee && precisionMulti > 1.01f);
                            impact.Armor = type == eLimbDamageType.Armor;
                        }
                    }
                }
                catch
                {
                }
                if (!melee && precisionMulti > 1.01f) impact.Weakspot = true;

                try { impact.HealthMax = damage._HealthMax_k__BackingField; }
                catch { impact.HealthMax = 0f; }

                Impacts[next] = impact;
                next = (next + 1) % Capacity;
            }
            catch
            {
                // Context capture is advisory; never break the damage call.
            }
        }

        internal static bool TryFind(Vector3 world, int frame, out Impact impact)
        {
            impact = default;
            if (!IsFinite(world)) return false;
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < Capacity; i++)
            {
                var candidate = Impacts[i];
                if (!candidate.Valid) continue;
                if (candidate.Frame < frame - FrameWindow || candidate.Frame > frame) continue;
                var distance = (candidate.Position - world).sqrMagnitude;
                if (distance > MatchDistance * MatchDistance) continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            if (best < 0) return false;
            impact = Impacts[best];
            return true;
        }

        internal static void Clear()
        {
            for (var i = 0; i < Capacity; i++) Impacts[i] = default;
            next = 0;
        }

        static bool IsFinite(Vector3 value)
            => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
