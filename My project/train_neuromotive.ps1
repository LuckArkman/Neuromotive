# Neuromotive Training Orchestrator (Windows)
# Use este script para iniciar a sessão de reforço de aprendizado no Python Side-car

$CONFIG_PATH = "Assets\Config\ml_agents_config.yaml"
$RUN_ID = "Neuromotive_Training_v1"

# Comando de lançamento do ML-Agents
# --env: Referencia o executável do Unity (ou porta aberta se rodando do Editor)
# --run-id: Identificador único da sessão para salvar checkpoints
# --resume: Adicione se quiser continuar um treino anterior

Write-Host "Iniciando Maratonas de Treino Neuromotive AI..." -ForegroundColor Cyan
Write-Host "Configuração: $CONFIG_PATH"

mlagents-learn $CONFIG_PATH --run-id=$RUN_ID --time-scale=20 --num-envs=1 --no-graphics

Write-Host "Sessão finalizada. Checkpoints salvos em results\$RUN_ID" -ForegroundColor Green
