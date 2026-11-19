# Decisões SOA&W — fluxAI

## Registro

### D-001 — API como fronteira entre clientes, IA e banco

- **Contexto:** painel C# e app mobile precisam de configuração/resultados; Python/SUMO permanece a execução da IA.
- **Decisão:** uma ASP.NET Core Web API em C# valida e persiste o domínio; clientes não acessam o SQLite; um adaptador/worker conversa com a API.
- **Alternativa considerada:** API ampla e banco acessível diretamente por cada cliente.
- **Motivo:** separa capacidades, reduz dependências entre linguagens e permite evoluir clientes sem compartilhar detalhes internos do banco.
- **Fonte:** SOMA, nota 02. Escolha de arquitetura do projeto, não exigência da matéria.

### D-002 — API REST/OpenAPI com contrato separado do servidor

- **Contexto:** a entrega pede REST, fonte, banco, migrations e testes manuais no Insomnia.
- **Decisão:** base `/api/v1`, contrato inicial OpenAPI 3.0.3; schemas de entrada, saída e erro distintos; operação identificada por método/caminho.
- **Alternativa considerada:** iniciar pelo CRUD implícito em entidades e documentá-lo depois.
- **Motivo:** os consumidores precisam saber os efeitos e as respostas antes de serem implementados.
- **Fonte:** SOMA, notas 05–06, 13–14 e 21; FIAP GS, p. 27. A especificação é uma proposta sujeita à revisão.

### D-003 — Execução e métrica idempotentes e rastreáveis

- **Contexto:** repetir uma solicitação de treino ou lote de telemetria poderia duplicar trabalho/contagens.
- **Decisão:** chave de idempotência por ator e conteúdo; métricas têm identidade por execução/escopo/amostra; dados divergentes na mesma chave retornam conflito.
- **Alternativa considerada:** confiar que a rede nunca repetirá requisições.
- **Motivo:** retry de cliente não deve iniciar vários treinos nem duplicar observações.
- **Fonte:** SOMA, notas 04 e 07; hashes/manifests existentes nos artefatos do SUMO. A importação implementa a identidade de origem e a chave da amostra em uma transação. A vinculação da idempotência a ator aguarda autenticação.

### D-004 — Configuração versionada e snapshot imutável por execução

- **Contexto:** parâmetros podem mudar enquanto uma execução está na fila ou depois de terminada.
- **Decisão:** editar uma revisão com If-Match/ETag; no início de cada execução persistir os parâmetros efetivos completos, cenário, seed, hashes e versão do modelo.
- **Alternativa considerada:** execução apontar somente para a configuração corrente.
- **Motivo:** o resultado precisa continuar reproduzível e explicável depois de novas edições.
- **Fonte:** SOMA, notas 10, 12 e 21; contratos de telemetria e treinamento fluxAI.

### D-005 — Experimentos não são operação de semáforo real

- **Contexto:** perfil C2 e medidas de desempenho atuais são de SUMO.
- **Decisão:** declarar `environment=simulation` ou `replay`; preservar amarelo e clearance; não publicar comandos de controle de campo nem chamar fluxo simulado de tempo real.
- **Alternativa considerada:** reutilizar a mesma interface para qualquer origem sem declarar o modo.
- **Motivo:** perfil atual documenta tempos e capacidade como hipóteses não calibradas em campo. O teste de RL de oito sinais não demonstra vantagem consistente.
- **Fonte:** `green_timing_profile_c2.json`, `docs/resultado-rl.md` e `docs/execucao-luna/contrato-telemetria.md`.

### D-006 — Valores ausentes permanecem ausentes

- **Contexto:** telemetria pode ter cobertura incompleta; exportação esparsa não significa genericamente zero.
- **Decisão:** cada métrica guarda nome, unidade, escopo, instante/episódio e fonte; amostra ausente é omitida e sua cobertura declarada. Contagens de chegadas, ativos e pendentes são campos separados.
- **Alternativa considerada:** preencher lacunas com zero e agregar no cliente.
- **Motivo:** evita resultados que parecem completos ou indicadores por semáforo sem atribuição observada.
- **Fonte:** SOMA, nota 04; `docs/execucao-luna/contrato-telemetria.md`.

### D-007 — Contrato identifica origem e cobertura da amostra

- **Contexto:** artefatos de avaliação e do callback de treinamento têm campos e níveis de agregação distintos.
- **Decisão:** o adapter transforma ambos em um `RunImport` tipado, preserva hashes, origem, unidades, instante/episódio e cobertura; cada amostra tem uma chave de fonte. Caminhos locais permanecem internos ao adapter.
- **Alternativa considerada:** aceitar um JSON arbitrário como resultado e exibi-lo diretamente no painel.
- **Motivo:** respostas de sistemas diferentes precisam manter significado estável e identidade da mesma execução.
- **Fonte:** SOMA, notas 04, 14 e 16; `docs/execucao-luna/contrato-telemetria.md`; `evaluation.json`, `final_simulation_verified.json`, `execution_header.json` e `episode_summary.json` de `stage15_final_verified_20261118`.

### D-008 — Limites por fase são um recurso contratado, ainda não suportado pelo worker

- **Contexto:** o controlador gera mínimo e máximo a partir de um perfil C2 compartilhado; o ambiente também normaliza observações temporais usando o teto de 90 s.
- **Decisão:** o domínio e OpenAPI permitem valores padrão e sobrescritas de fase, sempre com máximo de 90 s. A execução deve guardar o mínimo solicitado e o efetivo depois de aplicar a duração nominal da fase. Habilitar sobrescritas requer implementar e verificar sua tradução no worker/controle Python.
- **Alternativa considerada:** guardar valores por semáforo no banco e presumir que o processo Python os aplica.
- **Motivo:** persistência isolada não muda o comportamento do agente e uma normalização incompatível invalida a observação.
- **Fonte:** `sumo/scenarios/fluxia_stage6_c2/green_timing_profile_c2.json`, `scripts/fluxia_signal_control.py`, `scripts/fluxia_gym_env.py` e SOMA, notas 04, 14, 16 e 21.

## Lacunas registradas

- A API usa tokens Bearer locais por função, configurados no ambiente; não contém OAuth/JWT ou provedor de identidade. Em implantação real, ainda serão necessários gestão e rotação de credenciais e revisão de segurança.
- O contrato OpenAPI descreve as operações implementadas das Etapas 2-3. O documento 3.0.3 passou pelo Swagger Parser 12.1.0, incluindo suas referências e 15 rotas; a tentativa alternativa de instalar Microsoft.OpenApi.Hidi foi bloqueada pelo acesso ao NuGet. T01-T10 foram executados pela interface; a regressão final dos papéis e dos fluxos protegidos foi repetida por HTTP. Evidências em `docs/soa-w/evidencias/etapa-4-insomnia.md`.
- O worker atual ainda não aceita limites verdes por semáforo/fase individualmente; o contrato os prevê, então a integração Python terá de ser adaptada antes de habilitá-los no painel.
- Algumas perguntas de domínio permanecem: origem e chave canônica de cada cenário, retenção/volume de séries temporais, retenção de revisões e identificação escolhida para o cidadão. Não assumir valores antes de consultar rede e requisitos do grupo.

### D-009 — Usar .NET 10 LTS na implementação nova

- **Contexto:** a Etapa 2 vai iniciar uma API nova; o ambiente tem runtimes .NET 8, mas não tem SDK. A política oficial registra o fim do suporte ao .NET 8 em 10/11/2026.
- **Decisão:** direcionar a API para .NET 10 LTS e Entity Framework Core 10. Atualizar o README e os comandos de execução para essa base quando o SDK estiver disponível.
- **Alternativa considerada:** iniciar em .NET 8 porque há runtime instalado.
- **Motivo:** .NET 8 já está fora de suporte; .NET 10 LTS permanece ativo até 14/11/2028 conforme a política consultada.
- **Fonte:** [política oficial de suporte do .NET](https://dotnet.microsoft.com/pt-br/platform/support/policy), consultada em 18/11/2026.
- **Estado:** SDK 10.0.404 instalado; EF Core 10.0.15 restaurado; API compilada sem avisos; migration inicial e snapshot gerados por dotnet-ef. Banco vazio, seed C2 e persistência após reinício verificados em 18/11/2026.

### D-010 - Importar execuções concluídas por adaptador e preservar cobertura observada

- **Contexto:** SUMO e painel já produzem resumos, telemetria por controlador e artefatos com hashes; seus formatos diferem do recurso de execução da API.
- **Decisão:** o adaptador transforma uma execução concluída em `RunImport`; a API confere o fingerprint com o cenário cadastrado e grava execução, amostras e artefatos em uma transação. `sourceKey` identifica cada amostra e tem unicidade por execução. Repetição idêntica devolve o recurso existente; mesma identidade ou chave com dados divergentes resulta em 409.
- **Alternativa considerada:** receber caminhos arbitrários ou inserir resumo livre sem unidade e referência por métrica.
- **Motivo:** preservar a fronteira entre sistemas, rastrear origem e impedir duplicação ou conversão de ausência em zero.
- **Fonte:** SOMA, notas 04, 07, 14 e 16; `docs/execucao-luna/contrato-telemetria.md`; artefatos `execution_header.json`, `episode_summary.json`, `panel_session_summary.json` e `panel_telemetry.jsonl` do replay `rl_20261119T003313Z_073aa4`.
- **Estado:** API e migrations implementadas; replay importado e consultado; mesmo corpo/chave retornou o mesmo ID e corpo alterado retornou 409. O adaptador Python foi executado em modo local de normalização; o envio pelo próprio adaptador (`--post`) e o worker de treinamento permanecem pendentes.

### D-011 - Separar erro de sintaxe de violação de regra de domínio

- **Contexto:** o plano inicial dizia que qualquer validação de entrada retornaria 400, mas a API de configuração e importação usa respostas de validação semântica do ASP.NET Core.
- **Decisão:** JSON/parâmetros/cabeçalhos malformados usam 400; uma requisição bem formada que viola limites, identidade ou fase permitida usa 422 com mapa de erros por campo. ETag desatualizado usa 412.
- **Alternativa considerada:** mudar o servidor para 400 apenas para coincidir com o texto antigo do plano.
- **Motivo:** preservar a distinção já implementada entre entrada malformada e conteúdo que não pode ser processado pelas regras do domínio; contrato, roteiro e comportamento devem coincidir.
- **Fonte:** SOMA, notas 09 e 16; respostas HTTP observadas nos casos T03, T04 e T06 da Etapa 4.
- **Estado:** README, plano, OpenAPI e coleção foram alinhados. 404 e 412 de configuração agora usam `application/problem+json`.

### D-012 - Proteger operações por papel e documentar o comportamento real

- **Contexto:** as rotas de configuração e de importação de execução precisam distinguir operadores dos workers; uma declaração `security` no OpenAPI, sozinha, não protege o servidor.
- **Decisão:** exigir Bearer de operador em criação/leitura/atualização de configuração, Bearer de worker na importação e qualquer um desses papéis na leitura de execuções. Manter catálogo e saúde públicos. Ler os tokens de `Authentication__OperatorToken` e `Authentication__WorkerToken`; sem configuração, as rotas protegidas falham fechadas.
- **Alternativa considerada:** tratar todo consumidor como operador ou deixar as rotas sem autenticação durante a demonstração.
- **Motivo:** aplicar privilégio mínimo e tornar os status 401/403 observáveis e coerentes com o OpenAPI. Comparações de tokens usam tempo constante.
- **Fonte:** requisitos do projeto; conferência HTTP local dos casos T11a/T11b e das leituras por papel.
- **Limite:** autenticação local por token opaco não substitui OAuth/JWT, provedor de identidade, rotação e gestão de segredos de produção.
- **Estado:** implementada e verificada em 19/11/2026; OpenAPI 3.0.3 validado com Swagger Parser 12.1.0.

## Estado da implementação

- [x] Modelo de domínio e contrato inicial.
- [x] API ASP.NET Core, SQLite e migrations.
- [x] Catálogo demonstrativo C2 identificado como simulação.
- [x] Configuração versionada com ETag/If-Match.
- [x] Importação e consulta de execução concluída com métricas idempotentes (Etapa 3).
- [x] Execução do adaptador Python em modo local de normalização; envio próprio via `--post` e implementação do worker Python permanecem pendentes.
- [ ] Validação formal integral do OpenAPI e revisão humana do contrato; a instalação local do validador Microsoft.OpenApi.Hidi foi bloqueada pelo acesso indisponível ao índice do NuGet.
- [x] Casos T01-T10 executados pela interface do Insomnia e fluxos protegidos revalidados por HTTP; evidências em `docs/soa-w/evidencias/etapa-4-insomnia.md`.
- [x] Autenticação Bearer local por papel; T11a/T11b responderam `401`/`403`.
- [ ] Painel C# e aplicativo mobile.
