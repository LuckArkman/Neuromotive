using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que mapeia os dados de animação para o SRP Batcher / DOTS Instancing.
    /// O Shader do URP lê estes valores diretamente do buffer CBuffer do ECS.
    /// </summary>
    [MaterialProperty("_AnimTime")]
    public struct AgentAnimTimeProperty : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// Sistema de otimização de GPU Instancing.
    /// Transfere o estado de animação individual calculado na CPU para a GPU em massa,
    /// permitindo que 10.000 agentes sejam renderizados em uma única Draw Call indexada.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(CrowdVertexAnimationSystem))]
    [BurstCompile]
    public partial struct CrowdInstancingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Transfere o valor de tempo de animação procedural para a propriedade de material instanciada.
            // Isso permite que o Shader de Vértices saiba qual quadro da VAT ler por agente.
            new WriteAnimPropertyJob().ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que canaliza os dados de animação calculados para a pipeline de renderização.
    /// </summary>
    [BurstCompile]
    public partial struct WriteAnimPropertyJob : IJobEntity
    {
        public void Execute(
            ref AgentAnimTimeProperty property,
            in AgentAnimationData anim)
        {
            // Mapeia o resultado da simulação para a memória que o Shader consome na GPU.
            property.Value = anim.AnimationTime;
        }
    }
}
