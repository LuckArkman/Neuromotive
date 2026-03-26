using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Configuração do sensor de radar 360 do agente.
    /// </summary>
    public struct SensorConfig : IComponentData
    {
        public float Radius;
        public int RayCount;
        public CollisionFilter Filter;
    }

    /// <summary>
    /// Buffer para armazenar os resultados da percepção (distâncias).
    /// Usamos um IBufferElementData para permitir tamanhos variáveis de raios.
    /// </summary>
    public struct SensorResultElement : IBufferElementData
    {
        public float Distance;
        public int HitType; // 0: Nada, 1: Obstáculo, 2: Outro Agente
    }
}
