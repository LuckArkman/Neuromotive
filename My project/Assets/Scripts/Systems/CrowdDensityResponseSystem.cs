using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena a densidade local percebida pelo agente.
    /// </summary>
    public struct AgentCrowdDensity : IComponentData
    {
        public float DensityValue; // 0 a 1 (1 = Congestionamento total)
        public float SpeedMultiplier;
    }

    /// <summary>
    /// Sistema que detecta o nível de congestionamento ao redor do NPC e ajusta
    /// automaticamente o comportamento motor (Lentidão em pedestres densos).
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateAfter(typeof(NeuralGroupCohesionSystem))]
    [BurstCompile]
    public partial struct CrowdDensityResponseSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float searchRadius = 3.0f;
            int maxSafeCount = 10; // Acima disso, o agente começa a frear

            var query = SystemAPI.QueryBuilder().WithAll<AgentTransform>().Build();
            var agentPositions = query.ToComponentDataArray<AgentTransform>(Allocator.TempJob);

            new CalculateLocalDensityJob
            {
                NeighborPositions = agentPositions,
                RadiusSq = searchRadius * searchRadius,
                MaxSafeCount = maxSafeCount
            }.ScheduleParallel();
            
            state.Dependency.Complete();
            agentPositions.Dispose();
        }
    }

    /// <summary>
    /// Job que quantifica a densidade e gera o multiplicador de velocidade.
    /// </summary>
    [BurstCompile]
    public partial struct CalculateLocalDensityJob : IJobEntity
    {
        [ReadOnly] public NativeArray<AgentTransform> NeighborPositions;
        public float RadiusSq;
        public int MaxSafeCount;

        public void Execute(
            ref AgentCrowdDensity densityData,
            ref AgentVelocity velocity,
            in AgentTransform transform)
        {
            int neighbors = 0;

            for (int i = 0; i < NeighborPositions.Length; i++)
            {
                if (math.distancesq(transform.Position, NeighborPositions[i].Position) < RadiusSq)
                {
                    neighbors++;
                }
            }

            // Normaliza a densidade baseado no limite de segurança
            float normalizedDensity = math.clamp((float)neighbors / MaxSafeCount, 0, 1);
            
            // Quanto mais denso, menor a velocidade (Fator de freada social)
            float speedMult = math.lerp(1.0f, 0.3f, normalizedDensity);

            densityData.DensityValue = normalizedDensity;
            densityData.SpeedMultiplier = speedMult;

            // Aplica o ajuste de velocidade em tempo real
            velocity.Linear *= speedMult;
        }
    }
}
