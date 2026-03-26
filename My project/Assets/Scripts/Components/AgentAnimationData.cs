using Unity.Entities;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Componente que armazena os dados de animação procedural por vértice (VAT).
    /// Permite animar milhares de agentes sem o custo de um Animator/SkinnedMesh convencional.
    /// </summary>
    public struct AgentAnimationData : IComponentData
    {
        public float AnimationTime;
        public float AnimationSpeedMultiplier;
        public int CurrentSequenceId; // 0 = Idle, 1 = Walk, 2 = Special
    }
}
