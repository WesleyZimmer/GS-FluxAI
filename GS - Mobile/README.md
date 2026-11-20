# CitizenTech Mobile

Aplicativo nativo em React Native + Expo + TypeScript para a entrega FIAP GS 2026.2. O projeto usa telas nativas e Expo Router; não usa WebView.

## Equipe

| Integrante | RM |
| --- | --- |
| Wesley Zimmer | 560264 |
| João Victor Alvarenga | 93752 |

Os autores também estão declarados no campo `authors` do `package.json`.

## Requisitos e execução

Use Node.js compatível com o Expo SDK e Expo Go atualizado no celular. No PowerShell, dentro da pasta `mobile`:

```powershell
npm install
npx expo start
```

Leia o QR code com o Expo Go. Computador e celular precisam estar na mesma rede; se a rede bloquear a conexão local, inicie com `npx expo start --tunnel`.

## Funcionalidades

- Identificação por nome, validada e lembrada no aparelho.
- Início com resumo de trânsito e estados de carregamento, indisponibilidade e falha.
- Mapa esquemático e modo de demonstração ativado pela pessoa; indicadores fictícios aparecem identificados como simulados.
- Lista de ocorrências com estados de carregamento, vazio e erro.
- Formulário validado: categoria obrigatória, descrição entre 10 e 500 caracteres e confirmação antes de salvar.
- Localização em primeiro plano, solicitada somente após toque explícito. Permissão negada ou falha do GPS não impede o registro.
- Comprovante e detalhe do registro criado.

## Configuração da API

Copie `.env.example` para `.env` e ajuste `EXPO_PUBLIC_API_BASE_URL` para uma URL alcançável pelo celular. Em aparelho físico, use o IP local do computador, não `localhost`. `EXPO_PUBLIC_SCENARIO_ID` aceita o cenário da FIAP.

O app consulta `GET /api/v1/traffic-summary?scenarioId=<id>`. A integração `POST /api/v1/citizen-reports` ainda não está disponível no backend desta etapa; por isso, o cadastro é identificado como demonstração local. Não configure credenciais de operador no app.

## Verificação

```powershell
npx tsc --noEmit
npx expo lint
```

As decisões de escopo, privacidade e integração estão em [DECISOES.md](./DECISOES.md).
