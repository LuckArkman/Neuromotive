using Unity.Entities;
using Unity.Mathematics;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Componente que armazena os estados recorrentes da LSTM para cada agente.
    /// Contém o Estado Oculto (Hidden State - h_t) e o Estado de Célula (Cell State - c_t).
    /// </summary>
    [InternalBufferCapacity(64)] // Otimizado para até 64 neurônios LSTM
    public struct LSTMStateElement : IBufferElementData
    {
        public float HiddenValue;
        public float CellValue;
    }

    /// <summary>
    /// Metadados sobre o estado atual da LSTM.
    /// </summary>
    public struct LSTMStateMetadata : IComponentData
    {
        public int StateSize;
        public bool IsInitialized;
    }
}
