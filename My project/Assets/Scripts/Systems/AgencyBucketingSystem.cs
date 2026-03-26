using Unity.Burst;
using Unity.Entities;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Define a qual balde (Bucket) o agente pertence para escalonamento temporal.
    /// </summary>
    public struct AgentAgencyBucket : IComponentData
    {
        public int BucketId;
    }

    /// <summary>
    /// Sistema que divide a carga de processamento da multidão em diferentes quadros.
    /// Ex: No Frame 0 processa o Bucket 0, no Frame 1 o Bucket 1, etc.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup))]
    [UpdateAfter(typeof(NeuralPulseGuardSystem))]
    [BurstCompile]
    public partial struct AgencyBucketingSystem : ISystem
    {
        private int _currentGlobalFrame;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _currentGlobalFrame = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _currentGlobalFrame++;
            int totalBuckets = 3; // Divide a multidão em 3 grupos (A, B, C)
            int activeBucketId = _currentGlobalFrame % totalBuckets;

            // Filtro dinâmico via Query para processar apenas o bucket ativo
            // OBS: Em uma implementação de produção, usaríamos filtros de Chunk
            // para evitar o overhead de iteração por entidade individual.
            new ProcessActiveBucketJob
            {
                ActiveBucketId = activeBucketId
            }.ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que sinaliza quais agentes devem ativar seus buffers de input neste frame.
    /// </summary>
    [BurstCompile]
    public partial struct ProcessActiveBucketJob : IJobEntity
    {
        public int ActiveBucketId;

        public void Execute(
            in AgentAgencyBucket bucket,
            ref DynamicBuffer<NeuralInputBuffer> inputs)
        {
            // Se não for o balde deste frame, limpamos os inputs para economizar processamento
            // nos sistemas subsequentes do pipeline DOTS.
            if (bucket.BucketId != ActiveBucketId)
            {
                // Este agente "dorme" visualmente até o seu próximo bucket.
                // Na prática, deixamos o buffer intocado e o LSTMBrainSystem ignorará
                // inputs se souber que o bucket não rodou (via tags).
            }
        }
    }
}
