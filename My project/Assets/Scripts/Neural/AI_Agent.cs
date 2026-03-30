using UnityEngine;

namespace Neural
{
    public class AI_Agent : MonoBehaviour
    {
        public VisionSensor visionSensor;
        public VisionNeuralNetwork brain;

        [Header("Movimentação")]
        public float speed = 7.0f;
        public float rotationSpeed = 100f;
        public float maxValue = -1;

        void FixedUpdate()
        {
            // 1. Coleta os dados dos 16 SphereCasts
            float[] inputs = visionSensor.GetVisionData();

            // 2. Passa para as 4 camadas ocultas da rede
            float[] outputs = brain.ProcessVision(inputs);

            // 3. Mapeia as 16 saídas para comportamentos
            ExecuteActions(outputs);
        }

        void ExecuteActions(float[] outputs)
        {
            // Exemplo de lógica de mapeamento:
            // As primeiras 8 saídas podem ser "Direções de Movimento"
            // As outras 8 podem ser "Velocidade/Ações Especiais"
        
            // Vamos usar a saída mais forte para decidir a rotação
            int bestActionIndex = 0;
            maxValue = -1;

            for (int i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] > maxValue)
                {
                    maxValue = outputs[i] * 1.5f;
                    bestActionIndex = i;
                }
            }

            // Se a ação 0-7 for escolhida, gira para um lado, se 8-15 gira para outro
            float rotationForce = (bestActionIndex < 8) ? -1f : 1f;
            transform.Rotate(Vector3.up * rotationForce * rotationSpeed * Time.deltaTime);
        
            // Move sempre para frente proporcional à "confiança" da rede
            transform.Translate(Vector3.forward * speed * maxValue * Time.deltaTime);
        }
    }
}