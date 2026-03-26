using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;
using Neuromotive.AI.Systems;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema responsável por converter as saídas da rede neural (0-1 ou -1 a 1)
    /// em vetores de velocidade física real (Steering Head).
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateAfter(typeof(LSTMBrainSystem))]
    [BurstCompile]
    public partial struct NeuralMovementHeadSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Parâmetros de calibração física (Performance-tuned)
            float maxSpeed = 5.0f;
            float maxTurnSpeed = 10.0f;

            new ApplyMovementHeadJob 
            { 
                MaxSpeed = maxSpeed, 
                MaxTurnSpeed = maxTurnSpeed 
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct ApplyMovementHeadJob : IJobEntity
    {
        public float MaxSpeed;
        public float MaxTurnSpeed;

        public void Execute(
            ref AgentVelocity velocity,
            in DynamicBuffer<NeuralOutputBuffer> outputs)
        {
            if (outputs.Length < 2) return;

            // outputs[0] -> Desejo Linear (Frente/Trás)
            // outputs[1] -> Desejo Angular (Giro Esquerda/Direita)
            
            float linearInput = outputs[0].Value;
            float angularInput = outputs[1].Value;

            // O evitamento preditivo neural é garantido pela rede que recebeu o radar no input
            // Aqui aplicamos o sinal já processado e suavizado pela LSTM
            velocity.Linear = new float3(0, 0, linearInput * MaxSpeed);
            velocity.Angular = new float3(0, angularInput * MaxTurnSpeed, 0);
        }
    }
}
