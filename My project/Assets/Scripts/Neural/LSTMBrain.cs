using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Neural
{
    // ═════════════════════════════════════════════════════════════════════════
    // ESTRUTURAS DE SERIALIZAÇÃO
    // ═════════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class LSTMCellSaveData
    {
        public float[] Wf, Wi, Wg, Wo;
        public float[] Uf, Ui, Ug, Uo;
        public float[] bf, bi, bg, bo;
    }

    [System.Serializable]
    public class LSTMLayerSaveData
    {
        public int              inputSize;
        public int              hiddenSize;
        public LSTMCellSaveData cell;
    }

    [System.Serializable]
    public class LSTMOutputLayerSaveData
    {
        public float[] weights;
        public float[] biases;
        public int     inputSize;
        public int     outputSize;
    }

    [System.Serializable]
    public class LSTMBrainSaveData
    {
        public List<LSTMLayerSaveData> lstmLayers  = new List<LSTMLayerSaveData>();
        public LSTMOutputLayerSaveData outputLayer;
        public float                   fitness;
        public int                     generation;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CÉLULA LSTM
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Célula LSTM com gates Forget, Input, Candidate e Output.
    /// Mantém h (hidden state) e c (cell state) persistentes entre frames.
    /// </summary>
    [System.Serializable]
    public class LSTMCell
    {
        public int inputSize;
        public int hiddenSize;

        // Pesos input→gate  [hiddenSize × inputSize]
        public float[] Wf, Wi, Wg, Wo;
        // Pesos hidden→gate [hiddenSize × hiddenSize]
        public float[] Uf, Ui, Ug, Uo;
        // Biases [hiddenSize]
        public float[] bf, bi, bg, bo;
        // Estado interno persistente
        public float[] h;   // hidden state
        public float[] c;   // cell state

        public LSTMCell(int inSize, int hidSize)
        {
            inputSize  = inSize;
            hiddenSize = hidSize;

            Wf = XavierMatrix(hidSize, inSize);  Wi = XavierMatrix(hidSize, inSize);
            Wg = XavierMatrix(hidSize, inSize);  Wo = XavierMatrix(hidSize, inSize);
            Uf = XavierMatrix(hidSize, hidSize); Ui = XavierMatrix(hidSize, hidSize);
            Ug = XavierMatrix(hidSize, hidSize); Uo = XavierMatrix(hidSize, hidSize);

            bf = new float[hidSize]; bi = new float[hidSize];
            bg = new float[hidSize]; bo = new float[hidSize];
            h  = new float[hidSize]; c  = new float[hidSize];
        }

        // ── Reset de memória ─────────────────────────────────────────────────
        public void ResetState()
        {
            for (int i = 0; i < hiddenSize; i++) { h[i] = 0f; c[i] = 0f; }
        }

        // ── Forward pass (gates inline, sem delegates) ───────────────────────
        public float[] Forward(float[] x)
        {
            for (int i = 0; i < hiddenSize; i++)
            {
                float fSum = bf[i], iSum = bi[i], gSum = bg[i], oSum = bo[i];

                for (int j = 0; j < inputSize; j++)
                {
                    int k = i * inputSize + j;
                    fSum += Wf[k] * x[j]; iSum += Wi[k] * x[j];
                    gSum += Wg[k] * x[j]; oSum += Wo[k] * x[j];
                }
                for (int j = 0; j < hiddenSize; j++)
                {
                    int k = i * hiddenSize + j;
                    fSum += Uf[k] * h[j]; iSum += Ui[k] * h[j];
                    gSum += Ug[k] * h[j]; oSum += Uo[k] * h[j];
                }

                float fGate = Sigmoid(fSum);
                float iGate = Sigmoid(iSum);
                float gGate = TanhSafe(gSum);
                float oGate = Sigmoid(oSum);

                c[i] = fGate * c[i] + iGate * gGate;
                h[i] = oGate * TanhSafe(c[i]);
            }
            return h;
        }

        // ── Ativações ────────────────────────────────────────────────────────
        static float Sigmoid(float x)
        {
            float xc = Mathf.Clamp(x, -15f, 15f);
            return 1f / (1f + Mathf.Exp(-xc));
        }

        static float TanhSafe(float x)
        {
            float xc = Mathf.Clamp(x, -15f, 15f);
            float e2  = Mathf.Exp(2f * xc);
            return (e2 - 1f) / (e2 + 1f);
        }

        // ── Inicialização Xavier ─────────────────────────────────────────────
        static float[] XavierMatrix(int rows, int cols)
        {
            float limit = Mathf.Sqrt(6f / (rows + cols));
            float[] m   = new float[rows * cols];
            for (int i = 0; i < m.Length; i++)
                m[i] = Random.Range(-limit, limit);
            return m;
        }

        // ── Mutação genética ─────────────────────────────────────────────────
        public void Mutate(float rate, float amount)
        {
            MutArr(Wf, rate, amount); MutArr(Wi, rate, amount);
            MutArr(Wg, rate, amount); MutArr(Wo, rate, amount);
            MutArr(Uf, rate, amount); MutArr(Ui, rate, amount);
            MutArr(Ug, rate, amount); MutArr(Uo, rate, amount);
            MutArr(bf, rate, amount); MutArr(bi, rate, amount);
            MutArr(bg, rate, amount); MutArr(bo, rate, amount);
        }

        static void MutArr(float[] arr, float rate, float amount)
        {
            for (int i = 0; i < arr.Length; i++)
                if (Random.value < rate)
                    arr[i] += Random.Range(-amount, amount);
        }

        // ── Serialização ─────────────────────────────────────────────────────
        public LSTMCellSaveData ToSaveData()
        {
            return new LSTMCellSaveData
            {
                Wf = (float[])Wf.Clone(), Wi = (float[])Wi.Clone(),
                Wg = (float[])Wg.Clone(), Wo = (float[])Wo.Clone(),
                Uf = (float[])Uf.Clone(), Ui = (float[])Ui.Clone(),
                Ug = (float[])Ug.Clone(), Uo = (float[])Uo.Clone(),
                bf = (float[])bf.Clone(), bi = (float[])bi.Clone(),
                bg = (float[])bg.Clone(), bo = (float[])bo.Clone()
            };
        }

        public void FromSaveData(LSTMCellSaveData d)
        {
            Wf = (float[])d.Wf.Clone(); Wi = (float[])d.Wi.Clone();
            Wg = (float[])d.Wg.Clone(); Wo = (float[])d.Wo.Clone();
            Uf = (float[])d.Uf.Clone(); Ui = (float[])d.Ui.Clone();
            Ug = (float[])d.Ug.Clone(); Uo = (float[])d.Uo.Clone();
            bf = (float[])d.bf.Clone(); bi = (float[])d.bi.Clone();
            bg = (float[])d.bg.Clone(); bo = (float[])d.bo.Clone();
            ResetState();
        }

        public void CopyWeightsTo(LSTMCell target)
        {
            System.Array.Copy(Wf, target.Wf, Wf.Length);
            System.Array.Copy(Wi, target.Wi, Wi.Length);
            System.Array.Copy(Wg, target.Wg, Wg.Length);
            System.Array.Copy(Wo, target.Wo, Wo.Length);
            System.Array.Copy(Uf, target.Uf, Uf.Length);
            System.Array.Copy(Ui, target.Ui, Ui.Length);
            System.Array.Copy(Ug, target.Ug, Ug.Length);
            System.Array.Copy(Uo, target.Uo, Uo.Length);
            System.Array.Copy(bf, target.bf, bf.Length);
            System.Array.Copy(bi, target.bi, bi.Length);
            System.Array.Copy(bg, target.bg, bg.Length);
            System.Array.Copy(bo, target.bo, bo.Length);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LSTM BRAIN — MONOBEHAVIOUR
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cérebro LSTM do agente.
    /// Arquitetura (idêntica ao NeuralBrain original):
    ///   INPUT(16) → LSTM·1(16) → LSTM·2(16) → LSTM·3(16) → LSTM·4(16) → Dense(16)
    ///
    /// 4 camadas ocultas LSTM — memória episódica persiste durante toda a
    /// vida do agente; pesos são herdados e mutados geneticamente por geração.
    /// </summary>
    public class LSTMBrain : MonoBehaviour
    {
        [Header("Arquitetura")]
        public int inputSize  = 16;
        public int hiddenSize = 16;
        public int outputSize = 16;  // igual ao NeuralBrain original

        // ── 4 Camadas LSTM ocultas (espelhando H·1, H·2, H·3, H·4) ──────────
        public LSTMCell layer1;
        public LSTMCell layer2;
        public LSTMCell layer3;
        public LSTMCell layer4;

        // ── Camada densa de saída [outputSize × hiddenSize] ──────────────────
        public float[] denseWeights;
        public float[] denseBiases;

        /// <summary>Ativações por camada para o visualizador.</summary>
        public List<float[]> lastActivations = new List<float[]>();

        // ── Inicialização aleatória ───────────────────────────────────────────
        public void Init()
        {
            layer1 = new LSTMCell(inputSize,  hiddenSize);  // H·1: input  → hidden
            layer2 = new LSTMCell(hiddenSize, hiddenSize);  // H·2: hidden → hidden
            layer3 = new LSTMCell(hiddenSize, hiddenSize);  // H·3: hidden → hidden
            layer4 = new LSTMCell(hiddenSize, hiddenSize);  // H·4: hidden → hidden

            float limit = Mathf.Sqrt(6f / (hiddenSize + outputSize));
            denseWeights = new float[outputSize * hiddenSize];
            denseBiases  = new float[outputSize];
            for (int i = 0; i < denseWeights.Length; i++)
                denseWeights[i] = Random.Range(-limit, limit);
        }

        // ── Reset de memória episódica (chamado no início de cada vida) ───────
        public void ResetMemory()
        {
            layer1?.ResetState();
            layer2?.ResetState();
            layer3?.ResetState();
            layer4?.ResetState();
        }

        // ── Inferência ────────────────────────────────────────────────────────
        /// <summary>
        /// Processa os sensores; retorna 16 saídas.
        /// AgentController usa apenas [0]=Esq, [1]=Dir, [2]=Vel.
        /// </summary>
        public float[] Think(float[] inputs)
        {
            // Guard: inicializa se as camadas estiverem nulas ou mal dimensionadas
            // (pode acontecer quando o prefab tem dados serializados antigos).
            if (layer1 == null || layer4 == null ||
                denseWeights == null || denseWeights.Length < outputSize * hiddenSize ||
                denseBiases  == null || denseBiases.Length  < outputSize)
            {
                Init();
            }

            lastActivations.Clear();
            lastActivations.Add((float[])inputs.Clone());   // [0] INPUT

            float[] h1 = layer1.Forward(inputs);
            lastActivations.Add((float[])h1.Clone());       // [1] H·1

            float[] h2 = layer2.Forward(h1);
            lastActivations.Add((float[])h2.Clone());       // [2] H·2

            float[] h3 = layer3.Forward(h2);
            lastActivations.Add((float[])h3.Clone());       // [3] H·3

            float[] h4 = layer4.Forward(h3);
            lastActivations.Add((float[])h4.Clone());       // [4] H·4

            // Camada densa de saída (sigmoid)
            float[] output = new float[outputSize];
            for (int o = 0; o < outputSize; o++)
            {
                float sum = denseBiases[o];
                for (int j = 0; j < hiddenSize; j++)
                    sum += denseWeights[o * hiddenSize + j] * h4[j];
                output[o] = 1f / (1f + Mathf.Exp(-Mathf.Clamp(sum, -15f, 15f)));
            }
            lastActivations.Add((float[])output.Clone());   // [5] OUTPUT

            return output;
        }

        // ── Mutação genética ─────────────────────────────────────────────────
        public void ApplyMutation(float rate, float amount)
        {
            layer1.Mutate(rate, amount);
            layer2.Mutate(rate, amount);
            layer3.Mutate(rate, amount);
            layer4.Mutate(rate, amount);
            MutArr(denseWeights, rate, amount);
            MutArr(denseBiases,  rate, amount);
        }

        static void MutArr(float[] arr, float rate, float amount)
        {
            for (int i = 0; i < arr.Length; i++)
                if (Random.value < rate)
                    arr[i] += Random.Range(-amount, amount);
        }

        // ── Herança de pesos ─────────────────────────────────────────────────
        public void InheritFrom(LSTMBrain source)
        {
            if (source.layer1 == null || source.layer4 == null) { Init(); return; }

            layer1 = new LSTMCell(inputSize,  hiddenSize);
            layer2 = new LSTMCell(hiddenSize, hiddenSize);
            layer3 = new LSTMCell(hiddenSize, hiddenSize);
            layer4 = new LSTMCell(hiddenSize, hiddenSize);

            source.layer1.CopyWeightsTo(layer1);
            source.layer2.CopyWeightsTo(layer2);
            source.layer3.CopyWeightsTo(layer3);
            source.layer4.CopyWeightsTo(layer4);

            denseWeights = (float[])source.denseWeights.Clone();
            denseBiases  = (float[])source.denseBiases.Clone();
            ResetMemory();
        }

        // ── Persistência ─────────────────────────────────────────────────────

        public void Save(string fileName, float fitness = 0f, int generation = 0)
        {
            string json = JsonUtility.ToJson(ToSaveData(fitness, generation), true);

            File.WriteAllText(
                Path.Combine(Application.persistentDataPath, fileName + ".json"), json);

#if UNITY_EDITOR
            string dir = Path.Combine(Application.dataPath, "Resources");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, fileName + ".json"), json);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[LSTM] Salvo em Resources/{fileName}.json</color>");
#else
            Debug.Log($"[LSTM] Salvo em persistentDataPath/{fileName}.json");
#endif
        }

        public bool Load(string fileName)
        {
            string json = null;

            var ta = Resources.Load<TextAsset>(fileName);
            if (ta != null)
            {
                json = ta.text;
                Debug.Log($"<color=cyan>[LSTM] Carregado de Resources: {fileName}</color>");
            }
            else
            {
                string p = Path.Combine(Application.persistentDataPath, fileName + ".json");
                if (File.Exists(p)) { json = File.ReadAllText(p); }
            }

            if (json == null) { Init(); return false; }
            FromSaveData(JsonUtility.FromJson<LSTMBrainSaveData>(json));
            return true;
        }

        // ── Serialização interna ──────────────────────────────────────────────

        public LSTMBrainSaveData ToSaveData(float fitness = 0f, int generation = 0)
        {
            var data = new LSTMBrainSaveData { fitness = fitness, generation = generation };

            // 4 camadas LSTM
            data.lstmLayers.Add(new LSTMLayerSaveData
                { inputSize = layer1.inputSize, hiddenSize = layer1.hiddenSize, cell = layer1.ToSaveData() });
            data.lstmLayers.Add(new LSTMLayerSaveData
                { inputSize = layer2.inputSize, hiddenSize = layer2.hiddenSize, cell = layer2.ToSaveData() });
            data.lstmLayers.Add(new LSTMLayerSaveData
                { inputSize = layer3.inputSize, hiddenSize = layer3.hiddenSize, cell = layer3.ToSaveData() });
            data.lstmLayers.Add(new LSTMLayerSaveData
                { inputSize = layer4.inputSize, hiddenSize = layer4.hiddenSize, cell = layer4.ToSaveData() });

            data.outputLayer = new LSTMOutputLayerSaveData
            {
                weights    = (float[])denseWeights.Clone(),
                biases     = (float[])denseBiases.Clone(),
                inputSize  = hiddenSize,
                outputSize = outputSize
            };
            return data;
        }

        public void FromSaveData(LSTMBrainSaveData data)
        {
            if (data.lstmLayers == null || data.lstmLayers.Count < 4)
            {
                Debug.LogWarning("[LSTM] Dados salvos têm formato antigo ou < 4 camadas ocultas. Inicializando nova rede...");
                Init();
                return;
            }

            var l1 = data.lstmLayers[0];
            layer1 = new LSTMCell(l1.inputSize, l1.hiddenSize);
            layer1.FromSaveData(l1.cell);

            var l2 = data.lstmLayers[1];
            layer2 = new LSTMCell(l2.inputSize, l2.hiddenSize);
            layer2.FromSaveData(l2.cell);

            var l3 = data.lstmLayers[2];
            layer3 = new LSTMCell(l3.inputSize, l3.hiddenSize);
            layer3.FromSaveData(l3.cell);

            var l4 = data.lstmLayers[3];
            layer4 = new LSTMCell(l4.inputSize, l4.hiddenSize);
            layer4.FromSaveData(l4.cell);

            denseWeights = (float[])data.outputLayer.weights.Clone();
            denseBiases  = (float[])data.outputLayer.biases.Clone();
            outputSize   = data.outputLayer.outputSize;
            hiddenSize   = l1.hiddenSize;
            ResetMemory();
        }
    }
}
