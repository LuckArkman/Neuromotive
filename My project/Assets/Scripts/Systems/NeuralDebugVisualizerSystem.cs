using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Sistema de visualização de depuração profissional (Debug Visualizer).
    /// Renderiza Gizmos em tempo real para representar o estado interno da rede neural (Heatmaps de Intenção).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct NeuralDebugVisualizerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // O debug visual só deve rodar se o usuário solicitar via Editor (ou Flag Global)
            bool showDebug = true; // Em produção, isto seria uma flag de UI
            if (!showDebug) return;

            // Coleta dados para renderização de gizmos (Micro-Tick debug)
            foreach (var (transform, actionState, density) 
                     in SystemAPI.Query<AgentTransform, AgentActionState, AgentCrowdDensity>())
            {
                // Define a cor baseada no ID da ação (Heatmap Comportamental)
                // 0 = Idle (Verde), 1 = Walk (Azul), 2 = Interact (Amarelo), 3 = Alert (Vermelho)
                Color behaviorColor = Color.green;
                switch (actionState.CurrentActionId)
                {
                    case 1: behaviorColor = Color.blue; break;
                    case 2: behaviorColor = Color.yellow; break;
                    case 3: behaviorColor = Color.red; break;
                }

                // Desenha o vetor de intenção (Steering Ray)
                float3 headDirection = math.forward(transform.Rotation) * 1.5f;
                Debug.DrawRay(transform.Position + new float3(0, 1, 0), headDirection, behaviorColor);

                // Desenha a "Bolha Social" (Heatmap de Densidade)
                // Quanto mais denso, maior o anel de aviso ao redor do agente
                if (density.DensityValue > 0.1f)
                {
                    DrawCircle(transform.Position, 1.0f, Color.red * density.DensityValue);
                }
            }
        }

        private void DrawCircle(float3 center, float radius, Color color)
        {
            int segments = 8;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * math.PI * 2.0f;
                float nextAngle = ((i + 1) / (float)segments) * math.PI * 2.0f;

                float3 p1 = center + new float3(math.cos(angle) * radius, 0.1f, math.sin(angle) * radius);
                float3 p2 = center + new float3(math.cos(nextAngle) * radius, 0.1f, math.sin(nextAngle) * radius);
                
                Debug.DrawLine(p1, p2, color);
            }
        }
    }
}
