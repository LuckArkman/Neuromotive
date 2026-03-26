using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema que transforma os resultados brutos da física em vetores normalizados [0.0 - 1.0].
    /// Prepara os dados de entrada (Input Vector) para a rede neural.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [UpdateAfter(typeof(SensorSystem))]
    [BurstCompile]
    public partial struct SensorNormalizationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new NormalizeSensorsJob().ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct NormalizeSensorsJob : IJobEntity
    {
        public void Execute(in SensorConfig config, DynamicBuffer<SensorResultElement> sensorResults, DynamicBuffer<NeuralInputBuffer> neuralInputs)
        {
            neuralInputs.Clear();
            float invRadius = 1.0f / math.max(0.1f, config.Radius);

            // Cada raio gera 2 inputs no vetor da rede (Distância e Tipo)
            // Exemplo simplificado: [Distância_Norm, Tipo_Norm]
            // Para total de 16 raios = 32 inputs.
            for (int i = 0; i < sensorResults.Length; i++)
            {
                var hit = sensorResults[i];
                
                // 1. Distância Normalizada (1.0 = colisão imediata, 0.0 = livre)
                float distNorm = 0;
                if (hit.Distance > 0)
                {
                    distNorm = math.clamp(1.0f - (hit.Distance * invRadius), 0.0f, 1.0f);
                }

                // 2. Tipo One-Hot / Categoria Simplificada
                // 0.0: Livre | 0.5: Agente | 1.0: Parede
                float typeNorm = 0;
                if (hit.HitType == 1) typeNorm = 1.0f; // Parede
                else if (hit.HitType == 2) typeNorm = 0.5f; // Outro Agente

                neuralInputs.Add(new NeuralInputBuffer { Value = distNorm });
                neuralInputs.Add(new NeuralInputBuffer { Value = typeNorm });
            }
        }
    }
}
