using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Gerenciador de orçamento rígido de CPU (Hard Frame Budgeting).
    /// Interrompe o processamento neural se o threshold de tempo de frame for atingido.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct NeuralBudgetManagerSystem : ISystem
    {
        private float _hardLimitMs; // Limite absoluto de CPU para a IA por frame

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _hardLimitMs = 8.0f; // Máximo 8ms dedicados exclusivamente à IA
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<NeuralProfilingStats>()) return;

            var stats = SystemAPI.GetSingleton<NeuralProfilingStats>();
            
            // Se a média acumulada do frame (profiling) exceder o limite rígido,
            // desativamos o grupo de processamento pesado imediatamente.
            if (stats.TotalFrameMs > _hardLimitMs)
            {
                // Entra em modo de emergência: NPCs apenas mantêm a última ação conhecida
                // sem inferir novas percepções neste quadro catastrófico.
                state.Enabled = false; 
            }
            else
            {
                state.Enabled = true;
            }
        }
    }
}
