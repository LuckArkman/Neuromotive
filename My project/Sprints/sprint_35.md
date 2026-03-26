# Sprint 35: Otimização Intrínseca Burst (LSTM Core)

## Descrição Detalhada
Uso de SSE/AVX específicos no loop recorrente para latência mínima.

## Objetivos de Desenvolvimento
1. Implementar classes e structs fundamentais.
2. Garantir compatibilidade com Burst Compiler.
3. Otimizar para SIMD e Zero-Allocation.
4. Validar via Unit Tests e Profiler.

## Justificativa Técnica
Necessário para garantir a escalabilidade de milhares de agentes em tempo real.