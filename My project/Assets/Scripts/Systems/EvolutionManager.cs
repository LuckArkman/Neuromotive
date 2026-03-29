using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neural;
using UnityEngine;

namespace Systems
{
    public class EvolutionManager : MonoBehaviour
    {
        // ── Configurações de População ────────────────────────────────────────
        [Header("Configurações de População")]
        public GameObject agentPrefab;
        public Transform  spawnPoint;
        public int        populationSize = 20;

        // ── Configurações Genéticas ──────────────────────────────────────────
        [Header("Configurações Genéticas")]
        [Range(0f, 1f)] public float mutationRate   = 0.15f;
        [Range(0f, 1f)] public float mutationAmount = 0.20f;

        // ── Status da Simulação ───────────────────────────────────────────────
        [Header("Status da Simulação")]
        public int   generationCount   = 1;
        public float BestFitnessEver   = 0;
        public float timer             = 0;
        public float maxGenerationTime = 50f;

        // ── Melhor Indivíduo ──────────────────────────────────────────────────
        [Header("Melhor Indivíduo")]
        [Tooltip("Referência em tempo real ao agente com maior fitness na geração atual.")]
        public AgentController bestAgent;

        public List<AgentController> agents = new List<AgentController>();

        // ── Nomes dos arquivos de save ────────────────────────────────────────
        // "BestGen"    → melhor agente da geração atual (sobrescrito a cada nova geração)
        // "BestEver"   → melhor agente de TODAS as gerações (sobrescrito só quando bate recorde)
        private const string SaveNameBestGen  = "LSTM_BestGen";
        private const string SaveNameBestEver = "LSTM_BestEver";

        // ── Cérebro "semente" da geração anterior ─────────────────────────────
        // Armazenado como dados de save para não manter referências mortas.
        private LSTMBrainSaveData _inheritedBrain;
        private bool              _hasInheritedBrain = false;

        // ── Visualização ──────────────────────────────────────────────────────
        private Texture2D _circleTex;

        // ═════════════════════════════════════════════════════════════════════
        void Start()
        {
            InitViz();
            TryLoadGlobalBest();
            StartGeneration();
        }

        void Update()
        {
            timer += Time.deltaTime;

            // Rastreia o melhor agente VIVO em tempo real
            AgentController liveBest   = null;
            float           topFitness = float.MinValue;
            foreach (var a in agents)
            {
                if (a != null && a.isAlive && a.fitness > topFitness)
                {
                    topFitness = a.fitness;
                    liveBest   = a;
                }
            }
            if (liveBest != null) bestAgent = liveBest;

            if (agents.All(a => !a.isAlive) || timer >= maxGenerationTime)
                NextGeneration();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Geração
        // ─────────────────────────────────────────────────────────────────────

        void StartGeneration()
        {
            timer = 0;
            for (int i = 0; i < populationSize; i++)
            {
                var ctrl = SpawnAgent();

                if (_hasInheritedBrain)
                {
                    // ── Herança: todos recebem os pesos do melhor da geração anterior ──
                    ctrl.brain.FromSaveData(_inheritedBrain);

                    // O clone #0 é o agente elite — sem mutação
                    if (i > 0)
                    {
                        float r = (i > populationSize / 2) ? 0.40f : mutationRate;
                        float a = (i > populationSize / 2) ? 0.60f : mutationAmount;
                        ctrl.brain.ApplyMutation(r, a);
                    }
                }
                else
                {
                    // Primeira geração: inicializa aleatório
                    // Tenta carregar um save anterior de Resources/persistentDataPath
                    bool loaded = ctrl.brain.Load(SaveNameBestGen);
                    if (!loaded) ctrl.brain.Init();

                    if (i > 0) ctrl.brain.ApplyMutation(mutationRate, mutationAmount);
                }

                agents.Add(ctrl);
            }

            Debug.Log($"<color=yellow>[Geração {generationCount}] {populationSize} agentes inicializados.</color>");
        }

        public void NextGeneration()
        {
            timer = 0;

            // ── Seleciona o melhor ────────────────────────────────────────────
            var bestOfGen = agents.OrderByDescending(a => a.fitness).FirstOrDefault();

            if (bestOfGen != null && bestOfGen.brain != null)
            {
                Debug.Log($"Geração {generationCount} encerrada. " +
                          $"Melhor Fitness: {bestOfGen.fitness:F2} | " +
                          $"Células: {bestOfGen.ExploredCellCount}");

                // ── Salva o melhor desta geração em Resources ─────────────────
                bestOfGen.brain.Save(SaveNameBestGen, bestOfGen.fitness, generationCount);

                // ── Armazena os pesos para herança ────────────────────────────
                _inheritedBrain    = bestOfGen.brain.ToSaveData(bestOfGen.fitness, generationCount);
                _hasInheritedBrain = true;

                // ── Atualiza o Melhor de Todos os Tempos ──────────────────────
                if (bestOfGen.fitness > BestFitnessEver)
                {
                    BestFitnessEver = bestOfGen.fitness;
                    bestOfGen.brain.Save(SaveNameBestEver, bestOfGen.fitness, generationCount);
                    Debug.Log("<color=green>[LSTM] Novo Recorde Global! Rede salva em Resources.</color>");
                }
            }

            // ── Destrói agentes antigos ────────────────────────────────────────
            foreach (var a in agents) if (a != null) Destroy(a.gameObject);
            agents.Clear();
            bestAgent = null;

            generationCount++;
            StartGeneration();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Spawn
        // ─────────────────────────────────────────────────────────────────────

        AgentController SpawnAgent()
        {
            Vector3 offset = Quaternion.Euler(0, Random.Range(0, 360), 0) * Vector3.forward * 1.5f;
            var go   = Instantiate(agentPrefab, spawnPoint.position + offset, spawnPoint.rotation);
            var ctrl = go.GetComponent<AgentController>();
            return ctrl;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Boot — tenta carregar o save global ao abrir a cena
        // ─────────────────────────────────────────────────────────────────────

        void TryLoadGlobalBest()
        {
            // Verifica se existe um "BestEver" em Resources ou persistentDataPath
            var textAsset = Resources.Load<TextAsset>(SaveNameBestEver);
            string path   = Path.Combine(Application.persistentDataPath, SaveNameBestEver + ".json");

            if (textAsset != null || File.Exists(path))
            {
                // Cria um brain temporário só para ler os dados
                var tempGO    = new GameObject("__TempBrain");
                var tempBrain = tempGO.AddComponent<LSTMBrain>();
                tempBrain.inputSize  = 16;
                tempBrain.hiddenSize = 16;
                tempBrain.outputSize = 3;

                bool ok = tempBrain.Load(SaveNameBestEver);
                if (ok)
                {
                    _inheritedBrain    = tempBrain.ToSaveData();
                    _hasInheritedBrain = true;
                    BestFitnessEver    = _inheritedBrain.fitness;
                    Debug.Log($"<color=cyan>[LSTM] Recorde anterior carregado! " +
                              $"Fitness: {BestFitnessEver:F2} | Geração: {_inheritedBrain.generation}</color>");
                }
                Destroy(tempGO);
            }
            else
            {
                Debug.Log("[LSTM] Nenhum save anterior encontrado. Iniciando evolução do zero.");
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // INTERFACE (OnGUI) — mantida e atualizada para LSTM
        // ═════════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            DrawStatusPanel();

            if (bestAgent != null
                && bestAgent.brain != null
                && bestAgent.brain.lastActivations != null
                && bestAgent.brain.lastActivations.Count >= 2)
            {
                DrawBrainViz();
            }
        }

        // ── Painel de Status ─────────────────────────────────────────────────
        void DrawStatusPanel()
        {
            GUI.color = new Color(0.03f, 0.05f, 0.10f, 0.92f);
            GUI.DrawTexture(new Rect(10, 10, 290, 155), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.6f, 1f, 1f);
            GUI.DrawTexture(new Rect(10, 10, 290, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var title = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.4f, 0.85f, 1f);
            GUI.Label(new Rect(20, 16, 270, 20), "LSTM — PAINEL DE EVOLUÇÃO");

            var lbl = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            lbl.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(20, 40,  270, 20), $"Geração:         {generationCount}",              lbl);
            GUI.Label(new Rect(20, 58,  270, 20), $"Tempo:           {timer:F1}s / {maxGenerationTime}s", lbl);
            GUI.Label(new Rect(20, 76,  270, 20), $"Melhor Fitness:  {BestFitnessEver:F2}",           lbl);
            GUI.Label(new Rect(20, 94,  270, 20), $"Agentes Vivos:   {agents.Count(a => a != null && a.isAlive)}", lbl);
            if (bestAgent != null)
            {
                GUI.Label(new Rect(20, 112, 270, 20), $"Células Líder:   {bestAgent.ExploredCellCount}", lbl);
                GUI.Label(new Rect(20, 130, 270, 20), $"Fitness Líder:   {bestAgent.fitness:F1}",        lbl);
            }

            // Badge LSTM
            var badge = new GUIStyle(GUI.skin.label) { fontSize = 9, fontStyle = FontStyle.Bold };
            badge.normal.textColor = new Color(0.4f, 1f, 0.6f, 0.8f);
            GUI.Label(new Rect(210, 16, 80, 20), "", badge);
        }

        // ── Visualização do Cérebro LSTM (estilo original) ───────────────────
        void DrawBrainViz()
        {
            var brain = bestAgent.brain;

            // ── Padeia cada array de ativação para 16 neurônios ─────────────
            // A camada OUTPUT tem só 3 saídas reais; as restantes ficam a 0.
            const int neuronCount = 16;
            var rawActs    = brain.lastActivations;
            var activations = new System.Collections.Generic.List<float[]>(rawActs.Count);
            foreach (var a in rawActs)
            {
                if (a.Length >= neuronCount)
                {
                    activations.Add(a);
                }
                else
                {
                    var padded = new float[neuronCount]; // demais = 0f
                    System.Array.Copy(a, padded, a.Length);
                    activations.Add(padded);
                }
            }

            int   layerCount    = activations.Count;
            float neuronR       = 8f;
            float neuronSpacing = 27f;
            float layerSpacing  = 68f;

            float panelW = 36f + (layerCount - 1) * layerSpacing + 36f;
            float panelH = 48f + neuronCount * neuronSpacing + 28f;
            float panelX = Screen.width  - panelW - 10f;
            float panelY = 10f;


            // ── Fundo ──────────────────────────────────────────────────────
            GUI.color = new Color(0.03f, 0.04f, 0.09f, 0.95f);
            GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), Texture2D.whiteTexture);
            GUI.color = new Color(0.15f, 0.55f, 0.95f, 1f);
            GUI.DrawTexture(new Rect(panelX, panelY, panelW, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ── Título ─────────────────────────────────────────────────────
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.45f, 0.82f, 1f);
            GUI.Label(new Rect(panelX, panelY + 4f, panelW, 18f),
                      "CÉREBRO — MELHOR INDIVÍDUO", titleStyle);

            float originX = panelX + 22f;
            float originY = panelY + 38f;

            // ── Array 2D de centros [camada, neurônio] (estilo original) ───
            var centers = new Vector2[layerCount, neuronCount];
            for (int l = 0; l < layerCount; l++)
                for (int n = 0; n < neuronCount; n++)
                    centers[l, n] = new Vector2(
                        originX + l * layerSpacing + neuronR,
                        originY + n * neuronSpacing + neuronR);

            // ── Conexões com peso real (azul=positivo, vermelho=negativo) ──
            for (int l = 0; l < layerCount - 1; l++)
            {
                float[] srcActs = activations[l];
                float[] dstActs = activations[l + 1];

                int dstNeurons = Mathf.Min(dstActs.Length, neuronCount);
                int srcNeurons = Mathf.Min(srcActs.Length, neuronCount);

                for (int dN = 0; dN < dstNeurons; dN++)
                {
                    for (int sN = 0; sN < srcNeurons; sN++)
                    {
                        float w      = GetLSTMWeight(brain, l, dN, sN);
                        float signal = srcActs[sN] * Mathf.Abs(w);
                        if (signal < 0.18f) continue;

                        Color c = w > 0
                            ? new Color(0.20f, 0.60f, 1.00f, signal * 0.50f)
                            : new Color(1.00f, 0.28f, 0.20f, signal * 0.50f);
                        DrawLine(centers[l, sN], centers[l + 1, dN], c, 1f);
                    }
                }
            }

            // ── Neurônios ──────────────────────────────────────────────────
            string[] layerLabels = { "INPUT", "H·1", "H·2", "H·3", "H·4", "OUTPUT" };
            var labelSt = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleCenter };
            var valSt   = new GUIStyle(GUI.skin.label) { fontSize = 8 };
            valSt.normal.textColor = new Color(1f, 1f, 1f, 1f);

            for (int l = 0; l < layerCount; l++)
            {
                float[] acts = activations[l];

                labelSt.normal.textColor = new Color(0.45f, 0.82f, 1f, 0.85f);
                string lname = l < layerLabels.Length ? layerLabels[l] : $"L{l}";
                GUI.Label(new Rect(centers[l, 0].x - 18f, panelY + 20f, 36f, 15f), lname, labelSt);

                for (int n = 0; n < Mathf.Min(acts.Length, neuronCount); n++)
                {
                    float   act = acts[n];
                    Vector2 cc  = centers[l, n];
                    Color   col = GetNeuronColor(act);

                    // Halo (brilho)
                    float hR = neuronR + 4f;
                    GUI.color = new Color(col.r, col.g, col.b, act * 0.32f);
                    GUI.DrawTexture(new Rect(cc.x - hR, cc.y - hR, hR * 2f, hR * 2f), _circleTex);

                    // Corpo do neurônio
                    GUI.color = col;
                    GUI.DrawTexture(new Rect(cc.x - neuronR, cc.y - neuronR, neuronR * 2f, neuronR * 2f), _circleTex);

                    // Valor numérico: input (esquerda) e output (direita)
                    if (l == 0 || l == layerCount - 1)
                    {
                        GUI.color = Color.white;
                        float vx = (l == 0) ? cc.x - neuronR - 24f : cc.x + neuronR + 2f;
                        GUI.Label(new Rect(vx, cc.y - 8f, 22f, 16f), act.ToString("F2"), valSt);
                    }
                }
            }

            // ── Rodapé ────────────────────────────────────────────────────
            GUI.color = Color.white;
            var fitSt = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            fitSt.normal.textColor = new Color(0.35f, 1f, 0.45f);
            GUI.Label(
                new Rect(panelX + 8f, panelY + panelH - 24f, panelW - 16f, 20f),
                $"Fit: {bestAgent.fitness:F1}   Grid: {bestAgent.ExploredCellCount} células",
                fitSt);
        }

        /// <summary>
        /// Retorna o peso representativo da LSTM para colorir a conexão
        /// entre a camada <paramref name="fromLayer"/> e a seguinte.
        /// Usa o gate de OUTPUT (Wo/Uo) por ser o mais interpretável visualmente.
        /// </summary>
        static float GetLSTMWeight(LSTMBrain brain, int fromLayer, int dstN, int srcN)
        {
            switch (fromLayer)
            {
                case 0: // INPUT → H·1  — pesos de entrada do gate Output
                    if (brain.layer1 == null || brain.layer1.Wo == null) return 0f;
                    int i0 = dstN * brain.layer1.inputSize + srcN;
                    return i0 < brain.layer1.Wo.Length ? brain.layer1.Wo[i0] : 0f;

                case 1: // H·1 → H·2  — pesos recorrentes do gate Output
                    if (brain.layer2 == null || brain.layer2.Uo == null) return 0f;
                    int i1 = dstN * brain.layer2.hiddenSize + srcN;
                    return i1 < brain.layer2.Uo.Length ? brain.layer2.Uo[i1] : 0f;

                case 2: // H·2 → H·3  — pesos recorrentes do gate Output
                    if (brain.layer3 == null || brain.layer3.Uo == null) return 0f;
                    int i2 = dstN * brain.layer3.hiddenSize + srcN;
                    return i2 < brain.layer3.Uo.Length ? brain.layer3.Uo[i2] : 0f;

                case 3: // H·3 → H·4  — pesos recorrentes do gate Output
                    if (brain.layer4 == null || brain.layer4.Uo == null) return 0f;
                    int i3 = dstN * brain.layer4.hiddenSize + srcN;
                    return i3 < brain.layer4.Uo.Length ? brain.layer4.Uo[i3] : 0f;

                case 4: // H·4 → OUTPUT  — camada densa
                    if (brain.denseWeights == null) return 0f;
                    int i4 = dstN * brain.hiddenSize + srcN;
                    return i4 < brain.denseWeights.Length ? brain.denseWeights[i4] : 0f;

                default:
                    return 0f;
            }
        }


        // ═════════════════════════════════════════════════════════════════════
        // Helpers
        // ═════════════════════════════════════════════════════════════════════

        static Color GetNeuronColor(float act)
        {
            return act < 0.5f
                ? Color.Lerp(new Color(0.08f, 0.10f, 0.45f), new Color(0.00f, 0.75f, 0.90f), act * 2f)
                : Color.Lerp(new Color(0.00f, 0.75f, 0.90f), new Color(1.00f, 0.85f, 0.05f), (act - 0.5f) * 2f);
        }

        static void DrawLine(Vector2 start, Vector2 end, Color color, float width = 1f)
        {
            if (start == end) return;
            var saved = GUI.matrix;
            GUI.color = color;
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(
                new Rect(start.x, start.y - width * 0.5f, Vector2.Distance(start, end), width),
                Texture2D.whiteTexture);
            GUI.matrix = saved;
            GUI.color  = Color.white;
        }

        void InitViz()
        {
            const int SZ = 32;
            _circleTex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
            var   ctr  = new Vector2(SZ / 2f, SZ / 2f);
            float rad  = SZ / 2f - 1.5f;
            for (int y = 0; y < SZ; y++)
                for (int x = 0; x < SZ; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), ctr);
                    float a = d < rad ? 1f : Mathf.Clamp01(1f - (d - rad) / 1.8f);
                    _circleTex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            _circleTex.Apply();
        }
    }
}