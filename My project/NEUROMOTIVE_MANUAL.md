# 🧠 Neuromotive: Autonomous Neural Crowds (Unity 6 / DOTS)

## 🚀 Visão Geral
Neuromotive é um motor de IA neural de alta performance projetado para simular multidões massivas (10.000+ agentes) com inteligência recorrente profunda (LSTM). O sistema utiliza integralmente o Unity DOTS (ECS, Burst, Jobs) para garantir zero alocações e performance SIMD.

---

## 🏗️ Arquitetura em 7 Fases

### 1. Percepção (Radar & Cohesion)
*   **SensorRadarSystem:** Scan circular via raycast/overlap para detecção de vizinhos e obstáculos.
*   **NeuralGroupCohesionSystem:** Injeta métricas de centroide e alinhamento no buffer de input.

### 2. Cognição (Neural Pipeline)
*   **NeuralInferenceSystem:** Processa a rede LSTM em massa.
*   **LOD Cognitivo:** Reduz a frequência de pensamento de agentes distantes.
*   **NeuralLODModelSwitcher:** Troca entre LSTM complexa (perto) e MLP reativa (longe).

### 3. Orquestração (Load Balancing)
*   **AgencyBucketingSystem:** Divide o processamento em 3 quadros para suavizar a CPU.
*   **NeuralBudgetManager:** Hard-cap de 8ms para proteger o FPS global.

### 4. Execução (Motor & Interaction)
*   **NeuralMovementHeadSystem:** Converte saídas neurais em vetores de força física.
*   **InteractionStateMachine:** Gerencia estados procedurais (Conversar, Comprar).

### 5. Renderização (Mass Visualization)
*   **CrowdVertexAnimationSystem:** Animação via Vertex Animation Texture (VAT).
*   **CrowdInstancingSystem:** Desenha milhares de agentes em uma única Draw Call.

### 6. Diagnóstico (Telemetria)
*   **NeuralProfilingSystem:** Monitoramento de latência em nanosegundos.
*   **IndividualAgentTelemetry:** Inspeção microscópica de estados ocultos da LSTM.

---

## 🛠️ Como Adicionar Novos Comportamentos

Para criar uma nova "personalidade" ou reação na IA:

1.  **Ajuste os Pesos:** Treine um novo modelo no ML-Agents e use o `onnx_to_bin.py` para gerar os arquivos `.bin`.
2.  **Mapeie as Ações:** No `AgentActionState.cs`, adicione a nova ID de ação e defina a lógica de execução no `InteractionStateMachineSystem`.
3.  **Defina a Recompensa:** No `NeuralNavigationRewardSystem`, adicione a lógica matemática que incentiva o novo comportamento (ex: "Ganhe pontos ao estar perto de um NPC amigo").

---

## 📊 Performance Teórica
*   **500 Agentes:** < 0.2ms CPU (Inference + Physics).
*   **2000 Agentes:** < 1.0ms CPU (LOD Normal).
*   **10.000 Agentes:** < 4.0ms CPU (LOD Agressivo).

**Neuromotive: Inteligência sem limites para o futuro dos mundos abertos.**
