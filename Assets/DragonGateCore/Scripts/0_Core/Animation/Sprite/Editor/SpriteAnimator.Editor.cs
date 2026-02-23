#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    [CustomEditor(typeof(SpriteAnimator))]
    public class SpriteAnimatorEditor : UnityEditor.Editor
    {
        private SpriteAnimator _animator;
        private bool _isPreviewPlaying = false;
        private bool _subscribed = false;
        private double _lastTime;

        private void OnEnable()
        {
            if (target != null && target is SpriteAnimator animator)
            {
                _animator = animator;
                _lastTime = EditorApplication.timeSinceStartup;

                if (!_subscribed)
                {
                    EditorApplication.update += EditorTick;
                    _subscribed = true;
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("SpriteAnimatorEditor: Target is null or invalid.");
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space();
                if (_isPreviewPlaying == false)
                {
                    if (GUILayout.Button("▶ Preview Animation"))
                    {
                        _isPreviewPlaying = true;
                        _lastTime = EditorApplication.timeSinceStartup;
                        EditorApplication.update += EditorTick;
                    }
                }
                else
                {
                    if (GUILayout.Button("■ Stop Preview"))
                    {
                        StopPreview();
                    }
                }
            }
        }

        private void EditorTick()
        {
            if (target == null || _animator == null || _animator.gameObject == null || _animator.enabled == false || Application.isPlaying)
            {
                StopPreview();
                return;
            }
            
            if (_isPreviewPlaying == false) return;

            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - _lastTime);
            _lastTime = currentTime;

            _animator.EditorTick(deltaTime);
            SceneView.RepaintAll();
        }

        private void StopPreview()
        {
            _isPreviewPlaying = false;
            EditorApplication.update -= EditorTick;
            _subscribed = false;
        }

        private void OnDisable()
        {
            StopPreview();
        }
    }
}
#endif