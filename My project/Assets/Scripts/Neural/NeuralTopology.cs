namespace Neuromotive.AI.Neural
{
    /// <summary>
    /// Define a arquitetura padrão para a rede neural MLP dos agentes.
    /// Baseada na agregação de todos os sensores implementados (Fase 1-2).
    /// </summary>
    public static class NeuralTopology
    {
        // Inputs
        public const int RADAR_RAYS = 16;
        public const int SENSOR_FEATURES = 2; // Distância e Tipo
        public const int NEIGHBOR_FEATURES = 5; // Count, Dist, AvgVel(3)
        public const int TARGET_FEATURES = 3;  // Direção normalizada
        public const int GROUND_FEATURES = 2;  // Fricção e Tipo
        
        public const int TOTAL_INPUTS = (RADAR_RAYS * SENSOR_FEATURES) + NEIGHBOR_FEATURES + TARGET_FEATURES + GROUND_FEATURES;
        
        // Ouput Heads
        public const int MOVEMENT_OUTPUTS = 2; // Linear, Angular
        public const int ACTION_OUTPUTS = 4;   // Idle, Walk, Run, Panic
        
        public const int TOTAL_OUTPUTS = MOVEMENT_OUTPUTS + ACTION_OUTPUTS;

        // Hidden Layers (Exemplo de topologia profunda)
        public static readonly int[] DEFAULT_HIDDEN_SENSORS = { 64, 32 }; 
    }
}
