using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Neuromotive.AI.Components;
using Unity.Mathematics;


namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Tag para identificar o agente que está sendo monitorado pela telemetria fina.
    /// </summary>
    public struct SelectedForTelemetry : IComponentData {}

    /// <summary>
    /// Sistema de telemetria individual.
    /// Registra e exibe o "fluxo de pensamento" (Hidden States e Decisões) de um NPC selecionado.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderLast = true)]
    public partial struct IndividualAgentTelemetrySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Busca o agente selecionado para telemetria
            if (!SystemAPI.HasSingleton<SelectedForTelemetry>()) return;

            var entity = SystemAPI.GetSingletonEntity<SelectedForTelemetry>();
            
            // Coleta dados neurais do agente em tempo real
            var metadata = state.EntityManager.GetComponentData<LSTMStateMetadata>(entity);
            var actionState = state.EntityManager.GetComponentData<AgentActionState>(entity);
            var states = state.EntityManager.GetBuffer<LSTMStateElement>(entity);

            // Log de Telemetria Fina (Console e Diag)
            // Mostra o Hidden State dominante e a Ação Escolhida
            float dominantH = 0;
            if (states.Length > 0) dominantH = states[0].HiddenValue;

            string telemetryMsg = $"[Neuromotive Telemetry] Entity: {entity.Index} | Action: {actionState.CurrentActionId} | Confidence: {actionState.Confidence:F2} | StateSize: {metadata.StateSize} | H[0]: {dominantH:F4}";
            
            // Em uma implementação real, isto mandaria dados para uma UI customizada
            Debug.Log(telemetryMsg);

            // Visualização de "Linha de Pensamento" via Gizmos (Raycasting no HUD imaginário)
            var transform = state.EntityManager.GetComponentData<AgentTransform>(entity);
            Debug.DrawRay(transform.Position + new float3(0, 2, 0), new float3(0, 1, 0) * actionState.Confidence, Color.cyan);

        }
    }
}
