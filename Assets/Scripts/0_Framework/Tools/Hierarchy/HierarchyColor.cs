using UnityEngine;

namespace Framework
{
    public class HierarchyColor : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private Color backgroundColor = Color.clear;
        [SerializeField] private bool AutoTextColor = false;
        [SerializeField] private Color textColor = Color.white;
        
        private bool previousAutoText = true;
        private Color previousManualTextColor = Color.white;
        
        public Color BackgroundColor
        {
            get
            {
                backgroundColor.a = 1;
                return backgroundColor;
            }
        }

        public Color TextColor
        {
            get
            {
                if (AutoTextColor)
                {
                    return new Color(1f - backgroundColor.r, 1f - backgroundColor.g, 1f - backgroundColor.b);
                }
                textColor.a = 1;
                return textColor;
            }
        }
        
        private void OnValidate()
        {
            if (previousAutoText != AutoTextColor)
            {
                if (AutoTextColor)
                {
                    previousManualTextColor = textColor;
                    textColor = GetAutoTextColor();
                }
                else
                {
                    textColor = previousManualTextColor;
                }

                previousAutoText = AutoTextColor;
            }
        }

        private Color GetAutoTextColor()
        {
            return new Color(1f - backgroundColor.r, 1f - backgroundColor.g, 1f - backgroundColor.b);
        }
#endif
    }
}
