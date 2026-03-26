using Unity.Entities;
using Unity.Transforms;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Grupo que contém todos os sistemas de percepção sensorial (SphereCasts, Raycasts).
    /// Executa antes da simulação de IA principal.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class AIPerceptionGroup : ComponentSystemGroup { }

    /// <summary>
    /// Grupo central para inferência neural (MLP/LSTM).
    /// Executa após os sensores estarem prontos.
    /// </summary>
    [UpdateAfter(typeof(AIPerceptionGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class AIBrainGroup : ComponentSystemGroup { }

    /// <summary>
    /// Grupo para aplicação de forças e atualização de transformadas.
    /// Executa após a decisão da IA.
    /// </summary>
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(LocalToWorldSystem))] // Garante que a transformada foi atualizada ao menos uma vez
    public partial class AIActuationGroup : ComponentSystemGroup { }
}
