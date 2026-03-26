using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena os dados de animação procedural por vértice (VAT).
    /// Permite animar milhares de agentes sem o custo de um Animator/SkinnedMesh convencional.
    /// </summary>

    /// <summary>
    /// Sistema que sincroniza a animação visual com os dados de movimento da IA.
    /// Ajusta a taxa de caminhada baseado na velocidade física real do ECS.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct CrowdVertexAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            new UpdateAnimationTimeJob { DeltaTime = dt }.ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que atualiza o relógio de animação individual de cada agente.
    /// </summary>
    [BurstCompile]
    public partial struct UpdateAnimationTimeJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            ref AgentAnimationData anim,
            in AgentVelocity velocity,
            in AgentActionState actionState)
        {
            // Sincronização dinâmica: A velocidade da animação depende da velocidade física
            float speedMag = math.length(velocity.Linear);
            anim.AnimationSpeedMultiplier = math.clamp(speedMag * 0.5f, 0.5f, 2.0f);

            // Se o agente estiver parado (Idle), a animação deve ser lenta ou fixa
            if (actionState.CurrentActionId == 0) anim.AnimationSpeedMultiplier = 0.3f;

            anim.AnimationTime += DeltaTime * anim.AnimationSpeedMultiplier;
            
            // Loop da animação (0 a 1 para o shader ler a textura de vértices)
            if (anim.AnimationTime > 1.0f) anim.AnimationTime %= 1.0f;
            
            // Em produção: Aqui escreveríamos para um MaterialMeshInfo via Entities.Graphics
        }
    }
}
