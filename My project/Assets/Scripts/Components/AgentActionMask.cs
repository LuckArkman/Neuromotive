using Unity.Burst;
using Unity.Entities;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Componente de máscara que permite restringir ações neurais impossíveis em certos contextos.
    /// Ex: Se o agente estiver no ar, o bit de 'Pular' é mascarado para 0, impedindo a escolha.
    /// </summary>
    public struct AgentActionMask : IComponentData
    {
        public uint AllowedActionsMask; // Bitmask (bit 0 = ação 0 habilitada, etc)

        // Auxiliares para facilitar o uso nos sistemas
        public bool IsActionAllowed(int actionId) => (AllowedActionsMask & (1u << actionId)) != 0;
        
        public void SetActionAllowed(int actionId, bool allowed)
        {
            if (allowed) AllowedActionsMask |= (1u << actionId);
            else AllowedActionsMask &= ~(1u << actionId);
        }
    }
}
