using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public sealed class World : EngineObject
    {
        [SerializeField] private GameModeBase _gameMode;
        
        private HashSet<MonoBehaviour> _spawnedObjects = new HashSet<MonoBehaviour>();
        private Dictionary<Type, WorldSubsystem> _worldSubsystems = new();

        public void InitWorld()
        {
            SetWorldContext(this);
            _gameMode.InitMode();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DestroyWorldInternal();
        }

        public T GetWorldSubsystem<T>() where T : WorldSubsystem<T>
        {
            var result = _worldSubsystems.GetValueOrDefault(typeof(T), null);
            if (result == null)
            {
                var obj = new GameObject();
#if UNITY_EDITOR
                obj.name = $"WorldSubsystem_{typeof(T).Name}";
#endif
                result = obj.AddComponent<T>();
                result.InitSubsystem();
            }

            return result as T;
        }

        public T SpawnObject<T>(string key) where T : EngineObject
        {
            
            return null;
        }

        private void DestroyWorldInternal()
        {
            // 스폰 오브젝트
            foreach (var spawnedObject in _spawnedObjects)
            {
                if (spawnedObject != null)
                {
                    Destroy(spawnedObject.gameObject);
                }
            }
            _spawnedObjects.Clear();
            // 월드 서브 시스템
            foreach (var subsystem in _worldSubsystems.Values)
            {
                if (subsystem != null)
                {
                    Destroy(subsystem.gameObject);
                }
            }
            _worldSubsystems.Clear();
        }
    }
}