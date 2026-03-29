using UnityEngine;
using System.Collections.Generic;

namespace Neural
{
    /// <summary>
    /// Controlador do Agente com sistema de exploração por velocidade de descoberta de células.
    /// Um agente é eliminado se não descobrir células NOVAS do mapa em intervalos regulares.
    /// Isso torna círculos repetitivos inúteis: após a primeira volta, não há células novas.
    /// </summary>
    public class AgentController : MonoBehaviour
    {
        public NeuralBrain brain;
        public VisionSensor vision;

        [Header("Status")]
        public float fitness = 0;
        public bool  isAlive = true;

        [Header("Movimento")]
        public float speed     = 5f;
        public float turnSpeed = 180f;

        [Header("Exploração por Grid")]
        public float cellSize      = 5f;    // Tamanho da célula (5x5m)
        public float rewardPerCell = 100f;  // Fitness por célula nova

        [Header("Velocidade de Exploração (anti-círculo)")]
        [Tooltip("Intervalo em segundos para verificar o progresso de exploração")]
        public float explorationCheckInterval = 6f;
        [Tooltip("Mínimo de células NOVAS que o agente deve descobrir por intervalo")]
        public int   minNewCellsPerInterval   = 2;

        // Sensor thresholds (viewRadius = 10m, val = 1 - dist/10)
        private const float ThresholdDeath   = 0.975f; // dist < 0.25m → morte
        private const float ThresholdDanger  = 0.900f; // dist < 1.0m  → sem fitness
        private const float ThresholdWarning = 0.700f; // dist < 3.0m  → reflexo ativo

        // Controle de exploração
        private HashSet<Vector2Int> visitedCells    = new HashSet<Vector2Int>();
        private int                 lastCellCount   = 0;
        private float               explorationTimer = 0f;

        /// <summary>Número de células únicas do grid exploradas por este agente.</summary>
        public int ExploredCellCount => visitedCells.Count;

        // ─────────────────────────────────────────────────────────────────────

        void Start()
        {
            if (brain == null) brain = GetComponent<NeuralBrain>();
            brain.Init();
            visitedCells.Add(GetCurrentCell());
        }

        void FixedUpdate()
        {
            if (!isAlive) return;

            // ══════════════════════════════════════════════════════════════
            // BLOCO 1 — Leitura dos sensores + análise direcional de perigo
            // ══════════════════════════════════════════════════════════════
            float[] inputs = vision.GetVisionData();

            float frontThreat = 0f;
            float rightThreat = 0f;
            float leftThreat  = 0f;
            float maxVal      = 0f;

            for (int i = 0; i < inputs.Length; i++)
            {
                float v = inputs[i];
                if (v > maxVal) maxVal = v;

                // MORTE IMEDIATA — distância < 0.25m
                if (v > ThresholdDeath)
                {
                    isAlive = false;
                    return; // Sem fitness
                }

                // Cone frontal: índices 15, 0, 1, 2
                if (i == 0 || i == 1 || i == 2 || i == 15)
                    frontThreat = Mathf.Max(frontThreat, v);

                // Semicírculo esquerdo: 8 a 15
                if (i >= 8 && i <= 15)
                    leftThreat = Mathf.Max(leftThreat, v);

                // Semicírculo direito: 1 a 7
                if (i >= 1 && i <= 7)
                    rightThreat = Mathf.Max(rightThreat, v);
            }

            // ══════════════════════════════════════════════════════════════
            // BLOCO 2 — Verificação de Velocidade de Exploração (anti-círculo)
            // A cada N segundos, verifica se o agente descobriu células NOVAS.
            // Após uma volta completa, não há mais células novas → eliminado.
            // ══════════════════════════════════════════════════════════════
            explorationTimer += Time.fixedDeltaTime;

            if (explorationTimer >= explorationCheckInterval)
            {
                int currentCount = visitedCells.Count;
                int newCells     = currentCount - lastCellCount;

                if (newCells < minNewCellsPerInterval)
                {
                    // Não explorou células novas suficientes → eliminado por estagnação
                    isAlive = false;
                    return;
                }

                lastCellCount    = currentCount;
                explorationTimer = 0f;
            }

            // ══════════════════════════════════════════════════════════════
            // BLOCO 3 — Rede Neural
            // ══════════════════════════════════════════════════════════════
            float[] outputs = brain.Think(inputs);

            // outputs[0] = girar esquerda | outputs[1] = girar direita | outputs[2] = velocidade
            float netSteer = (outputs[1] - outputs[0]);  // -1 a +1
            float netSpeed =  outputs[2];                 //  0 a 1

            // ══════════════════════════════════════════════════════════════
            // BLOCO 4 — Reflexo de Evasão (blend com rede neural)
            // Quanto mais perto do obstáculo, mais o reflexo domina.
            // Perigo à direita → vira à esquerda (e vice-versa).
            // ══════════════════════════════════════════════════════════════
            float reflexSteer   = (leftThreat - rightThreat);
            float reflexBrake   = frontThreat;
            float dangerBlend   = Mathf.Clamp01(maxVal / ThresholdWarning);

            float finalSteer    = Mathf.Lerp(netSteer, reflexSteer, dangerBlend);
            float finalSpeed    = Mathf.Clamp01(netSpeed - reflexBrake * dangerBlend);
            finalSpeed          = Mathf.Max(finalSpeed, 0.15f); // Velocidade mínima garantida

            transform.Rotate(Vector3.up * finalSteer * turnSpeed * Time.fixedDeltaTime);
            transform.Translate(Vector3.forward * finalSpeed * speed * Time.fixedDeltaTime);

            // ══════════════════════════════════════════════════════════════
            // BLOCO 5 — Fitness condicional (zero em zona de perigo)
            // ══════════════════════════════════════════════════════════════
            if (maxVal <= ThresholdDanger)
            {
                // Zona Segura: recompensa exploração genuína
                Vector2Int cell = GetCurrentCell();
                if (!visitedCells.Contains(cell))
                {
                    visitedCells.Add(cell);
                    fitness += rewardPerCell;
                }
                fitness += Time.fixedDeltaTime * 0.5f; // Bônus de sobrevivência limpa
            }
            // Zona de Perigo/Morte → zero fitness. Não recompensa aproximação.
        }

        // ─────────────────────────────────────────────────────────────────────

        Vector2Int GetCurrentCell()
        {
            return new Vector2Int(
                Mathf.FloorToInt(transform.position.x / cellSize),
                Mathf.FloorToInt(transform.position.z / cellSize)
            );
        }

        void OnCollisionEnter(Collision col) => CheckCollision(col.gameObject.layer);
        void OnTriggerEnter(Collider other)  => CheckCollision(other.gameObject.layer);

        void CheckCollision(int layer)
        {
            if (((1 << layer) & vision.detectionMask) != 0)
                isAlive = false;
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.2f);
            foreach (var c in visitedCells)
                Gizmos.DrawCube(
                    new Vector3(c.x * cellSize + cellSize * 0.5f, 0.1f, c.y * cellSize + cellSize * 0.5f),
                    new Vector3(cellSize, 0.05f, cellSize));
        }
    }
}