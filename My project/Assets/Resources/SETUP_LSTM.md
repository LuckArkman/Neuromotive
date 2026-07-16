# Guia de Migração: Feed-Forward → LSTM

## O que foi feito

Após a compilação no Unity, você precisa fazer **um único passo manual** para religar o componente `LSTMBrain` ao prefab.

---

## Passo a Passo no Unity Editor

### 1. Abra o Prefab `Assets/prefab/Capsule.prefab`

No Inspector você verá que o componente antigo `NeuralBrain` aparece como **"Missing Script"**.

### 2. Adicione o `LSTMBrain`

- Com o prefab aberto, clique em **Add Component**
- Busque por `LSTM Brain` e adicione

### 3. Remova o "Missing Script"

- Clique com o botão direito no componente `Missing Script`
- Selecione **Remove Component**

### 4. Conecte o `brain` no `AgentController`

- Selecione o componente `AgentController` no Inspector
- Arraste o componente `LSTMBrain` para o campo **Brain**

> **Dica**: Como o `AgentController.Start()` usa `GetComponent<LSTMBrain>()` como fallback,  
> mesmo que o campo não esteja conectado, o sistema funcionará corretamente.

---

## Onde os saves são armazenados

| Arquivo | Localização | Conteúdo |
|---|---|---|
| `LSTM_BestGen.json` | `Assets/Resources/` + `persistentDataPath` | Melhor agente da última geração |
| `LSTM_BestEver.json` | `Assets/Resources/` + `persistentDataPath` | Melhor agente de TODOS os tempos |

Os arquivos em `Assets/Resources/` podem ser carregados via `Resources.Load<TextAsset>()` em builds.
