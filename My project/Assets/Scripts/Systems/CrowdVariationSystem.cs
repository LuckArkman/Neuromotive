using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente que armazena a cor base do agente para o Shader (DOTS Instancing).
    /// Permite variar a aparência da multidão sem novos materiais.
    /// </summary>
    [MaterialProperty("_BaseColor")]
    public struct AgentColorProperty : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// Sistema de variação randômica de arquétipos visuais e comportamentais.
    /// Garante que a multidão não pareça um exército de clones, diversificando cores e agilidade.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct CrowdVariationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // O sistema só atua em agentes que acabaram de ser spawnados (sem cor inicial).
            // Em produção: Usaríamos Enableable Component ou um Tag de inicialização.
            new RandomizeCrowdVisualsJob { Seed = (uint)SystemAPI.Time.ElapsedTime + 1 }.ScheduleParallel();
        }
    }

    /// <summary>
    /// Job que aplica diversidade visual e comportamental em massa.
    /// </summary>
    [BurstCompile]
    public partial struct RandomizeCrowdVisualsJob : IJobEntity
    {
        public uint Seed;

        public void Execute(
            [EntityIndexInQuery] int index,
            ref AgentColorProperty color,
            ref AgentMacroTarget target)
        {
            var random = new Random((uint)(Seed + index));

            // 1. Diversidade Visual: Cores randômicas (tons de pele/roupa)
            // Impede o efeito de clones visuais.
            color.Value = new float4(
                random.NextFloat(0.4f, 1.0f), 
                random.NextFloat(0.4f, 0.8f), 
                random.NextFloat(0.3f, 0.6f), 
                1.0f);

            // 2. Diversidade Comportamental: Variância de confiança
            // Agentes diferentes têm thresholds de decisão diferentes, criando caos orgânico.
            target.ConfidenceThreshold = random.NextFloat(0.3f, 0.7f);
        }
    }
}
