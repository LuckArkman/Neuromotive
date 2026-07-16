using System.Collections.Generic;
using UnityEngine;

namespace Neural
{
    [System.Serializable]
    public class Neuron
    {
        public float[] weights;
        public float bias;

        public Neuron(int inputCount)
        {
            weights = new float[inputCount];
            bias = Random.Range(-1f, 1f);
            for (int i = 0; i < inputCount; i++) weights[i] = Random.Range(-1f, 1f);
        }

        public float Activate(float[] inputs)
        {
            float sum = 0;
            for (int i = 0; i < inputs.Length; i++) sum += inputs[i] * weights[i];
            return 1.0f / (1.0f + Mathf.Exp(-(sum + bias))); // Sigmoid
        }

        // Função de Mutação
        public void Mutate(float rate, float amount)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                if (Random.value < rate) weights[i] += Random.Range(-amount, amount);
            }

            if (Random.value < rate) bias += Random.Range(-amount, amount);
        }
    }
}