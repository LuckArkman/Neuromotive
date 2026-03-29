using UnityEngine;

namespace Neural
{
    public class VisionSensor : MonoBehaviour
    {
        [Header("Configurações do Sensor")]
        public int numSensors = 16;         // Suas 16 entradas
        public float viewRadius = 10f;      // Distância máxima de visão
        public float sphereRadius = 0.5f;   // Largura do "túnel" de visão
        public LayerMask detectionMask;     // O que o agente deve "enxergar"

        /// <summary>
        /// Dispara 16 SphereCasts e retorna um array de 16 floats (0 a 1).
        /// </summary>
        public float[] GetVisionData()
        {
            float[] sensorInputs = new float[numSensors];
            float angleStep = 360f / numSensors;

            for (int i = 0; i < numSensors; i++)
            {
                // Calcula a direção baseada no ângulo atual
                float currentAngle = i * angleStep;
                Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * transform.forward;

                RaycastHit hit;
                // Executa o SphereCast
                if (Physics.SphereCast(transform.position, sphereRadius, direction, out hit, viewRadius, detectionMask))
                {
                    // Normaliza a distância: 1 = encostado no agente, 0 = longe/não viu nada
                    // Isso ajuda a rede neural a processar os dados melhor (valores entre 0 e 1)
                    sensorInputs[i] = 1.0f - (hit.distance / viewRadius);
                }
                else
                {
                    sensorInputs[i] = 0f; // Nada detectado
                }

                // Visualização no Editor do Unity
                Debug.DrawRay(transform.position, direction * viewRadius, sensorInputs[i] > 0 ? Color.red : Color.green);
            }

            return sensorInputs;
        }
    }
}