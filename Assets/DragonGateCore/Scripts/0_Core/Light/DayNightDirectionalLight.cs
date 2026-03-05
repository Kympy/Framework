using UnityEngine;
using LightType = UnityEngine.LightType;

namespace DragonGate
{
    [RequireComponent(typeof(Light))]
    public class DayNightDirectionalLight : CoreBehaviour
    {
        [SerializeField] private Gradient colorOverDay;
        [SerializeField] private AnimationCurve intensityOverDay;
        
        private Light _directionalLight;

        private void Awake()
        {
            _directionalLight = GetComponent<Light>();
            _directionalLight.type = LightType.Directional;
        }

        public void UpdateLight(float normalizedTime)
        {
            var color = colorOverDay.Evaluate(normalizedTime);
            var intensity = intensityOverDay.Evaluate(normalizedTime);
            _directionalLight.color = color;
            _directionalLight.intensity = intensity;
        }

        public void UpdateLight(GameTime gameTime)
        {
            var normalizedDayTime = gameTime.GetNormalizedDayTime();
            UpdateLight(normalizedDayTime);
        }
    }
}
