using UnityEngine;
using LightType = UnityEngine.LightType;

namespace DragonGate
{
    [RequireComponent(typeof(Light))]
    public class DayNightDirectionalLight : CoreBehaviour
    {
        [SerializeField] private Gradient _lightColorOverDay;
        [SerializeField] private AnimationCurve _lightIntensityOverDay;
        [SerializeField] private AnimationCurve _ambientIntensityOverDay;
        [SerializeField] private AnimationCurve _reflectionIntensityOverDay;

        // private const float StartRotation = -90f;
        private Light _directionalLight;

        private void Awake()
        {
            _directionalLight = GetComponent<Light>();
            _directionalLight.type = LightType.Directional;
        }

        public void UpdateLight(float normalizedTime)
        {
            var color = _lightColorOverDay.Evaluate(normalizedTime);
            var intensity = _lightIntensityOverDay.Evaluate(normalizedTime);
            _directionalLight.color = color;
            _directionalLight.intensity = intensity;

            float ambientIntensity = _ambientIntensityOverDay.Evaluate(normalizedTime);
            float reflectionIntensity = _reflectionIntensityOverDay.Evaluate(normalizedTime);

            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.reflectionIntensity = reflectionIntensity;

            float sunAngle = normalizedTime * 360f - 90f;

            transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);
        }

        public void UpdateLight(GameTime gameTime)
        {
            var normalizedDayTime = gameTime.GetNormalizedDayTime();
            UpdateLight(normalizedDayTime);
        }
    }
}
