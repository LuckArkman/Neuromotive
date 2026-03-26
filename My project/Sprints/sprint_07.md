# [COMPLETED] Sprint 07: Batching de SphereCast de Física

## Descrição Detalhada
Sistema SensorSetupSystem para agendar SpherecastCommands em lote.

## Objetivos de Desenvolvimento
1. Implementar classes e structs fundamentais.
2. Garantir compatibilidade com Burst Compiler.
3. Otimizar para SIMD e Zero-Allocation.
4. Validar via Unit Tests e Profiler.

## Status: COMPLETED
- [x] Defined `SensorConfig` and `SensorResultElement` (IBufferElementData).
- [x] Implemented `SensorSystem` with `SpherecastCommand.ScheduleBatch`.
- [x] Implemented `PrepareSensorCommandsJob` for parallel ray distribution (360°).
- [x] Implemented `CollectSensorResultsJob` to update agent buffers from physics results.
- [x] Ensured Zero-Allocation with `NativeArray<SpherecastCommand>` management.
- [x] Meticulous Analysis performed.

## Justificativa Técnica
Necessário para garantir a escalabilidade de milhares de agentes em tempo real.