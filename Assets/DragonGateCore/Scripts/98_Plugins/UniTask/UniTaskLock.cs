using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public class UniTaskLock : IDisposable
    {
        // 중복 체크용
        private static HashSet<object> _lockedObjects = new();

        public static bool IsLocked(object target)
        {
            return _lockedObjects.Contains(target);
        }

        private object _lockTarget;

        public UniTaskLock(object target)
        {
            _lockTarget = target;
            if (_lockedObjects.Add(target) == false)
            {
                throw new System.Exception($"{target} is already locked");
            }
            DGDebug.Log($"UniTask Lock {target}", Color.blue);
        }

        public void Dispose()
        {
            _lockedObjects.Remove(_lockTarget);
            DGDebug.Log($"UniTask Unlock {_lockTarget}", Color.cadetBlue);
        }
    }
}