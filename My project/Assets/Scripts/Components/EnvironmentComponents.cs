using Unity.Entities;
using Unity.Mathematics;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Marca uma entidade como um ponto de interesse para os agentes.
    /// </summary>
    public struct InterestPoint : IComponentData
    {
        public float Weight; // Atratividade do ponto
        public AgentType TargetType; // Qual tipo de agente deve se interessar
    }
}
