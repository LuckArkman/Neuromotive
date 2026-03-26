using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema de monitoramento de integridade e estabilidade (Integrity Monitor).
    /// Detecta e corrige anomalias físicas ou neurais durante simulações de longa duração.
    /// Vital para manter multidões massivas estáveis por horas de jogo.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct ProjectIntegrityMonitorSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // O monitoramento de integridade garante que nenhum agente entre em estado "corrompido" (NaN)
            new SanitizeAgentPhysicalStateJob().ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que limpa e valida os vetores de velocidade e posição da multidão.
    /// </summary>
    [BurstCompile]
    public partial struct SanitizeAgentPhysicalStateJob : IJobEntity
    {
        public void Execute(ref AgentVelocity velocity, ref AgentTransform transform)
        {
            // 1. Proteção contra NaN (Not a Number)
            // Se algum cálculo neural falhar, impedimos que o agente "desapareça" do mapa.
            if (math.any(math.isnan(velocity.Linear))) velocity.Linear = float3.zero;
            if (math.any(math.isnan(transform.Position))) transform.Position = float3.zero;

            // 2. Clamp de Velocidade de Segurança
            // Impede que agentes atinjam velocidades supersônicas por erro de precisão float.
            float speed = math.length(velocity.Linear);
            if (speed > 10.0f) velocity.Linear = math.normalizesafe(velocity.Linear) * 10.0f;
            
            // 3. Keep-On-Floor (Garante que a simulação permaneça no plano Y=0 se necessário)
            transform.Position.y = 0;
        }
    }
}
