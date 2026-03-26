using Unity.Burst;
using Unity.Entities;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema responsável por garantir a integridade dos buffers recorrentes (LSTM).
    /// Gerencia a inicialização do estado de memória quando novos agentes surgem ou mudam de Chunk.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateBefore(typeof(LSTMBrainSystem))]
    [BurstCompile]
    public partial struct LSTMStatePersistenceSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Inicializa memórias vazias para novos agentes
            new InitializeLSTMStateJob().ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que garante que cada agente possua a estrutura de memória correta (H-State/C-State) limpa.
    /// </summary>
    [BurstCompile]
    public partial struct InitializeLSTMStateJob : IJobEntity
    {
        public void Execute(ref LSTMStateMetadata metadata, DynamicBuffer<LSTMStateElement> states)
        {
            if (!metadata.IsInitialized)
            {
                states.Clear();
                // Aloca o buffer para o tamanho da rede neural configurada
                for (int i = 0; i < metadata.StateSize; i++)
                {
                    states.Add(new LSTMStateElement { HiddenValue = 0, CellValue = 0 });
                }
                metadata.IsInitialized = true;
            }
        }
    }
}
