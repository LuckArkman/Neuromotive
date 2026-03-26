using Unity.Entities;
using Unity.Mathematics;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Contém metadados básicos de identificação.
    /// </summary>
    public struct AgentComponent : IComponentData
    {
        public int AgentID;
        public AgentType Type;
    }

    public enum AgentType : byte
    {
        Pedestrian,
        Vehicle,
        Drone
    }

    /// <summary>
    /// Versão otimizada de Transform para lógica interna da IA.
    /// </summary>
    public struct AgentTransform : IComponentData
    {
        public float3 Position;
        public float4 Rotation; // Quaternion
    }

    /// <summary>
    /// Armazena velocidade linear e angular.
    /// </summary>
    public struct AgentVelocity : IComponentData
    {
        public float3 Linear;
        public float Angular;
    }

    /// <summary>
    /// Objetivo atual do agente.
    /// </summary>
    public struct AgentTarget : IComponentData
    {
        public float3 Position;
        public float StoppingDistance;
        public bool IsReached;
    }
}
