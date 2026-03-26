using Unity.Entities;
using Unity.Mathematics;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Armazena informações processadas sobre vizinhos dinâmicos.
    /// </summary>
    public struct NeighborEncodingComponent : IComponentData
    {
        public int NeighborCount;
        public float3 AverageNeighborVelocity;
        public float ClosestNeighborDistance;
    }
}
