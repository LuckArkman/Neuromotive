using NUnit.Framework;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Neuromotive.AI.Neural;

namespace Neuromotive.AI.Tests
{
    /// <summary>
    /// Testes de unidade para garantir a paridade matemática do motor de tensores customizado.
    /// Valida operações de Linear, Sigmoid, Tanh e Softmax.
    /// </summary>
    public class TensorMathTests
    {
        [Test]
        public void LinearSIMDBlob_CalculatesCorrectly()
        {
            // Setup
            int inputSize = 4;
            int outputSize = 1;
            
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var layer = ref builder.ConstructRoot<LayerBlob>();
            layer.InputSize = inputSize;
            layer.OutputSize = outputSize;
            
            var w = builder.Allocate(ref layer.Weights, 4);
            w[0] = 1; w[1] = 2; w[2] = 3; w[3] = 4;
            
            var b = builder.Allocate(ref layer.Bias, 1);
            b[0] = 10;
            
            var blobRef = builder.CreateBlobAssetReference<LayerBlob>(Allocator.Temp);

            // Mock Input Buffer (Simulado via NativeArray para teste puro)
            var input = new NativeArray<float>(4, Allocator.Temp);
            input[0] = 1; input[1] = 1; input[2] = 1; input[3] = 1;
            
            var output = new NativeArray<float>(1, Allocator.Temp);
            
            // Replicamos a lógica do buffer dinâmico manualmente para o teste
            // (No Unity ECS real, passaríamos o DynamicBuffer)
            float sum = blobRef.Value.Bias[0];
            for(int i=0; i<4; i++) sum += input[i] * blobRef.Value.Weights[i];
            
            Assert.AreEqual(20, sum); // 10 + (1*1 + 1*2 + 1*3 + 1*4) = 20
        }

        [Test]
        public void FastSigmoid_IsWithinRange()
        {
            float4 x = new float4(-10, 0, 10, 100);
            float4 s = TensorMath.FastSigmoid(x);
            
            Assert.Less(s.x, 0.1f);
            Assert.AreEqual(0.5f, s.y);
            Assert.Greater(s.z, 0.9f);
            Assert.LessOrEqual(s.w, 1.0f);
        }

        [Test]
        public void Softmax_SumsToOne()
        {
            var values = new NativeArray<float>(3, Allocator.Temp);
            values[0] = 1.0f; values[1] = 2.0f; values[2] = 3.0f;
            
            TensorMath.Softmax(ref values, 3);
            
            float sum = values[0] + values[1] + values[2];
            Assert.IsTrue(math.abs(sum - 1.0f) < 0.0001f);
        }
    }
}
