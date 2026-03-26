using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena dados de proximidade imediata a obstáculos estáticos.
    /// Atua como um "Alerta de Colisão Iminente" para a rede neural.
    /// </summary>
    public struct StaticEncodingComponent : IComponentData
    {
        public float WallProximity; // 1.0 = tocando, 0.0 = longe
        public float3 NearestWallNormal;
    }

    /// <summary>
    /// Sistema que extrai e codifica a relação do agente com o cenário estático.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [UpdateAfter(typeof(SensorNormalizationSystem))]
    [BurstCompile]
    public partial struct StaticObstacleEncodingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new EncodeStaticObstaclesJob().ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct EncodeStaticObstaclesJob : IJobEntity
    {
        public void Execute(ref StaticEncodingComponent encoding, DynamicBuffer<SensorResultElement> sensorResults)
        {
            float maxProx = 0;
            
            // Analisa os resultados dos sensores para detectar a parede mais próxima
            for (int i = 0; i < sensorResults.Length; i++)
            {
                var hit = sensorResults[i];
                if (hit.HitType == 1) // Assumindo HitType 1 como estático/parede
                {
                    // Inversamente proporcional à distância
                    float prox = hit.Distance > 0 ? (1.0f / math.max(1.0f, hit.Distance)) : 0.5f;
                    if (prox > maxProx) maxProx = prox;
                }
            }

            encoding.WallProximity = math.clamp(maxProx, 0, 1);
        }
    }
}
