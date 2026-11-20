# FluxAI — painel C#

Aplicação desktop Windows Forms para configurar o treinamento PPO e acompanhar execuções do FluxAI.

## Requisitos
- Windows 10 ou 11
- .NET SDK 10 ou superior
- API do FluxAI disponível para os recursos conectados

## Executar

A partir da raiz do repositório:

```powershell
dotnet restore "GS - C#/FluxAI.sln"
dotnet run --project "GS - C#/src/FluxAI.Desktop/FluxAI.Desktop.csproj"
```

Para usar a integração com a API, inicie o serviço localizado em `SOA-W/src/FluxAI.Api` e informe no painel o endereço da API e o token de operador. O token também pode ser fornecido pela variável de ambiente `FLUXAI_OPERATOR_TOKEN`; ele não deve ser salvo no código nem enviado ao repositório.

## Estrutura
- `src/FluxAI.Desktop/Api`: contratos e cliente HTTP.
- `src/FluxAI.Desktop/Forms`: telas, gráficos, tema visual e dados de demonstração.
- `FluxAI.sln`: solução para abrir no Visual Studio ou pela CLI do .NET.

O backend, a simulação de trânsito e o worker de treinamento permanecem na pasta `SOA-W`.
