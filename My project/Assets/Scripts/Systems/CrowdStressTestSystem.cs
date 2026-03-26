using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Configuração do spawner de multidão, criada via CrowdSpawnerAuthoring.
    /// </summary>
    public struct CrowdSpawnerConfig : IComponentData
    {
        public Entity AgentPrefab;
        public int AgentCount;
        public bool Spawned;
    }

    /// <summary>
    /// Sistema de Stress Test: instancia os agentes e injeta os componentes neurais necessários.
    /// Usa AddComponent para garantir que componentes sejam criados mesmo que o Prefab não os possua.
    /// 
    /// NOTA: Obtemos uma CÓPIA local do Singleton ANTES de fazer mudanças estruturais (AddComponent/Instantiate).
    /// Mudanças estruturais invalidam qualquer ponteiro de RefRW, causando ObjectDisposedException.
    /// Depois das mudanças, usamos SetSingleton para gravar de volta.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct CrowdStressTestSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<CrowdSpawnerConfig>()) return;

            // ✅ Cópia local ANTES de qualquer mudança estrutural
            var spawnerData = SystemAPI.GetSingleton<CrowdSpawnerConfig>();
            if (spawnerData.Spawned) return;

            int agentCount = spawnerData.AgentCount;
            var prefab = spawnerData.AgentPrefab;
            var em = state.EntityManager;

            // Instantiate é uma mudança estrutural — feito ANTES de tentar gravar de volta
            var entities = em.Instantiate(prefab, agentCount, Allocator.Temp);
            var random = new Random(1234);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var pos = random.NextFloat3(new float3(-50, 0, -50), new float3(50, 0, 50));

                // Injeta AgentTransform se não existir
                if (!em.HasComponent<AgentTransform>(entity))
                    em.AddComponentData(entity, new AgentTransform { Position = pos, Rotation = quaternion.identity });
                else
                    em.SetComponentData(entity, new AgentTransform { Position = pos, Rotation = quaternion.identity });

                // Injeta AgentVelocity se não existir
                if (!em.HasComponent<AgentVelocity>(entity))
                    em.AddComponentData(entity, new AgentVelocity { Linear = float3.zero, Angular = float3.zero });

                // Injeta AgentAgencyBucket se não existir
                if (!em.HasComponent<AgentAgencyBucket>(entity))
                    em.AddComponentData(entity, new AgentAgencyBucket { BucketId = i % 3 });
                else
                    em.SetComponentData(entity, new AgentAgencyBucket { BucketId = i % 3 });

                // Injeta AgentActionState se não existir
                if (!em.HasComponent<AgentActionState>(entity))
                    em.AddComponentData(entity, new AgentActionState { CurrentActionId = 0, Confidence = 0f });

                // Injeta AgentActionMask se não existir
                if (!em.HasComponent<AgentActionMask>(entity))
                    em.AddComponentData(entity, new AgentActionMask { AllowedActionsMask = 0b1111u });
            }

            entities.Dispose();

            // ✅ Gravamos de volta APÓS todas as mudanças estruturais estarem completas
            spawnerData.Spawned = true;
            SystemAPI.SetSingleton(spawnerData);
        }
    }
}
