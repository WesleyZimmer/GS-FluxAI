# fluxAI — SOA & Webservices

O fluxAI é um projeto acadêmico CitizenTech que estuda como dados de tráfego e aprendizado por reforço podem ajudar a avaliar estratégias de controle semafórico. Esta entrega de SOA&W fornece a API e a persistência que conectam a simulação aos clientes.

## Benefício e limite da demonstração

A hipótese de benefício público é reduzir filas e tempos de espera em cruzamentos. A avaliação usa métricas como fila por controlador, viagens concluídas, veículos ativos ao fim da janela e tempo de espera, sempre identificando cobertura e origem. O enquadramento proposto é o ODS 11, pela relação com mobilidade urbana; isso não substitui uma medição de impacto. A demonstração atual trabalha com simulação/replay no SUMO; ela não controla semáforos reais nem demonstra impacto medido nas ruas.

O projeto integra quatro frentes: API e banco em C#/.NET; simulação e treinamento em Python/SUMO; painel Windows Forms; aplicativo React Native/Expo. Nesta entrega, a API, a persistência, a importação de resultados e a autenticação local por função estão implementadas. Worker de treinamento, painel e aplicativo permanecem pendentes.

## Integrantes

| Integrante | RM |
|---|---|
| Wesley Zimmer | 560264 |
| João Victor Alvarenga | 93752 |

## Estrutura desta entrega

- `src/FluxAI.Api/` — ASP.NET Core, domínio, SQLite, migrations e endpoints.
- `scripts/fluxai_api_adapter.py` — adaptador inicial dos artefatos de execução para o contrato da API.
- `sumo/scenarios/fluxia_stage6_c2/` — cenário e perfil C2 usados na carga demonstrativa.
- `docs/soa-w/openapi.yaml` — contrato OpenAPI 3.0.3, com operações existentes e operações futuras identificadas por etapa.
- `docs/soa-w/insomnia/` — coleção de requisições e carga de importação da Etapa 3.
- `docs/soa-w/evidencias/` — registros das verificações executadas e suas limitações.
- `prompts.md` — registro resumido do uso de assistência de IA.

## Requisitos e execução local

- Windows com .NET SDK `10.0.404` (a versão é fixada em `global.json`).
- O primeiro início da API aplica as migrations e carrega o cenário C2 demonstrativo no SQLite.

Na raiz do projeto:

```powershell
dotnet restore src/FluxAI.Api/FluxAI.Api.csproj
dotnet run --project src/FluxAI.Api/FluxAI.Api.csproj
```

O perfil local usa `http://localhost:5169`. A verificação de saúde está em `http://localhost:5169/healthz`; a base da API é `http://localhost:5169/api/v1`. O banco local fica em `src/FluxAI.Api/App_Data/fluxia.db`.

## Operações implementadas

| Método e rota | Uso |
|---|---|
| `GET /healthz` | Verificar conexão com SQLite. |
| `GET /api/v1/scenarios` | Listar cenários cadastrados. |
| `GET /api/v1/signal-controllers?scenarioId=...` | Consultar controladores e fases. |
| `POST /api/v1/configurations` | Criar configuração de experimento. |
| `GET /api/v1/configurations/{id}` | Consultar a revisão atual e seu `ETag`. |
| `PUT /api/v1/configurations/{id}` | Substituir a configuração usando `If-Match`. |
| `POST /api/v1/run-imports` | Importar uma simulação/replay concluído com `Idempotency-Key`. |
| `GET /api/v1/runs` e `GET /api/v1/runs/{id}` | Consultar histórico e execução. |
| `GET /api/v1/runs/{id}/metrics` | Consultar métricas paginadas e cobertura quando disponível. |
| `GET /api/v1/runs/{id}/signal-results` | Consultar resultados agregados por controlador. |

As regras de domínio retornam `422` com detalhes de campo; JSON e parâmetros inválidos retornam `400`. Conflito de idempotência retorna `409`; revisão desatualizada retorna `412`. Catálogo e saúde são públicos; configurações exigem Bearer de operador, importação exige Bearer de worker e consultas de execução aceitam operador ou worker. Configure `Authentication__OperatorToken` e `Authentication__WorkerToken` no ambiente antes de iniciar a API. Sem tokens configurados, operações protegidas falham fechadas. As chaves são opacas para a demonstração local; isso não substitui OAuth/JWT e gestão de identidades.

## Verificações e coleção Insomnia

O Insomnia é um cliente para testar a API manualmente: cada requisição envia um método, uma rota, parâmetros e, quando necessário, um corpo JSON; a ferramenta mostra o status HTTP e a resposta. Ele serve para demonstrar e conferir o contrato. Não faz parte da API em execução e não é uma dependência do código entregue.

Para reproduzir, configure dois tokens locais fora do repositório, inicie a API e importe `docs/soa-w/insomnia/fluxAI-SOAW.insomnia.json` no Insomnia. Copie os mesmos valores para `operator_token` e `worker_token` no ambiente local da coleção; o arquivo versionado contém essas variáveis vazias. Use as instruções em `docs/soa-w/insomnia/README.md`. A carga `run-import-etapa-3.json` é um replay de demonstração e repetir a mesma identidade/carga deve reutilizar a execução.

Na etapa 4, T01-T10 foram executados pela interface do Insomnia contra a API local com SQLite temporário. Depois da implementação de autenticação, os fluxos foram revalidados por HTTP com os papéis de operador e worker: catálogo `200`, escrita sem credencial `401`, papel incorreto `403`, importação idempotente `201`/`200`, validações `422`, ETag atual/desatualizado `200`/`412`, paginação e cobertura `600/600`. Configuração e execução também foram lidas após reiniciar a API. T11 foi verificado por HTTP, com `401` para ausência de credencial e `403` para worker tentando editar configuração. O banco principal do projeto não foi alterado; os resultados estão em `docs/soa-w/evidencias/etapa-4-insomnia.md`.

A validação do OpenAPI 3.0.3 passou com Swagger Parser 12.1.0: o validador aceitou o documento e suas referências locais, cobrindo 15 rotas. A instalação alternativa do Microsoft.OpenApi.Hidi não avançou porque o ambiente não conseguiu acessar o índice do NuGet. A coleção inclui T11a e T11b; a API respondeu `401` sem credencial e `403` com token de worker na rota de configuração. O registro está em `docs/soa-w/evidencias/etapa-4-insomnia.md`.

O `.gitignore` limita a entrega versionada ao código e aos materiais necessários para SOA&W, mantendo fora artefatos locais, bancos de teste, dados grandes e arquivos das demais frentes.

## Dados e limitações

O cenário C2 e o replay são dados de simulação. Ausência de amostra não é convertida em zero, e indicadores só devem ser comparados quando origem, unidade e cobertura forem compatíveis. A métrica de chegada representa viagens concluídas conforme o critério registrado no replay, não uma contagem de tráfego real.

O adaptador Python foi executado em modo de geração local de payload com os artefatos do replay: produziu 4.807 métricas normalizadas, 11 referências de artefato e cobertura de fila de 4.800/4.800. A importação HTTP foi validada com o payload da coleção e credencial de worker; o envio HTTP pelo próprio adaptador (`--post`) ainda não foi verificado. Worker de treinamento, rotas de execução acompanhada, painel e app são etapas futuras.
