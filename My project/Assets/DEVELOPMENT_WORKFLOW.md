# WORKFLOW DE DESENVOLVIMENTO: NEUROMOTIVE IA

Este guia define os processos e ferramentas para o desenvolvimento do sistema de simulação de multidões via DOTS.

## 1. Ferramentas de Editor (Unity 6)
O projeto está configurado para o **Entities Hierarchy** e o **Systems Window**. Use o menu customizado `Neuromotive/DOTS/` para acesso rápido.

### Janelas Essenciais:
*   **Entities Hierarchy:** Visualize todas as entidades em tempo real.
*   **Systems Window:** Monitore a performance e o tempo de execução de cada sistema ECS.
*   **Journal:** Histórico de criação e deleção de entidades (Crítico para debugging de memory leaks).

## 2. Padrões de Código
*   **Structs over Classes:** Todos os dados de agentes devem ser `IComponentData` (structs).
*   **Burst Compilation:** Todos os sistemas devem ser marcados com `[BurstCompile]` para performance SIMD.
*   **Zero-Allocation:** Evite `new` dentro de loops de sistemas. Use `NativeArray`, `NativeList` e `BlobAssets`.

## 3. Workflow de Debug
1.  **Scene View Gizmos:** Use os gizmos globais para visualizar os alvos e direções dos agentes.
2.  **Entity Debugging:** Selecione uma entidade na hierarquia para inspecionar seus componentes no Inspetor customizado.
3.  **Profiling:** Utilize o `Window > Analysis > Profiler` para monitorar picos de CPU causados pela inferência neural.

## 4. Orquestração (UniTask)
*   As tarefas assíncronas de alto nível (Macro-Tick) devem ser gerenciadas via `UniTask`.
*   Sempre trate o `CancellationToken` para evitar que processos assíncronos continuem após a parada da cena.
