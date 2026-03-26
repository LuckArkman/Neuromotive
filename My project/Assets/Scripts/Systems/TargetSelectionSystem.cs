using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Neuromotive.AI.Components;
using Neuromotive.AI.Math;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema responsável por gerenciar e atualizar os objetivos dos agentes.
    /// Define critérios de prioridade e modo "Wander" caso nenhum alvo seja encontrado.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [UpdateAfter(typeof(SpatialGridSystem))]
    [BurstCompile]
    public partial struct TargetSelectionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var randomSource = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            
            new UpdateAgentTargetJob
            {
                Time = (float)SystemAPI.Time.ElapsedTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct UpdateAgentTargetJob : IJobEntity
    {
        public float Time;

        public void Execute(ref AgentTarget target, in AgentTransform transform, in AgentComponent agentInfo)
        {
            // Se já alcançou o alvo ou não tem um, define novo objetivo (Lógica Wander simplificada)
            float distSq = NeuroMath.DistanceSq(transform.Position, target.Position);
            
            if (target.IsReached || distSq < (target.StoppingDistance * target.StoppingDistance))
            {
                target.IsReached = true;
                
                // Simulação: Pick um ponto aleatório em um raio de 50m para teste da IA
                // Usando o AgentID como seed para determinismo
                var rnd = Unity.Mathematics.Random.CreateFromIndex((uint)(agentInfo.AgentID + (int)(Time * 10)));
                float2 randDir = rnd.NextFloat2Direction();
                target.Position = transform.Position + new float3(randDir.x, 0, randDir.y) * rnd.NextFloat(10f, 50f);
                target.IsReached = false;
            }
        }
    }
}
