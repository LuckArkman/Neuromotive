using Unity.Entities;
using UnityEngine;
using Neuromotive.AI.Systems;

namespace Neuromotive.AI.Authoring
{
    public class CrowdSpawnerAuthoring : MonoBehaviour
    {
        public GameObject AgentPrefab;
        public int AgentCount = 2000;

        public class Baker : Baker<CrowdSpawnerAuthoring>
        {
            public override void Bake(CrowdSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CrowdSpawnerConfig
                {
                    AgentPrefab = GetEntity(authoring.AgentPrefab, TransformUsageFlags.Dynamic),
                    AgentCount = authoring.AgentCount,
                    Spawned = false
                });
            }
        }
    }
}
