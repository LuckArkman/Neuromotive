using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena o histórico imediato de posição e velocidade.
    /// Usado para calcular diferenciais temporais (features dinâmicas).
    /// </summary>
    public struct TemporalHistoryComponent : IComponentData
    {
        public float3 PreviousPosition;
        public float3 PreviousVelocity;
        public float LastDeltaTime;
    }

    /// <summary>
    /// Sistema responsável por extrair variações temporais de movimento (velocidade, aceleração observada).
    /// Estas features são injetadas na LSTM para melhor predição de colisão.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [BurstCompile]
    public partial struct NeuralTemporalEncodingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            new ExtractTemporalFeaturesJob { DeltaTime = deltaTime }.ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que calcula a variação temporal de cada agente.
    /// </summary>
    [BurstCompile]
    public partial struct ExtractTemporalFeaturesJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            ref TemporalHistoryComponent history,
            in AgentTransform transform,
            in AgentVelocity velocity,
            ref DynamicBuffer<NeuralInputBuffer> inputs)
        {
            // Calcula variação de posição e real aceleração (DeltaV / DeltaT)
            float3 deltaPos = transform.Position - history.PreviousPosition;
            float3 acceleration = (velocity.Linear - history.PreviousVelocity) / (DeltaTime + 0.0001f);
            
            // Injeta como features temporais (Ex: Índices 60-63 do buffer de entrada)
            // Para a Sprint 34, apenas garantimos o cálculo e armazenamento no histórico
            history.PreviousPosition = transform.Position;
            history.PreviousVelocity = velocity.Linear;
            history.LastDeltaTime = DeltaTime;

            // Exemplo de injeção de feature dinâmica: Magnitude da variação de posição
            inputs.Add(new NeuralInputBuffer { Value = math.length(deltaPos) });
            inputs.Add(new NeuralInputBuffer { Value = math.length(acceleration) });
        }
    }
}
