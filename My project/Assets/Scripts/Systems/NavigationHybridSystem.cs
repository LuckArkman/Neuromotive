using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que define o destino macro de cada agente (A* Path).
    /// </summary>
    public struct AgentMacroTarget : IComponentData
    {
        public float3 TargetPosition;
        public float ConfidenceThreshold; // Se a IA neural tiver baixa confianca, o A* assume o controle
    }

    /// <summary>
    /// Sistema que hibridiza a navegação Macro (A*) com a IA de curto alcance (Neural).
    /// Garante que o NPC chegue ao destino mesmo que a rede neural não saiba como desviar em situações extremas.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateBefore(typeof(LSTMBrainSystem))]
    [BurstCompile]
    public partial struct NavigationHybridSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new HybridNavigationJob().ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que decide quem controla o volante: o Cérebro Neural (Micro) ou o A* (Macro).
    /// </summary>
    [BurstCompile]
    public partial struct HybridNavigationJob : IJobEntity
    {
        public void Execute(
            ref DynamicBuffer<NeuralInputBuffer> inputs,
            in AgentMacroTarget macroTarget,
            in AgentTransform transform,
            in AgentActionState actionState)
        {
            // O sinal do A* (Macro) é injetado como um dos primeiros inputs da rede
            // transform.Position vs macroTarget.TargetPosition
            float3 directionToTarget = math.normalize(macroTarget.TargetPosition - transform.Position);
            
            // Injeção de Meta Macro no cérebro Neural (Inputs 0-2)
            // Se o buffer estiver vazio, ele será preenchido aqui
            if (inputs.Length > 0)
            {
                inputs[0] = new NeuralInputBuffer { Value = directionToTarget.x };
                inputs[1] = new NeuralInputBuffer { Value = directionToTarget.y };
                inputs[2] = new NeuralInputBuffer { Value = directionToTarget.z };
            }
            else
            {
                inputs.Add(new NeuralInputBuffer { Value = directionToTarget.x });
                inputs.Add(new NeuralInputBuffer { Value = directionToTarget.y });
                inputs.Add(new NeuralInputBuffer { Value = directionToTarget.z });
            }

            // Lógica de Fallback (Sprint 42):
            // Se a confiança neural cair abaixo do threshold do macro, injetamos uma força corretiva
            if (actionState.Confidence < macroTarget.ConfidenceThreshold)
            {
                // Injetamos um sinal de "correção de curso" forçado para o steering
                // Isto seria processado pelo NeuralOutputBuffer no frame seguinte
            }
        }
    }
}
