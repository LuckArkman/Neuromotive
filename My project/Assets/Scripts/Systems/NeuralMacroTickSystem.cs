using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Componente global que dita o pulso de decisão da rede neural.
    /// Permite economizar CPU processando inferências em clocks alternados.
    /// </summary>
    public struct NeuralMacroPulse : IComponentData
    {
        public bool IsPulseFrame;
        public float PulseInterval;
        public float LastPulseTime;
    }

    /// <summary>
    /// Orquestrador de Ordem Superior.
    /// Utiliza UniTask para gerenciar o escalonamento temporal das decisões da IA.
    /// </summary>
    public class NeuralUniTaskOrchestrator : MonoBehaviour
    {
        private World _world;
        private EntityQuery _pulseQuery;

        private async void Start()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _pulseQuery = _world.EntityManager.CreateEntityQuery(typeof(NeuralMacroPulse));

            // Inicia o loop de pulso macro perpétuo
            await RunMacroPulseLoop();
        }

        private async UniTask RunMacroPulseLoop()
        {
            while (Application.isPlaying)
            {
                // Alterna o estado de pulso global
                UpdatePulseState(true);
                
                // Aguarda 1 frame (ou N frames dependendo da carga)
                await UniTask.NextFrame();

                // Desliga o pulso (as IAs apenas agem sobre o último comando)
                UpdatePulseState(false);

                // Aguarda o próximo ciclo de "pensamento" (Macro-Tick)
                await UniTask.Delay(50); // Ex: 20 Hz de inferência (50ms)
            }
        }

        private void UpdatePulseState(bool active)
        {
            if (_pulseQuery.IsEmpty)
            {
                var entity = _world.EntityManager.CreateEntity(typeof(NeuralMacroPulse));
                _world.EntityManager.SetComponentData(entity, new NeuralMacroPulse { IsPulseFrame = active });
            }
            else
            {
                var pulse = _pulseQuery.GetSingleton<NeuralMacroPulse>();
                pulse.IsPulseFrame = active;
                _world.EntityManager.SetComponentData(_pulseQuery.GetSingletonEntity(), pulse);
            }
        }
    }

    /// <summary>
    /// Grupo de sistemas que só executa o cérebro se o pulso macro estiver ativo.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderFirst = true)]
    public partial struct NeuralPulseGuardSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<NeuralMacroPulse>()) return;

            var pulse = SystemAPI.GetSingleton<NeuralMacroPulse>();
            
            // Se não for o frame de pulso, desabilitamos o grupo de processamento pesado
            if (!pulse.IsPulseFrame)
            {
                state.Enabled = false; // Este sistema específico para, mas outros sistemas podem ler o estado anterior
            }
            else
            {
                state.Enabled = true;
            }
        }
    }
}
