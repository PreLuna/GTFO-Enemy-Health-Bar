using System;
using Enemies;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using UnityEngine;

namespace PreLuna.EnemyHealthBar.Runtime
{
    internal static class EnemyFocus
    {
        const int BufferSize = 16;
        static readonly Il2CppStructArray<RaycastHit> Hits = new(new RaycastHit[BufferSize]);

        internal static bool TryFind(out PlayerAgent local, out EnemyAgent target, out Camera camera, float maxDistance)
        {
            local = null;
            target = null;
            camera = null;
            try
            {
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return false;
                local = PlayerManager.GetLocalPlayerAgent();
                camera = CameraManager.GetCurrentCamera();
                if (local == null || camera == null || local.FPSCamera == null) return false;

                Vector3 origin = local.FPSCamera.CameraRayPos;
                Vector3 direction = local.FPSCamera.CameraRayDir;
                if (!Finite(origin) || origin.sqrMagnitude < .0001f) origin = local.FPSCamera.Position;
                if (!Finite(direction) || direction.sqrMagnitude < .0001f) direction = local.FPSCamera.Forward;
                if (!Finite(origin) || !Finite(direction) || direction.sqrMagnitude < .0001f) return false;
                direction = direction.normalized;

                if (TryRay(local, origin, direction, maxDistance, out target)) return true;

                // Third-person fallback: if the eye ray is blocked by the local body or
                // an eye-level prop, try the camera's crosshair ray.
                var viewRay = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
                return TryRay(local, viewRay.origin, viewRay.direction, maxDistance, out target);
            }
            catch
            {
                target = null;
                return false;
            }
        }

        static bool TryRay(PlayerAgent local, Vector3 origin, Vector3 direction, float maxDistance, out EnemyAgent target)
        {
            target = null;
            int mask = LayerManager.MASK_BULLETWEAPON_RAY | LayerManager.MASK_CAMERA_RAY
                | LayerManager.MASK_ENEMY | LayerManager.MASK_ENEMY_DAMAGABLE;
            int count = Physics.RaycastNonAlloc(origin, direction, Hits, maxDistance, mask, QueryTriggerInteraction.Ignore);
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit.collider == null) continue;
                try
                {
                    // Skip the local player's own colliders (third-person body/weapon).
                    if (hit.collider.transform.IsChildOf(local.transform)) continue;
                }
                catch { }
                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    best = i;
                }
            }
            if (best < 0) return false;
            var nearest = Hits[best];
            try { target = nearest.collider.GetComponentInParent<EnemyAgent>(); }
            catch { target = null; }
            if (target == null || target.Damage == null) return false;
            return target.Alive;
        }

        static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
