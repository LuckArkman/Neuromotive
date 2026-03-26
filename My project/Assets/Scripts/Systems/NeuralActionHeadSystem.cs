using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Neuromotive.AI.Components;
using Neuromotive.AI.Neural;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena a ação discreta decidida pela rede neural.
    /// Ex: 0 = Idle, 1 = Caminhar, 2 = Interagir, 3 = Alerta.
    /// </summary>

    /// <summary>
    /// Sistema responsável por decidir acoes discretas baseadas nas saídas neurais.
    /// Implementa a cabeça de decisão multinomial (Action Head).
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateAfter(typeof(LSTMBrainSystem))]
    [BurstCompile]
    public partial struct NeuralActionHeadSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DecipherActionHeadJob().ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct DecipherActionHeadJob : IJobEntity
    {
        public void Execute(
            ref AgentActionState actionState,
            in AgentActionMask actionMask,
            in DynamicBuffer<NeuralOutputBuffer> outputs)
        {
            if (outputs.Length < 6) return; 

            int bestAction = 0;
            float maxVal = -100f;

            for (int j = 0; j < 4; j++)
            {
                // Verifica na máscara se esta ação é permitida antes de considerar seu valor
                if (!actionMask.IsActionAllowed(j)) continue;

                float val = outputs[j + 2].Value;
                if (val > maxVal)
                {
                    maxVal = val;
                    bestAction = j;
                }
            }

            actionState.CurrentActionId = bestAction;
            actionState.Confidence = maxVal;
        }
    }
}
