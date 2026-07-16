using System.Collections.Generic;
using UnityEngine;

namespace Neural
{
    using System.IO;

    public class NeuralBrain : MonoBehaviour
    {
        public List<Neuron[]> layers = new List<Neuron[]>();
        /// <summary>Ativações de cada camada após o último Think(). Índice 0 = inputs.</summary>
        public List<float[]> lastActivations = new List<float[]>();
        private int inputSize = 16;
        private int hiddenSize = 16;
        private int outputSize = 16;

        public void Init()
        {
            layers.Clear();
            layers.Add(CreateLayer(hiddenSize, inputSize)); // Oculta 1
            for (int i = 0; i < 3; i++) layers.Add(CreateLayer(hiddenSize, hiddenSize)); // Ocultas 2, 3, 4
            layers.Add(CreateLayer(outputSize, hiddenSize)); // Saída
        }

        Neuron[] CreateLayer(int size, int inputs)
        {
            Neuron[] layer = new Neuron[size];
            for (int i = 0; i < size; i++) layer[i] = new Neuron(inputs);
            return layer;
        }

        public float[] Think(float[] inputs)
        {
            // Registra inputs como camada 0
            lastActivations.Clear();
            lastActivations.Add((float[])inputs.Clone());

            float[] current = inputs;
            foreach (var layer in layers)
            {
                float[] next = new float[layer.Length];
                for (int i = 0; i < layer.Length; i++) next[i] = layer[i].Activate(current);
                lastActivations.Add((float[])next.Clone()); // Registra ativações desta camada
                current = next;
            }

            return current;
        }

        public void ApplyMutation(float rate, float amount)
        {
            foreach (var layer in layers)
            {
                foreach (var neuron in layer) neuron.Mutate(rate, amount);
            }
        }

        public void SaveBestBrain(string fileName)
        {
            BrainSaveData data = new BrainSaveData();

            foreach (var layer in layers)
            {
                LayerSaveData layerData = new LayerSaveData();
                layerData.neurons = new NeuronSaveData[layer.Length];

                for (int i = 0; i < layer.Length; i++)
                {
                    layerData.neurons[i] = new NeuronSaveData
                    {
                        weights = layer[i].weights,
                        bias = layer[i].bias
                    };
                }

                data.layers.Add(layerData);
            }

            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
            File.WriteAllText(path, json);

            Debug.Log($"Cérebro salvo com sucesso em: {path}");
        }

        public void LoadBrain(string fileName)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName + ".json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                BrainSaveData data = JsonUtility.FromJson<BrainSaveData>(json);

                layers.Clear();
                foreach (var layerData in data.layers)
                {
                    Neuron[] layer = new Neuron[layerData.neurons.Length];
                    for (int i = 0; i < layerData.neurons.Length; i++)
                    {
                        // Recria o neurônio com o tamanho de pesos correto
                        layer[i] = new Neuron(layerData.neurons[i].weights.Length);
                        layer[i].weights = layerData.neurons[i].weights;
                        layer[i].bias = layerData.neurons[i].bias;
                    }

                    layers.Add(layer);
                }

                Debug.Log("Cérebro carregado e aplicado!");
            }
            else
            {
                Debug.LogWarning("Arquivo de cérebro não encontrado. Inicializando novo.");
                Init();
            }
        }
    }
}