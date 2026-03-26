using Unity.Entities;

namespace Neuromotive.AI.Components
{
    /// <summary>
    /// Marca uma entidade como um obstáculo estático (Parede, Prédio, etc).
    /// </summary>
    public struct StaticObstacleTag : IComponentData { }

    /// <summary>
    /// Marca uma entidade como o Jogador.
    /// </summary>
    public struct PlayerTag : IComponentData { }

    /// <summary>
    /// Tag específica para pedestres.
    /// </summary>
    public struct PedestrianTag : IComponentData { }

    /// <summary>
    /// Tag específica para veículos.
    /// </summary>
    public struct VehicleTag : IComponentData { }

    /// <summary>
    /// Tag para obstáculos que se movem mas não são agentes inteligentes de IA.
    /// </summary>
    public struct MobileObstacleTag : IComponentData { }
}
