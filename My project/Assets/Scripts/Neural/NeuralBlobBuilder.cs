using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Neuromotive.AI.Neural
{
    /// <summary>
    /// Utilitário para construir BlobAssets de redes neurais a partir de dados brutos.
    /// Converte pesos de float[] para a estrutura imutável de alta performance.
    /// </summary>
    public static class NeuralBlobBuilder
    {
        public static BlobAssetReference<NeuralNetworkBlob> CreateNetworkBlob(
            int[] layersInputSize, 
            int[] layersOutputSize, 
            float[][] weightsData, 
            float[][] biasData)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var networkBlob = ref builder.ConstructRoot<NeuralNetworkBlob>();
            
            var layerArray = builder.Allocate(ref networkBlob.Layers, layersInputSize.Length);
            
            for (int i = 0; i < layersInputSize.Length; i++)
            {
                layerArray[i].InputSize = layersInputSize[i];
                layerArray[i].OutputSize = layersOutputSize[i];
                
                // Aloca Pesos
                var wArray = builder.Allocate(ref layerArray[i].Weights, weightsData[i].Length);
                for (int w = 0; w < weightsData[i].Length; w++) wArray[w] = weightsData[i][w];
                
                // Aloca Bias
                var bArray = builder.Allocate(ref layerArray[i].Bias, biasData[i].Length);
                for (int b = 0; b < biasData[i].Length; b++) bArray[b] = biasData[i][b];
            }
            
            var reference = builder.CreateBlobAssetReference<NeuralNetworkBlob>(Allocator.Persistent);
            return reference;
        }
    }
}
