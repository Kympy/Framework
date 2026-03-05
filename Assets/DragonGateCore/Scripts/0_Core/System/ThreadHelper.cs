using System;
using UnityEngine;

namespace DragonGate
{
    public static class ThreadHelper
    {
        private static int _mainThreadId;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public static void EnsureMainThread()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                throw new InvalidOperationException("Must be called from main thread.");
            }
        }
    }
}
