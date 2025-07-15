using System;
using System.Collections.Generic;

namespace Framework
{
    public static class UIAsset
    {
        private static Dictionary<Type, string> _uiAssets = new Dictionary<Type, string>()
        {

        };

        public static string GetKey<T>()
        {
            return _uiAssets[typeof(T)];
        }
    }
}
