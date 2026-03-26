using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema de particionamento espacial que organiza agentes em um Grid Hash-Map.
    /// Essencial para buscas de vizinhança em O(1) médio.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [BurstCompile]
    public partial struct SpatialGridSystem : ISystem
    {
        public static float CellSize = 2.0f;
        
        // HashMap que mapeia Hash da célula (int) para a Entidade do Agente.
        public NativeParallelMultiHashMap<int, Entity> Grid;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            Grid = new NativeParallelMultiHashMap<int, Entity>(2000, Allocator.Persistent);
            state.RequireForUpdate<AgentTransform>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (Grid.IsCreated) Grid.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Grid.Clear();
            
            // Re-estimar capacidade baseada no número de agentes
            var entityQuery = SystemAPI.QueryBuilder().WithAll<AgentTransform>().Build();
            if (entityQuery.CalculateEntityCount() > Grid.Capacity)
            {
                Grid.Capacity = entityQuery.CalculateEntityCount();
            }

            // Job para preencher o Grid
            new HashGridJob
            {
                Grid = Grid.AsParallelWriter(),
                CellSize = CellSize
            }.ScheduleParallel();
        }

        [BurstCompile]
        public static int GetCellHash(float3 position, float cellSize)
        {
            int3 cell = (int3)math.floor(position / cellSize);
            // Simples hash de 3D para 1D
            return (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);
        }
    }

    [BurstCompile]
    public partial struct HashGridJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, Entity>.ParallelWriter Grid;
        public float CellSize;

        public void Execute(Entity entity, in AgentTransform transform)
        {
            int hash = SpatialGridSystem.GetCellHash(transform.Position, CellSize);
            Grid.Add(hash, entity);
        }
    }
}
