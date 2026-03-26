using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Neuromotive.AI.Neural;
using Neuromotive.AI.Components;
using Unity.Burst.CompilerServices;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema mestre que orquestra a inferência recorrente (LSTM) para todos os agentes.
    /// Converte a sequência temporal de estados oculados em comandos motores imediatos (Sequence-to-Value).
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [BurstCompile]
    public partial struct LSTMBrainSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new LSTMBrainInferenceJob();
            job.ScheduleParallel();
        }
    }

    /// <summary>
    /// Job paralelo que executa o ciclo completo da LSTM por agente.
    /// Otimizado com Intrínsecos Burst para vazão máxima.
    /// </summary>
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    public partial struct LSTMBrainInferenceJob : IJobEntity
    {
        public void Execute(
            Entity entity,
            [NoAlias] ref LSTMStateMetadata metadata,
            [NoAlias] DynamicBuffer<LSTMStateElement> states,
            [ReadOnly, NoAlias] in DynamicBuffer<NeuralInputBuffer> inputs,
            [NoAlias] ref DynamicBuffer<NeuralOutputBuffer> outputs)
        {
            if (inputs.Length == 0) return;

            for (int i = 0; i < metadata.StateSize; i++)
            {
                float h_prev = states[i].HiddenValue;
                float c_prev = states[i].CellValue;
                
                float4 netInput = new float4(inputs[0].Value, h_prev, 0, 0); 
                
                float4 f_t = TensorMath.CalculateForgetGate(netInput);
                float4 i_t = TensorMath.CalculateInputGate(netInput);
                float4 c_tilde = TensorMath.CalculateCandidateState(netInput);
                
                float4 c_t = TensorMath.UpdateLSTMCell(f_t, new float4(c_prev), i_t, c_tilde);
                float4 o_t = TensorMath.CalculateOutputGate(netInput);
                float4 h_t = TensorMath.UpdateLSTMHidden(o_t, c_t);
                
                states[i] = new LSTMStateElement { HiddenValue = h_t.x, CellValue = c_t.x };
            }

            for (int j = 0; j < outputs.Length; j++)
            {
                outputs[j] = new NeuralOutputBuffer { Value = states[j % states.Length].HiddenValue };
            }
        }
    }
}
