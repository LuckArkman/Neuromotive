using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena as recompensas acumuladas para o treinamento ML-Agents.
    /// Define a "motivação" do agente para aprender comportamentos eficientes.
    /// </summary>
    public struct AgentRewardData : IComponentData
    {
        public float CumulativeReward;
        public float PrevDistanceToTarget;
        public float3 PrevVelocity;
    }

    /// <summary>
    /// Sistema que calcula as funções de recompensa (Reward Functions) para navegação.
    /// Avalia a eficiência de tempo e a suavidade da trajetória.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct NeuralNavigationRewardSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new CalculateNavigationRewardsJob().ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que quantifica o progresso e penaliza oscilações bruscas (Jitter).
    /// </summary>
    [BurstCompile]
    public partial struct CalculateNavigationRewardsJob : IJobEntity
    {
        public void Execute(
            ref AgentRewardData reward,
            in AgentTransform transform,
            in AgentVelocity velocity,
            in AgentMacroTarget target)
        {
            float dist = math.distance(transform.Position, target.TargetPosition);

            // 1. Recompensa de Progresso (Shaping Reward)
            // Agente ganha pontos se estiver mais perto do alvo do que no frame anterior.
            float progressReward = (reward.PrevDistanceToTarget - dist) * 1.5f;

            // 2. Penalidade de Estagnação (Time Penalty)
            // Agente perde pontos por cada frame que passa sem chegar ao objetivo.
            float timePenalty = -0.01f;

            // 3. Penalidade de Suavidade (Trajectory Smoothness)
            // Grande aceleração angular ou variação de velocidade rápida gera penalidade.
            float jitterPenalty = math.length(velocity.Linear - reward.PrevVelocity) * -0.05f;

            // 4. Recompensa de Conclusão (Terminal Reward)
            float terminalReward = 0;
            if (dist < 1.0f) terminalReward = 10.0f;

            // Atualiza o acumulado para o coletor do ML-Agents
            reward.CumulativeReward += progressReward + timePenalty + jitterPenalty + terminalReward;
            
            // Persistência para o próximo frame
            reward.PrevDistanceToTarget = dist;
            reward.PrevVelocity = velocity.Linear;
        }
    }
}
