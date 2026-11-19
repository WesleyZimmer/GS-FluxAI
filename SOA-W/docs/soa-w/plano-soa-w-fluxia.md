---
projeto: fluxAI
disciplina: SOA & Webservices
status: etapas-1-a-4-concluidas-localmente
versao_plano: 0.5
referencia_principal: SOMA/Conteudos/SOA e APIs
---

# fluxAI — plano de trabalho de SOA&W

## 1. O que vamos construir e para que serve

**Uma API de gerenciamento do fluxAI, com banco de dados e integração com a IA em Python.** Ela será a ponte entre o treinamento/simulação, o painel C# e o aplicativo do cidadão.

O painel permitirá configurar experimentos, consultar execuções e entender o desempenho dos semáforos. O aplicativo poderá consultar informações de trânsito e enviar ocorrências. A API concentrará regras, validação e persistência, sem expor o banco diretamente aos clientes.

Este documento registra as decisões e etapas do projeto. As Etapas 1-4 estão concluídas localmente: API, persistência, importação, autenticação por papel, coleção Insomnia, evidências, README com os integrantes e validação do OpenAPI 3.0.3. A publicação Git foi deixada para uma etapa posterior conforme combinado. Worker e clientes são as Etapas 5-6.

| Matéria | O que faremos | Para que serve |
|---|---|---|
| SOA&W | API REST, domínio, banco, migrations, contratos e testes no Insomnia | Integrar as partes e manter informações consistentes |
| C# | Painel em Windows Forms consumindo a API | Gerenciar parâmetros e visualizar resultados/treinamento |
| IA | Simulação e treinamento em Python/SUMO; adaptador de integração | Aprender e avaliar estratégias de controle |
| Mobile | App React Native/Expo para consulta e registro de ocorrências | Dar acesso ao cidadão e preparar uma fonte adicional de dados |

**Ordem:** contrato e domínio de SOA → banco e API → integração com resultados existentes → testes e evidências → execução acompanhada pelo painel → integração do app.

## 2. O que a FIAP exige

Referência: *GS CitizenTech - 3ES Ago- FIAP 2026 (1).pdf*, página 27. A tecnologia é uma escolha do projeto; o enunciado admite qualquer linguagem.

| Critério de SOA&W | Valor | Evidência que vamos produzir |
|---|---:|---|
| Estrutura e domínio | 3,0 | Entidades relacionadas, responsabilidades separadas e regras de negócio demonstradas |
| Conexão com banco | 3,0 | Gravação e consulta reais; dados preservados após reiniciar a API |
| Migrations | 2,5 | Histórico versionado e criação do banco vazio pelas migrations |
| Testes manuais | 1,5 | Coleção Insomnia, requisições, respostas e evidências dos efeitos no banco |

Entregar código-fonte completo no GitHub, README com descrição, execução e integrantes, e evidências da aplicação. O envio de SOA&W é o link do repositório. O prazo registrado no enunciado é **19/11/2026 às 23h55**.

O enunciado também pede coerência entre as disciplinas, identificação do benefício ao cidadão e registro da assistência de IA em `prompts.md`. Integrantes informados: Wesley Zimmer (RM 560264) e João Victor Alvarenga (RM 93752).

**Prioridade:** concluir os quatro critérios avaliados antes de ampliar a infraestrutura. Banco e migrations somam 5,5 pontos; uma tela bonita ou um contrato sem servidor não substitui essas entregas.

## 3. Como o Obsidian será nosso motor

Cofre: **SOMA**. Pasta: **Conteudos/SOA e APIs**.

Caminho local de consulta:

```text
C:\Users\jonat\OneDrive\Área de Trabalho\SOMA_TOTAL\OBSIDIAN\SOMA\Conteudos\SOA e APIs
```

| Nota | Consulta no Obsidian | Aplicação no fluxAI |
|---|---|---|
| Mapa | [Mapa - SOA e APIs](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2FMapa%20-%20SOA%20e%20APIs.md) | Localizar assuntos e escolher a próxima consulta. |
| 02 | [02 - Contratos - fronteiras e principios de servicos](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F02%20-%20Contratos%20-%20fronteiras%20e%20principios%20de%20servicos.md) | Separar responsabilidades da API, do Python e dos clientes. |
| 04 | [04 - Integrar sistemas com adaptadores e contrato canonico](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F04%20-%20Integrar%20sistemas%20com%20adaptadores%20e%20contrato%20canonico.md) | Converter os arquivos da IA para o contrato da API, com identidade, unidades e origem. |
| 05–06 | [06 - Modelar recursos URIs e consultas](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F06%20-%20Modelar%20recursos%20URIs%20e%20consultas.md) | Modelar recursos e consultas; conferir também a nota 05 sobre REST. |
| 07 | [07 - HTTP - metodos seguros e idempotencia](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F07%20-%20HTTP%20-%20metodos%20seguros%20e%20idempotencia.md) | Evitar que uma repetição crie dois treinamentos ou duplique métricas. |
| 09 | [09 - Status HTTP e erros de API](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F09%20-%20Status%20HTTP%20e%20erros%20de%20API.md) | Definir sucesso, erros, cabeçalhos e Problem Details. |
| 10 | [10 - Stateless cache ETag e concorrencia](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F10%20-%20Stateless%20cache%20ETag%20e%20concorrencia.md) | Evitar dependência entre chamadas e perda de alterações concorrentes. |
| 12 | [12 - Evolucao de APIs - versionamento e compatibilidade](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F12%20-%20Evolucao%20de%20APIs%20-%20versionamento%20e%20compatibilidade.md) | Manter painel, app e worker compatíveis com a API. |
| 13 | [13 - OpenAPI - estrutura do contrato HTTP](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F13%20-%20OpenAPI%20-%20estrutura%20do%20contrato%20HTTP.md) | Escrever o contrato OpenAPI, operações e respostas. |
| 14 | [14 - Schemas - campos exemplos e limites de validacao](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F14%20-%20Schemas%20-%20campos%20exemplos%20e%20limites%20de%20validacao.md) | Separar entrada/saída e especificar campos, unidades, limites e ausência de dados. |
| 16 | [16 - Validar contratos e evitar documentacao divergente](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F16%20-%20Validar%20contratos%20e%20evitar%20documentacao%20divergente.md) | Conferir documento, exemplos, respostas HTTP e efeitos reais. |
| 17 | [17 - Autenticacao autorizacao bearer JWT e OAuth](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F17%20-%20Autenticacao%20autorizacao%20bearer%20JWT%20e%20OAuth.md) | Separar autenticação de autorização e proteger operações administrativas. |
| 21 | [21 - Roteiro integrado para desenhar e revisar uma API](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F21%20-%20Roteiro%20integrado%20para%20desenhar%20e%20revisar%20uma%20API.md) | Roteiro principal de execução e revisão do trabalho. |
| 22 | [22 - Exemplos - escopo execucao e verificacao](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F22%20-%20Exemplos%20-%20escopo%20execucao%20e%20verificacao.md) | Reutilizar ideias dos exemplos respeitando o que eles realmente implementam. |
| 23 | [23 - Fontes localizadores e cobertura](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F23%20-%20Fontes%20localizadores%20e%20cobertura.md) | Rastrear material original e lacunas. |
| 24 | [24 - Relatorio editorial - filtragem correcoes e adicoes](obsidian://open?vault=SOMA&file=Conteudos%2FSOA%20e%20APIs%2F24%20-%20Relatorio%20editorial%20-%20filtragem%20correcoes%20e%20adicoes.md) | Consultar correções e distinguir complementação editorial de exigência da aula. |

### Rotina obrigatória do nosso trabalho

1. Antes de cada etapa, abrir a nota indicada neste plano e conferir seu conteúdo atual.
2. Registrar a decisão do fluxAI, a nota utilizada e a regra aplicada.
3. Implementar um comportamento pequeno e demonstrável.
4. Conferir o contrato e o efeito real da operação, inclusive erros relevantes.
5. Atualizar a evidência e o próximo passo neste plano ou no registro de decisões.

Modelo de registro: **problema → decisão → alternativa considerada → fonte → arquivos alterados → verificação → pendências**. Marcar cada afirmação como requisito da GS, conceito do material ou decisão do fluxAI.

Não copiar o acervo inteiro para o código. Manter o Obsidian como referência didática e a documentação do projeto como memória das decisões aplicadas. As notas revisadas já distinguem material original, correções e complementações; preservar essa distinção.

**Limites encontrados no acervo:** a aula completa de autenticação não está incluída; os exemplos de contrato/adaptadores não são uma API pronta e não demonstram autenticação operacional. As notas 17, 22 e 23 registram esses limites. A tecnologia, a arquitetura e as rotas abaixo são propostas do fluxAI, não exigências atribuídas ao professor.

## 4. Arquitetura proposta

**Escolha inicial:** ASP.NET Core Web API em C#, Entity Framework Core e SQLite para o protótipo local. Confirmar o SDK disponível ao iniciar a implementação e fixar a versão no projeto e no README. A escolha aproveita a matéria de C#; o painel Windows Forms continua sendo um cliente separado.

Começar com uma API organizada em módulos de domínio, aplicação, infraestrutura e HTTP. Não há necessidade inicial de vários microsserviços, broker, gateway ou SOAP.

| Componente | Responsabilidade | Como se comunica |
|---|---|---|
| API fluxAI | Validar configurações, registrar execuções, guardar resultados e atender clientes | HTTP/JSON com contrato versionado |
| Banco relacional | Persistir entidades, vínculos, versões e resultados | Acesso exclusivamente pela camada de persistência da API |
| Adaptador/worker Python | Ler trabalho autorizado, gerar configuração, executar scripts e enviar métricas | API; reaproveita os scripts e artefatos atuais |
| Painel Windows Forms | Editar configuração, solicitar execução e exibir progresso/resultados | API; sem acesso direto ao SQLite |
| App do cidadão | Consultar resumo e enviar ocorrência geolocalizada | API pública/restrita conforme operação |

**Dois tipos de integração, explicitamente identificados:**

- Importação de execução existente: alimenta o histórico com resultados reais já produzidos em simulação.
- Execução nova acompanhada: um worker recebe a solicitação e acompanha o processo Python.

Importar um resultado não será apresentado como treinamento iniciado pela API. A rota que inicia treinamento só ficará disponível quando houver execução real por trás dela.

## 5. Domínio e persistência

Modelar inicialmente as entidades abaixo. Os nomes são propostas para refinamento junto ao contrato.

| Entidade | Dados principais | Regra relevante |
|---|---|---|
| Cenário | ID, rede/catálogo, versão e hashes dos artefatos | Um resultado pertence a uma rede e demanda identificáveis |
| Controlador semafórico | ID, cenário e fases conhecidas | Uma fase só pode ser configurada no controlador ao qual pertence |
| Configuração | Nome, cenário, revisão, parâmetros e datas | Editável para próxima execução; revisão protege edição concorrente |
| Parâmetro de verde | Configuração, controlador, fase, mínimo e máximo em segundos | Valores finitos, mínimo positivo e máximo compatível com mínimo e perfil |
| Execução | ID, origem, modo, seed, estado, datas e snapshot de configuração | Snapshot imutável; histórico não muda quando a configuração é editada |
| Métrica de execução | Execução, episódio/instante, nome, valor, unidade e escopo | Identidade única impede duplicação; ausência não vira zero |
| Resultado por controlador | Execução, controlador, janela e indicadores disponíveis | Só comparar indicadores realmente medidos e atribuíveis |
| Ocorrência cidadã — etapa mobile | Tipo, localização, descrição, data e estado de análise | Receber relato não autoriza alteração de sinal |

Relacionamentos: cenário possui controladores; configuração possui parâmetros por fase; execução aponta para cenário e revisão, além de manter sua cópia efetiva; métricas/resultados pertencem à execução. Catálogo de modelos e artefatos pode começar como metadados associados, sem criar tabelas desnecessárias.

Criar migrations de esquema com chaves estrangeiras, índices e restrições adequadas. Preparar uma carga demonstrativa identificada como simulação. Não usar criação automática de tabelas como substituta das migrations.

**Prova de persistência:** criar uma configuração pela API, reiniciar o servidor e consultá-la; depois criar um banco vazio pelas migrations e executar a carga demonstrativa. Registrar comandos e resultados efetivamente observados.

## 6. Contrato HTTP inicial

Base proposta: `/api/v1`. Usar **OpenAPI 3.0.3**, alinhado ao material estudado, separando modelos de entrada, saída e erro. Versão do documento OpenAPI, versão da API e revisão de configuração são conceitos diferentes.

| Operação proposta | Finalidade | Resultado principal | Etapa |
|---|---|---|---|
| GET /scenarios | Listar cenários disponíveis | 200 | Núcleo |
| GET /signal-controllers?scenarioId=... | Consultar controladores/fases | 200 | Núcleo |
| POST /configurations | Criar configuração de experimento | 201 + Location | Núcleo |
| GET /configurations/{id} | Consultar configuração e revisão | 200 + ETag | Núcleo |
| PUT /configurations/{id} | Substituir todos os campos editáveis, com If-Match | 200 + novo ETag | Núcleo |
| POST /run-imports | Importar resultado validado de uma execução existente | 201 + Location da execução | Integração inicial |
| GET /runs?scenarioId=...&status=... | Listar execuções, com paginação | 200 | Núcleo |
| GET /runs/{id} | Consultar estado, origem e configuração efetiva | 200 | Núcleo |
| GET /runs/{id}/metrics | Consultar série/episódios por filtros documentados | 200 | Integração inicial |
| GET /runs/{id}/signal-results | Consultar indicadores por controlador | 200 | Integração inicial |
| POST /runs | Solicitar nova execução ao worker | 202 + Location de acompanhamento | Orquestração |
| POST /citizen-reports | Registrar ocorrência do app | 201 + Location | Integração mobile |
| GET /traffic-summary | Expor resumo apropriado ao cidadão | 200, com origem e atualização | Integração mobile |

As rotas do worker, incluindo obtenção de trabalho, confirmação de início e envio de lotes de métricas, serão fechadas no contrato da etapa de orquestração. O backend deve controlar transições; o worker não pode atribuir livremente qualquer estado.

**Regras comuns:**

- Listas com limite padrão 20 e máximo 100 como convenção inicial do projeto; filtros e ordenação em lista permitida. Séries temporais têm janela e limite próprios documentados.
- 400 para JSON, consultas ou cabeçalhos malformados; 401 para ausência de credencial; 403 para papel insuficiente; 422 para uma entrada bem formada que viola regra de domínio; 404 para recurso ausente; 409 para conflito de domínio; 412 para revisão desatualizada. O servidor valida tokens Bearer locais de operador e worker nas rotas protegidas.
- Validações de entrada, inclusive limites de verde, retornam 422 com detalhes de campo. Falta de If-Match obrigatório retorna 400; ETag desatualizado retorna 412.
- Erros usam `application/problem+json`; validações incluem um mapa `errors`. Código de domínio estável e identificador próprio de ocorrência ainda não foram implementados. Não depender do texto livre para tomar decisões.
- GET não altera configuração nem inicia treino. PUT substitui a representação editável completa; não será um PATCH disfarçado.
- Não expor DELETE para apagar histórico de execução. Cancelamento, se adicionado, será uma transição própria.
- Requisições trazem o recurso e contexto necessários; não haverá variável global de “última execução do usuário”.
- Definir cache explicitamente; começar com `no-store` para configuração/estado operacional. ETag de escrita será validado atomicamente junto à atualização.

**Repetição:** `POST /runs` exigirá chave de idempotência, vinculada ao ator e ao conteúdo normalizado. Mesma chave/conteúdo retorna a mesma execução; conteúdo diferente retorna 409. Persistir e impor unicidade em transação; um cabeçalho sozinho não basta. Importações e lotes de métricas também terão identificação estável para evitar duplicação.

## 7. Controle dos parâmetros da IA

### O que já existe

O script [train_fluxia_ppo.py](../../scripts/train_fluxia_ppo.py) recebe perfil de tempos, cenário/rotas, seed, duração e parâmetros de treinamento. Produz arquivos de episódios e checkpoints. O perfil atual está em [green_timing_profile_c2.json](../../sumo/scenarios/fluxia_stage6_c2/green_timing_profile_c2.json).

O controlador [fluxia_signal_control.py](../../scripts/fluxia_signal_control.py) já constrói limites por fase. Porém, o perfil atual é global e calcula o mínimo efetivo como o menor entre o mínimo de decisão e a duração nominal importada daquela fase. **Portanto, o campo “mínimo” do painel não pode prometer um comportamento diferente sem ajuste explícito do adaptador/controlador.**

O ambiente [fluxia_gym_env.py](../../scripts/fluxia_gym_env.py) possui normalização relacionada a 90 segundos. Alterar limites além do perfil compatível exige revisar observações, contrato do modelo e avaliação.

### O que implementaremos

1. Configuração inicial com parâmetros globais; evolução para sobrescrita por controlador/fase.
2. Tela/API distinguindo **valor solicitado** de **valor efetivo por fase**.
3. Snapshot e hash dos parâmetros enviados a cada execução.
4. Adaptador que traduz esse snapshot para o formato esperado pelo Python e valida sua compatibilidade.
5. Confirmação, nos resultados, de qual configuração realmente foi utilizada.
6. Alterações aplicadas em nova execução. Ajuste durante execução fica fora da primeira versão.

Começar com mínimo/máximo do verde, seed, horizonte e quantidade de passos. Parâmetros avançados de PPO entram depois, com limites explícitos. IDs de cenário e modelo serão resolvidos por catálogo interno; a API não aceitará comandos de shell nem caminhos arbitrários do cliente.

No experimento atual, 10 s e 90 s são valores do perfil de simulação, não tempos oficiais de campo. Amarelo, transições e tempos de limpeza permanecem sob as regras do controlador existente. Não incluir botões de acionamento de semáforos reais.

## 8. Integração de resultados e acompanhamento

Usar como referência técnica do projeto o [contrato de telemetria](../execucao-luna/contrato-telemetria.md) e o [estado atual dos resultados de RL](../resultado-rl.md).

### Adaptador de importação — primeira integração

Ler artefatos conhecidos de uma execução concluída, validar identidade e formato, normalizar e enviar para a API. Priorizar resumo da execução e episódios; acrescentar séries por controlador conforme disponibilidade. O adaptador registra origem, versão, hashes e cobertura.

Regras a preservar:

- Tempo simulado em segundos; data real com fuso/UTC. Não misturar relógios.
- Ocupação em fração de 0 a 1; velocidade em m/s; duração/espera em segundos.
- Ausente, zero e não medido são diferentes. Só inferir zero em exportação esparsa quando catálogo e cobertura justificarem expressamente.
- Separar veículos que chegaram, ativos e pendentes. Não chamar veículo ativo de “atendido”.
- Não tratar proxy de bloqueio como colisão confirmada.
- Rejeitar execução/controlador incompatível e lote duplicado divergente.
- Informar `simulation` e se a apresentação é histórica/replay ou acompanhamento de execução. Não rotular isso como trânsito urbano ao vivo.

### Worker — segunda integração

Fluxo: **solicitação persistida → obtenção exclusiva pelo worker → execução do Python → envio de progresso → conclusão ou falha registrada**.

Estados propostos: `queued`, `running`, `succeeded`, `failed`, `interrupted`. Registrar heartbeat e detectar perda do worker; nunca converter silêncio em sucesso. Não reiniciar automaticamente treino interrompido, pois isso pode repetir trabalho e resultados.

Uma execução ativa por worker no primeiro protótipo. Polling HTTP é suficiente inicialmente. Limitar recursos e guardar referência de logs/checkpoints. Retomar checkpoint não será anunciado como pausa/retomada exata do simulador: o código atual oferece warm start, com limitações documentadas.

### O que o painel mostrará

| Visão | Indicadores |
|---|---|
| Treinamento | Episódio, passos, recompensa e seus componentes, estado e último progresso |
| Execução | Configuração efetiva, cenário, seed, duração, origem e versão do modelo |
| Trânsito | Chegadas, fila/espera quando medidas, ativos, pendentes, colisões/teleportes |
| Por controlador | Fila e espera atribuíveis às faixas de entrada, janela e cobertura dos dados |
| Comparação | Baseline × política da IA no mesmo cenário, demanda, seeds e horizonte |

“Treinou” não significa “melhorou o trânsito”. O registro atual de RL ainda não demonstra ganho consistente. O painel deve permitir enxergar essa diferença, inclusive resultados piores.

Classificar um semáforo como melhor/pior somente com indicador definido, dados suficientes e comparação compatível. Sem medição suficiente, mostrar **sem dados suficientes**. Se ainda não existe uma métrica individual, implementar sua coleta/agregação antes de criar o indicador visual. Não atribuir chegadas de toda a rede a cada semáforo.

## 9. Acesso e aplicativo do cidadão

Separar operações de operador, worker e cidadão. O servidor aplica as permissões; esconder um botão não é controle de acesso.

Para a demonstração local, começar com credenciais opacas distintas de operador e worker, em configuração fora do repositório, e middleware que autentica e autoriza de verdade. Isso é uma limitação do protótipo, não gestão completa de usuários ou OAuth. A integração mobile exigirá definir identificação, permissões e exposição dos relatos; não embutir a credencial administrativa no app.

Guardar apenas os dados necessários da ocorrência. Na GS, usar dados simulados/de demonstração identificados. A listagem pública não deve revelar identidade nem localização individual detalhada do autor.

O relato do cidadão inicialmente entra como ocorrência consultável. A futura utilização como entrada da IA depende de validação, qualidade, vínculo espacial/temporal e avaliação. Não ligar um relato diretamente à mudança do sinal.

## 10. Etapas e critérios de conclusão

| Etapa | Fazer | Consultar | Concluída quando |
|---|---|---|---|
| 0 — Planejamento | Delimitar entrega, integração e fontes | Mapa, 21, GS p.27 | Este plano está disponível |
| 1 — Domínio e contrato | Fechar entidades, relações, schemas, operações e exemplos | 02, 05–07, 09, 13–14, 21 | Diagrama/modelo + OpenAPI revisados, com regras e erros explícitos |
| 2 — Banco e migrations | Criar API, persistência, migrations e carga de demonstração | 02, 14; critérios da GS | Banco vazio é criado por migrations e dados sobrevivem a reinício |
| 3 - API e integração inicial | Configurações, consultas e importação dos resultados existentes | 04, 06-07, 09-10, 16, 22 | **Concluída:** replay identificado importado e consultado; retry idempotente não duplica métricas e corpo divergente retorna conflito |
| 4 - Evidências e entrega SOA | Executar Insomnia, corrigir divergências, preparar README/repositório | 12, 16, 21; GS p.27 | T01-T10 executados, divergências corrigidas, OpenAPI 3.0.3 validado e evidências reproduzíveis revisadas; publicação ainda pendente |
| 5 — Execução acompanhada | Worker, fila persistida, parâmetros efetivos e progresso | 04, 07, 10, 14, 16–17 | Uma execução curta nasce pela API e termina com resultados verificáveis |
| 6 — Clientes | Conectar Windows Forms e depois app Expo | 02, 12–14, 17 | Painel usa API e app consulta/envia ocorrência com estados de erro |

**Corte de entrega:** concluir 1–4 primeiro. Isso entrega SOA com integração real aos resultados da IA. Etapa 5 completa o controle de treinamento solicitado para o painel; etapa 6 atende às interfaces das outras matérias. Se houver tempo antes da entrega, antecipar a execução curta da etapa 5, sem deixar migrations e evidências para o final.

Não iniciar treinamentos longos para provar integração. Uma execução curta, identificada como teste, demonstra o fluxo; avaliação de desempenho da IA é outro trabalho.

## 11. Roteiro de testes manuais no Insomnia

Salvar coleção exportável e ambiente sem segredos. T01-T10 foram executados no Insomnia 13.4.0; os fluxos foram revalidados por HTTP após proteger as rotas. T11a/T11b confirmaram 401/403. Os resultados, efeitos e limitações estão em `docs/soa-w/evidencias/etapa-4-insomnia.md`. T12 foi verificado com banco vazio. T13-T15 pertencem às etapas futuras.

| Caso | Ação | Verificação esperada |
|---|---|---|
| T01 | Listar cenário/controladores conhecidos | 200 e IDs coerentes |
| T02 | Criar configuração válida | 201, Location e registro persistido |
| T03 | Enviar mínimo maior que máximo; negativo; fase desconhecida | 422 com erro de campo e nenhuma gravação parcial |
| T04 | Consultar ID inexistente | 404 em Problem Details |
| T05 | Substituir configuração com revisão atual | 200, dados completos e nova revisão |
| T06 | Repetir escrita com revisão antiga | 412 e preservação da edição mais recente |
| T07 | Reiniciar API e consultar configuração | Dados e revisão continuam presentes |
| T08 | Importar execução conhecida e repetir importação | Mesma identidade, sem duplicar registros |
| T09 | Importar dados com identidade/unidade inválida | Rejeição documentada e sem resultado silenciosamente adulterado |
| T10 | Consultar métricas e limites de paginação | Valores, unidades, origem e ausência coerentes com o contrato |
| T11a | Acessar escrita sem credencial | `401` com desafio Bearer e Problem Details |
| T11b | Worker tentar escrita de operador | `403` com Problem Details; papel sem permissão |
| T12 | Criar banco vazio a partir das migrations | Esquema criado e roteiro principal reproduzível |
| T13 — etapa 5 | Solicitar execução curta e repetir com mesma chave | Uma única execução; 202 não é apresentado como conclusão |
| T14 — etapa 5 | Acompanhar execução/falha do worker | Transições válidas, configuração efetiva e estado final fiel |
| T15 — etapa mobile | Enviar ocorrência válida/inválida | Persistência da válida, rejeição explicada da inválida |

Além das chamadas, validar o OpenAPI com ferramenta compatível com sua versão, os exemplos e o comportamento real. O verificador didático do Obsidian possui cobertura limitada; não anunciá-lo como validador universal. Escolher verificações automatizadas pequenas para invariantes de domínio e integração, sem substituir os testes manuais pedidos.

## 12. Organização dos arquivos e entrega

Estrutura preparada para a entrega SOA&W e evolução posterior:

| Local | Conteúdo |
|---|---|
| `src/FluxAI.Api/` | API, domínio/aplicação, persistência e migrations |
| `scripts/fluxai_api_adapter.py` | Normaliza resultados concluídos e envia-os à API com hashes e cobertura |
| `scripts/fluxia_api_worker.py` | Worker da etapa 5, a ser implementado |
| `docs/soa-w/openapi.yaml` | Contrato revisado |
| `docs/soa-w/decisoes.md` | Decisões com referência às notas do SOMA |
| `docs/soa-w/insomnia/` | Coleção e ambientes exportados sem credenciais |
| `docs/soa-w/evidencias/` | Evidências reais, identificadas por caso |
| `README.md` | Ficha CitizenTech, integrantes/RMs, pré-requisitos, execução e limitações |
| `prompts.md` | Uso de assistência de IA e revisão humana |

No planejamento inicial, estes componentes foram propostos. A API das Etapas 2-3 e o adaptador inicial estão implementados. README, prompts, coleção Insomnia e fixtures foram preparados, T01-T10 foram executados pela interface e o OpenAPI 3.0.3 foi validado com Swagger Parser 12.1.0. A publicação em repositório remoto permanece pendente conforme combinado.

No README: explicar benefício ao cidadão, relação entre matérias, dados/fontes, indicador de impacto, como criar banco, subir API, executar testes e reproduzir a demonstração. ODS 11 é um enquadramento proposto, a justificar pela solução. Informar explicitamente que a demonstração opera sobre simulação.

O repositório Git local foi inicializado na branch `main` e delimitado ao material SOA&W pelo `.gitignore`. Ainda não há commit nem remoto: a conexão ao GitHub depende do destino correto. Não inventar commits, PRs ou evidências concluídas.

## 13. Primeira etapa executada

O domínio foi modelado em modelo-de-dominio.md, o contrato inicial em openapi.yaml e as decisões/proveniência em decisoes.md. O contrato inicial também delimita importação de resultados, execução por worker e integração mobile para as próximas etapas.

## 14. Etapa 2 concluída - banco, migrations e configuração

**Concluída em 18/11/2026.** A API ASP.NET Core em .NET 10 usa EF Core 10.0.15 e SQLite. A migration 20261119024340_InitialPersistence cria o esquema a partir de um banco vazio. A API aplica migrations ao iniciar e carrega de forma idempotente o piloto C2 de demonstração: um cenário, oito controladores adaptativos e uma configuração baseline. A base padrão fica em src/FluxAI.Api/App_Data/fluxia.db; o .gitignore local exclui o banco e os artefatos bin/obj.

Rotas implementadas nesta etapa:

- GET /healthz
- GET /api/v1/scenarios
- GET /api/v1/signal-controllers?scenarioId=fluxia-stage6-c2-8-signals
- POST /api/v1/configurations
- GET /api/v1/configurations/{id}
- PUT /api/v1/configurations/{id}, exigindo If-Match e criando nova revisão imutável

**Verificações executadas:** dotnet build src/FluxAI.Api/FluxAI.Api.csproj --no-restore terminou com 0 avisos e 0 erros. Em uma base SQLite temporária vazia, a migration criou as tabelas e o seed retornou 1 cenário, 8 controladores e a configuração baseline. Após parar e reiniciar a API, esses dados permaneceram. Na base local, POST e GET criaram e consultaram uma configuração; PUT com If-Match "1" produziu revisão 2; depois do reinício, o GET retornou a mesma configuração na revisão 2. A API também respondeu healthy em /healthz. Essas chamadas foram feitas localmente por HTTP; ainda não foram repetidas no Insomnia.

**Limites ao fim da Etapa 2:** autenticação/autorizações ainda não estavam implementadas, embora o contrato descreva bearerAuth; o servidor estava preparado para uso local da etapa acadêmica. O cenário e os limites de sinal são somente de simulação.

**Próxima ação na Etapa 2:** importar uma execução existente e consultar resultados sem duplicar métricas. Isso foi executado na Etapa 3 abaixo.

Checklist atualizado:

- [x] Planejamento, domínio e contrato inicial.
- [x] API ASP.NET Core e SDK .NET 10.0.404.
- [x] Persistência SQLite e migration inicial.
- [x] Carga demonstrativa C2 identificada como simulação.
- [x] Criação, consulta e atualização versionada de configuração.
- [x] Criação de banco vazio por migration e persistência confirmada após reinício.
- [x] Validação do OpenAPI 3.0.3 com Swagger Parser 12.1.0; 15 rotas e referências locais aceitas.
- [x] Autenticação Bearer local por papel; T11a/T11b retornam 401/403.
- [x] Importação idempotente de execuções e métricas.
- [x] T01-T10 executados no Insomnia; fluxos protegidos revalidados por HTTP; T11a/T11b retornaram 401/403.
- [ ] Publicar no GitHub após definir o repositório de destino.
- [x] Preencher nomes e RMs: Wesley Zimmer (560264) e João Victor Alvarenga (93752).
- [ ] Worker de execução, painel C# e app mobile.
## 15. Etapa 3 concluída - importação e consultas de resultados

**Concluída em 19/11/2026.** Foram adicionadas as entidades `TrainingRun`, `RunMetric` e `RunArtifact`, a migration `20261119031702_AddImportedRuns` e a correção de ordenação de execuções para SQLite em `20261119032401_AddRunCreatedAtOrderKey`. O POST de importação valida o fingerprint do cenário, origem concluída, unidades, escopos/controladores, timestamps, limites e hashes dos artefatos. A transação persiste execução, pontos de métrica e artefatos; a chave de idempotência e a identidade cenário/origem têm índices únicos.

Rotas desta etapa:

- `POST /api/v1/run-imports` - importa simulação/replay concluído; retorno `201`; repetição idêntica retorna `200` com `Idempotency-Replayed: true`; identidade ou corpo divergente retorna `409`.
- `GET /api/v1/runs` - lista com filtros por cenário, estado, tipo e paginação.
- `GET /api/v1/runs/{id}` - consulta origem, fingerprint, parâmetros efetivos, resumo, artefatos e limitações.
- `GET /api/v1/runs/{id}/metrics` - consulta amostras com filtros, paginação e cobertura coerente com a série.
- `GET /api/v1/runs/{id}/signal-results` - calcula média das filas observadas por controlador e informa cobertura; não classifica sinais como bons ou ruins.

**Demonstração importada:** `rl_20261119T003313Z_073aa4`, replay da seed `20261301`, cenário `fluxia-stage6-c2-8-signals`, horizonte de 600 s. A origem registra 595 veículos inseridos, 218 chegadas, 377 ativos, 6 pendentes, zero eventos de colisão confirmados, zero passos de veículo em colisão e zero teletransportes. Foram persistidos 4.800 pontos de fila por controlador, cobertura 4.800/4.800, e 11 referências de artefatos com hashes.

**Verificações executadas por HTTP local:** importação `201`; retry com mesmo corpo/chave `200` e mesma identidade; corpo alterado na mesma chave `409`; apenas uma execução com aquela identidade; consultas da execução, listagem, métricas paginadas e oito resumos por controlador; reinício da API e leitura dos dados persistidos. Build .NET 10 terminou com zero avisos e zero erros; as migrations foram aplicadas ao SQLite padrão do projeto.

**Limite de validação do adaptador:** `scripts/fluxai_api_adapter.py` restringe caminhos ao workspace e confere hashes da rede, catálogos, rotas, perfil e checkpoint antes de montar o payload. O modo local de normalização foi executado com o replay: produziu 4.807 métricas, 11 referências de artefato e cobertura de fila 4.800/4.800. O envio HTTP pelo próprio adaptador (`--post`) e o worker de treinamento permanecem pendentes.

**Próxima ação:** concluir evidências e revisão local da Etapa 4. Worker e painel ficam para a Etapa 5. A validação formal do OpenAPI depende de acesso ao NuGet; a publicação Git foi combinada para outro momento.

## 16. Etapa 4 concluída localmente — revisão de contrato e evidências

**Concluída localmente em 19/11/2026.** Foram preparados `README.md`, `prompts.md`, a coleção Insomnia sem credenciais, a carga JSON do replay conhecido, instruções de execução e requisições HTTP de leitura. T01-T10 foram conferidos por HTTP e pela interface do Insomnia 13.4.0 usando SQLite temporário isolado. Após implementar autenticação, os fluxos protegidos e as permissões foram revalidados por HTTP; T11a/T11b retornaram 401/403. Status, corpos, revisões, retry, cobertura e paginação estão em `docs/soa-w/evidencias/etapa-4-insomnia.md`.

A revisão identificou e corrigiu divergências de segurança declarada, descrição de implementação, porta local, formato de Problem Details, exemplo de ETag, status de validação semântica e parâmetro obrigatório de paginação por controlador. A validação de regras de configuração e importação retorna 422; 404/412 de configuração agora usam `application/problem+json`.

**Validação e nome:** o OpenAPI 3.0.3 passou no Swagger Parser 12.1.0, com 15 rotas e referências locais aceitas. O projeto SOA&W foi renomeado de FluxIA para fluxAI; a API está em `src/FluxAI.Api`, e a coleção e o adaptador usam os novos nomes. A instalação alternativa de Microsoft.OpenApi.Hidi 3.10.2 não avançou porque o feed do NuGet não pôde ser acessado. A coleção, README e evidências contêm Wesley Zimmer (RM 560264) e João Victor Alvarenga (RM 93752). A publicação Git foi deixada para outro momento conforme solicitado. T01-T11 estão verificados e registrados; T13-T15 pertencem às etapas futuras.

**Pacote local preparado:** `dist/fluxAI-SOAW-entrega-local-20261119.zip` foi aberto e conferido: 56 arquivos; inclui API, contrato, coleção, evidências, README, registro de assistência e cenário C2; exclui Git, banco, artefatos de build e este plano interno com referência local.
