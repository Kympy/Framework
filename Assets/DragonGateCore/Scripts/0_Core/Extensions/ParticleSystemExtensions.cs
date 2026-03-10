using UnityEngine;

namespace DragonGate
{
    public static class ParticleSystemExtensions
    {
        public static void SetSimulationSpeed(this ParticleSystem particleSystem, float value)
        {
            var main = particleSystem.main;
            main.simulationSpeed = value;
        }

        public static void SetStopAction(this ParticleSystem particleSystem, ParticleSystemStopAction stopAction)
        {
            var main = particleSystem.main;
            main.stopAction = stopAction;
        }
    }
}
