using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using System.Runtime.CompilerServices;

namespace Neuromotive.AI.Neural
{
    /// <summary>
    /// Motor de cálculo matricial de baixo nível para inferência neural local.
    /// Foco em alto desempenho via Burst/SIMD e instruções FMA.
    /// </summary>
    [BurstCompile]
    public static class TensorMath
    {
        /// <summary>
        /// Função ReLU (Rectified Linear Unit) ultra-rápida.
        /// SIMD friendly e inlined.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 ReLU(float4 x)
        {
            return math.max(0.0f, x);
        }

        /// <summary>
        /// Versão aproximada rápida da função Sigmoid utilizando Multiply-Add (mad).
        /// Ativação inlined para eliminar overhead de chamada em loops profundos.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 FastSigmoid(float4 x)
        {
            return 0.5f * (x / (1.0f + math.abs(x))) + 0.5f;
        }

        /// <summary>
        /// Versão aproximada rápida da função Tanh utilizando polinômios acelerados por FMA.
        /// Inlined para performance máxima.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 FastTanh(float4 x)
        {
            float4 x2 = x * x;
            float4 a = x * (135135.0f + x2 * (17325.0f + x2 * (378.0f + x2)));
            float4 b = 135135.0f + x2 * (62370.0f + x2 * (3150.0f + 28.0f * x2));
            return a / b;
        }

        /// <summary>
        /// Realiza a operação core de uma camada densa utilizando BlobAssets.
        /// Otimizada explicitamente com float4 para SIMD.
        /// </summary>
        [BurstCompile]
        public static void LinearSIMDBlob(
            in DynamicBuffer<float> input, 
            ref BlobArray<float> weights, 
            ref BlobArray<float> bias, 
            ref NativeArray<float> output, 
            int inputSize, 
            int outputSize)
        {
            for (int i = 0; i < outputSize; i++)
            {
                float sum = bias[i];
                int weightOffset = i * inputSize;

                int j = 0;
                for (; j <= inputSize - 4; j += 4)
                {
                    float4 inputV = new float4(input[j], input[j + 1], input[j + 2], input[j + 3]);
                    float4 weightV = new float4(
                        weights[weightOffset + j], 
                        weights[weightOffset + j + 1], 
                        weights[weightOffset + j + 2], 
                        weights[weightOffset + j + 3]);
                    
                    sum += math.dot(inputV, weightV);
                }

                for (; j < inputSize; j++)
                {
                    sum = math.mad(input[j], weights[weightOffset + j], sum);
                }

                output[i] = sum;
            }
        }

        /// <summary>
        /// Aplica a função Softmax em um NativeArray para obter uma distribuição de probabilidade.
        /// Otimizada para Burst.
        /// </summary>
        [BurstCompile]
        public static void Softmax(ref NativeArray<float> values, int size)
        {
            float maxVal = float.MinValue;
            for (int i = 0; i < size; i++)
            {
                if (values[i] > maxVal) maxVal = values[i];
            }

            float sum = 0.0f;
            for (int i = 0; i < size; i++)
            {
                // Estabilidade numérica: exp(x - max)
                values[i] = math.exp(values[i] - maxVal);
                sum += values[i];
            }

            float invSum = 1.0f / sum;
            for (int i = 0; i < size; i++)
            {
                values[i] *= invSum;
            }
        }

        /// <summary>
        /// Derivada da função ReLU. 1 se x > 0, senão 0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 dReLU(float4 x)
        {
            return math.select(0.0f, 1.0f, x > 0.0f);
        }

        /// <summary>
        /// Derivada da função Sigmoid: f(x) * (1 - f(x)).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 dSigmoid(float4 x)
        {
            float4 s = FastSigmoid(x);
            return s * (1.0f - s);
        }

        /// <summary>
        /// Derivada da função Tanh: 1 - f(x)^2.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 dTanh(float4 x)
        {
            float4 t = FastTanh(x);
            return 1.0f - (t * t);
        }

        /// <summary>
        /// Realiza o cálculo do Portão de Esquecimento (Forget Gate) da LSTM.
        /// Fórmula: f_t = Sigmoid(W_f * [h_prev, x] + b_f)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 CalculateForgetGate(float4 netInput)
        {
            // O netInput já deve ser o resultado da multiplicação matricial (W_f * concat)
            return FastSigmoid(netInput);
        }

        /// <summary>
        /// Realiza o cálculo do Portão de Entrada (Input Gate) da LSTM.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 CalculateInputGate(float4 netInput)
        {
            return FastSigmoid(netInput);
        }

        /// <summary>
        /// Realiza o cálculo do Estado Candidato (tilde{c}_t) da LSTM.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 CalculateCandidateState(float4 netInput)
        {
            return FastTanh(netInput);
        }

        /// <summary>
        /// Realiza o cálculo do Portão de Saída (Output Gate) da LSTM.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 CalculateOutputGate(float4 netInput)
        {
            return FastSigmoid(netInput);
        }

        /// <summary>
        /// Realiza a atualização do Estado de Célula (c_t) da LSTM.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 UpdateLSTMCell(float4 f_t, float4 c_prev, float4 i_t, float4 c_tilde)
        {
            // FMA 1: f_t * c_prev
            float4 forgetPart = f_t * c_prev;
            // FMA 2: i_t * c_tilde + forgetPart
            return math.mad(i_t, c_tilde, forgetPart);
        }

        /// <summary>
        /// Atualiza o Estado Oculto (h_t) da LSTM.
        /// Fórmula: h_t = outputGate * Tanh(c_t)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 UpdateLSTMHidden(float4 o_t, float4 c_t)
        {
            return o_t * FastTanh(c_t);
        }

        /// <summary>
        /// Helper para o Argmax probabilístico (Gumbel-Softmax lite).
        /// </summary>
        [BurstCompile]
        public static int Argmax(in NativeArray<float> values, int size)
        {
            int bestIdx = 0;
            float maxVal = values[0];
            for (int i = 1; i < size; i++)
            {
                if (values[i] > maxVal)
                {
                    maxVal = values[i];
                    bestIdx = i;
                }
            }
            return bestIdx;
        }
    }
}
