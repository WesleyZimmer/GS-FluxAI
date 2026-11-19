# Coleção Insomnia — fluxAI SOA&W

## Importar e preparar

1. Gere dois tokens locais fortes e mantenha-os fora do repositório. No PowerShell, por exemplo:

   ```powershell
   $operatorBytes = [byte[]]::new(32)
   [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($operatorBytes)
   $env:Authentication__OperatorToken = [BitConverter]::ToString($operatorBytes).Replace('-', '').ToLowerInvariant()

   $workerBytes = [byte[]]::new(32)
   [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($workerBytes)
   $env:Authentication__WorkerToken = [BitConverter]::ToString($workerBytes).Replace('-', '').ToLowerInvariant()
   ```

2. Na mesma janela do PowerShell, inicie a API pela raiz do projeto: `dotnet run --project src/FluxAI.Api/FluxAI.Api.csproj`.
3. No Insomnia, importe `fluxAI-SOAW.insomnia.json` por **Import > File** e selecione **Base Environment**.
4. No ambiente local do Insomnia, cole os mesmos valores gerados em `operator_token` e `worker_token`. O arquivo versionado deixa essas variáveis vazias e não contém credenciais.
5. `base_url` aponta para `http://localhost:5169/api/v1`. O catálogo é público; configurações usam `operator_token`; importações usam `worker_token`; consultas de execução aceitam qualquer um dos dois.
6. Envie T01 para localizar o cenário/controladores. T02 cria uma configuração; copie o `id` da resposta para `configuration_id`. T05 usa `If-Match: "1"`; depois execute T06 com o ETag antigo.
7. Envie T08 duas vezes sem alterar o corpo ou a chave. Copie o `id` retornado para `run_id` para executar T10.
8. Após T05, pare e reinicie a API mantendo o mesmo SQLite temporário; depois execute T07.
9. T11a deve retornar `401` sem credencial; T11b deve retornar `403` quando o token de worker tenta criar uma configuração. O T11b envia somente `worker_token`.

O adaptador Python também lê o token do worker pela variável `FLUXAI_WORKER_TOKEN` quando usa `--post`; use o mesmo valor de `Authentication__WorkerToken` da API, sem registrá-lo no código ou na coleção. A API não cria tokens padrão: sem configuração, as rotas protegidas respondem `401`.
## Sequência de casos

| Caso | Requisição | Resultado esperado |
|---|---|---|
| T01 | Listar cenários e controladores | `200`; IDs coerentes com o cenário C2. |
| T02 | Criar configuração válida | `201`, `Location`, revisão 1 e ETag. |
| T03 | Limite mínimo maior que máximo | `422` e detalhes de validação; sem gravação. |
| T04 | Consultar configuração com ID inexistente | `404` em Problem Details. |
| T05 | Substituir configuração com ETag atual | `200`, revisão incrementada e novo ETag. |
| T06 | Repetir com ETag antigo | `412`; a revisão mais nova permanece. |
| T07 | Consultar após reiniciar API | Configuração e revisão permanecem no SQLite. |
| T08 | Importar e repetir o replay conhecido | Primeira chamada `201` em banco vazio; repetição `200`, mesmo ID e sem duplicar métricas. Em banco já preenchido, ambas podem responder `200`. |
| T09 | Enviar identidade de origem inválida | `422` com erro de campo e sem nova execução. |
| T10 | Consultar métricas paginadas/resultados | Unidades, paginação e cobertura correspondem à resposta da API. |
| T11a | Criar configuração sem credencial | `401` com `WWW-Authenticate: Bearer`. |
| T11b | Worker tenta criar configuração | `403`; token de worker é válido, mas não tem papel de operador. |

T12 (banco vazio/migrations) já foi verificado na Etapa 2. T13–T15 pertencem às etapas de worker e mobile.

## Registro da execução

Marque a data, o ambiente/porta e o status HTTP observado para cada caso em `../evidencias/etapa-4-insomnia.md`. Para demonstrar paginação, guarde o `nextPageToken` da primeira página e informe-o na variável `page_token` antes de enviar a requisição de página seguinte.
