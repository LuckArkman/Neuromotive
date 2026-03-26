using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema que identifica outros agentes em movimento utilizando o Grid Hashing.
    /// Codifica velocidade e distância média dos vizinhos para o cérebro neural.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [UpdateAfter(typeof(SpatialGridSystem))]
    [BurstCompile]
    public partial struct NeighborEncodingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridSystem = state.World.GetExistingSystemManaged<SpatialGridSystem>();
            if (!gridSystem.Grid.IsCreated) return;

            new NeighborProcessJob
            {
                Grid = gridSystem.Grid,
                CellSize = SpatialGridSystem.CellSize,
                Velocities = SystemAPI.GetComponentLookup<AgentVelocity>(true),
                Transforms = SystemAPI.GetComponentLookup<AgentTransform>(true)
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct NeighborProcessJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int, Entity> Grid;
        public float CellSize;
        [ReadOnly] public ComponentLookup<AgentVelocity> Velocities;
        [ReadOnly] public ComponentLookup<AgentTransform> Transforms;

        public void Execute(Entity entity, ref NeighborEncodingComponent encoding, in AgentTransform transform)
        {
            int hash = SpatialGridSystem.GetCellHash(transform.Position, CellSize);
            float3 totalVel = float3.zero;
            int count = 0;
            float minDist = float.MaxValue;

            // Busca na célula atual (simplificado para sprint atual)
            if (Grid.TryGetFirstValue(hash, out Entity neighbor, out var iterator))
            {
                do
                {
                    if (neighbor == entity) continue;
                    
                    if (Transforms.TryGetComponent(neighbor, out var nTransform))
                    {
                        float dist = math.distance(transform.Position, nTransform.Position);
                        if (dist < 10.0f) // Raio de interesse
                        {
                            if (Velocities.TryGetComponent(neighbor, out var nVel))
                            {
                                totalVel += nVel.Linear;
                            }
                            count++;
                            if (dist < minDist) minDist = dist;
                        }
                    }
                } while (Grid.TryGetNextValue(out neighbor, ref iterator));
            }

            encoding.NeighborCount = count;
            encoding.AverageNeighborVelocity = count > 0 ? totalVel / count : float3.zero;
            encoding.ClosestNeighborDistance = count > 0 ? minDist : 0;
        }
    }
}
