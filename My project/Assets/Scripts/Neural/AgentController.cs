using UnityEngine;
using System.Collections.Generic;

namespace Neural
{
    /// <summary>
    /// Controlador do Agente com LSTM — sistema de exploração por velocidade de
    /// descoberta de células.
    ///
    /// Mudanças em relação à versão Feed-Forward:
    ///   • Usa LSTMBrain em vez de NeuralBrain.
    ///   • A memória LSTM (h, c) é resetada no início de cada vida (ResetMemory).
    ///   • Os PESOS são herdados do melhor agente da geração anterior via InheritFrom.
    /// </summary>
    public class AgentController : MonoBehaviour
    {
        public LSTMBrain brain;
        public VisionSensor vision;

        [Header("Status")]
        public float fitness = 0;
        public bool  isAlive = true;

        [Header("Movimento")]
        public float speed     = 5f;
        public float turnSpeed = 180f;

        [Header("Exploração por Grid")]
        public float cellSize      = 5f;
        public float rewardPerCell = 100f;

        [Header("Velocidade de Exploração (anti-círculo)")]
        [Tooltip("Intervalo em segundos para verificar o progresso de exploração")]
        public float explorationCheckInterval = 6f;
        [Tooltip("Mínimo de células NOVAS que o agente deve descobrir por intervalo")]
        public int   minNewCellsPerInterval   = 2;

        // ── Thresholds do sensor (viewRadius = 10m) ──────────────────────────
        private const float ThresholdDeath   = 0.975f;
        private const float ThresholdDanger  = 0.900f;
        private const float ThresholdWarning = 0.700f;

        // ── Controle de exploração ─────────────────────────────────────────
        private HashSet<Vector2Int> visitedCells    = new HashSet<Vector2Int>();
        private int                 lastCellCount   = 0;
        private float               explorationTimer = 0f;

        /// <summary>Número de células únicas exploradas por este agente.</summary>
        public int ExploredCellCount => visitedCells.Count;

        // ─────────────────────────────────────────────────────────────────────

        void Start()
        {
            if (brain == null) brain = GetComponent<LSTMBrain>();

            // Inicializa as 4 camadas LSTM com pesos aleatórios.
            // (O EvolutionManager sobrescreverá com herança genética logo após.)
            brain.Init();

            // Reset da memória episódica — pesos são mantidos, h/c voltam a 0.
            brain.ResetMemory();
            visitedCells.Add(GetCurrentCell());
        }

        void FixedUpdate()
        {
            if (!isAlive) return;

            // ══════════════════════════════════════════════════════════════════
            // BLOCO 1 — Leitura dos sensores + análise de perigo direcional
            // ══════════════════════════════════════════════════════════════════
            float[] inputs = vision.GetVisionData();

            float frontThreat = 0f, rightThreat = 0f, leftThreat = 0f, maxVal = 0f;

            for (int i = 0; i < inputs.Length; i++)
            {
                float v = inputs[i];
                if (v > maxVal) maxVal = v;

                // MORTE IMEDIATA — dist < 0.25m
                if (v > ThresholdDeath) { isAlive = false; return; }

                if (i == 0 || i == 1 || i == 2 || i == 15)
                    frontThreat = Mathf.Max(frontThreat, v);
                if (i >= 8 && i <= 15)
                    leftThreat  = Mathf.Max(leftThreat,  v);
                if (i >= 1 && i <= 7)
                    rightThreat = Mathf.Max(rightThreat, v);
            }

            // ══════════════════════════════════════════════════════════════════
            // BLOCO 2 — Kill Switch de Estagnação (anti-círculo)
            // ══════════════════════════════════════════════════════════════════
            explorationTimer += Time.fixedDeltaTime;
            if (explorationTimer >= explorationCheckInterval)
            {
                int newCells = visitedCells.Count - lastCellCount;
                if (newCells < minNewCellsPerInterval) { isAlive = false; return; }
                lastCellCount    = visitedCells.Count;
                explorationTimer = 0f;
            }

            // ══════════════════════════════════════════════════════════════════
            // BLOCO 3 — Inferência LSTM
            // A LSTM recebe os 16 sensores e retorna [steerL, steerR, speed].
            // O estado interno (h,c) é mantido frame a frame — o agente
            // "lembra" o que viu nos frames anteriores.
            // ══════════════════════════════════════════════════════════════════
            float[] outputs = brain.Think(inputs);

            // outputs[0] = girar esquerda | outputs[1] = girar direita | outputs[2] = velocidade
            float netSteer = outputs[1] - outputs[0];  // -1 a +1
            float netSpeed = outputs[2];               //  0 a  1

            // ══════════════════════════════════════════════════════════════════
            // BLOCO 4 — Reflexo de Evasão (blend com inferência da LSTM)
            // ══════════════════════════════════════════════════════════════════
            float reflexSteer = leftThreat - rightThreat;
            float reflexBrake = frontThreat;
            float dangerBlend = Mathf.Clamp01(maxVal / ThresholdWarning);

            float finalSteer = Mathf.Lerp(netSteer, reflexSteer, dangerBlend);
            float finalSpeed = Mathf.Clamp01(netSpeed - reflexBrake * dangerBlend);
            finalSpeed       = Mathf.Max(finalSpeed, 0.15f); // velocidade mínima garantida

            transform.Rotate(Vector3.up * finalSteer * turnSpeed * Time.fixedDeltaTime);
            transform.Translate(Vector3.forward * finalSpeed * speed * Time.fixedDeltaTime);

            // ══════════════════════════════════════════════════════════════════
            // BLOCO 5 — Fitness condicional (zero em zona de perigo)
            // ══════════════════════════════════════════════════════════════════
            if (maxVal <= ThresholdDanger)
            {
                Vector2Int cell = GetCurrentCell();
                if (!visitedCells.Contains(cell))
                {
                    visitedCells.Add(cell);
                    fitness += rewardPerCell;
                }
                fitness += Time.fixedDeltaTime * 0.5f;
            }
        }

        // ─────────────────────────────────────────────────────────────────────

        Vector2Int GetCurrentCell() => new Vector2Int(
            Mathf.FloorToInt(transform.position.x / cellSize),
            Mathf.FloorToInt(transform.position.z / cellSize));

        void OnCollisionEnter(Collision col) => CheckCollision(col.gameObject.layer);
        void OnTriggerEnter(Collider other)  => CheckCollision(other.gameObject.layer);

        void CheckCollision(int layer)
        {
            if (((1 << layer) & vision.detectionMask) != 0) isAlive = false;
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