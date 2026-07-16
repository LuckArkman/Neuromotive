using System.Collections.Generic;
using UnityEngine;

namespace Neural
{
    public class VisionNeuralNetwork : MonoBehaviour
    {
        [Header("Configurações de I/O")] private const int InputSize = 16; // 16 Sensores de Ângulo
        private const int OutputSize = 16; // 16 Ações/Saídas

        [Header("Arquitetura Oculta")] public int neuronsPerHiddenLayer = 16; // Densidade das 4 camadas

        private List<Neuron[]> layers = new List<Neuron[]>();

        void Awake()
        {
            InitNetwork();
        }

        void InitNetwork()
        {
            // Camada Oculta 1 (Recebe 16 entradas)
            layers.Add(CreateLayer(neuronsPerHiddenLayer, InputSize));

            // Camadas Ocultas 2, 3 e 4
            for (int i = 0; i < 3; i++)
                layers.Add(CreateLayer(neuronsPerHiddenLayer, neuronsPerHiddenLayer));

            // Camada de Saída (Retorna 16 valores)
            layers.Add(CreateLayer(OutputSize, neuronsPerHiddenLayer));

            Debug.Log($"Rede de Visão Inicializada: 16 In -> [4x Hidden {neuronsPerHiddenLayer}] -> 16 Out");
        }

        Neuron[] CreateLayer(int size, int inputsPerNeuron)
        {
            Neuron[] layer = new Neuron[size];
            for (int i = 0; i < size; i++)
                layer[i] = new Neuron(inputsPerNeuron);
            return layer;
        }

        public float[] ProcessVision(float[] visionSensors)
        {
            if (visionSensors.Length != InputSize)
            {
                Debug.LogError("A rede espera exatamente 16 entradas de sensores!");
                return null;
            }

            float[] currentData = visionSensors;

            // Propagação (Feed Forward)
            foreach (var layer in layers)
            {
                float[] nextData = new float[layer.Length];
                for (int i = 0; i < layer.Length; i++)
                {
                    nextData[i] = layer[i].Activate(currentData);
                }

                currentData = nextData;
            }

            return currentData; // Retorna os 16 valores de saída
        }
    }
}