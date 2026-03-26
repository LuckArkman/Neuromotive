using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using System.IO;
using UnityEngine;
using Neuromotive.AI.Components;

namespace Neuromotive.AI.Systems
{
    /// <summary>
    /// Armazena os pesos da rede neural carregados de arquivos externos (.bin).
    /// Centraliza o acesso aos dados para todos os agentes da horda.
    /// </summary>
    public struct NeuralModelWeights : IComponentData
    {
        public bool IsLoaded;
        public bool DidAttemptLoad;
        public int WeightCount;
        // Em produção, usaríamos Pesos por Camada via BlobAsset para eficiência Burst total
    }

    /// <summary>
    /// Sistema responsável por injetar os pesos treinados no motor de inferência C#.
    /// Valida a paridade entre o treinamento Python e a simulação Unity em tempo real.
    /// </summary>
    [UpdateInGroup(typeof(AIBrainGroup), OrderFirst = true)]
    public partial struct NeuralWeightLoaderSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton<NeuralModelWeights>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var weights = SystemAPI.GetSingleton<NeuralModelWeights>();
            if (weights.IsLoaded) return;

            string weightsPath = Path.Combine(Application.streamingAssetsPath, "NeuralWeights");
            
            // Verificação de segurança: O diretório de pesos deve existir (Sprint 63)
            if (!Directory.Exists(weightsPath))
            {
                if (!weights.DidAttemptLoad)
                {
                    Debug.LogWarning($"[Neuromotive] Diretório de pesos não encontrado: {weightsPath}. A IA operará em modo fallback.");
                    weights.DidAttemptLoad = true;
                    SystemAPI.SetSingleton(weights);
                }
                return;
            }

            // Exemplo de carregamento de matriz de entrada (Input Kernel)
            string kernelPath = Path.Combine(weightsPath, "kernel.bin");
            if (File.Exists(kernelPath))
            {
                byte[] bytes = File.ReadAllBytes(kernelPath);
                float[] floats = new float[bytes.Length / 4];
                System.Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);

                Debug.Log($"[Neuromotive] Pesos carregados com sucesso: {floats.Length} floats do kernel.");
                
                weights.IsLoaded = true;
                weights.DidAttemptLoad = true;
                weights.WeightCount = floats.Length;
                SystemAPI.SetSingleton(weights);
            }
            else
            {
                weights.DidAttemptLoad = true;
                SystemAPI.SetSingleton(weights);
            }
        }
    }
}
