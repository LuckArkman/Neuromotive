using Unity.Entities;
using Unity.Mathematics;
using System.Runtime.InteropServices;

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
    /// Componente de transformação otimizado para cache.
    /// Alinhado para 64 bytes para evitar Cache Thrashing em densidades altas.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct AgentTransform : IComponentData
    {
        [FieldOffset(0)] public float3 Position;
        [FieldOffset(16)] public quaternion Rotation;
        
        // Padding automático garantido pelo Size=64
    }

    /// <summary>
    /// Estado de velocidade do agente.
    /// Alinhado para 32 bytes (2x SIMD widths).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct AgentVelocity : IComponentData
    {
        [FieldOffset(0)] public float3 Linear;
        [FieldOffset(16)] public float3 Angular;
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
