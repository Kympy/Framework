using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    internal static class PoolScopeColor
    {
        private static readonly List<Color> ColorPalette = new List<Color>()
        {
            new Color(1f,    0.7f,  0f),
            new Color(1f,    0.41f, 0.71f),
            new Color(0f,    0.5f,  0.5f),
            new Color(0.56f, 0f,    1f),
            new Color(0f,    0f,    0.8f),
            new Color(0.95f, 0.95f, 0.15f),
            new Color(0.75f, 1f,    0f),
            new Color(0.53f, 0.81f, 0.92f),
            new Color(1f,    0.5f,  0.31f),
            new Color(0.2f,  0.84f, 0f),
            new Color(0.5f,  0.64f, 0.8f),
            new Color(1f,    0.64f, 0.8f),
            new Color(0.5f,  0f,    0f),
            new Color(0.79f, 0.4f,  0.11f),
            new Color(1f,    0.84f, 0f),
        };

        private static int _nextIndex = 0;

        public static Color GetNextColor()
        {
            Color color = ColorPalette[_nextIndex % ColorPalette.Count];
            _nextIndex++;
            return color;
        }
    }
}
