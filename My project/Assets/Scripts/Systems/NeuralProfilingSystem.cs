using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente singleton que contém os dados de diagnóstico de performance do projeto.
    /// Monitora a "nanometria" de processamento da rede neural.
    /// </summary>
    public struct NeuralProfilingStats : IComponentData
    {
        public double AverageInferenceNs; // Nanosegundos por agente
        public double TotalFrameMs;
        public int ActiveAgentCount;
    }

    /// <summary>
    /// Sistema de monitoramento de performance em tempo real.
    /// Integrado com o Unity Profiler para visualização de vazão de dados.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct NeuralProfilingSystem : ISystem
    {
        private static readonly ProfilerMarker BrainInferenceMarker = new ProfilerMarker("Neuromotive.InferenceCycle");

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton<NeuralProfilingStats>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Inicia o marcador de perfil para visibilidade no Unity Profiler (CPU)
            using (BrainInferenceMarker.Auto())
            {
                var query = SystemAPI.QueryBuilder().WithAll<LSTMStateMetadata>().Build();
                int agentCount = query.CalculateEntityCount();

                if (SystemAPI.HasSingleton<NeuralProfilingStats>())
                {
                    var stats = SystemAPI.GetSingleton<NeuralProfilingStats>();
                    stats.ActiveAgentCount = agentCount;
                    
                    // Simulação de cálculo de nanometria baseada na carga de trabalho
                    // O tempo real viria do mapeamento de JobHandles (JobHandle.Complete)
                    stats.TotalFrameMs = SystemAPI.Time.DeltaTime * 1000.0f;
                    stats.AverageInferenceNs = (stats.TotalFrameMs * 1_000_000.0) / math.max(1, agentCount);

                    SystemAPI.SetSingleton(stats);
                }
            }
        }
    }
}
