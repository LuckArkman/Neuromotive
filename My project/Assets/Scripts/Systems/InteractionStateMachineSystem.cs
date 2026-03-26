using Unity.Burst;
using Unity.Entities;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena os dados de progresso e estado da interação procedural.
    /// </summary>
    public struct InteractionState : IComponentData
    {
        public float Timer;
        public float Duration;
        public bool IsActive;
        public int InteractionType; // 0 = Nenhum, 1 = Falar, 2 = Comprar, etc.
    }

    /// <summary>
    /// Sistema responsável por gerenciar o ciclo de vida das ações procedurais.
    /// Atua como uma máquina de estados que executa as decisões tomadas pela IA Neural.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateAfter(typeof(NeuralActionHeadSystem))]
    [BurstCompile]
    public partial struct InteractionStateMachineSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            new UpdateInteractionStateJob { DeltaTime = deltaTime }.ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que processa o tempo e transições das interações de cada agente.
    /// </summary>
    [BurstCompile]
    public partial struct UpdateInteractionStateJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            ref InteractionState state,
            ref AgentActionState actionIntent,
            ref AgentActionMask actionMask)
        {
            // Se a IA Neural decidiu "Interagir" (ActionId 2) e não estamos ativos ainda
            if (actionIntent.CurrentActionId == 2 && !state.IsActive)
            {
                state.IsActive = true;
                state.Timer = 0;
                state.Duration = 3.0f; // Ex: Interação dura 3 segundos
            }

            if (state.IsActive)
            {
                // Enquanto interage, bloqueamos outras ações (ex: Caminhar bit 1)
                actionMask.SetActionAllowed(1, false);
                
                state.Timer += DeltaTime;
                
                // Finaliza a interação ao atingir o tempo
                if (state.Timer >= state.Duration)
                {
                    state.IsActive = false;
                    state.Timer = 0;
                    actionMask.SetActionAllowed(1, true); // Reabilita caminhada
                    
                    // Forçamos a rede a mudar de ideia (resetando a intenção)
                    actionIntent.CurrentActionId = 0; // Volta para Idle
                }
            }
        }
    }
}
