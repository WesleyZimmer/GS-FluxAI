# FluxIA — pacote portátil de IA/ML

O ZIP inclui o notebook, código, modelo, dados, rede SUMO, cenários de tráfego e este guia. Os caminhos do notebook são relativos à pasta extraída.

Autores: Wesley Zimmer (RM 560264) e João Victor Alvarenga (RM 93752).

## Requisitos

- Windows 10/11, 64 bits.
- Python 3.13, 64 bits (preparado com Python 3.13.14).
- Eclipse SUMO 1.28.0 instalado separadamente. Instalação oficial para Windows: https://sumo.dlr.de/docs/Installing/index.html. Deixe a pasta com sumo.exe no PATH do Windows.
- A avaliação usa CPU e não exige GPU.

## Instalar e abrir

Extraia o ZIP, abra PowerShell na pasta extraída e execute:

~~~powershell
py -3.13 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements-notebook.txt
.\.venv\Scripts\python.exe -m notebook
~~~

Abra FluxIA_IA_ML.ipynb, escolha o kernel da pasta .venv e selecione Run All. O notebook analisa a CET e os cenários, carrega o modelo em CPU e apresenta a avaliação já registrada.

Para repetir uma avaliação SUMO de 600 segundos, altere EXECUTAR_NOVA_SIMULACAO para True na célula de integração. Os resultados novos serão gravados em outputs/evaluation_reproduced/.

## Treinamento e resultado

O checkpoint escolhido foi salvo no marco de 46.080 passos (46.084 passos internos). O treinamento pediu 100.608 passos e não chegou ao total. A validação usa cinco sementes por perfil e não mostrou melhora consistente sobre o PPO anterior. O treino completo está desligado por padrão; a célula opcional usa CPU e pode levar bastante tempo.

## Limites

O protótipo cobre oito semáforos de um recorte central. A CET contém registros de lentidão, não contagem de carros. As rotas são sintéticas. A validação não usou o conjunto de teste. A integração é com SUMO/TraCI, não com câmeras reais nem semáforos da rua.

## Entrega

Anexe FluxIA_IA_ML.ipynb e FluxIA_material_para_execucao.zip à tarefa do Teams. O notebook também está dentro do ZIP.

A Jupyter Notebook é instalado pelo arquivo requirements-notebook.txt. Guia oficial: https://jupyter.org/install
