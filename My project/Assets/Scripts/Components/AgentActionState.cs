using Unity.Entities;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Componente que armazena a ação discreta decidida pela rede neural.
    /// Ex: 0 = Idle, 1 = Caminhar, 2 = Interagir, 3 = Alerta.
    /// </summary>
    public struct AgentActionState : IComponentData
    {
        public int CurrentActionId;
        public float Confidence;
    }
}
