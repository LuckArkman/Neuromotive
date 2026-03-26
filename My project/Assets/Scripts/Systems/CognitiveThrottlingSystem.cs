using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que define o nível de detalhe cognitivo (LOD) do agente.
    /// NPCs distantes "pensam" menos frequentemente do que NPCs próximos à camera.
    /// </summary>
    public struct CognitiveLOD : IComponentData
    {
        public float DistanceToCamera;
        public int ThrottleScale; // 1 = cada frame de pulso, 2 = pula 1 pulso, etc.
        public int SkipCounter;
    }

    /// <summary>
    /// Sistema que gerencia o Cognitive Throttling.
    /// Economiza CPU reduzindo a frequência de inferência neural para agentes fora de foco.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateAfter(typeof(AgencyBucketingSystem))]
    [BurstCompile]
    public partial struct CognitiveThrottlingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Posição simulada da camera (em produção viria do sistema de renderização)
            float3 cameraPos = float3.zero;

            new CalculateCognitiveLODJob
            {
                CameraPosition = cameraPos
            }.ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que calcula a distância e ajusta a escala de Throttling.
    /// </summary>
    [BurstCompile]
    public partial struct CalculateCognitiveLODJob : IJobEntity
    {
        public float3 CameraPosition;

        public void Execute(
            ref CognitiveLOD lod,
            in AgentTransform transform)
        {
            float distSq = math.distancesq(transform.Position, CameraPosition);
            lod.DistanceToCamera = math.sqrt(distSq);

            // LOD 0 (Close): < 20m -> Sem throttling (Scale 1)
            // LOD 1 (Medium): 20m - 50m -> Throttling Scale 2
            // LOD 2 (Far): > 50m -> Throttling Scale 4 (IA "pensa" raramente)
            
            if (lod.DistanceToCamera < 20f) lod.ThrottleScale = 1;
            else if (lod.DistanceToCamera < 50f) lod.ThrottleScale = 2;
            else lod.ThrottleScale = 4;
            
            // Incrementa o contador de pulso para o próximo frame
            // Os sistemas de cérebro devem ler lod.SkipCounter % lod.ThrottleScale == 0
        }
    }
}
