using Unity.Entities;
using Unity.Mathematics;

namespace Neuromotive.AI.Neural
{
    /// <summary>
    /// Estrutura de pesos armazenada em BlobAsset para acesso imutável e rápido.
    /// Contém pesos (Weights) e vieses (Bias).
    /// </summary>
    public struct LayerBlob
    {
        public BlobArray<float> Weights;
        public BlobArray<float> Bias;
        public int InputSize;
        public int OutputSize;
    }

    /// <summary>
    /// Referência para as camadas de uma rede neural.
    /// </summary>
    public struct NeuralNetworkBlob
    {
        public BlobArray<LayerBlob> Layers;
    }

    /// <summary>
    /// Componente que carrega a referência para os pesos da rede neural do agente.
    /// </summary>
    public struct AgentNeuralNetwork : IComponentData
    {
        public BlobAssetReference<NeuralNetworkBlob> NetworkRef;
    }
}
