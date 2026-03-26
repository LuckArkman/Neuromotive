using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Authoring
{
    /// <summary>
    /// Script Authoring principal para converter um GameObject em um Agente Neural Neuromotive.
    /// Este script DEVE ser adicionado ao GameObject (ex: Cápsula, NPC) em vez dos Sistemas.
    /// </summary>
    public class NeuromotiveAgentAuthoring : MonoBehaviour
    {
        [Header("Percepção")]
        public float SensorRadius = 10f;
        
        [Header("Movimentação")]
        public float MaxSpeed = 3.5f;

        [Header("Inteligência (LSTM)")]
        public int HiddenStateSize = 128;

        public class Baker : Baker<NeuromotiveAgentAuthoring>
        {
            public override void Bake(NeuromotiveAgentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // 1. Identidade e Transformação Física
                AddComponent(entity, new AgentComponent { AgentID = entity.Index });
                AddComponent(entity, new AgentTransform { Position = authoring.transform.position, Rotation = authoring.transform.rotation });
                AddComponent(entity, new AgentVelocity { Linear = float3.zero, Angular = float3.zero });

                // 2. Configuração de Sensores (Radar)
                AddComponent(entity, new SensorConfig { Radius = authoring.SensorRadius });
                AddBuffer<SensorResultElement>(entity);

                // 3. Memória Neural (LSTM)
                AddComponent(entity, new LSTMStateMetadata { StateSize = authoring.HiddenStateSize });
                var stateBuffer = AddBuffer<LSTMStateElement>(entity);
                // Inicializa o buffer com zeros
                for (int i = 0; i < authoring.HiddenStateSize * 2; i++)
                {
                    stateBuffer.Add(new LSTMStateElement { HiddenValue = 0 });
                }

                // 4. Estados de Ação e Animação
                AddComponent(entity, new AgentActionState { CurrentActionId = 0, Confidence = 0 });
                AddComponent(entity, new AgentAnimationData { AnimationTime = 0, AnimationSpeedMultiplier = 1 });
            }
        }
    }
}
