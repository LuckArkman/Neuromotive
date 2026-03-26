using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema de balanceamento de carga adaptativo (Throttle Automático).
    /// Monitora a saúde do frame rate e ajusta a frequência de decisão da IA em tempo real.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct GlobalAILoadBalancerSystem : ISystem
    {
        private float _targetFrameTime; // 16.6ms para 60 FPS

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _targetFrameTime = 0.0166f;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<NeuralMacroPulse>()) return;

            var pulsePulse = SystemAPI.GetSingleton<NeuralMacroPulse>();
            float currentDt = SystemAPI.Time.DeltaTime;

            // Se o frame rate cair (dt alto), aumentamos o intervalo entre pensamentos neurais
            // para aliviar a CPU e estabilizar a simulação.
            if (currentDt > _targetFrameTime * 1.5f) // Abaixo de 40 FPS
            {
                // Aumenta o intervalo (IA pensa menos frequentemente)
                pulsePulse.PulseInterval = math.min(0.2f, pulsePulse.PulseInterval + 0.01f);
            }
            else if (currentDt < _targetFrameTime) // Acima de 60 FPS (CPU Folgada)
            {
                // Diminui o intervalo (IA pensa mais perto do tempo real)
                pulsePulse.PulseInterval = math.max(0.0166f, pulsePulse.PulseInterval - 0.005f);
            }

            SystemAPI.SetSingleton(pulsePulse);
        }
    }
}
