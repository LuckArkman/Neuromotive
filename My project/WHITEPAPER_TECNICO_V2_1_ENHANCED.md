# WHITEPAPER TÉCNICO V2.1: ARQUITETURA DE IA NEURAL AUTÔNOMA DO ZERO
## Sistema de Simulação de Multidões em Unity: Arquitetura Híbrida com DOTS, LSTM e Motor de Tensores Customizado (C# Burst/SIMD)

---

### 1. Resumo Executivo (Abstract)
Este documento detalha a implementação de um sistema de Inteligência Artificial neural para simulação de multidões em larga escala na engine Unity. Diferente de abordagens convencionais que dependem de bibliotecas externas (Sentis, Barracuda, TensorFlow), esta arquitetura propõe a construção de um **Motor de Inferência de Tensores nativo**, escrito integralmente em C# utilizando o **Burst Compiler** e **SIMD**. O sistema processa redes neurais recorrentes (LSTM) para centenas de agentes simultâneos em regime de alocação zero (*Zero-Allocation*), integrando-se nativamente ao Data-Oriented Technology Stack (DOTS) para máxima vazão de dados e paralelismo.

---

### 2. Objetivos de Engenharia
* **Independência de Frameworks:** Eliminar o overhead de marshalling e conversão de dados entre C# e bibliotecas C++/GPU externas.
* **Latência Determinística:** Garantir que a inferência neural ocorra dentro do budget de frame (sub-milissegundos) para milhares de agentes.
* **Memória Recorrente (LSTM):** Prover aos NPCs a capacidade de "lembrar" estados passados para evitar oscilações em decisões de navegação.
* **Escalabilidade Massiva:** Suporte a >2.000 agentes ativos com percepção complexa em 60 FPS estáveis.

---

### 3. A Espinha Dorsal: Data-Oriented Technology Stack (DOTS)
A eficiência do sistema reside na migração do paradigma Orientado a Objetos para o Orientado a Dados (ECS).

*   **Identidade vs. Dados:** NPCs deixam de ser classes `MonoBehaviour` e tornam-se IDs de entidade em um **ECS (Entity Component System)**.
*   **Acesso a Memória (Cache-Friendliness):** Os pesos da rede neural e os estados dos sensores são armazenados em blocos lineares de memória (**Chunks**). Isso maximiza o *cache hit* da CPU, permitindo que o processador processe centenas de neurônios sem interrupção de busca na RAM lenta.
*   **Burst Compiler & SIMD:** O motor utiliza instruções específicas do processador (SSE, AVX, NEON). Operações em vetores `float4` permitem que uma única instrução CPU processe 4 pesos neurais simultaneamente, chegando a 16 neurônios por ciclo em CPUs modernas com registros de 512 bits.

---

### 4. O Coração do Sistema: Motor de Tensores "Do Zero" (Custom Inference Engine)
Para maximizar o desempenho, abandonamos o Unity Sentis em favor de uma implementação matricial direta otimizada para o **Burst Compiler**.

#### 4.1. Armazenamento de Pesos (BlobAssets)
Os pesos e bias da rede treinada são carregados em estruturas `BlobAssetReference`.
*   **Vantagem:** Dados imutáveis, mapeados em memória, que podem ser acessados diretamente por múltiplos sistemas ECS em diferentes threads sem risco de race conditions ou necessidade de cópias.
*   **Estrutura:** Matrizes são "achatadas" (*Flattened*) em `BlobArray<float>` para evitar o custo de ponteiros em arrays multidimensionais.

#### 4.2. Operações de Tensores SIMD-Vetorizadas
Utiliza-se a biblioteca `Unity.Mathematics` para operações fundamentais:
*   **GEMM (General Matrix Multiply):** Implementado via `math.mad(float4, float4, float4)` — a instrução *Multiply-Add* fundida.
*   **Funções de Ativação:** Implementações customizadas de `Sigmoid` e `Tanh` utilizando aproximações polinomiais e funções intrínsecas do Burst para evitar chamadas lentas a bibliotecas padrão.

---

### 5. Implementação de LSTM "Scratch" em ECS
A capacidade regenerativa e de memória dos agentes deriva de uma rede neural recorrente do tipo **LSTM (Long Short-Term Memory)**.

#### 5.1. Gestão de Estado (H-State e C-State)
Diferente de MLPs simples, a LSTM exige armazenamento persistente por agente:
*   `LSTMStateComponent : IComponentData`: Contém o estado oculto ($h_t$) e o estado da célula ($c_t$).
*   **Otimização:** Para redes com 64 neurônios ocultos, cada agente carrega apenas 512 bytes de estado, mantendo o footprint de memória extremamente baixo.

#### 5.2. Gates de Controle (Forget, Input, Output)
A inferência ocorre por meio de quatro transformações lineares seguidas de ativações:
1.  **Forget Gate ($f_t$):** Decide qual informação do frame anterior deve ser descartada.
2.  **Input Gate ($i_t$):** Adiciona novas informações dos sensores ao estado da célula.
3.  **Cell Candidate ($\tilde{c}_t$):** Processa o input atual com ativação `Tanh`.
4.  **Output Gate ($o_t$):** Filtra o estado interno para gerar a ação final do NPC.

**Matemática de Inferência (Burst Code Snippet):**
```csharp
// Exemplo conceitual do cálculo de portões em SIMD
float4 combinedInput = math.mad(weights_f, h_prev, math.mad(weights_i, x_t, bias));
float4 forget_gate = math.sigmoid(combinedInput);
// Atualização do estado de célula C
c_current = math.mul(forget_gate, c_prev) + math.mul(input_gate, cell_candidate);
```

---

### 6. Sistema de Percepção: Sensor Batching 360°
Os NPCs utilizam uma "Visão por Radar" baseada em **Batching de Física**.

*   **Comando de Lote:** Em vez de `Physics.SphereCast` (thread principal), o sistema gera um `NativeArray<SpherecastCommand>`.
*   **Processamento Assíncrono:** Este vetor é enviado para a `PhysicsWorld` do Unity, que resolve os 16.000+ raios de percepção em Worker Threads paralelas usando o motor de colisão DOTS.
*   **Normalização de Input:** Os dados de colisão (distância, material, velocidade do alvo) são convertidos em um vetor de entrada normalizado $[0, 1]$ para alimentar a primeira camada da rede neural.

---

### 7. Saídas: Decisão Multi-Head & Action Masking
O resultado da LSTM é mapeado para dois tipos de saídas:
1.  **Continuous Action (Navegação):** Um par de floats $[V, \omega]$ que define a velocidade linear e a taxa de rotação (steering).
2.  **Discrete Action (Comportamento):** Um neurônio de ativação Softmax que seleciona o estado (ex: Vagando, Interagindo, Fugindo).
*   **Action Masking:** Antes de aplicar o movimento, um sistema lógico de segurança ECS verifica se o NPC está preso ou colidindo, "mascarando" ações que levariam a comportamentos físicamente impossíveis, refinando a inteligência bruta da IA.

---

### 8. Orquestração com UniTask
Enquanto o ECS cuida da computação pesada, o **UniTask** orquestra o ciclo de vida:
*   **Cognitive Throttling:** Se o tempo de frame exceder o budget, o `AIManager` (via UniTask) pode distribuir a inferência neural dos NPCs em grupos (*Buckets*), processando 50% dos agentes no frame A e 50% no frame B, sem quebrar a fluidez visual da simulação.
*   **Weight Hot-Reload:** Permite carregar novos modelos de IA dinamicamente sem travar a engine, facilitando iterações rápidas durante o desenvolvimento.

---

### 9. Pipeline de Treinamento e Evolução
*   **Unity ML-Agents:** O treinamento é realizado externamente via Reinforcement Learning (RL), utilizando o algoritmo **PPO** ou **MA-POCA** para cooperação em grupo.
*   **Exportação customizada:** Ao fim do treinamento, o modelo `.onnx` é processado por um script customizado que extrai as matrizes de pesos para arquivos binários puros, que são então injetados nos `BlobAssets` da arquitetura ECS.

---

### 10. Conclusão
A arquitetura proposta redefine os limites da IA em tempo real ao remover camadas de abrações desnecessárias. Ao construir um motor de tensores customizado sobre a base do Unity DOTS, alcançamos um nível de eficiência onde a complexidade neural dos NPCs não é mais o gargalo, mas sim o diferencial competitivo da simulação. Este sistema é a base para criar ecossistemas vivos e reativos em mundos abertos onde cada indivíduo da multidão possui uma "mente" verdadeiramente autônoma e histórica.
