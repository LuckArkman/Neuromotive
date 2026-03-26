# ROADMAP DE IMPLEMENTAÇÃO: IA NEURAL AUTÔNOMA DO ZERO (DOTS)

Este roadmap detalha rigorosamente as etapas para a construção de um sistema de simulação de multidões autônomo e de alta performance em Unity. O foco é a implementação de baixo nível, eliminando dependências externas e maximizando o uso de arquitetura orientada a dados.

---

## FASE 1: INFRAESTRUTURA E FUNDAMENTOS DOTS
**Objetivo:** Estabelecer o ambiente ECS e a estrutura de dados base para os agentes.

### 1.1. Definição do Agente (Entities & Components)
*   **Struct `AgentComponent`**: Contém metadados básicos (ID, tipo de agente).
*   **Struct `AgentTransform`**: Substitui o Transform tradicional para velocidade máxima (Position, Rotation).
*   **Struct `AgentVelocity`**: Armazena velocidade linear e angular atual.
*   **Justificativa**: Componentes pequenos e focados permitem que os sistemas processem apenas os dados necessários, reduzindo o uso de banda de memória.

---

## FASE 2: SISTEMA DE PERCEPÇÃO SENSORIAL (RADAR BATCH)
**Objetivo:** Criar uma visão 360° otimizada para os NPCs.

### 2.1. Batching de Física (SphereCasts)
*   **Sistema `SensorSetupSystem`**: Calcula as direções de amostragem (16 raios por NPC) e preenche um `NativeArray<SpherecastCommand>`.
*   **Justificativa**: Escalar milhares de raios em paralelo via C# Job System evita o gargalo da thread principal da Unity.
*   **Componente `SensorResultsComponent`**: Armazena as distâncias e tipos de objetos detectados (normalizados para a rede neural).

---

## FASE 3: MOTOR DE TENSORES CUSTOMIZADO (C# BURST/SIMD)
**Objetivo:** Implementar a matemática matricial do zero, otimizada para o processador.

### 3.1. Infraestrutura de Tensores
*   **Struct `TensorMatrix`**: Representa uma matriz achatada em memória contígua.
*   **Função `Linear(input, weights, bias)`**: Implementação de camada densa usando `math.mad()` para operações de acumulação aceleradas por hardware.
*   **Funções de Ativação**:
    *   `Sigmoid(float4)`: Implementação aproximada rápida para o portão forget.
    *   `Tanh(float4)`: Para a saída de movimento e candidatos de célula.
*   **Justificativa**: O uso de `float4` permite a vetorização SIMD automática pelo Burst, processando 4 pesos em um único ciclo.

### 3.2. Armazenamento de Pesos (BlobAssets)
*   **`NeuralWeightsBlob`**: Armazena os pesos de todas as camadas em um formato imutável e mapeado em memória.
*   **Justificativa**: Evita a necessidade de gerenciar `NativeArrays` dinâmicos durante o runtime, permitindo acesso ultra-rápido de qualquer thread.

---

## FASE 4: IMPLEMENTAÇÃO DO CÉREBRO (LSTM COMPLETA)
**Objetivo:** Prover memória temporal aos agentes para navegação fluida.

### 4.1. Camada Recorrente LSTM
*   **Classe/Struct `LSTMLayer`**: Gerencia os portões `Forget`, `Input`, `Output` e `Cell`.
*   **Componente `LSTMStateComponent`**: Armazena `h_t` e `c_t` para cada NPC.
*   **Função `Infer(state, input, weights)`**: Realiza a inferência completa por agente.
*   **Justificativa**: NPCs sem memória "oscilam" em decisões complexas (como portas). A LSTM sincroniza o passado com o presente para suavizar rotas.

---

## FASE 5: DECODIFICAÇÃO E EXECUÇÃO MOTORA
**Objetivo:** Transformar a saída da IA em movimento físico no mundo.

### 5.1. Saídas Multi-Head
*   **Head de Movimento**: Saída de 2 valores para aceleração linear e angular.
*   **Head de Ação**: Softmax para ações discretas (Idle, Atacar, Interagir).
*   **Action Masking Logic**: Impede que a IA tome ações impossíveis no contexto atual (ex: virar em direção a uma parede detectada pelos sensores).
*   **Justificativa**: Garante que o comportamento "emergente" da rede neural respeite as limitações físicas do ambiente de jogo.

---

## FASE 6: ORQUESTRAÇÃO ASSÍNCRONA E CICLO DE VIDA
**Objetivo:** Gerenciar o balanceamento de CPU e o carregamento do sistema.

### 6.1. AI Manager (UniTask)
*   **`CognitiveThrottlingSystem`**: Distribui a inferência de milhares de agentes em "Buckets" de processamento. NPCs distantes da câmera são processados em menor frequência (LOD Cognitivo).
*   **Carga de Pesos**: Carregamento assíncrono do arquivo de pesos (.bin ou .json customizado) direto para `BlobAssets`.
*   **Justificativa**: Garante que a simulação de multidão nunca cause "stutter" na taxa de quadros principal.

---

## FASE 7: PIPELINE DE TREINAMENTO (WORKFLOW EXERNO)
**Objetivo:** Gerar cérebros eficientes para as multidões.

### 7.1. Integração ML-Agents
*   **Ambiente de Simulação**: Cenário isolado para treinamento focado em navegação e colaboração.
*   **Extrator de Pesos (Python/C#)**: Script que converte o arquivo treinado para o formato binário compatível com o sistema customizado.
*   **Justificativa**: O treinamento via Aprendizado por Reforço (RL) cria comportamentos orgânicos que seriam impossíveis de programar manualmente via If/Else.

---

## FASE 8: OTIMIZAÇÃO RIGOROSA E REFINAMENTO
**Objetivo:** Polimento técnico para garantir máxima eficiência.

### 8.1. Alinhamento de Memória e Cache
*   **`MemoryAlignmentSystem`**: Garante que os componentes do NPC estejam alinhados em fronteiras de cache line (64 bytes).
*   **Burst Inspector Audit**: Revisão do assembly gerado para garantir que não haja "Scalar Fallbacks" nas operações de rede neural.

---

## FASE 9: TESTES DE ESTRESSE E ENTREGA
**Objetivo:** Validação final do sistema.

### 9.1. Benchmark de Multidão
*   Cenário com >2.000 NPCs em um ambiente urbano simulado.
*   Medição de latência de inferência e tempo total de Worker Thread.

---

### TABELA DE COMPONENTES E FUNÇÕES CRÍTICAS

| Nome | Tipo | Função | Justificativa |
| :--- | :--- | :--- | :--- |
| `AgentBrainSystem` | `ISystem` | Executa o motor de tensores/LSTM | Core da inteligência, processa as camadas neurais. |
| `math.mad()` | Intrinsics | Operação de somatório de pesos | Otimizada por hardware (FMA), reduz latência. |
| `BlobAssetReference` | Container | Armazena pesos treinados | Alocação fixa, zero garbage collection, alto desempenho. |
| `UniTask` | Async Lib | Coordena o macro-tick da IA | Orquestração leve sem o custo de Threads nativas ou Coroutines. |
