using System.Globalization;
using System.Text.Json;
using FluxAI.Desktop.Api;

namespace FluxAI.Desktop.Forms;

internal sealed class RunResultsForm : Form
{
    private static readonly JsonSerializerOptions PrettyJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly FluxAiApiClient? _api;
    private readonly DemoRunData? _demoAnalysis;
    private readonly IReadOnlyList<RunChoice> _choices;
    private readonly ComboBox _runSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 500 };
    private readonly ComboBox _comparisonSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 500 };
    private readonly Button _loadButton = new() { Text = "Carregar análise", AutoSize = true };
    private readonly Button _compareButton = new() { Text = "Comparar", AutoSize = true };
    private readonly Label _status = new() { Text = "Selecione uma execução para ver os resultados.", AutoSize = true };
    private readonly TextBox _summary = ReadOnlyTextBox();
    private readonly TextBox _technicalInfo = ReadOnlyTextBox();
    private readonly TextBox _artifactInfo = ReadOnlyTextBox();
    private readonly DataGridView _metricsGrid = CreateGrid();
    private readonly DataGridView _signalGrid = CreateGrid();
    private readonly DataGridView _comparisonGrid = CreateGrid();
    private readonly ComboBox _metricSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Width = 500 };
    private readonly ComboBox _signalMetricSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Width = 500 };
    private readonly Label _metricCoverage = new() { Text = "A cobertura será exibida junto às métricas.", AutoSize = true, Tag = "muted" };
    private readonly Label _signalCoverage = new() { Text = "A cobertura será exibida junto às métricas.", AutoSize = true, Tag = "muted" };
    private readonly FluxAiChart _metricsChart = new("Série temporal da métrica") { Dock = DockStyle.Fill };
    private readonly FluxAiChart _signalChart = new("Resultados agregados por controlador", FluxAiChartKind.Bar) { Dock = DockStyle.Fill };
    private readonly Label _summaryHeadline = new() { Text = "Selecione uma execução", AutoSize = true, Tag = "page-title" };
    private readonly Label _summaryDeparted = new() { Text = "Não informado", AutoSize = true, Tag = "metric-value" };
    private readonly Label _summaryArrived = new() { Text = "Não informado", AutoSize = true, Tag = "metric-value" };
    private readonly Label _summaryWaiting = new() { Text = "Não informado", AutoSize = true, Tag = "metric-value" };
    private readonly Label _summaryCollisions = new() { Text = "Não informado", AutoSize = true, Tag = "metric-value" };
    private IReadOnlyList<MetricPoint> _currentMetrics = [];
    private IReadOnlyList<SignalResultItem> _currentSignalResults = [];

    public RunResultsForm(FluxAiApiClient? api, IReadOnlyList<TrainingRun> runs, Guid selectedRunId, DemoRunData? demoAnalysis = null)
    {
        _api = api;
        _demoAnalysis = demoAnalysis;
        _choices = runs.Select(run => new RunChoice(run)).ToArray();

        Text = "fluxAI | Resultados e comparação";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 620);
        Size = new Size(1160, 800);
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        FluxAiTheme.Apply(this);
        FluxAiTheme.StylePrimaryButton(_loadButton);
        FluxAiTheme.StyleSecondaryButton(_compareButton);
        _runSelector.SelectedIndexChanged += (_, _) => UpdateComparisonButton();
        _metricSelector.SelectedIndexChanged += (_, _) => RenderMetricChart();
        _signalMetricSelector.SelectedIndexChanged += (_, _) => RenderSignalChart();
        _runSelector.DataSource = _choices.ToList();
        _runSelector.DisplayMember = nameof(RunChoice.DisplayName);
        SelectRun(_runSelector, selectedRunId);
        _comparisonSelector.DataSource = _choices.ToList();
        _comparisonSelector.DisplayMember = nameof(RunChoice.DisplayName);
        SelectComparisonCandidate(selectedRunId);

        _loadButton.Click += async (_, _) => await LoadSelectedRunAsync();
        _compareButton.Click += async (_, _) => await CompareSelectedRunsAsync();
        Shown += async (_, _) => await LoadSelectedRunAsync();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20),
            Tag = "canvas"
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHeader(), 0, 0);
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 10, 0, 10),
            Tag = "canvas"
        };
        actions.Controls.Add(new Label { Text = "Execução A", AutoSize = true, Margin = new Padding(4, 8, 4, 0) });
        actions.Controls.Add(_runSelector);
        actions.Controls.Add(_loadButton);
        actions.Controls.Add(_status);
        root.Controls.Add(actions, 0, 1);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(new TabPage("Visão geral") { Controls = { BuildSummaryPanel() } });
        tabs.TabPages.Add(new TabPage("Métricas") { Controls = { BuildMetricsPanel() } });
        tabs.TabPages.Add(new TabPage("Por semáforo") { Controls = { BuildSignalPanel() } });
        tabs.TabPages.Add(new TabPage("Artefatos") { Controls = { _artifactInfo } });
        tabs.TabPages.Add(new TabPage("Detalhes técnicos") { Controls = { _technicalInfo } });
        if (_demoAnalysis is null)
            tabs.TabPages.Add(new TabPage("Comparar") { Controls = { BuildComparisonPanel() } });
        root.Controls.Add(tabs, 0, 2);
        Controls.Add(root);
    }

    private static Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            Padding = new Padding(18, 10, 18, 10),
            Tag = "brand"
        };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = FluxAiTheme.Brand,
            Tag = "brand",
            Margin = Padding.Empty
        };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(new Label
        {
            Text = "fluxAI | Resultados do treinamento",
            AutoSize = true,
            Tag = "header-title",
            Margin = new Padding(0, 0, 0, 1)
        }, 0, 0);
        content.Controls.Add(new Label
        {
            Text = "Analise métricas, cobertura, artefatos e compatibilidade entre execuções.",
            AutoSize = true,
            Tag = "header-subtitle",
            Margin = Padding.Empty
        }, 0, 1);
        header.Controls.Add(content);
        return header;
    }

    private Control BuildMetricsPanel()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 270));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var selectorRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 0, 0, 8) };
        selectorRow.Controls.Add(new Label { Text = "Série", AutoSize = true, Margin = new Padding(4, 8, 8, 0), Tag = "field-label" });
        selectorRow.Controls.Add(_metricSelector);
        layout.Controls.Add(selectorRow, 0, 0);
        layout.Controls.Add(_metricCoverage, 0, 1);
        layout.Controls.Add(_metricsChart, 0, 2);

        AddTextColumn(_metricsGrid, "Métrica", "Metric", 22);
        AddTextColumn(_metricsGrid, "Valor", "Value", 12);
        AddTextColumn(_metricsGrid, "Unidade", "Unit", 10);
        AddTextColumn(_metricsGrid, "Escopo", "Scope", 12);
        AddTextColumn(_metricsGrid, "Controlador", "Controller", 15);
        AddTextColumn(_metricsGrid, "Episódio", "Episode", 8);
        AddTextColumn(_metricsGrid, "Passo", "Step", 8);
        AddTextColumn(_metricsGrid, "Tempo sim. (s)", "Time", 12);
        AddTextColumn(_metricsGrid, "Chave de origem", "SourceKey", 22);
        layout.Controls.Add(_metricsGrid, 0, 3);
        return layout;
    }

    private Control BuildSignalPanel()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 270));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var selectorRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 0, 0, 8) };
        selectorRow.Controls.Add(new Label { Text = "Métrica agregada", AutoSize = true, Margin = new Padding(4, 8, 8, 0), Tag = "field-label" });
        selectorRow.Controls.Add(_signalMetricSelector);
        layout.Controls.Add(selectorRow, 0, 0);
        layout.Controls.Add(_signalCoverage, 0, 1);
        layout.Controls.Add(_signalChart, 0, 2);

        AddTextColumn(_signalGrid, "Controlador", "Controller", 24);
        AddTextColumn(_signalGrid, "Métrica agregada", "Metric", 22);
        AddTextColumn(_signalGrid, "Média", "Value", 12);
        AddTextColumn(_signalGrid, "Unidade", "Unit", 10);
        AddTextColumn(_signalGrid, "Cobertura", "Coverage", 16);
        AddTextColumn(_signalGrid, "Nota de cobertura", "Note", 40);
        layout.Controls.Add(_signalGrid, 0, 3);
        return layout;
    }

    private Control BuildSummaryPanel()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(18) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_summaryHeadline, 0, 0);
        var cards = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 1, AutoSize = true, Margin = new Padding(0, 18, 0, 18) };
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.Controls.Add(CreateResultCard("VEÍCULOS QUE SAÍRAM", _summaryDeparted), 0, 0);
        cards.Controls.Add(CreateResultCard("VEÍCULOS QUE CHEGARAM", _summaryArrived), 1, 0);
        cards.Controls.Add(CreateResultCard("ESPERA MÉDIA", _summaryWaiting), 2, 0);
        cards.Controls.Add(CreateResultCard("COLISÕES CONFIRMADAS", _summaryCollisions), 3, 0);
        layout.Controls.Add(cards, 0, 1);
        _summary.Dock = DockStyle.Fill;
        _summary.Font = new Font("Segoe UI", 9.5F);
        _summary.WordWrap = true;
        _summary.ScrollBars = ScrollBars.Vertical;
        layout.Controls.Add(_summary, 0, 2);
        return layout;
    }

    private static Control CreateResultCard(string title, Label value)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, AutoSize = true, Padding = new Padding(14), Margin = new Padding(0, 0, 10, 0), BackColor = Color.White };
        card.Controls.Add(new Label { Text = title, AutoSize = true, Tag = "sidebar-caption", Margin = new Padding(0, 0, 0, 8) }, 0, 0);
        card.Controls.Add(value, 0, 1);
        return card;
    }

    private Control BuildComparisonPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var selectors = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        selectors.Controls.Add(new Label { Text = "Execução B (comparar com A)", AutoSize = true, Margin = new Padding(4, 8, 4, 0) });
        selectors.Controls.Add(_comparisonSelector);
        _comparisonSelector.SelectedIndexChanged += (_, _) => UpdateComparisonButton();
        _compareButton.Enabled = false;
        selectors.Controls.Add(_compareButton);
        layout.Controls.Add(selectors, 0, 0);

        var note = new Label
        {
            Text = "A comparação exige duas execuções concluídas com cenário, ambiente, tipo, seed, horizonte e hashes de rede, catálogo, demanda e tempos compatíveis. Δ é B − A; não indica melhora por si só.",
            AutoSize = true,
            MaximumSize = new Size(1050, 0),
            Margin = new Padding(4, 8, 4, 8)
        };
        layout.Controls.Add(note, 0, 1);

        AddTextColumn(_comparisonGrid, "Métrica", "Metric", 20);
        AddTextColumn(_comparisonGrid, "Escopo", "Scope", 12);
        AddTextColumn(_comparisonGrid, "Controlador", "Controller", 14);
        AddTextColumn(_comparisonGrid, "Tempo sim. (s)", "Time", 12);
        AddTextColumn(_comparisonGrid, "A", "A", 10);
        AddTextColumn(_comparisonGrid, "B", "B", 10);
        AddTextColumn(_comparisonGrid, "Δ (B − A)", "Delta", 10);
        AddTextColumn(_comparisonGrid, "% Δ", "Percent", 10);
        layout.Controls.Add(_comparisonGrid, 0, 2);
        return layout;
    }

    private async Task LoadSelectedRunAsync()
    {
        if (_runSelector.SelectedItem is not RunChoice choice)
            return;

        if (_demoAnalysis is not null)
        {
            SetBusy(true, "Carregando dados fictícios locais...");
            try
            {
                DisplayRun(_demoAnalysis.Run, _demoAnalysis.Metrics, _demoAnalysis.Coverage, _demoAnalysis.SignalResults);
                _status.Text = "Demonstração: valores aleatórios locais, não enviados pela API nem calculados por uma simulação real.";
            }
            finally
            {
                SetBusy(false);
            }
            return;
        }
        if (_api is null)
        {
            _status.Text = "A conexão com a API não está disponível.";
            return;
        }

        SetBusy(true, "Consultando execução e resultados...");
        try
        {
            var run = await _api.GetRunAsync(choice.Run.Id);
            var (metrics, coverage) = await _api.GetRunMetricsAsync(run.Id);
            var signalResults = await _api.GetSignalResultsAsync(run.Id);
            DisplayRun(run, metrics, coverage, signalResults);
            _status.Text = $"Análise carregada: {metrics.Count} pontos de métrica, {signalResults.Count} resultados por controlador.";
        }
        catch (Exception exception)
        {
            _status.Text = "Não foi possível carregar a análise.";
            MessageBox.Show(this, exception.Message, "Falha ao consultar resultado", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            UpdateComparisonButton();
        }
    }

    private void DisplayRun(
        TrainingRun run,
        IReadOnlyList<MetricPoint> metrics,
        MetricCoverage? metricCoverage,
        IReadOnlyList<SignalResultItem> signalResults)
    {
        _summary.Text = BuildSummary(run, metricCoverage ?? run.MetricCoverage);
        _summaryHeadline.Text = $"{run.ScenarioId}  ·  {TranslateStatus(run.Status)}";
        var traffic = run.TrafficSummary;
        _summaryDeparted.Text = traffic?.Departed?.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")) ?? "Não informado";
        _summaryArrived.Text = traffic?.Arrived?.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")) ?? "Não informado";
        _summaryWaiting.Text = traffic?.MeanCompletedWaitingSeconds is double waiting
            ? $"{FormatNumber(waiting)} s"
            : "Não informado";
        _summaryCollisions.Text = traffic?.CollisionEventsConfirmed?.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")) ?? "Não informado";
        _technicalInfo.Text = BuildTechnicalInfo(run);

        _currentMetrics = metrics;
        var effectiveCoverage = metricCoverage ?? run.MetricCoverage;
        _metricCoverage.Text = CoverageDescription(effectiveCoverage, "métricas");
        _metricsGrid.Rows.Clear();
        foreach (var metric in metrics)
        {
            _metricsGrid.Rows.Add(
                metric.MetricName,
                FormatNumber(metric.Value),
                metric.Unit,
                metric.Scope.Type,
                metric.Scope.SignalControllerId ?? "—",
                metric.Episode?.ToString(CultureInfo.InvariantCulture) ?? "—",
                metric.Step?.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")) ?? "—",
                metric.SimulationTimeSeconds is null ? "—" : FormatNumber(metric.SimulationTimeSeconds.Value),
                metric.SourceKey);
        }
        if (metrics.Count == 0)
            _metricsGrid.Rows.Add("Nenhuma métrica registrada", "—", "—", "—", "—", "—", "—", "—", "—");
        var metricSeries = BuildMetricSeriesChoices(metrics);
        _metricSelector.DataSource = null;
        _metricSelector.DataSource = metricSeries;
        _metricSelector.DisplayMember = nameof(MetricSeriesChoice.DisplayName);
        _metricSelector.SelectedIndex = metricSeries.Count > 0 ? 0 : -1;
        RenderMetricChart();

        _currentSignalResults = signalResults;
        _signalGrid.Rows.Clear();
        foreach (var result in signalResults)
        {
            _signalGrid.Rows.Add(
                result.SignalControllerId,
                result.Metric.MetricName,
                FormatNumber(result.Metric.Value),
                result.Metric.Unit,
                $"{result.Coverage.ObservedSamples:N0}/{result.Coverage.ExpectedSamples:N0} ({(result.Coverage.Complete ? "completa" : "parcial")})",
                result.Coverage.Note ?? "—");
        }
        if (signalResults.Count == 0)
            _signalGrid.Rows.Add("—", "Nenhum resultado agregado disponível", "—", "—", "—", "—");
        var signalMetrics = signalResults
            .GroupBy(result => new { result.Metric.MetricName, result.Metric.Unit })
            .Select(group => new SignalMetricChoice(group.Key.MetricName, group.Key.Unit))
            .OrderBy(choice => choice.MetricName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _signalMetricSelector.DataSource = null;
        _signalMetricSelector.DataSource = signalMetrics;
        _signalMetricSelector.DisplayMember = nameof(SignalMetricChoice.DisplayName);
        _signalMetricSelector.SelectedIndex = signalMetrics.Count > 0 ? 0 : -1;
        RenderSignalChart();

        _artifactInfo.Text = BuildArtifactInfo(run.Artifacts ?? []);
    }

    private static string BuildSummary(TrainingRun run, MetricCoverage? coverage)
    {
        var coverageText = coverage is null
            ? "Não informada"
            : $"{coverage.ObservedSamples:N0}/{coverage.ExpectedSamples:N0} amostras ({(coverage.Complete ? "completa" : "parcial")})" +
              (string.IsNullOrWhiteSpace(coverage.Note) ? "" : $" — {coverage.Note}");

        return string.Join(Environment.NewLine,
        [
            $"ID: {run.Id:D}",
            $"Origem: {run.Origin} | Tipo: {run.RunType} | Estado: {TranslateStatus(run.Status)}",
            $"Cenário: {run.ScenarioId} | Ambiente: {run.Environment}",
            $"Seed: {run.Seed} | Horizonte: {FormatNumber(run.HorizonSeconds)} s",
            $"Configuração: {(run.ConfigurationId is null ? "não informada" : run.ConfigurationId.Value.ToString("D"))}" +
                (run.ConfigurationRevision is null ? "" : $" | revisão {run.ConfigurationRevision}"),
            $"Modelo: {run.Model?.Algorithm ?? "não informado"}" +
                (string.IsNullOrWhiteSpace(run.Model?.ArtifactId) ? "" : $" | artefato {run.Model.ArtifactId}"),
            $"Criada: {FormatDate(run.CreatedAtUtc)} | Início: {FormatDate(run.StartedAtUtc)} | Término: {FormatDate(run.FinishedAtUtc)}",
            $"Falha: {(run.Failure is null ? "nenhuma informada" : $"{run.Failure.Code} — {run.Failure.Message}")}",
            $"Cobertura das métricas: {coverageText}",
            "Limitações registradas:",
            run.Limitations is { Count: > 0 } ? string.Join(Environment.NewLine, run.Limitations.Select(item => "• " + item)) : "Nenhuma informada."
        ]);
    }

    private static string BuildTechnicalInfo(TrainingRun run) => JsonSerializer.Serialize(new
    {
        run.Id,
        run.SourceRunId,
        run.ConfigurationId,
        run.ConfigurationRevision,
        run.Origin,
        run.Environment,
        run.HorizonSeconds,
        run.ScenarioFingerprint,
        run.Model,
        run.TrafficSummary,
        EffectiveParameters = run.EffectiveParameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? (object?)null
            : run.EffectiveParameters,
        run.Progress,
        run.Failure,
        run.Limitations
    }, PrettyJson);

    private static IReadOnlyList<MetricSeriesChoice> BuildMetricSeriesChoices(IReadOnlyList<MetricPoint> metrics) => metrics
        .GroupBy(metric => new
        {
            metric.MetricName,
            metric.Unit,
            ScopeType = metric.Scope.Type,
            ControllerId = metric.Scope.SignalControllerId,
            metric.SourceKey
        })
        .Select(group => new MetricSeriesChoice(
            group.Key.MetricName,
            group.Key.Unit,
            group.Key.ScopeType,
            group.Key.ControllerId,
            group.Key.SourceKey,
            group.OrderBy(MetricSortValue)
                .Select((metric, index) => new FluxAiChartPoint(MetricLabel(metric, index), metric.Value))
                .ToArray()))
        .OrderBy(choice => choice.MetricName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(choice => choice.ScopeType, StringComparer.OrdinalIgnoreCase)
        .ThenBy(choice => choice.ControllerId, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static double MetricSortValue(MetricPoint metric) => metric.SimulationTimeSeconds
        ?? metric.Step
        ?? metric.Episode
        ?? 0;

    private static string MetricLabel(MetricPoint metric, int index) => metric.SimulationTimeSeconds is double time
        ? $"t={time:0.#}s"
        : metric.Step is long step
            ? $"passo {step:N0}"
            : metric.Episode is int episode
                ? $"ep. {episode}"
                : $"ponto {index + 1}";

    private void RenderMetricChart()
    {
        if (_metricSelector.SelectedItem is not MetricSeriesChoice series)
        {
            _metricsChart.Title = "Série temporal da métrica";
            _metricsChart.Subtitle = null;
            _metricsChart.Points = [];
            return;
        }
        _metricsChart.Title = $"{series.MetricName} ({series.Unit})";
        _metricsChart.Subtitle = $"Escopo: {series.ScopeType}" +
            (string.IsNullOrWhiteSpace(series.ControllerId) ? "" : $" · controlador {series.ControllerId}") +
            (string.IsNullOrWhiteSpace(series.SourceKey) ? "" : $" · origem {series.SourceKey}");
        _metricsChart.EmptyMessage = "Esta série não possui pontos válidos para desenhar.";
        _metricsChart.Points = series.Points;
    }

    private void RenderSignalChart()
    {
        if (_signalMetricSelector.SelectedItem is not SignalMetricChoice metric)
        {
            _signalChart.Title = "Resultados agregados por controlador";
            _signalChart.Subtitle = null;
            _signalChart.Points = [];
            _signalCoverage.Text = "Não há resultados agregados por semáforo nesta execução.";
            return;
        }
        var results = _currentSignalResults
            .Where(result => result.Metric.MetricName == metric.MetricName && result.Metric.Unit == metric.Unit)
            .ToArray();
        _signalChart.Title = $"{metric.MetricName} por controlador ({metric.Unit})";
        _signalChart.Subtitle = "Valores agregados retornados pela API; cada barra representa um controlador.";
        _signalChart.EmptyMessage = "Nenhum controlador tem valor disponível para esta métrica.";
        _signalChart.Points = results.Select(result => new FluxAiChartPoint(result.SignalControllerId, result.Metric.Value)).ToArray();
        _signalCoverage.Text = results.Length == 0
            ? "Não há cobertura disponível."
            : $"Cobertura: {results.Sum(result => result.Coverage.ObservedSamples):N0}/{results.Sum(result => result.Coverage.ExpectedSamples):N0} amostras agregadas; " +
              $"{results.Count(result => !result.Coverage.Complete)} de {results.Length} controladores com cobertura parcial.";
    }

    private static string CoverageDescription(MetricCoverage? coverage, string subject) => coverage is null
        ? $"Cobertura de {subject} não informada pela API."
        : $"Cobertura de {subject}: {coverage.ObservedSamples:N0}/{coverage.ExpectedSamples:N0} amostras · " +
          (coverage.Complete ? "completa" : "parcial") +
          (string.IsNullOrWhiteSpace(coverage.Note) ? "" : $" · {coverage.Note}");

    private static string BuildArtifactInfo(IReadOnlyList<RunArtifactItem> artifacts)
    {
        if (artifacts.Count == 0)
            return "A execução não possui metadados de artefatos registrados.\r\n\r\nA API disponibiliza nomes lógicos e hashes; ela não envia os arquivos nesta consulta.";

        return string.Join(Environment.NewLine + Environment.NewLine, artifacts.Select(artifact =>
            $"{artifact.LogicalName}\r\nSHA-256: {artifact.Sha256}\r\nTamanho: {(artifact.SizeBytes is null ? "não informado" : $"{artifact.SizeBytes.Value:N0} bytes")}\r\nTipo: {artifact.ContentType ?? "não informado"}"));
    }

    private async Task CompareSelectedRunsAsync()
    {
        if (_api is null)
        {
            _status.Text = "A comparação exige execuções reais carregadas pela API.";
            return;
        }
        if (_runSelector.SelectedItem is not RunChoice leftChoice ||
            _comparisonSelector.SelectedItem is not RunChoice rightChoice)
            return;
        if (leftChoice.Run.Id == rightChoice.Run.Id)
        {
            MessageBox.Show(this, "Escolha duas execuções diferentes.", "Comparação inválida", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true, "Verificando compatibilidade e comparando métricas...");
        _comparisonGrid.Rows.Clear();
        try
        {
            var left = await _api.GetRunAsync(leftChoice.Run.Id);
            var right = await _api.GetRunAsync(rightChoice.Run.Id);
            var issues = CompatibilityIssues(left, right);
            if (issues.Count > 0)
            {
                _status.Text = "Comparação bloqueada: os registros não comprovam compatibilidade suficiente.";
                MessageBox.Show(this,
                    "Não comparei as métricas porque faltam condições compatíveis:\n\n• " + string.Join("\n• ", issues),
                    "Sem dados suficientes para comparar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var (leftMetrics, _) = await _api.GetRunMetricsAsync(left.Id);
            var (rightMetrics, _) = await _api.GetRunMetricsAsync(right.Id);
            var rightByKey = rightMetrics.ToDictionary(MetricIdentity, StringComparer.Ordinal);
            var matched = 0;
            foreach (var metricA in leftMetrics)
            {
                if (!rightByKey.TryGetValue(MetricIdentity(metricA), out var metricB))
                    continue;
                matched++;
                var delta = metricB.Value - metricA.Value;
                var percent = Math.Abs(metricA.Value) < double.Epsilon
                    ? "n/d"
                    : FormatNumber(delta / Math.Abs(metricA.Value) * 100) + "%";
                _comparisonGrid.Rows.Add(
                    metricA.MetricName,
                    metricA.Scope.Type,
                    metricA.Scope.SignalControllerId ?? "—",
                    metricA.SimulationTimeSeconds is null ? "—" : FormatNumber(metricA.SimulationTimeSeconds.Value),
                    FormatNumber(metricA.Value),
                    FormatNumber(metricB.Value),
                    FormatNumber(delta),
                    percent);
            }

            if (matched == 0)
                _comparisonGrid.Rows.Add("Nenhuma métrica com identidade correspondente", "—", "—", "—", "—", "—", "—", "—");
            if (matched == 0)
            {
                _status.Text = "Sem dados suficientes: não há pontos de métrica com identidade correspondente nas duas execuções.";
                return;
            }
            _status.Text = $"Comparação descritiva: {matched:N0} pontos correspondentes. Cobertura A: {CoverageLabel(left.MetricCoverage)}; B: {CoverageLabel(right.MetricCoverage)}. A = {ModelName(left)}, B = {ModelName(right)}. Δ = B − A; isoladamente, não estabelece melhora.";
        }
        catch (Exception exception)
        {
            _status.Text = "Não foi possível comparar as execuções.";
            MessageBox.Show(this, exception.Message, "Falha ao comparar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static IReadOnlyList<string> CompatibilityIssues(TrainingRun left, TrainingRun right)
    {
        var issues = new List<string>();
        if (left.Status != "succeeded" || right.Status != "succeeded")
            issues.Add("as duas execuções precisam estar concluídas tecnicamente (succeeded)");
        if (!string.Equals(left.ScenarioId, right.ScenarioId, StringComparison.Ordinal))
            issues.Add("cenários diferentes");
        if (!string.Equals(left.Environment, right.Environment, StringComparison.Ordinal))
            issues.Add("ambientes diferentes");
        if (!string.Equals(left.RunType, right.RunType, StringComparison.Ordinal))
            issues.Add("tipos de execução diferentes");
        if (left.Seed != right.Seed)
            issues.Add("seeds diferentes");
        if (Math.Abs(left.HorizonSeconds - right.HorizonSeconds) > 1e-9)
            issues.Add("horizontes diferentes");

        var a = left.ScenarioFingerprint;
        var b = right.ScenarioFingerprint;
        if (a is null || b is null)
        {
            issues.Add("fingerprint do cenário ausente");
            return issues;
        }
        if (!SameHash(a.NetworkSha256, b.NetworkSha256)) issues.Add("hashes de rede diferentes ou ausentes");
        if (!SameHash(a.CatalogSha256, b.CatalogSha256)) issues.Add("hashes de catálogo diferentes ou ausentes");
        if (!SameHash(a.TimingProfileSha256, b.TimingProfileSha256)) issues.Add("hashes de perfil de tempos diferentes ou ausentes");
        if (!SameHash(a.RouteDemandSha256, b.RouteDemandSha256)) issues.Add("hashes de demanda diferentes ou ausentes");
        return issues;
    }

    private static bool SameHash(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static string MetricIdentity(MetricPoint metric) => string.Join("\u001f",
        metric.MetricName,
        metric.Unit,
        metric.Scope.Type,
        metric.Scope.SignalControllerId ?? "",
        metric.Episode?.ToString(CultureInfo.InvariantCulture) ?? "",
        metric.Step?.ToString(CultureInfo.InvariantCulture) ?? "",
        metric.SimulationTimeSeconds?.ToString("R", CultureInfo.InvariantCulture) ?? "",
        metric.SourceKey);

    private void SelectRun(ComboBox selector, Guid runId)
    {
        for (var index = 0; index < selector.Items.Count; index++)
        {
            if (selector.Items[index] is RunChoice choice && choice.Run.Id == runId)
            {
                selector.SelectedIndex = index;
                return;
            }
        }
    }

    private void SelectComparisonCandidate(Guid selectedRunId)
    {
        for (var index = 0; index < _comparisonSelector.Items.Count; index++)
        {
            if (_comparisonSelector.Items[index] is RunChoice choice && choice.Run.Id != selectedRunId)
            {
                _comparisonSelector.SelectedIndex = index;
                return;
            }
        }
    }

    private void UpdateComparisonButton() => _compareButton.Enabled =
        _loadButton.Enabled &&
        _api is not null &&
        _runSelector.SelectedItem is RunChoice left &&
        _comparisonSelector.SelectedItem is RunChoice right &&
        left.Run.Id != right.Run.Id;

    private void SetBusy(bool busy, string? message = null)
    {
        _loadButton.Enabled = !busy;
        _compareButton.Enabled = !busy && _runSelector.SelectedItem is RunChoice left &&
            _comparisonSelector.SelectedItem is RunChoice right && left.Run.Id != right.Run.Id;
        FluxAiTheme.StylePrimaryButton(_loadButton);
        FluxAiTheme.StyleSecondaryButton(_compareButton);
        UseWaitCursor = busy;
        if (message is not null)
            _status.Text = message;
    }

    private static string TranslateStatusShort(string status) => status switch
    {
        "queued" => "Na fila",
        "running" => "Em andamento",
        "succeeded" => "Concluído",
        "failed" => "Falhou",
        "interrupted" => "Interrompido",
        _ => status
    };
    private static string TranslateStatus(string status) => status switch
    {
        "queued" => "na fila",
        "running" => "em andamento",
        "succeeded" => "concluída tecnicamente",
        "failed" => "falhou",
        "interrupted" => "interrompida",
        _ => status
    };

    private static string ModelName(TrainingRun run) => run.Model?.Algorithm ?? run.Origin;

    private static string CoverageLabel(MetricCoverage? coverage) => coverage is null
        ? "não informada"
        : $"{coverage.ObservedSamples:N0}/{coverage.ExpectedSamples:N0} ({(coverage.Complete ? "completa" : "parcial")})";

    private static string FormatDate(DateTimeOffset? value) =>
        value?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.GetCultureInfo("pt-BR")) ?? "não informado";

    private static string FormatNumber(double value) =>
        value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));

    private static TextBox ReadOnlyTextBox() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Font = new Font("Consolas", 9F)
    };

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoGenerateColumns = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false
    };

    private static void AddTextColumn(DataGridView grid, string header, string name, float fillWeight) =>
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            Name = name,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.Automatic
        });

    private sealed record RunChoice(TrainingRun Run)
    {
        public string DisplayName => $"{Run.ScenarioId} | seed {Run.Seed} | {Run.CreatedAtUtc.ToLocalTime():dd/MM HH:mm} | {TranslateStatusShort(Run.Status)}";
    }

    private sealed record MetricSeriesChoice(
        string MetricName,
        string Unit,
        string ScopeType,
        string? ControllerId,
        string SourceKey,
        IReadOnlyList<FluxAiChartPoint> Points)
    {
        public string DisplayName => $"{MetricName} ({Unit}) · {ScopeType}" +
            (string.IsNullOrWhiteSpace(ControllerId) ? "" : $" · {ControllerId}") +
            (string.IsNullOrWhiteSpace(SourceKey) ? "" : $" · {SourceKey}");
    }

    private sealed record SignalMetricChoice(string MetricName, string Unit)
    {
        public string DisplayName => $"{MetricName} ({Unit})";
    }
}
