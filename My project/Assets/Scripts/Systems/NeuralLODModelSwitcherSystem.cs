using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;
using Neuromotive.AI.Neural;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema que decide qual modelo de inferência usar (LOD Neural).
    /// Agentes próximos usam a LSTM complexa (recorrente), enquanto
    /// agentes distantes usam uma MLP simplificada (reativa) para economizar CPU.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateAfter(typeof(CognitiveThrottlingSystem))]
    [BurstCompile]
    public partial struct NeuralLODModelSwitcherSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // O sistema de model swapping é acionado pelo LOD já calculado na Sprint 48
            new NeuralLODModelSwitcherJob().ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que executa a inferência simplificada (MLP) para agentes em LOD baixo.
    /// </summary>
    [BurstCompile]
    public partial struct NeuralLODModelSwitcherJob : IJobEntity
    {
        public void Execute(
            in CognitiveLOD lod,
            ref DynamicBuffer<NeuralOutputBuffer> outputs,
            in DynamicBuffer<NeuralInputBuffer> inputs)
        {
            // Se o agente estiver em LOD 2 (Muito longe), executamos uma lógica reativa simples
            // ignorando o loop recorrente pesado da LSTM.
            if (lod.ThrottleScale >= 4)
            {
                if (inputs.Length == 0) return;

                // Inferência "Linear" simplificada (MLP 1-layer mock)
                // Apenas mapeia o input frontal diretamente para o steering
                float forwardIntention = inputs[0].Value;
                
                // Sobrescreve as saídas neurais com a ação de "Siga em frente" (reativo)
                if (outputs.Length >= 2)
                {
                    outputs[0] = new NeuralOutputBuffer { Value = forwardIntention }; // Forward
                    outputs[1] = new NeuralOutputBuffer { Value = 0 }; // No Turn
                }
            }
        }
    }
}
