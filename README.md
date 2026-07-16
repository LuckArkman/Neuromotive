# Neuromotive

**Neuromotive** é um projeto desenvolvido no Unity focado em inteligência artificial e aprendizado de máquina evolutivo. O sistema utiliza uma rede neural recorrente do tipo **LSTM (Long Short-Term Memory)** totalmente personalizada (escrita do zero em C#) e um **Algoritmo Genético** para treinar agentes autônomos a explorar um ambiente tridimensional, desviar de obstáculos e otimizar sua rota de exploração.

---

## 🧠 Arquitetura do Sistema Neural

A inteligência do projeto é implementada sem o uso de bibliotecas externas de Machine Learning, baseando-se em cálculos matriciais e lógica matemática nativa. A arquitetura é dividida em namespaces principais: `Neural` e `Systems`.

### 1. Células LSTM (`LSTMCell.cs`)
O coração da memória do agente é implementado na classe `LSTMCell`.
- **Estrutura**: Cada célula possui os clássicos 4 _gates_: **Forget**, **Input**, **Candidate (Cell State)** e **Output**.
- **Memória Episódica**: Mantém os vetores de _hidden state_ (`h`) e _cell state_ (`c`) persistentes a cada frame, permitindo ao agente "lembrar" do que visualizou em frames anteriores e tomar decisões baseadas em contexto temporal.
- **Funções de Ativação**: Implementações otimizadas de _Sigmoid_ e _Tanh_ seguras (limitadas para evitar overflow/underflow em `Mathf.Exp`).
- **Inicialização**: Pesos são inicializados usando o algoritmo de **Xavier (Glorot)**, garantindo uma variância adequada para evitar o desaparecimento de gradientes no início do treinamento.

### 2. O Cérebro LSTM (`LSTMBrain.cs`)
O componente central anexado ao agente que coordena as camadas da rede.
- **Topologia**:
  - **Input Layer**: 16 neurônios (recebendo dados dos sensores).
  - **Hidden Layers (LSTM)**: 4 camadas ocultas sequenciais (`layer1` a `layer4`), todas com tamanho 16.
  - **Dense/Output Layer**: 1 camada densa no final que reduz os 16 sinais ocultos para 3 saídas através de ativação sigmóide.
- **Forward Pass**: Recebe os inputs de visão, propaga pelas 4 camadas LSTM atualizando os estados de memória e produz as 3 saídas vitais: `[0] Girar Esquerda`, `[1] Girar Direita`, `[2] Acelerar/Velocidade`.

### 3. Sistema Sensorial Visão (`VisionSensor.cs`)
O agente interage com o mundo usando Raycasts esféricos simulando "visão":
- **16 Sensores 360º**: Disparam 16 `SphereCasts` distribuídos uniformemente ao redor do agente.
- **Normalização**: As distâncias de colisão são mapeadas de `0` (nenhum obstáculo) até `1` (obstáculo encostado no agente), facilitando o processamento numérico pelas ativações sigmóides da rede.

---

## 🧬 Algoritmo Genético e Evolução (`EvolutionManager.cs`)

O aprendizado ocorre através de algoritmos genéticos não-supervisionados que operam sobre a população de agentes em ciclos de gerações.

### O Ciclo de Geração
1. **Spawn**: Instancia um número configurado de agentes (ex: 20). O Agente 0 recebe os pesos exatos do melhor agente da geração anterior (elitismo).
2. **Mutação**: Os demais agentes sofrem mutações em seus pesos (adição de ruído aleatório). O sistema balanceia a exploração, aplicando taxas e quantidades de mutação mais agressivas na metade inferior da população.
3. **Simulação**: O tempo corre livremente. O ciclo encerra se todos os agentes morrerem ou se o limite de tempo global (`maxGenerationTime`) for atingido.
4. **Seleção**: Ao final do ciclo, os agentes são avaliados pela sua métrica de **Fitness**. O indivíduo mais apto é escolhido para perpetuar seus "genes".
5. **Herança**: A estrutura `LSTMBrainSaveData` clona matrizes de pesos do agente vencedor, que serão reaplicadas na próxima geração.

---

## 🚙 O Agente e a Navegação (`AgentController.cs`)

O controlador define como o corpo físico do agente responde aos comandos do cérebro e interage com o ambiente.

### Regras de Condução e Evasão
- O Output da rede neural dita a **Velocidade** (um valor de 0 a 1) e o **Esterçamento (Steering)**, deduzido pela diferença entre virar à esquerda e direita (-1 a 1).
- **Reflexo de Evasão (Blend)**: Como medida de sobrevivência auxiliar, há uma lógica procedural _hardcoded_ atuando como um "Tronco Cerebral". Quando a ameaça nos sensores chega perto demais do limite (`ThresholdWarning`), os controles baseados em reflexo sobrepõem-se gradativamente às decisões da LSTM.
- **Kill-Switch (Colisão)**: Se o sensor detectar uma distância crítica (`ThresholdDeath`), o agente morre imediatamente.

### Função de Fitness e Anti-Estagnação
O sistema recompensa agentes por explorarem efetivamente o mapa.
- **Grid de Células**: O mapa é subdividido logicamente em quadrados virtuais (ex: 5x5m). Cada vez que o agente adentra uma nova célula do grid, ele recebe uma recompensa robusta de Fitness (`rewardPerCell`).
- **Punição Anti-Círculo**: Um temporizador verifica a cada `N` segundos se o agente descobriu novas células. Se o agente estagnar e ficar girando em círculos, ele é instantaneamente desativado, otimizando o tempo de processamento da geração e favorecendo estratégias de exploração de longa distância.

---

## 💾 Infraestrutura e Persistência de Dados

O projeto conta com um sistema customizado e leve de gravação/leitura de dados via JSON para salvar o cérebro (arquitetura e pesos) que sobrevive mesmo entre diferentes sessões do Unity.

- **BestGen (`LSTM_BestGen.json`)**: Salva o melhor agente da geração que acabou de encerrar.
- **BestEver (`LSTM_BestEver.json`)**: Arquivo master que só é subscrito caso um agente recém avaliado ultrapasse a marca do **Maior Fitness Histórico Global**.
- **Carregamento Inicial**: Ao dar Play no Unity, o `EvolutionManager` vasculha o diretório local e o `persistentDataPath` da Unity à procura do arquivo `BestEver`. Caso encontre, o sistema não começa "do zero" aleatório, mas faz um boot retomando do conhecimento acumulado anteriormente, servindo como uma semente de elite (transfer learning rudimentar).

---

## 📊 Visualização de Debug (OnGUI)

A simulação acompanha uma robusta UI no `EvolutionManager` que traça em tempo real os status da evolução.
- **Painel de Status**: Exibe a geração atual, timer, população viva, e dados do líder atual.
- **Desenho Dinâmico da Rede Neural**: Lê as ativações ocultas da rede em tempo real durante a execução do melhor agente. 
- O Cérebro desenha as camadas (INPUT, H1, H2, H3, H4, OUTPUT), interpolando a cor dos neurônios com base em suas saídas (Sigmoid/Tanh) e projetando conexões onde os pesos `Wo/Uo` (Output gates) têm relevância, exibindo visualmente como o cérebro processa o espaço.

---

## Estrutura de Diretórios e Ferramentas Auxiliares

* `/Assets/Scripts/Neural`: Contém toda a lógica de inteligência artificial (LSTMBrain, LSTMCell, AgentController, VisionSensor).
* `/Assets/Scripts/Systems`: Contém os gerentes macro como o `EvolutionManager`.
* `/Assets/Resources`: Guarda as instâncias JSON salvas contendo os cérebros de alta performance (`LSTM_BestGen.json`, `LSTM_BestEver.json`).
* **ProBuilder (ProCore)**: O projeto acompanha extensões de edição procedural de malha interna (`ProBuilder`), provavelmente utilizadas para gerar e manipular facilmente os mapas e os circuitos de obstáculos para os agentes navegarem de forma modular.
