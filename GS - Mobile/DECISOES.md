# DECISÕES — CitizenTech Mobile

## Escopo e arquitetura

O aplicativo foi implementado com React Native e Expo Router, em telas nativas. A navegação tem início, lista de ocorrências, cadastro e detalhe. O TypeScript está em modo estrito. Modelos, parâmetros de rotas e respostas da API ficam tipados; dados do armazenamento ou da rede são validados antes do uso. A validação do nome, da categoria, da descrição e da localização é uma função pura em `src/utils/validation.ts`.

## Identidade e registros

O nome de exibição é solicitado na primeira abertura, validado (2–40 caracteres) e persistido em AsyncStorage. Ao reabrir o app, a pessoa não precisa informar o nome novamente. Os registros criados nesta versão também ficam no armazenamento local para demonstrar o formulário, a confirmação, a lista e o detalhe sem depender de um serviço indisponível.

O comprovante local exibe explicitamente “demonstração local”. Não se afirma que a ocorrência foi recebida pela central ou enviada ao backend. O resumo de trânsito mostra dados da API quando recebe o contrato esperado. Sem esses dados, a tela apresenta um mapa esquemático e oferece ativar o modo de demonstração; indicadores fictícios só aparecem após essa ação e são identificados como simulados. Eles não representam o trânsito atual nem dados de operação em tempo real.

## Recurso nativo e privacidade

O botão “Usar minha localização” solicita permissão de localização em primeiro plano. A permissão é pedida somente depois da ação explícita da pessoa; não há leitura em segundo plano. Negar a permissão ou não obter posição não bloqueia o cadastro. As coordenadas existem em memória durante o formulário e são descartadas após a validação. No armazenamento fica apenas um indicador de que o GPS foi confirmado, nunca latitude ou longitude.

## Integração com backend

O serviço `src/services/api.ts` consulta `GET /api/v1/traffic-summary`, configurado por `EXPO_PUBLIC_API_BASE_URL` e `EXPO_PUBLIC_SCENARIO_ID`. A URL precisa ser acessível pelo telefone. A rota de envio de ocorrência ainda não está implementada no backend desta etapa, conforme o plano do projeto; por isso, o formulário salva um comprovante local. A futura integração deverá usar autenticação própria de cidadão, fornecida pelo backend. Segredos de operador não devem ser incluídos no cliente mobile.

## Atendimento à rubrica

- **40% — Funcionalidade:** quatro telas navegáveis; lista com carregando, erro e vazio; formulário validado que cria um item; identificação lembrada ao reabrir.
- **30% — TypeScript:** modo estrito, modelos e rotas tipados, validação de dados externos e verificação por `tsc --noEmit`.
- **20% — Recurso nativo:** localização em primeiro plano com solicitação de permissão no momento de uso, tratamento de recusa/falha e sem persistir coordenadas.
- **10% — Organização:** `services/`, validação pura, README com instalação e execução e este documento.

## Autores

- Wesley Zimmer — RM 560264
- João Victor Alvarenga — RM 93752
