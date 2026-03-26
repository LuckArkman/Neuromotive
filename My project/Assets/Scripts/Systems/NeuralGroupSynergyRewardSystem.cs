using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena dados de grupo para o cálculo de recompensas sinérgicas.
    /// Vital para o algoritmo MA-POCA (Multi-Agent Posthumous Credit Assignment).
    /// </summary>
    public struct GroupSynergyData : IComponentData
    {
        public int GroupId;
        public float GroupSuccessFactor;
        public float AvgGroupVelocity;
    }

    /// <summary>
    /// Sistema que calcula as recompensas de sinergia de grupo.
    /// Premia o alinhamento e a coesão coletiva, incentivando o flocking neural.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderLast = true)]
    [UpdateAfter(typeof(NeuralNavigationRewardSystem))]
    [BurstCompile]
    public partial struct NeuralGroupSynergyRewardSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Coleta métricas globais para cálculo de média de grupo
            var query = SystemAPI.QueryBuilder().WithAll<AgentVelocity, GroupSynergyData>().Build();
            var allVelocities = query.ToComponentDataArray<AgentVelocity>(Allocator.TempJob);

            new CalculateGroupSynergyRewardsJob
            {
                AllVelocities = allVelocities
            }.ScheduleParallel();
            
            state.Dependency.Complete();
            allVelocities.Dispose();
        }
    }

    /// <summary>
    /// Job que quantifica a sinergia entre o indivíduo e a massa do grupo.
    /// </summary>
    [BurstCompile]
    public partial struct CalculateGroupSynergyRewardsJob : IJobEntity
    {
        [ReadOnly] public NativeArray<AgentVelocity> AllVelocities;

        public void Execute(
            ref AgentRewardData reward,
            in AgentVelocity velocity,
            in GroupSynergyData group)
        {
            // 1. Recompensa de Alinhamento (Alignment Reward)
            // O agente ganha pontos se estiver movendo-se na mesma direção do bando.
            float3 avgVel = float3.zero;
            if (AllVelocities.Length > 0)
            {
                for (int i = 0; i < AllVelocities.Length; i++) avgVel += AllVelocities[i].Linear;
                avgVel /= AllVelocities.Length;
            }

            float alignmentBonus = math.dot(math.normalizesafe(velocity.Linear), math.normalizesafe(avgVel)) * 0.1f;

            // 2. Penalidade de Colisão Iminente (Crowd Pressure)
            // Se o agente estiver desviando bruscamente do grupo sem necessidade, ganha penalidade.
            float synergyBonus = (alignmentBonus > 0.05f) ? 0.05f : -0.01f;

            // Aplica a recompensa de sinergia coletiva
            reward.CumulativeReward += synergyBonus;
        }
    }
}
