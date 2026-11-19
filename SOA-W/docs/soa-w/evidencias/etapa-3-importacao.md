# Evidência de integração da Etapa 3

Data: 19/11/2026. Ambiente: API ASP.NET Core .NET 10, SQLite local.

- Execução-fonte: `rl_20261119T003313Z_073aa4` (replay de validação, seed `20261301`, 600 s).
- ID persistido: `746ac962-28c9-43d2-ba46-79240b2cf044`.
- Resumo importado: 595 inseridos, 218 chegadas, 377 ativos, 6 pendentes, zero eventos de colisão confirmados, zero passos de veículo em colisão e zero teletransportes.
- Séries: 4.800 amostras de fila, cobertura 4.800/4.800; oito resultados por controlador; onze referências de artefatos.
- Primeiro POST: `201 Created`. Retry idêntico após reinício da API: `200 OK`, `Idempotency-Replayed: true`, mesmo ID. Corpo divergente com a mesma chave: `409 Conflict`. A lista continha uma execução com essa origem.
- GET de execução, lista, métricas paginadas, resultados por controlador e `/healthz` responderam após reiniciar o servidor.
- `dotnet build` terminou com zero avisos e zero erros; migrations `20261119031702_AddImportedRuns` e `20261119032401_AddRunCreatedAtOrderKey` foram aplicadas.

As chamadas deste registro foram feitas por HTTP local. Depois, o adaptador Python foi executado em modo de normalização local: produziu 4.807 métricas, 11 referências de artefato e cobertura de fila `4800/4800`. O envio HTTP pelo próprio adaptador (`--post`) ainda não foi verificado; a importação desta demonstração usou o payload normalizado na coleção. A regressão final de autenticação e autorização está registrada em `etapa-4-insomnia.md`.
