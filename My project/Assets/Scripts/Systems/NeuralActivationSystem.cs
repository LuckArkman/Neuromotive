using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Neuromotive.AI.Neural;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena as ativações brutas da última camada neural (Logits).
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct NeuralOutputBuffer : IBufferElementData
    {
        public float Value;
    }

    /// <summary>
    /// Sistema responsável por aplicar a ativação Tanh SIMD na saída motora da rede neural.
    /// Garante que os comandos de comando estejam no intervalo [-1, 1].
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [BurstCompile]
    public partial struct NeuralActivationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ApplyTanhActivationJob().ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que processa a saída da rede neural em paralelo para todos os agentes.
    /// Utiliza o motor de tensores otimizado via SIMD.
    /// </summary>
    [BurstCompile]
    public partial struct ApplyTanhActivationJob : IJobEntity
    {
        public void Execute(DynamicBuffer<NeuralOutputBuffer> outputs)
        {
            // Processa a saída motora (ex: 2 valores: linear e angular)
            // Se tivermos 4 ou mais saídas, usamos o loop SIMD
            int i = 0;
            for (; i <= outputs.Length - 4; i += 4)
            {
                float4 x = new float4(outputs[i].Value, outputs[i+1].Value, outputs[i+2].Value, outputs[i+3].Value);
                float4 activated = TensorMath.FastTanh(x);
                
                outputs[i] = new NeuralOutputBuffer { Value = activated.x };
                outputs[i+1] = new NeuralOutputBuffer { Value = activated.y };
                outputs[i+2] = new NeuralOutputBuffer { Value = activated.z };
                outputs[i+3] = new NeuralOutputBuffer { Value = activated.w };
            }

            // Fallback para o restante
            for (; i < outputs.Length; i++)
            {
                // Como não temos a versão escalar de Tanh ainda em NeuroMath, usamos math.tanh do kernel.
                // Mas aqui aplicaremos a nossa lógica SIMD sobre um float4 parcial se necessário.
                float4 x = new float4(outputs[i].Value, 0, 0, 0);
                outputs[i] = new NeuralOutputBuffer { Value = TensorMath.FastTanh(x).x };
            }
        }
    }
}
