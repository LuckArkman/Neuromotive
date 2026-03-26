using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema que aplica forças de repulsão social (Social Force Model) para evitar 
    /// que pedestres fiquem muito próximos uns dos outros, criando uma "bolha de conforto".
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateAfter(typeof(NeuralMovementHeadSystem))]
    [BurstCompile]
    public partial struct SocialForceRepulsionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float socialDistance = 1.0f; // Raio da bolha social
            float repulsionStrength = 2.0f;

            // Busca todos os agentes para cálculo de vizinhança
            var agentQuery = SystemAPI.QueryBuilder().WithAll<AgentTransform, AgentVelocity>().Build();
            var agentPositions = agentQuery.ToComponentDataArray<AgentTransform>(Allocator.TempJob);

            new ApplySocialRepulsionJob
            {
                NeighborPositions = agentPositions,
                SocialDistance = socialDistance,
                RepulsionStrength = repulsionStrength
            }.ScheduleParallel();
            
            state.Dependency.Complete();
            agentPositions.Dispose();
        }
    }

    /// <summary>
    /// Job que calcula forças de repulsão local entre as entidades de multidão.
    /// </summary>
    [BurstCompile]
    public partial struct ApplySocialRepulsionJob : IJobEntity
    {
        [ReadOnly] public NativeArray<AgentTransform> NeighborPositions;
        public float SocialDistance;
        public float RepulsionStrength;

        public void Execute(
            ref AgentVelocity velocity,
            in AgentTransform transform)
        {
            float3 totalRepulsion = float3.zero;

            // Loop simples de vizinhança (Otimizado para milhares de agentes via SIMD)
            for (int i = 0; i < NeighborPositions.Length; i++)
            {
                float3 diff = transform.Position - NeighborPositions[i].Position;
                float distSq = math.lengthsq(diff);
                
                // Se estiver dentro da bolha (e não for eu mesmo)
                if (distSq > 0.01f && distSq < SocialDistance * SocialDistance)
                {
                    float dist = math.sqrt(distSq);
                    totalRepulsion += (diff / dist) * (SocialDistance - dist) * RepulsionStrength;
                }
            }

            // Blending: A força social ajuda o steering neural a manter o espaçamento
            velocity.Linear += totalRepulsion;
        }
    }
}
