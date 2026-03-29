using Systems;
using UnityEngine;

namespace Neural
{
    /// <summary>
    /// Câmera top-down que segue o melhor indivíduo da geração atual.
    /// Conecta-se ao EvolutionManager para obter a referência de bestAgent em tempo real.
    /// </summary>
    public class AgentCamera : MonoBehaviour
    {
        [Header("Referência")]
        [Tooltip("Arraste o GameObject que contém o EvolutionManager aqui.")]
        public EvolutionManager evolutionManager;

        [Header("Configuração Top-Down")]
        [Tooltip("Altura da câmera acima do agente (eixo Y).")]
        public float height = 20f;

        [Tooltip("Ângulo de inclinação para frente (0 = ortogonal puro, positivo = perspectiva suave).")]
        [Range(0f, 90f)]
        public float tiltAngle = 10f;

        [Header("Suavização")]
        [Tooltip("Velocidade de suavização do movimento (maior = mais responsivo).")]
        public float followSmoothSpeed = 4f;

        [Tooltip("Velocidade de suavização da rotação ao trocar de alvo.")]
        public float rotateSmoothSpeed = 3f;

        [Header("Fallback (sem agente vivo)")]
        [Tooltip("Posição para onde a câmera volta quando não há agente ativo.")]
        public Vector3 idlePosition = Vector3.zero;

        // Estado interno
        private Vector3         _currentVelocity = Vector3.zero;
        private AgentController _lastKnownAgent;

        // ─────────────────────────────────────────────────────────────────────

        void Awake()
        {
            // Tentativa de auto-detecção se não foi configurado no Inspector
            if (evolutionManager == null)
                evolutionManager = FindObjectOfType<EvolutionManager>();

            if (evolutionManager == null)
                Debug.LogError("[AgentCamera] EvolutionManager não encontrado na cena!");
        }

        void LateUpdate()
        {
            AgentController target = GetCurrentTarget();
            UpdateCameraPosition(target);
            UpdateCameraRotation();
        }

        // ── Obtém o alvo atual ────────────────────────────────────────────────
        AgentController GetCurrentTarget()
        {
            if (evolutionManager == null) return null;

            AgentController best = evolutionManager.bestAgent;

            // Valida se o agente ainda está vivo
            if (best != null && best.isAlive)
            {
                _lastKnownAgent = best;
                return best;
            }

            // Se o bestAgent atual morreu, mantém o último alvo conhecido
            // (evita corte brusco de câmera durante a transição de geração)
            if (_lastKnownAgent != null && _lastKnownAgent.gameObject != null)
                return _lastKnownAgent;

            return null;
        }

        // ── Posição: segue o alvo com SmoothDamp ─────────────────────────────
        void UpdateCameraPosition(AgentController target)
        {
            Vector3 desiredPos;

            if (target != null)
            {
                Vector3 agentPos = target.transform.position;
                desiredPos = new Vector3(agentPos.x, agentPos.y + height, agentPos.z);
            }
            else
            {
                // Sem alvo: retorna à posição de repouso centralizada
                desiredPos = new Vector3(idlePosition.x, idlePosition.y + height, idlePosition.z);
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPos,
                ref _currentVelocity,
                1f / followSmoothSpeed);
        }

        // ── Rotação: ângulo top-down com tilt opcional ────────────────────────
        void UpdateCameraRotation()
        {
            // Orientação alvo: olha para baixo com inclinação suave para frente
            Quaternion targetRot = Quaternion.Euler(90f - tiltAngle, 0f, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * rotateSmoothSpeed);
        }

        // ── Gizmo: visualiza a área de cobertura da câmera no Editor ─────────
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Vector3 groundPos = transform.position;
            groundPos.y = 0f;
            Gizmos.DrawWireSphere(groundPos, 1.5f);
            Gizmos.DrawLine(transform.position, groundPos);
        }
    }
}
