using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema responsável por validar as condições físicas do agente e atualizar a máscara de ações.
    /// Ex: Desabilita 'Pular' se o agente não estiver no chão.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [UpdateBefore(typeof(LSTMBrainSystem))]
    [BurstCompile]
    public partial struct PhysicalActionValidationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new UpdatePhysicalActionMaskJob().ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que verifica o estado físico e restringe as comunicações com a rede neural.
    /// </summary>
    [BurstCompile]
    public partial struct UpdatePhysicalActionMaskJob : IJobEntity
    {
        public void Execute(
            ref AgentActionMask actionMask,
            in AgentTransform transform)
        {
            // Verificação simples de "No Chão" (Grounded) para a Sprint 39
            // Se Y estiver muito alto (ex: caiu de uma plataforma), desabilitamos ações de interação terrestre
            bool isGrounded = transform.Position.y < 0.1f; 

            // Habilita/Desabilita ações específicas (IDs simulados)
            // Ação 0: Idle (Sempre permitida)
            // Ação 1: Caminhar (Só permitida se no chão)
            // Ação 2: Pular (Só permitida se no chão)
            // Ação 3: Interagir (Só permitida se no chão)
            
            actionMask.SetActionAllowed(0, true);
            actionMask.SetActionAllowed(1, isGrounded);
            actionMask.SetActionAllowed(2, isGrounded);
            actionMask.SetActionAllowed(3, isGrounded);
        }
    }
}
