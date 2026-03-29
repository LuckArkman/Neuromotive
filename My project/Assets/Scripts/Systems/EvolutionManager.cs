using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neural;
using UnityEngine;

namespace Systems
{
    public class EvolutionManager : MonoBehaviour
    {
        [Header("Configurações de População")]
        public GameObject agentPrefab;
        public Transform  spawnPoint;
        public int        populationSize = 20;

        [Header("Configurações Genéticas")]
        [Range(0f, 1f)] public float mutationRate   = 0.15f;
        [Range(0f, 1f)] public float mutationAmount = 0.20f;

        [Header("Status da Simulação")]
        public int   generationCount   = 1;
        public float BestFitnessEver   = 0;
        public float timer             = 0;
        public float maxGenerationTime = 50f;

        [Header("Melhor Indivíduo")]
        [Tooltip("Referência em tempo real ao agente com maior fitness na geração atual.")]
        public AgentController bestAgent;

        public List<AgentController> agents = new List<AgentController>();

        // ── Visualização do Cérebro ───────────────────────────────────────────
        private Texture2D _circleTex;

        // ═════════════════════════════════════════════════════════════════════
        void Start()
        {
            InitViz();
            LoadGlobalBest();
            StartGeneration();
        }

        void Update()
        {
            timer += Time.deltaTime;

            // Rastreia o melhor agente VIVO em tempo real
            AgentController liveBest    = null;
            float           topFitness  = float.MinValue;
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
        void StartGeneration()
        {
            timer = 0;
            for (int i = 0; i < populationSize; i++)
            {
                Vector3 offset = Quaternion.Euler(0, Random.Range(0, 360), 0) * Vector3.forward * 1.5f;
                var go = Instantiate(agentPrefab, spawnPoint.position + offset, spawnPoint.rotation);
                var ctrl = go.GetComponent<AgentController>();
                ctrl.brain.Init();
                agents.Add(ctrl);
            }
        }

        public void NextGeneration()
        {
            timer = 0;
            var bestOfGen = agents.OrderByDescending(a => a.fitness).FirstOrDefault();

            if (bestOfGen != null)
            {
                Debug.Log($"Geração {generationCount} encerrada. Melhor Fitness: {bestOfGen.fitness:F2}");
                if (bestOfGen.fitness > BestFitnessEver)
                {
                    BestFitnessEver = bestOfGen.fitness;
                    bestOfGen.brain.SaveBestBrain("BestAgent_NeuralData");
                    Debug.Log("<color=green>Novo Recorde Global Salvo!</color>");
                }
            }

            foreach (var a in agents) if (a != null) Destroy(a.gameObject);
            agents.Clear();
            bestAgent = null;

            generationCount++;
            SpawnNewGeneration();
        }

        void SpawnNewGeneration()
        {
            for (int i = 0; i < populationSize; i++)
            {
                Vector3 offset = Quaternion.Euler(0, Random.Range(0, 360), 0) * Vector3.forward * 1.5f;
                var go   = Instantiate(agentPrefab, spawnPoint.position + offset, spawnPoint.rotation);
                var ctrl = go.GetComponent<AgentController>();
                ctrl.brain.LoadBrain("BestAgent_NeuralData");

                if (i > 0)
                {
                    float r = (i > populationSize / 2) ? 0.40f : mutationRate;
                    float a = (i > populationSize / 2) ? 0.60f : mutationAmount;
                    ctrl.brain.ApplyMutation(r, a);
                }
                agents.Add(ctrl);
            }
        }

        void LoadGlobalBest()
        {
            string path = Path.Combine(Application.persistentDataPath, "BestAgent_NeuralData.json");
            if (File.Exists(path))
                Debug.Log("Recorde anterior encontrado e pronto para evolução.");
        }

        // ═════════════════════════════════════════════════════════════════════
        // INTERFACE (OnGUI)
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
            GUI.DrawTexture(new Rect(10, 10, 270, 130), Texture2D.whiteTexture);
            // Borda azul topo
            GUI.color = new Color(0.2f, 0.6f, 1f, 1f);
            GUI.DrawTexture(new Rect(10, 10, 270, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var title = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.4f, 0.85f, 1f);
            GUI.Label(new Rect(20, 16, 250, 20), "PAINEL DE EVOLUÇÃO", title);

            var lbl = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            lbl.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(20, 40,  250, 20), $"Geração:        {generationCount}",          lbl);
            GUI.Label(new Rect(20, 58,  250, 20), $"Tempo:          {timer:F1}s / {maxGenerationTime}s", lbl);
            GUI.Label(new Rect(20, 76,  250, 20), $"Melhor Fitness: {BestFitnessEver:F2}",        lbl);
            GUI.Label(new Rect(20, 94,  250, 20), $"Agentes Vivos:  {agents.Count(a => a != null && a.isAlive)}", lbl);
            if (bestAgent != null)
                GUI.Label(new Rect(20, 112, 250, 20), $"Células Líder:  {bestAgent.ExploredCellCount}", lbl);
        }

        // ── Visualização do Cérebro Neural ───────────────────────────────────
        void DrawBrainViz()
        {
            var brain       = bestAgent.brain;
            var activations = brain.lastActivations; // índice 0 = inputs, 1..5 = camadas

            int   layerCount    = activations.Count;   // Tipicamente 6
            int   neuronCount   = 16;
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
            // Borda superior
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

            // Pré-calcula centros [camada, neurônio]
            var centers = new Vector2[layerCount, neuronCount];
            for (int l = 0; l < layerCount; l++)
                for (int n = 0; n < neuronCount; n++)
                    centers[l, n] = new Vector2(
                        originX + l * layerSpacing + neuronR,
                        originY + n * neuronSpacing + neuronR);

            // ── Conexões ───────────────────────────────────────────────────
            // brain.layers[l] = neurônios da camada l+1, cujos pesos vêm de l
            for (int l = 0; l < layerCount - 1 && l < brain.layers.Count; l++)
            {
                Neuron[] dstNeurons = brain.layers[l];
                float[]  srcActs    = activations[l];
                float[]  dstActs    = activations[l + 1];

                for (int dN = 0; dN < Mathf.Min(dstNeurons.Length, neuronCount); dN++)
                {
                    for (int sN = 0; sN < Mathf.Min(dstNeurons[dN].weights.Length, neuronCount); sN++)
                    {
                        float w      = dstNeurons[dN].weights[sN];
                        float signal = srcActs[sN] * Mathf.Abs(w);
                        if (signal < 0.18f) continue; // Omite conexões fracas

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
            valSt.normal.textColor = new Color(1f, 1f, 1f, 0.75f);

            for (int l = 0; l < layerCount; l++)
            {
                float[] acts = activations[l];

                // Rótulo da camada
                labelSt.normal.textColor = new Color(0.45f, 0.82f, 1f, 0.85f);
                string lname = l < layerLabels.Length ? layerLabels[l] : $"H·{l}";
                GUI.Label(new Rect(centers[l, 0].x - 18f, panelY + 20f, 36f, 15f), lname, labelSt);

                for (int n = 0; n < Mathf.Min(acts.Length, neuronCount); n++)
                {
                    float   act = acts[n];
                    Vector2 c   = centers[l, n];
                    Color   col = GetNeuronColor(act);

                    // Halo (brilho)
                    float hR = neuronR + 4f;
                    GUI.color = new Color(col.r, col.g, col.b, act * 0.32f);
                    GUI.DrawTexture(new Rect(c.x - hR, c.y - hR, hR * 2f, hR * 2f), _circleTex);

                    // Corpo do neurônio
                    GUI.color = col;
                    GUI.DrawTexture(new Rect(c.x - neuronR, c.y - neuronR, neuronR * 2f, neuronR * 2f), _circleTex);

                    // Valor numérico para Input e Output
                    if (l == 0 || l == layerCount - 1)
                    {
                        GUI.color = Color.white;
                        float vx = (l == 0) ? c.x - neuronR - 24f : c.x + neuronR + 2f;
                        GUI.Label(new Rect(vx, c.y - 8f, 22f, 16f), act.ToString("F2"), valSt);
                    }
                }
            }

            // ── Rodapé: fitness e células ──────────────────────────────────
            GUI.color = Color.white;
            var fitSt = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            fitSt.normal.textColor = new Color(0.35f, 1f, 0.45f);
            GUI.Label(
                new Rect(panelX + 8f, panelY + panelH - 24f, panelW - 16f, 20f),
                $"Fit: {bestAgent.fitness:F1}   Grid: {bestAgent.ExploredCellCount} células",
                fitSt);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Helpers
        // ═════════════════════════════════════════════════════════════════════
        static Color GetNeuronColor(float act)
        {
            // 0.0 → azul escuro │ 0.5 → ciano │ 1.0 → dourado
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
            // Textura circular suave (32×32) para representar os neurônios
            const int SZ = 32;
            _circleTex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
            var ctr = new Vector2(SZ / 2f, SZ / 2f);
            float rad = SZ / 2f - 1.5f;
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