# Modelo de domínio do fluxAI

**Etapa 1 do plano SOA&W — proposta inicial, antes da implementação do banco.**

O domínio descreve a experiência de controle de tráfego em SUMO. A unidade de controle configurável é uma fase verde de um controlador semafórico, identificada pela rede e pelo índice da fase. O projeto não configura amarelo nem clearance; essas transições vêm do perfil/rede e continuam protegidas pelo controlador Python.

## Entidades e responsabilidades

| Entidade | Identidade | Responsabilidade e invariantes |
|---|---|---|
| **Scenario** | ID estável do catálogo | Identificar rede, catálogo ordenado de controladores, perfil de controle, versão e hashes dos arquivos usados. Um cenário selecionado pelo cliente precisa constar do catálogo permitido. |
| **SignalController** | Par (ScenarioId, ControllerId) | Representar o controlador dentro de uma rede específica. Guardar suas fases e quais são verdes/adaptativas. O mesmo ID em outra rede é outro recurso. |
| **TrainingConfiguration** | UUID | Guardar a revisão editável para execuções futuras: cenário, parâmetros de verde, perfil de bloqueio e hiperparâmetros de aprendizado. Alterar uma revisão nunca altera uma execução antiga. |
| **GreenPhaseOverride** | (ConfigurationId, ControllerId, PhaseIndex) | Sobrescrever mínimo de decisão e/ou máximo de verde para uma fase verde conhecida desse controlador. A fase precisa existir e ser verde no catálogo. |
| **TrainingRun** | UUID gerado pelo servidor | Registrar treino, avaliação, diagnóstico ou replay. Congelar cenário, configuração efetiva, seed, modelo e versões. Estado permitido: `queued → running → succeeded/failed/interrupted`. |
| **RunMetric** | (RunId, escopo, chave de origem, nome e amostra) | Preservar valor, unidade, janela, origem e cobertura. Uma amostra repetida com a mesma identidade não cria outro registro; conteúdo divergente é conflito. |
| **RunArtifact** | UUID gerado pela API | Guardar referência lógica, nome, SHA-256 e tamanho do artefato. Guardar caminhos sob controle do servidor, nunca os fornecidos pelo painel. |
| **CitizenReport** | UUID gerado pelo servidor | Registrar um relato com tipo, localização, momento e estado de análise. Relatos não alteram sinais nem viram dado de treinamento antes de validação. |

### Relações previstas

- Um Scenario contém muitos SignalControllers.
- Uma TrainingConfiguration pertence a um Scenario e guarda zero ou mais GreenPhaseOverrides.
- Uma TrainingRun usa um Scenario, registra a revisão de configuração escolhida e guarda um **snapshot imutável** dos parâmetros efetivos.
- Uma TrainingRun tem muitas RunMetrics e RunArtifacts.
- Uma RunMetric pode ser agregada para a rede toda ou para um controlador identificável no mesmo Scenario.
- CitizenReport é independente dos resultados de treinamento. O vínculo futuro entre relato e sinal exige análise espacial e de qualidade.

## Regras do modelo

1. IDs de execução, revisão e artefato são atribuídos pela API. Um cliente não define estado terminal nem identidade de outra execução.
2. Parâmetros são números finitos; o mínimo verde é positivo e não supera o máximo. A fase precisa pertencer ao catálogo da rede.
3. Para o perfil C2 atual, o mínimo efetivo de uma fase é `min(mínimo de decisão configurado, duração nominal verde da fase)`; o teto experimental global é 90 s. O snapshot guarda os valores solicitados **e** os efetivos. Uma fase com duração nominal inferior não recebe um mínimo impossível.
4. A normalização temporal do ambiente da IA pressupõe o teto de 90 s. Aceitar teto superior exige atualizar e compatibilizar a observação/modelo; até lá o servidor o rejeita.
5. Hiperparâmetros são validados em conjunto: por exemplo, `batchSize` não pode exceder `rolloutSteps`; regras mais específicas permanecem na camada de domínio, não apenas no schema HTTP.
6. Métrica ausente significa “não medida”. Ela não pode ser substituída por zero. Zero é guardado como valor medido somente quando a execução o reporta.
7. `arrived`, `activeAtEnd` e `duePendingAtEnd` são contagens distintas. `collisionVehicleSteps`, colisões confirmadas e teletransportes também não são intercambiáveis.
8. Repetir uma solicitação de execução com a mesma chave e conteúdo retorna a mesma execução; repetir a chave com conteúdo diferente dá conflito.
9. A mudança concorrente de uma configuração precisa conferir a revisão/ETag na mesma operação de banco que a grava. O snapshot da execução mantém a revisão que recebeu.
10. `succeeded` descreve conclusão técnica do processo. Não significa que a política reduziu congestionamento ou é melhor que a baseline.

## Valores iniciais de experimento conhecidos

O perfil em `sumo/scenarios/fluxia_stage6_c2/green_timing_profile_c2.json` declara mínimo de decisão de 10 s, máximo verde de 90 s, bloqueio observado de 0,95, confirmação em duas amostras e idade máxima da medição de 6 s. O próprio arquivo identifica esses números como hipóteses para **simulação**, sem calibração de campo. Amarelo e clearance não são parâmetros de treinamento editáveis.

A configuração C2 guarda um perfil global. Sobrescritas por controlador/fase fazem parte do domínio e do contrato futuro; exigem adaptar o worker Python para traduzi-las e comprovar os valores efetivos. Não basta persistir o campo no SQLite.

## Mapeamento relacional proposto

- `Scenarios(Id, CatalogRevision, NetworkSha256, CatalogSha256, TimingProfileId, IsEnabled)`
- `SignalControllers(ScenarioId, ControllerId, IsAdaptive, PhaseCatalogJson)`, chave primária composta.
- `TrainingConfigurations(Id, ScenarioId, Name, Revision, GreenTimingJson, BlockingJson, PpoJson, CreatedAtUtc, UpdatedAtUtc)`; índice único por `(Id, Revision)` se revisões forem tabelas separadas, ou chave otimista em `Revision` se a linha for atualizada.
- `GreenPhaseOverrides(ConfigurationId, ControllerId, PhaseIndex, DecisionMinimumSeconds, MaximumSeconds)`; chave única `(ConfigurationId, ControllerId, PhaseIndex)`.
- `TrainingRuns(Id, ScenarioId, ConfigurationId, ConfigurationRevision, RunType, Origin, Environment, Status, Seed, ModelId, EffectiveParametersJson, ProgressJson, CreatedAtUtc, StartedAtUtc, FinishedAtUtc)`.
- `RunMetrics(Id, RunId, Scope, ControllerId, MetricName, Unit, Value, Episode, Step, SimulationTimeSeconds, SourceArtifactId, SourceSampleKey)`; índice único definido pela chave de idempotência da amostra.
- `RunArtifacts(Id, RunId, LogicalName, Sha256, SizeBytes)`, com chave única por `(RunId, LogicalName)`.
- `CitizenReports(Id, Type, Latitude, Longitude, Description, OccurredAtUtc, Status, CreatedAtUtc)`, sem identidade pessoal ou coordenada pública na resposta.

O JSON de catálogo de fases fica inicialmente versionado pelo hash do cenário e é tratado como entrada imutável. Normalizar fases em tabela própria quando o painel precisar filtrá-las/consultá-las independentemente. Antes da migration, decidir se revisões são cópias completas em tabela `ConfigurationRevisions`; essa opção facilita auditoria e histórico. A chave de revisão nunca deve ser só um número enviado pelo cliente.

## Fontes da decisão

- SOMA, nota 02: fronteira do serviço e independência da implementação.
- SOMA, nota 04: validar identidade, unidade, proveniência e política de incompletude no adaptador.
- SOMA, notas 06–07: recursos, efeitos da operação e repetição.
- SOMA, notas 13–14: contrato, schemas, entrada/saída e limites de validação.
- SOMA, nota 21: sequência domínio → interface → contrato → políticas → implementação → verificação.
- Projeto: `docs/execucao-luna/contrato-telemetria.md`, `docs/resultado-rl.md`, `scripts/fluxia_signal_control.py`, `scripts/fluxia_gym_env.py` e perfil C2.

As entidades e invariantes são **decisões propostas do fluxAI**. O material didático orienta como documentar e validar; não determina esses nomes ou este esquema.

