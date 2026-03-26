using Unity.Entities;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Buffer que contém os dados de entrada já processados e normalizados para a rede neural.
    /// Estrutura: [Distância_Ray0, Type_Ray0_A, Type_Ray0_B, ...]
    /// </summary>
    [InternalBufferCapacity(64)] // Otimizado para 16 raios x 3/4 inputs por raio
    public struct NeuralInputBuffer : IBufferElementData
    {
        public float Value;
    }
}
