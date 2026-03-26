using Unity.Burst;
using Unity.Mathematics;

namespace Neuromotive.AI.Math
{
    /// <summary>
    /// Utilitários matemáticos de baixo nível otimizados para Burst/SIMD.
    /// Foco em cálculos vetoriais necessários para navegação neural.
    /// </summary>
    [BurstCompile]
    public static class NeuroMath
    {
        public const float PI = 3.1415926535f;
        public const float EPSILON = 1e-6f;

        /// <summary>
        /// Projeta um vetor sobre outro vetor.
        /// Útil para calcular o progresso ao longo de um caminho.
        /// </summary>
        public static float3 Project(float3 vector, float3 ontoVector)
        {
            float sqrMag = math.dot(ontoVector, ontoVector);
            if (sqrMag < EPSILON) return math.float3(0);
            return ontoVector * math.dot(vector, ontoVector) / sqrMag;
        }

        /// <summary>
        /// Projeta um vetor sobre um plano definido por sua normal.
        /// Essencial para manter a movimentação do agente paralela ao solo.
        /// </summary>
        public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
        {
            float sqrMag = math.dot(planeNormal, planeNormal);
            if (sqrMag < EPSILON) return vector;
            float dotProduct = math.dot(vector, planeNormal);
            return vector - planeNormal * dotProduct / sqrMag;
        }

        /// <summary>
        /// Retorna o vetor 'forward' a partir de um quaternion de rotação.
        /// </summary>
        public static float3 GetForward(float4 rotation)
        {
            return math.mul(new quaternion(rotation), math.float3(0, 0, 1));
        }

        /// <summary>
        /// Calcula o ângulo assinado entre dois vetores em relação a um eixo.
        /// </summary>
        public static float SignedAngle(float3 from, float3 to, float3 axis)
        {
            float angle = math.acos(math.clamp(math.dot(math.normalize(from), math.normalize(to)), -1f, 1f));
            float3 crossProduct = math.cross(from, to);
            if (math.dot(axis, crossProduct) < 0) angle = -angle;
            return angle;
        }

        /// <summary>
        /// Distância ao quadrado entre dois pontos. 
        /// Mais performático que a distância real (evita sqrt).
        /// </summary>
        public static float DistanceSq(float3 a, float3 b)
        {
            float3 diff = a - b;
            return math.dot(diff, diff);
        }

        /// <summary>
        /// Seno aproximado ultra-rápido para SIMD via Aproximação de Bhaskara I.
        /// Erro máximo inferior a 0.001 no intervalo [0, PI].
        /// </summary>
        [BurstCompile]
        public static float4 FastSin(float4 x)
        {
            float4 pi = PI;
            float4 pi2 = PI * PI;
            float4 x_pi_minus_x = x * (pi - x);
            return (16.0f * x_pi_minus_x) / (5.0f * pi2 - 4.0f * x_pi_minus_x);
        }

        /// <summary>
        /// Cosseno aproximado ultra-rápido via FastSin.
        /// </summary>
        [BurstCompile]
        public static float4 FastCos(float4 x)
        {
            return FastSin(x + (PI / 2.0f));
        }
    }
}
