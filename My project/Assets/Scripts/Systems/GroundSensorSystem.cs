using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena informações do terreno onde o agente está.
    /// Ex: Se o terreno é "Caminhável", "Lento" ou "Mortal".
    /// </summary>
    public struct GroundContextComponent : IComponentData
    {
        public float Friction;
        public float SurfaceType; // Mapeado do Splatmap (0: Asfalto, 1: Grama, etc)
        public float3 SurfaceNormal;
    }

    /// <summary>
    /// Sistema que consulta as propriedades do solo para cada agente.
    /// Utiliza amostragem de dados enviada via CPU para o ECS.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [UpdateAfter(typeof(StaticObstacleEncodingSystem))]
    [BurstCompile]
    public partial struct GroundSensorSystem : ISystem
    {
        // Exemplo de como passaríamos o Splatmap para o Burst
        // public NativeArray<float> SplatmapData;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SampleGroundJob().ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct SampleGroundJob : IJobEntity
    {
        public void Execute(ref GroundContextComponent ground, in AgentTransform transform)
        {
            // Lógica de amostragem (Representação simplificada)
            // Futuramente integrará com um buffer de Splatmap real.
            ground.SurfaceNormal = new float3(0, 1, 0); // Assume plano por enquanto
            ground.Friction = 1.0f;
            
            // Simulação de detecção de superfície (ex: asfalto vs grama baseado em posição)
            ground.SurfaceType = (math.floor(transform.Position.x) + math.floor(transform.Position.z)) % 2 == 0 ? 0 : 1;
        }
    }
}
