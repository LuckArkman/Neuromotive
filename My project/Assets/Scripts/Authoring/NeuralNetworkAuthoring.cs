using Unity.Entities;
using UnityEngine;
using Neuromotive.AI.Neural;

namespace Neuromotive.AI.Authoring
{
    /// <summary>
    /// Authoring component para configurar a rede neural de um agente no Editor.
    /// Futuramente carregará de arquivos binários externos.
    /// </summary>
    public class NeuralNetworkAuthoring : MonoBehaviour
    {
        public int[] LayerSizes = { 32, 16, 2 }; // Exemplo: 32 inputs -> 16 hidden -> 2 outputs

        public class Baker : Baker<NeuralNetworkAuthoring>
        {
            public override void Bake(NeuralNetworkAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Simulação de pesos aleatórios para o baking inicial da Sprint 20
                float[][] wData = new float[authoring.LayerSizes.Length - 1][];
                float[][] bData = new float[authoring.LayerSizes.Length - 1][];
                int[] inSizes = new int[authoring.LayerSizes.Length - 1];
                int[] outSizes = new int[authoring.LayerSizes.Length - 1];

                for (int i = 0; i < authoring.LayerSizes.Length - 1; i++)
                {
                    inSizes[i] = authoring.LayerSizes[i];
                    outSizes[i] = authoring.LayerSizes[i+1];
                    wData[i] = new float[inSizes[i] * outSizes[i]];
                    bData[i] = new float[outSizes[i]];
                }

                var blobRef = NeuralBlobBuilder.CreateNetworkBlob(inSizes, outSizes, wData, bData);
                
                AddComponent(entity, new AgentNeuralNetwork { NetworkRef = blobRef });
            }
        }
    }
}
