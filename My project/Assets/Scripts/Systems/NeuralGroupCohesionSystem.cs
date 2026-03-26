using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema encarregado de extrair as métricas de coesão de grupo para a rede neural.
    /// Calcula o centroide (Cohesion) e a velocidade média (Alignment) da vizinhança.
    /// Estas informações permitem o surgimento de comportamentos de bando (Flocking) neurais.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [UpdateAfter(typeof(NeuralTemporalEncodingSystem))]
    [BurstCompile]
    public partial struct NeuralGroupCohesionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Parâmetros de vizinhança para o bando
            float cohesionRadius = 5.0f;

            // Coleta todas as posições e velocidades para análise global de massa
            var query = SystemAPI.QueryBuilder().WithAll<AgentTransform, AgentVelocity>().Build();
            var allPositions = query.ToComponentDataArray<AgentTransform>(Allocator.TempJob);
            var allVelocities = query.ToComponentDataArray<AgentVelocity>(Allocator.TempJob);

            new ExtractGroupCohesionJob
            {
                AllPositions = allPositions,
                AllVelocities = allVelocities,
                RadiusSq = cohesionRadius * cohesionRadius
            }.ScheduleParallel();
            
            state.Dependency.Complete();
            allPositions.Dispose();
            allVelocities.Dispose();
        }
    }

    /// <summary>
    /// Job que extrai a média do grupo para cada NPC individualmente.
    /// </summary>
    [BurstCompile]
    public partial struct ExtractGroupCohesionJob : IJobEntity
    {
        [ReadOnly] public NativeArray<AgentTransform> AllPositions;
        [ReadOnly] public NativeArray<AgentVelocity> AllVelocities;
        public float RadiusSq;

        public void Execute(
            in AgentTransform transform,
            in AgentVelocity velocity,
            ref DynamicBuffer<NeuralInputBuffer> inputs)
        {
            float3 centroid = float3.zero;
            float3 avgVelocity = float3.zero;
            int count = 0;

            // Análise de proximidade social (O(N) simplificado por Job)
            for (int i = 0; i < AllPositions.Length; i++)
            {
                float distSq = math.distancesq(transform.Position, AllPositions[i].Position);
                if (distSq > 0.01f && distSq < RadiusSq)
                {
                    centroid += AllPositions[i].Position;
                    avgVelocity += AllVelocities[i].Linear;
                    count++;
                }
            }

            if (count > 0)
            {
                centroid /= count;
                avgVelocity /= count;
            }
            else
            {
                centroid = transform.Position;
                avgVelocity = velocity.Linear;
            }

            // Injeta o centroide relativo e o alinhamento de velocidade no cérebro neural
            float3 relativeCentroid = centroid - transform.Position;
            
            inputs.Add(new NeuralInputBuffer { Value = relativeCentroid.x });
            inputs.Add(new NeuralInputBuffer { Value = relativeCentroid.z });
            inputs.Add(new NeuralInputBuffer { Value = avgVelocity.x });
            inputs.Add(new NeuralInputBuffer { Value = avgVelocity.z });
        }
    }
}
