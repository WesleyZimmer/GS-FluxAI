# fluxAI — CitizenTech de mobilidade urbana

O fluxAI estuda como estratégias de controle semafórico podem reduzir filas e tempos de espera em cruzamentos. A avaliação é feita com simulação e replay no SUMO; esta entrega não controla semáforos reais nem mede impacto nas ruas.

## Ficha da CitizenTech

- **Território de referência:** São Paulo; demonstração em simulação, sem bairro de implantação especificado.
- **Cidadãos beneficiados:** motoristas e moradores afetados por filas em cruzamentos, além das equipes que avaliam estratégias semafóricas.
- **Problema:** é difícil avaliar como ajustes nos semáforos afetam filas e espera sem métricas consistentes de tráfego.
- **Como funciona:** simulações/replays geram resultados; a API armazena configurações e execuções e disponibiliza métricas por REST.
- **Parte entregue neste repositório:** backend SOA&W em C#/.NET com SQLite, domínio, endpoints, migrations, contrato OpenAPI e testes manuais no Insomnia.
- **Indicadores de simulação:** tamanho das filas por controlador, tempo de espera, viagens concluídas e veículos ativos ao fim da janela.
- **ODS relacionado:** ODS 11 — Cidades e Comunidades Sustentáveis.

## Entrega SOA&W

O código completo, as instruções de execução, a documentação, as evidências e a coleção do Insomnia estão na pasta [SOA-W](SOA-W/README.md).

Para executar, use a pasta `SOA-W` como raiz do projeto:

```powershell
dotnet restore src/FluxAI.Api/FluxAI.Api.csproj
dotnet run --project src/FluxAI.Api/FluxAI.Api.csproj
```

## Integrantes

| Integrante | RM |
|---|---:|
| Wesley Zimmer | 560264 |
| João Victor Alvarenga | 93752 |
