using System;
using System.Collections.Generic;

namespace DragonGate
{
    public class UniTaskLock : IDisposable
    {
        // 중복 체크용
        private static Dictionary<int, object> _lockedObjects = new Dictionary<int, object>();

        public static bool IsLocked(object target)
        {
            return _lockedObjects.ContainsKey(target.GetHashCode());
        }

        private int _lockTarget;

        public UniTaskLock(object target)
        {
            _lockTarget = target.GetHashCode();
            if (_lockedObjects.TryAdd(target.GetHashCode(), target) == false)
            {
                throw new System.Exception($"{target} is already locked");
            }
        }

        public void Dispose()
        {
            _lockedObjects.Remove(_lockTarget);
        }
    }
}