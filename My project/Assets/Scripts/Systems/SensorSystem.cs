using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema que organiza e executa SphereCasts em lote para todos os agentes.
    /// Utiliza a API de lote (Batch) da Unity para paralelizar cálculos de física 360°.
    /// </summary>
    [UpdateInGroup(typeof(AIPerceptionGroup))]
    [BurstCompile]
    public partial struct SensorSystem : ISystem
    {
        // Buffers para os comandos e resultados do batch de física
        private NativeArray<SpherecastCommand> _commands;
        private NativeArray<RaycastHit> _results;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SensorConfig>();
            state.RequireForUpdate<AgentTransform>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_commands.IsCreated) _commands.Dispose();
            if (_results.IsCreated) _results.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityQuery = SystemAPI.QueryBuilder().WithAll<AgentTransform, SensorConfig>().Build();
            int agentCount = entityQuery.CalculateEntityCount();
            if (agentCount == 0) return;

            // Supondo 16 raios por agente conforme o Whitepaper
            int rayPerAgent = 16; 
            int totalRays = agentCount * rayPerAgent;

            // Alocação dinâmica re-estimada se o número de agentes subir
            if (!_commands.IsCreated || _commands.Length < totalRays)
            {
                if (_commands.IsCreated) _commands.Dispose();
                if (_results.IsCreated) _results.Dispose();
                _commands = new NativeArray<SpherecastCommand>(totalRays, Allocator.Persistent);
                _results = new NativeArray<RaycastHit>(totalRays, Allocator.Persistent);
            }

            // Job para preparar os comandos (360 graus distribuídos)
            var prepareHandle = new PrepareSensorCommandsJob
            {
                Commands = _commands,
                RayPerAgent = rayPerAgent
            }.ScheduleParallel(state.Dependency);

            // Executar o batch de física no Job System
            // O ScheduleBatch retorna um JobHandle que podemos aguardar no próximo sistema ou no final do frame.
            var physicsHandle = SpherecastCommand.ScheduleBatch(_commands, _results, 1, prepareHandle);

            // Job para preencher o buffer de resultados para a IA
            state.Dependency = new CollectSensorResultsJob
            {
                Results = _results,
                RayPerAgent = rayPerAgent
            }.ScheduleParallel(physicsHandle);
        }
    }

    [BurstCompile]
    public partial struct PrepareSensorCommandsJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public NativeArray<SpherecastCommand> Commands;
        public int RayPerAgent;

        public void Execute([EntityIndexInQuery] int entityIndex, in AgentTransform transform, in SensorConfig config)
        {
            float angleStep = (math.PI * 2) / RayPerAgent;
            int baseIndex = entityIndex * RayPerAgent;

            for (int i = 0; i < RayPerAgent; i++)
            {
                float currentAngle = i * angleStep;
                float3 dir = new float3(math.sin(currentAngle), 0, math.cos(currentAngle));
                
                // Rotacionar direção para coincidir com a orientação do agente
                dir = math.mul(transform.Rotation, dir);

                Commands[baseIndex + i] = new SpherecastCommand(
                    transform.Position, 
                    0.25f, // Raio da esfera do cast
                    dir, 
                    config.Radius,
                    -1 // LayerMask de exemplo (todos)
                );
            }
        }
    }

    [BurstCompile]
    public partial struct CollectSensorResultsJob : IJobEntity
    {
        [ReadOnly] public NativeArray<RaycastHit> Results;
        public int RayPerAgent;

        public void Execute([EntityIndexInQuery] int entityIndex, DynamicBuffer<SensorResultElement> buffer)
        {
            buffer.Clear();
            int baseIndex = entityIndex * RayPerAgent;

            for (int i = 0; i < RayPerAgent; i++)
            {
                var hit = Results[baseIndex + i];
                buffer.Add(new SensorResultElement
                {
                    Distance = hit.distance > 0 ? hit.distance : 0,
                    HitType = hit.distance > 0 ? 1 : 0 // Tipo simplificado (1 = Obstáculo)
                });
            }
        }
    }
}
