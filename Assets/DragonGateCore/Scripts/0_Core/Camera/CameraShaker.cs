#if DOTWEEN
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public class CameraShaker : Singleton<CameraShaker>
    {
        // Registered cameras and per-camera state
        private readonly Dictionary<int, Camera> _cameras = new Dictionary<int, Camera>();
        private readonly Dictionary<int, Tween> _activeTweens = new Dictionary<int, Tween>();

        /// <summary>
        /// Register a camera to be shake-controlled. Safe to call multiple times.
        /// Caches its current localPosition as the rest/default position.
        /// </summary>
        public void AddCamera(Camera camera)
        {
            if (camera == null) return;
            int key = camera.gameObject.GetInstanceID();
            if (_cameras.ContainsKey(key)) return;

            _cameras.Add(key, camera);
        }

        /// <summary>
        /// Unregister a camera and kill any active tween on it, restoring its default local position.
        /// </summary>
        public void RemoveCamera(Camera camera)
        {
            if (camera == null) return;
            int key = camera.gameObject.GetInstanceID();
            if (_activeTweens.TryGetValue(key, out var tw) && tw.IsActive())
            {
                tw.Kill(true);
            }

            _activeTweens.Remove(key);
            _cameras.Remove(key);
        }

        // public void ShakeMain(float duration, Vector3 strength, bool ignoreTimeScale = false)
        // {
        //     var mainCamera = GamePlayStatics.GetGameMode().MainCamera;
        //     if (mainCamera == null)
        //     {
        //         Shake(Camera.main, duration, strength, ignoreTimeScale);
        //     }
        //     else
        //     {
        //         Shake(mainCamera, duration, strength, ignoreTimeScale);
        //     }
        // }
        //
        // public void ShakeMain(float duration, float strength, bool ignoreTimeScale = false)
        // {
        //     var mainCamera = GamePlayStatics.GetGameMode().MainCamera;
        //     if (mainCamera == null)
        //     {
        //         Shake(Camera.main, duration, strength, ignoreTimeScale);
        //     }
        //     else
        //     {
        //         Shake(mainCamera, duration, strength, ignoreTimeScale);
        //     }
        // }

        /// <summary>
        /// Shake a specific camera.
        /// </summary>
        /// <param name="camera">Camera to shake. Will be auto-registered if not already.</param>
        /// <param name="duration">Shake duration in seconds.</param>
        /// <param name="strength">Per-axis strength (x,y,z).</param>
        /// <param name="ignoreTimeScale">If true, uses unscaled time.</param>
        public Tween Shake(Camera camera, float duration, Vector3 strength, bool ignoreTimeScale = false)
        {
            if (camera == null) return null;
            // if (GameOptionManager.Values.CameraShake == false) return;
            int key = camera.gameObject.GetInstanceID();
            if (!_cameras.ContainsKey(key))
            {
                AddCamera(camera);
            }

            // Kill previous tween (if any) and reset to rest position before starting a new shake
            if (_activeTweens.TryGetValue(key, out var prev) && prev.IsActive())
            {
                prev.Kill(true);
            }

            var t = camera.transform;

            var tween = t.DOShakePosition(duration, strength)
                .SetUpdate(ignoreTimeScale);

            _activeTweens[key] = tween;
            return tween;
        }

        /// <summary>
        /// Shake a specific camera with uniform strength.
        /// </summary>
        public void Shake(Camera camera, float duration, float strength, bool ignoreTimeScale = false)
            => Shake(camera, duration, new Vector3(strength, strength, 0f), ignoreTimeScale);

        /// <summary>
        /// Shake all registered cameras.
        /// </summary>
        public void ShakeAll(float duration, Vector3 strength, bool ignoreTimeScale = false)
        {
            foreach (var kv in _cameras)
            {
                Shake(kv.Value, duration, strength, ignoreTimeScale);
            }
        }

        public void ShakeAll(float duration, float strength, bool ignoreTimeScale = false)
            => ShakeAll(duration, new Vector3(strength, strength, 0f), ignoreTimeScale);

        /// <summary>
        /// Immediately reset a camera to its cached rest local position and kill any active tween.
        /// </summary>
        public void Reset(Camera camera)
        {
            if (camera == null) return;
            int key = camera.gameObject.GetInstanceID();
            if (_activeTweens.TryGetValue(key, out var tw) && tw.IsActive())
            {
                tw.Kill(true);
            }
        }

        /// <summary>
        /// Reset and clear all registered cameras/tweens.
        /// </summary>
        public void ClearAll()
        {
            foreach (var kv in _cameras)
            {
                Reset(kv.Value);
            }
            _activeTweens.Clear();
            _cameras.Clear();
        }
    }
}
#endif