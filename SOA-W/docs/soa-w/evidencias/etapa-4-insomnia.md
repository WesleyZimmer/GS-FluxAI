# Etapa 4 — evidências locais e coleção Insomnia

Data: 19/11/2026. A API foi executada em `http://127.0.0.1:5169` usando SQLite temporário isolado, inicializado pelas migrations e pela carga C2. T01-T10 foram verificados por HTTP local e repetidos pela interface do Insomnia 13.4.0. As duas rodadas usaram bancos temporários separados; por isso, IDs de execução podem diferir entre a tabela de HTTP e a tabela da interface.

## Casos verificados por HTTP

| Caso | Observação registrada |
|---|---|
| T01 — catálogo | `GET /scenarios` e `GET /signal-controllers` retornaram `200`; o cenário de demonstração e seus 8 controladores foram encontrados. |
| T02 — criar configuração | `POST /configurations` retornou `201`, `Location`, revisão `1` e `ETag: "1"`. |
| T03 — validações | Mínimo maior que máximo, valor negativo e fase inexistente retornaram `422` com erros nos campos correspondentes. O fluxo de validação termina antes da gravação. |
| T04 — ID ausente | `GET /configurations/{id}` retornou `404` e `Content-Type: application/problem+json`. |
| T05 — atualizar com ETag atual | `PUT /configurations/{id}` com `If-Match: "1"` retornou `200`, revisão `2` e `ETag: "2"`. |
| T06 — ETag antigo | Repetir o `PUT` com `If-Match: "1"` retornou `412` e Problem Details; a leitura seguinte manteve a revisão `2`. |
| T07 — persistência após reinício | Após reiniciar a API, o GET da configuração de teste retornou `200` com revisão `2`. |
| T08 — importação e retry | O primeiro envio do replay `rl_20261119T003313Z_073aa4` retornou `201`; o retry idêntico retornou `200`, `Idempotency-Replayed: true` e o mesmo ID (`c9459523-b4f2-4b05-a999-5b524b62d315` no banco temporário). A resposta registrou cobertura de filas `4800/4800` e 11 referências de artefato. |
| T09 — identidade inválida | `sourceRunId` com espaços nas extremidades retornou `422`, apontando `sourceRunId`. |
| T10 — paginação e cobertura | A consulta de fila por controlador com `signalControllerId=457026848` retornou `200`, 3 itens, cobertura `600/600`, `complete=true` e próxima página. A página seguinte também retornou `200`; resultados agregados retornaram 8 controladores. |

Uma primeira chamada de T10 sem `signalControllerId` retornou `422`, pois a API exige o controlador quando `scope=signalController`. A coleção e o arquivo `.http` foram corrigidos para incluir esse parâmetro.

A coleção JSON foi lida estruturalmente: formato 4, 23 recursos, 17 requisições, pais válidos e corpos JSON analisáveis. Os corpos T08 e T09 da própria coleção também foram enviados por HTTP local ao banco temporário: T08 retornou `200` com replay verdadeiro e mesmo ID; T09 retornou `422` em `sourceRunId`. A execução pela interface está registrada separadamente abaixo; T01-T10 foram confirmados na interface com ambiente e SQLite isolados.

## Execução pela interface Insomnia

- A coleção `fluxAI-SOAW.insomnia.json` foi importada no espaço local Scratch Pad do Insomnia 13.4.0.
- T01 — Listar cenários: `GET http://localhost:5169/api/v1/scenarios?pageSize=20` retornou `200 OK` (116 ms, 474 B). O corpo apresentou 1 cenário habilitado, com 8 controladores.
- T02 - Criar configuração: `201 Created`; a configuração de teste recebeu revisão `1` e foi usada nos testes de concorrência otimista.
- T03a-T03c - Validações: os três corpos inválidos retornaram `422 Unprocessable Entity` (mínimo maior que máximo, mínimo negativo e fase inválida).
- T04-T06 - Ausência e ETag: ID inexistente retornou `404`; atualização com ETag atual retornou `200` e revisão `2`; repetição com ETag antigo retornou `412`.
- T07 - Persistência: depois de reiniciar a API mantendo o SQLite temporário, a configuração foi lida com `200` e revisão `2`.
- T08 - Importação: o primeiro envio retornou `201`; o retry idêntico retornou `200` e o mesmo ID (`d4fd2a1f-f4c1-47de-be37-a66f97335dae`), confirmando idempotência.
- T09 - Identidade inválida: `sourceRunId` com espaços nas extremidades retornou `422`, apontando o campo `sourceRunId`.
- T10 - Resultados e paginação: resultados agregados retornaram `200` com 8 controladores. A primeira página de métricas retornou `200`, `pageSize=3`, cobertura `600/600`, `complete=true` e cursor `Mw`; a página seguinte também retornou `200`, cobrindo os segundos 4-6 e retornando o cursor seguinte `Ng`.
- A API foi iniciada com um SQLite temporário em `%TEMP%\fluxAI-Stage4-Insomnia\fluxia.db`; o banco principal do projeto não foi alterado. A API foi encerrada após a conferência.

Na execução pela interface, a API ainda não tinha autenticação implementada; os status observados nesse bloco são evidência histórica dos fluxos funcionais. Após proteger as rotas, foi feita a regressão HTTP abaixo. Para entrega, considerar os resultados dessa regressão como o comportamento atual.

## Regressão final após implementação de autenticação

Executada em 19/11/2026 contra `http://127.0.0.1:5169`, com SQLite temporário recém-criado em `%TEMP%`; o banco do projeto não foi usado. Tokens de teste existiram apenas no processo local e não foram gravados na coleção ou no repositório.

| Verificação | Resultado observado |
|---|---|
| `/healthz` e catálogo de cenários sem credencial | `200`; saúde e catálogo continuam públicos. O catálogo retornou 1 cenário habilitado. |
| Criar configuração sem credencial (T11a) | `401`, `application/problem+json` e `WWW-Authenticate: Bearer`. |
| Worker tentando criar configuração (T11b) | `403` e `application/problem+json`; token válido, papel insuficiente. |
| Criar/ler configuração como operador; ler execução como operador e worker | `201` na criação; `200` nas leituras permitidas. |
| Ler execução sem credencial | `401`. |
| Regras semânticas e ETag | Três entradas inválidas `422`; configuração inexistente `404`; atualização atual `200` com ETag `"2"`; ETag antigo `412`. |
| Importação e idempotência com token de worker | `201` na primeira importação, `200` no retry e mesmo ID; identidade de origem com espaços retornou `422`. |
| Métricas e agregados | Primeira página e página seguinte `200`, 3 itens por página e cobertura `600/600`; resultados agregados retornaram 8 controladores. |
| Reinício e persistência | Após reiniciar a API usando o mesmo SQLite temporário, a configuração de revisão 2 e a execução importada foram lidas com `200`. |

O processo de teste usou um banco isolado e foi encerrado. A migration inicial e as migrations de execução foram aplicadas no primeiro início desse banco.

## Divergências corrigidas

- A abertura do OpenAPI dizia que nenhum endpoint estava implementado, usava porta `5080` e aplicava Bearer globalmente. Agora identifica as operações das Etapas 2 e 3, usa a porta local `5169`, exige Bearer nas rotas protegidas e mantém catálogo/saúde públicos, conforme o servidor.
- O esquema de Problem Details exigia campos `code` e `instance` não emitidos pela API e modelava erros de campo como lista. O contrato agora corresponde ao formato observado do ASP.NET Core, incluindo o mapa `errors`.
- A regra de configuração retorna `422`, não `400`. Plano e coleção foram alinhados ao comportamento observado.
- Respostas de configuração `404` e ETag desatualizado `412` passaram a usar `application/problem+json`, conforme o contrato.
- O exemplo de ETag foi alinhado ao formato efetivamente emitido (`"1"`, `"2"`, ...).

## Validação do contrato OpenAPI

- Em 19/11/2026, `docs/soa-w/openapi.yaml` passou pela validação do Swagger Parser 12.1.0, já disponível junto ao Insomnia. O parser reconheceu OpenAPI `3.0.3`, validou o documento e as referências locais e encontrou 15 rotas.
- A instalação de Microsoft.OpenApi.Hidi 3.10.2 foi tentada, mas o feed do NuGet não pôde ser acessado. A validação acima foi concluída com o parser local.

## Pendências

- T11a/T11b foram verificados após implementar autenticação por papel. T13-T15 pertencem às etapas 5 e mobile.
