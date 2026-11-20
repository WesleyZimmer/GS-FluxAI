using System.Drawing;
using System.Text.Json;
using FluxAI.Desktop.Api;

namespace FluxAI.Desktop.Forms;

internal sealed class MainForm : Form
{
    private readonly TextBox _apiUrl = new() { Text = "http://localhost:5169", Width = 250 };
    private readonly TextBox _operatorToken = new()
    {
        Text = Environment.GetEnvironmentVariable("FLUXAI_OPERATOR_TOKEN") ?? string.Empty,
        UseSystemPasswordChar = true,
        Width = 230
    };
    private readonly Button _connectButton = new() { Text = "Conectar", AutoSize = true };
    private readonly Label _connectionState = new() { Text = "Desconectado", AutoSize = true, ForeColor = Color.DarkRed, Tag = "status" };
    private readonly Label _sidebarConnectionState = new() { Text = "Desconectado", AutoSize = true, ForeColor = Color.FromArgb(229, 167, 151) };
    private readonly CheckBox _demoToggle = new() { Text = "Modo demonstração", AutoSize = true, Checked = true, ForeColor = Color.White, BackColor = FluxAiTheme.Sidebar, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
    private readonly Label _demoBadge = new() { Text = "DEMO · DADOS FICTÍCIOS", AutoSize = true, Visible = false, Tag = "demo-badge" };
    private readonly ComboBox _scenario = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly Label _controllerSummary = new()
    {
        Text = "Selecione um cenário após conectar.",
        AutoSize = true,
        MaximumSize = new Size(900, 0)
    };
    private readonly TextBox _configurationName = new() { Text = "Treinamento fluxAI", Width = 260, MaxLength = 120 };
    private readonly NumericUpDown _seed = Number(0, int.MaxValue, 4172026, 1);
    private readonly NumericUpDown _horizon = Number(1, 3600, 600, 60);

    private readonly NumericUpDown _totalTimesteps = Number(128, 100000, 4096, 256);
    private readonly NumericUpDown _checkpointEvery = Number(0, 100000, 2048, 256);
    private readonly NumericUpDown _rolloutSteps = Number(16, 4096, 128, 16);
    private readonly NumericUpDown _batchSize = Number(8, 4096, 64, 1);
    private readonly NumericUpDown _epochs = Number(1, 100, 10, 1);
    private readonly NumericUpDown _learningRate = Number(0.000001m, 0.1m, 0.0003m, 0.0001m, 6);
    private readonly NumericUpDown _gamma = Number(0, 1, 0.99m, 0.01m, 2);
    private readonly NumericUpDown _gaeLambda = Number(0, 1, 0.95m, 0.01m, 2);
    private readonly NumericUpDown _clipRange = Number(0.0001m, 1, 0.2m, 0.01m, 4);
    private readonly NumericUpDown _entropy = Number(0, 1, 0.01m, 0.01m, 4);

    private readonly NumericUpDown _minimumGreen = Number(0.01m, 90, 10, 1, 2);
    private readonly NumericUpDown _maximumGreen = Number(1, 90, 90, 1);
    private readonly NumericUpDown _blockingThreshold = Number(0.01m, 1, 0.95m, 0.01m, 2);
    private readonly NumericUpDown _confirmationSamples = Number(2, 20, 2, 1);
    private readonly NumericUpDown _measurementAge = Number(1, 60, 6, 1);

    private readonly Button _saveConfigurationButton = new() { Text = "Salvar configuração", AutoSize = true, Enabled = false };
    private readonly Button _startTrainingButton = new() { Text = "Iniciar treinamento", AutoSize = true, Enabled = false };
    private readonly Label _configurationState = new() { Text = "Nenhuma configuração salva.", AutoSize = true, Tag = "muted" };
    private readonly Label _trainingState = new() { Text = "Nenhum treinamento acompanhado.", AutoSize = true };
    private readonly Label _progressText = new() { Text = "Progresso indisponível.", AutoSize = true };
    private readonly Label _lastUpdate = new() { Text = "", AutoSize = true };
    private readonly Label _monitorHelp = new() { AutoSize = true, MaximumSize = new Size(780, 0), Tag = "muted" };
    private readonly Label _setupNote = new() { AutoSize = true, ForeColor = FluxAiTheme.Muted, Tag = "muted", Margin = new Padding(0, 8, 0, 4) };
    private readonly Label _reviewText = new() { AutoSize = true, MaximumSize = new Size(760, 0), Font = new Font("Segoe UI", 11), ForeColor = FluxAiTheme.Ink, Location = new Point(28, 28) };
    private readonly ToolTip _setupToolTip = new() { AutoPopDelay = 12000, InitialDelay = 400, ReshowDelay = 100 };
    private readonly ProgressBar _progressBar = new() { Dock = DockStyle.Top, Height = 24, Minimum = 0, Maximum = 1000 };
    private readonly DataGridView _historyGrid = new()
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
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill, BackColor = FluxAiTheme.Canvas, Padding = new Padding(22, 14, 22, 20) };
    private readonly Label _pageTitle = new() { Text = "Visão geral", AutoSize = true, Tag = "page-title" };
    private readonly Label _pageDescription = new() { Text = "Acompanhe as execuções e configure os experimentos da IA.", AutoSize = true, Tag = "muted" };
    private readonly Dictionary<string, Button> _navigationButtons = new();
    private readonly Label _dashboardRunCount = new() { Text = "—", AutoSize = true, Tag = "metric-value" };
    private readonly Label _dashboardSuccessCount = new() { Text = "—", AutoSize = true, Tag = "metric-value" };
    private readonly Label _dashboardLatestStatus = new() { Text = "Sem dados", AutoSize = true, Tag = "metric-value" };
    private readonly Label _dashboardScenario = new() { Text = "-", AutoSize = false, Height = 48, Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Tag = "metric-value" };
    private readonly ToolTip _dashboardToolTip = new();
    private readonly Label _dashboardRecentText = new() { Text = "Conecte-se à API para carregar as execuções.", AutoSize = true, MaximumSize = new Size(500, 0), Tag = "muted" };
    private readonly Button _dashboardRefreshButton = new() { Text = "Atualizar dados", AutoSize = true, Anchor = AnchorStyles.Right };
    private readonly FluxAiChart _dashboardChart = new("Execuções por situação", FluxAiChartKind.Bar)
    {
        EmptyMessage = "As execuções aparecerão aqui depois da conexão com a API."
    };
    private IReadOnlyList<TrainingRun> _historyRuns = [];
    private IReadOnlyList<DemoRunData> _demoRuns = [];
    private bool _demoMode;

    private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = 3000 };
    private FluxAiApiClient? _api;
    private ScenarioItem? _selectedScenario;
    private IReadOnlyList<SignalControllerItem> _loadedControllers = [];
    private ConfigurationResponse? _savedConfiguration;
    private string? _savedConfigurationSignature;
    private bool _configurationVerified;
    private Guid? _activeRunId;
    private string? _pendingRunFingerprint;
    private string? _pendingIdempotencyKey;
    private bool _pollInProgress;
    private bool _loadingScenarioList;
    private string? _lastRunStatus;

    public MainForm()
    {
        Text = "fluxAI | Controle de Treinamento";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 700);
        Size = new Size(1180, 820);
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        FluxAiTheme.Apply(this);
        ShowPage("overview");
        FluxAiTheme.StyleSecondaryButton(_connectButton);
        FluxAiTheme.StyleSecondaryButton(_saveConfigurationButton);
        FluxAiTheme.StylePrimaryButton(_startTrainingButton);
        _connectButton.Click += async (_, _) => await ConnectAsync();
        _demoToggle.CheckedChanged += async (_, _) => await SetDemoModeAsync(_demoToggle.Checked);
        Shown += async (_, _) => await SetDemoModeAsync(_demoToggle.Checked);
        _saveConfigurationButton.Click += async (_, _) => await SaveConfigurationAsync();
        _startTrainingButton.Click += async (_, _) => await StartTrainingAsync();
        _pollTimer.Tick += async (_, _) => await PollActiveRunAsync();
        FormClosed += (_, _) =>
        {
            _pollTimer.Stop();
            _api?.Dispose();
        };
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty, Tag = "canvas" };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 232));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildSidebar(), 0, 0);

        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = Padding.Empty, Margin = Padding.Empty, Tag = "canvas" };
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.Controls.Add(BuildPageHeader(), 0, 0);
        workspace.Controls.Add(_pageHost, 0, 1);
        root.Controls.Add(workspace, 1, 0);
        Controls.Add(root);

        RegisterPage("overview", BuildDashboardPanel(), "Visão geral", "Resumo das execuções e acesso rápido às ferramentas.");
        RegisterPage("setup", BuildSetupPanel(), "Novo treinamento", "Configure cenário, algoritmo e regras dos semáforos em etapas.");
        RegisterPage("monitor", BuildMonitorPanel(), "Monitoramento", "Acompanhe a execução selecionada e o progresso informado pela API.");
        RegisterPage("runs", BuildHistoryPanel(), "Execuções", "Consulte o histórico, acompanhe uma execução ou abra sua análise.");
        RegisterPage("api", BuildConnectionSettingsPage(), "Conexão com a API", "Endereço e credencial usados para carregar cenários e operar treinamentos.");
        ShowPage("overview");
    }

    private Control BuildSidebar()
    {
        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(16, 18, 14, 16), Tag = "sidebar" };
        sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var brand = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, AutoSize = true, Tag = "sidebar" };
        brand.Controls.Add(new Label { Text = "fluxAI", AutoSize = true, Tag = "sidebar-brand" }, 0, 0);
        brand.Controls.Add(new Label { Text = "LABORATÓRIO DE TRÁFEGO", AutoSize = true, Tag = "sidebar-caption", Margin = new Padding(2, 1, 0, 18) }, 0, 1);
        sidebar.Controls.Add(brand, 0, 0);

        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Tag = "sidebar", Padding = new Padding(0, 8, 0, 0) };
        AddNavigationButton(nav, "overview", "Visão geral");
        AddNavigationButton(nav, "setup", "Novo treinamento");
        AddNavigationButton(nav, "monitor", "Monitoramento");
        AddNavigationButton(nav, "runs", "Execuções");
        AddNavigationButton(nav, "api", "Conexão com a API");
        sidebar.Controls.Add(nav, 0, 1);

        var footer = new TableLayoutPanel { Dock = DockStyle.Bottom, ColumnCount = 1, RowCount = 5, AutoSize = true, Tag = "sidebar" };
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.Controls.Add(new Label { Text = "SERVIÇO", AutoSize = true, Tag = "sidebar-caption", Margin = new Padding(0, 0, 0, 5) }, 0, 0);
        _sidebarConnectionState.Tag = "sidebar-status";
        footer.Controls.Add(_sidebarConnectionState, 0, 1);
        footer.Controls.Add(new Panel { Height = 1, Dock = DockStyle.Top, BackColor = Color.FromArgb(58, 80, 84), Margin = new Padding(0, 12, 0, 6), Tag = "sidebar-separator" }, 0, 2);
        _demoToggle.Margin = new Padding(0, 8, 0, 2);
        footer.Controls.Add(_demoToggle, 0, 3);
        footer.Controls.Add(new Label { Text = "Dados locais de exemplo", AutoSize = true, Tag = "sidebar-caption", Margin = new Padding(0, 2, 0, 0) }, 0, 4);
        sidebar.Controls.Add(footer, 0, 2);
        return sidebar;
    }

    private void AddNavigationButton(Control parent, string key, string text)
    {
        var button = new Button { Text = text, Width = 198, Height = 44, TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat, Tag = "navigation", Margin = new Padding(0, 3, 0, 3), Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => ShowPage(key);
        _navigationButtons[key] = button;
        parent.Controls.Add(button);
    }

    private Control BuildPageHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(28, 16, 28, 12), BackColor = Color.White };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.Controls.Add(_pageTitle, 0, 0);
        header.Controls.Add(_pageDescription, 0, 1);
        header.Controls.Add(_demoBadge, 1, 0);
        var apiButton = new Button { Text = "API", AutoSize = true, Tag = "navigation-outline", Margin = new Padding(12, 8, 0, 0) };
        apiButton.Click += (_, _) => ShowPage("api");
        header.Controls.Add(apiButton, 2, 0);
        header.SetColumnSpan(_pageDescription, 3);
        return header;
    }

    private void RegisterPage(string key, Control page, string title, string description)
    {
        page.Dock = DockStyle.Fill;
        page.Visible = false;
        page.Tag = (key, title, description);
        _pageHost.Controls.Add(page);
    }

    private void ShowPage(string key)
    {
        foreach (Control page in _pageHost.Controls)
        {
            if (page.Tag is not ValueTuple<string, string, string> info)
                continue;
            page.Visible = info.Item1 == key;
            if (page.Visible)
            {
                _pageTitle.Text = info.Item2;
                _pageDescription.Text = info.Item1 == "monitor" && _demoMode
                    ? "Acompanhe uma amostra fictícia de execução gerada localmente."
                    : info.Item3;
                page.BringToFront();
            }
        }
        foreach (var (navKey, button) in _navigationButtons)
            button.BackColor = navKey == key ? FluxAiTheme.SidebarActive : FluxAiTheme.Sidebar;
    }

    private Control BuildDashboardPanel()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(4), Tag = "canvas" };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var hero = new Panel { Dock = DockStyle.Top, Height = 122, Padding = new Padding(22, 18, 18, 16), BackColor = Color.FromArgb(231, 244, 242), Margin = new Padding(0, 0, 0, 16) };
        var heroTitle = new Label { Text = "Treine sinais para um trânsito mais fluido", AutoSize = true, Font = new Font("Segoe UI Semibold", 17, FontStyle.Bold), ForeColor = FluxAiTheme.Brand, Location = new Point(22, 18) };
        var heroSubtitle = new Label { Text = "Configure experimentos, acompanhe a execução e entenda os resultados.", AutoSize = true, ForeColor = FluxAiTheme.Muted, Location = new Point(24, 53) };
        var startButton = new Button { Text = "Criar treinamento", AutoSize = true, Location = new Point(22, 82), Tag = "primary" };
        startButton.Click += (_, _) => ShowPage("setup");
        hero.Controls.Add(heroTitle);
        hero.Controls.Add(heroSubtitle);
        hero.Controls.Add(startButton);
        layout.Controls.Add(hero, 0, 0);
        layout.SetColumnSpan(hero, 2);

        var cards = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 1, AutoSize = true, Margin = new Padding(0, 0, 0, 16), Tag = "canvas" };
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.Controls.Add(CreateMetricCard("EXECUÇÕES", _dashboardRunCount, "No histórico"), 0, 0);
        cards.Controls.Add(CreateMetricCard("CONCLUÍDAS", _dashboardSuccessCount, "Registros finalizados"), 1, 0);
        cards.Controls.Add(CreateMetricCard("ÚLTIMA SITUAÇÃO", _dashboardLatestStatus, "Execução mais recente"), 2, 0);
        cards.Controls.Add(CreateMetricCard("CENÁRIO RECENTE", _dashboardScenario, "Última execução"), 3, 0);
        layout.Controls.Add(cards, 0, 1);
        layout.SetColumnSpan(cards, 2);

        var chartCard = CreateContentCard("Atividade das execuções", _dashboardChart);
        chartCard.Margin = new Padding(0, 0, 10, 0);
        _dashboardChart.Dock = DockStyle.Fill;
        layout.Controls.Add(chartCard, 0, 2);

        var recentPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(18), BackColor = Color.White };
        recentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        recentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        recentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        recentPanel.Controls.Add(new Label { Text = "Atividade recente", AutoSize = true, Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold), ForeColor = FluxAiTheme.Ink, Margin = new Padding(0, 0, 0, 14) }, 0, 0);
        recentPanel.Controls.Add(_dashboardRecentText, 0, 1);
        _dashboardRefreshButton.Click += async (_, _) => await LoadHistoryAsync();
        recentPanel.Controls.Add(_dashboardRefreshButton, 0, 2);
        layout.Controls.Add(recentPanel, 1, 2);
        return layout;
    }

    private static Control CreateMetricCard(string title, Label value, string caption)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, AutoSize = true, Padding = new Padding(16, 12, 14, 12), Margin = new Padding(0, 0, 10, 0), BackColor = Color.White };
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.Controls.Add(new Label { Text = title, AutoSize = true, Tag = "sidebar-caption", Margin = new Padding(0, 0, 0, 10) }, 0, 0);
        card.Controls.Add(value, 0, 1);
        card.Controls.Add(new Label { Text = caption, AutoSize = true, Tag = "muted", Margin = new Padding(0, 6, 0, 0) }, 0, 2);
        return card;
    }

    private static Control CreateContentCard(string title, Control content)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(18), BackColor = Color.White };
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold), ForeColor = FluxAiTheme.Ink, Margin = new Padding(0, 0, 0, 12) }, 0, 0);
        card.Controls.Add(content, 0, 1);
        return card;
    }

    private Control BuildConnectionSettingsPage()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, AutoSize = true, Padding = new Padding(8), Tag = "canvas" };
        layout.Controls.Add(BuildConnectionPanel(), 0, 0);
        layout.Controls.Add(new Label { Text = "A URL e o token são enviados diretamente à API configurada. O token fica mascarado na tela.", AutoSize = true, MaximumSize = new Size(780, 0), Tag = "muted", Margin = new Padding(6, 10, 6, 0) }, 0, 1);
        return layout;
    }

    private async Task SetDemoModeAsync(bool enabled)
    {
        if (_demoMode == enabled && (_demoRuns.Count > 0 || !enabled))
            return;

        _demoMode = enabled;
        _demoBadge.Visible = enabled;
        _configurationState.Visible = !enabled;
        _demoToggle.Text = enabled ? "Modo demonstração: ativo" : "Modo demonstração";
                _setupNote.Text = enabled
            ? "Demonstra\u00e7\u00e3o local: dados fict\u00edcios; salvar e iniciar est\u00e3o desativados."
            : "A configura\u00e7\u00e3o \u00e9 salva na API; o treinamento depende do worker Python.";
        _reviewText.Text = enabled
            ? "Estes valores aleatórios foram gerados localmente para mostrar os controles. Nenhuma configuração ou execução será enviada enquanto a demonstração estiver ativa."
            : "Revise os parâmetros nas etapas anteriores.\r\n\r\nSalvar configuração valida e registra os parâmetros na API. Iniciar treinamento envia uma solicitação de execução; a disponibilidade depende do endpoint de fila e do worker no backend.";
        _monitorHelp.Text = enabled
            ? "Dados fictícios gerados localmente. O progresso e as métricas são exemplos para visualizar o painel e não representam uma simulação real."
            : "A barra só representa avanço quando a API informa os timesteps concluídos e o total. Fase, episódio e observações aparecem conforme chegam do worker.";
        _dashboardRefreshButton.Text = enabled ? "Gerar novos exemplos" : "Atualizar dados";
        _connectButton.Enabled = !enabled;
        _saveConfigurationButton.Enabled = !enabled && _api is not null && _selectedScenario is not null && _loadedControllers.Count > 0;
        _startTrainingButton.Enabled = !enabled && _configurationVerified && _savedConfiguration is not null;
        _apiUrl.Enabled = !enabled;
        _operatorToken.Enabled = !enabled;
        FluxAiTheme.StylePrimaryButton(_startTrainingButton);
        FluxAiTheme.StyleSecondaryButton(_saveConfigurationButton);

        if (enabled)
        {
            _pollTimer.Stop();
            _savedConfiguration = null;
            _savedConfigurationSignature = null;
            _configurationVerified = false;
            _configurationState.Text = "Modo demonstração: configurações de exemplo não são enviadas à API.";
            ConfigureDemoScenarios();
            FillDemoConfiguration();
            GenerateDemoData();
            _connectionState.Text = _api is null
                ? "Demonstração local ativa; sem conexão com a API."
                : "Demonstração ativa; a API não será chamada.";
            _connectionState.ForeColor = FluxAiTheme.Muted;
            _sidebarConnectionState.Text = "Demonstração local";
            _sidebarConnectionState.ForeColor = Color.FromArgb(150, 220, 188);
            return;
        }

        _demoRuns = [];
        _historyRuns = [];
        _historyGrid.Rows.Clear();
        _scenario.DataSource = null;
        _scenario.Items.Clear();
        _selectedScenario = null;
        _loadedControllers = [];
        _configurationState.Text = "Conecte à API para salvar uma configuração real.";
        _trainingState.Text = "Nenhum treinamento acompanhado.";
        _progressText.Text = "Progresso indisponível.";
        _progressBar.Value = 0;
        _lastUpdate.Text = "";
        UpdateDashboard();
        if (_api is not null)
            await ConnectAsync();
        else
        {
            _controllerSummary.Text = "Conecte à API para carregar cenários reais.";
            SetConnectionState("Desconectado", false);
            SetBusy(false);
        }
    }

    private void ConfigureDemoScenarios()
    {
        var scenarios = new[]
        {
            new ScenarioItem("demo-centro", "Centro - Av. Paulista", "DEMO", "SUMO", 6, true),
            new ScenarioItem("demo-norte", "Corredor Norte", "DEMO", "SUMO", 4, true),
            new ScenarioItem("demo-leste", "Zona Leste - Radial", "DEMO", "SUMO", 8, true)
        };
        _loadingScenarioList = true;
        try
        {
            _scenario.DataSource = scenarios;
            _scenario.DisplayMember = nameof(ScenarioItem.Name);
            _scenario.ValueMember = nameof(ScenarioItem.Id);
        }
        finally
        {
            _loadingScenarioList = false;
        }
        _selectedScenario = _scenario.SelectedItem as ScenarioItem;
        ConfigureDemoScenarioSelection();
    }

    private void ConfigureDemoScenarioSelection()
    {
        _selectedScenario = _scenario.SelectedItem as ScenarioItem;
        _loadedControllers = _selectedScenario is null
            ? []
            : Enumerable.Range(1, _selectedScenario.ControllerCount)
                .Select(index => new SignalControllerItem(
                    $"DEMO-SIG-{index:00}",
                    _selectedScenario.Id,
                    index % 2 == 0,
                    [new SignalPhase(28, "G"), new SignalPhase(4, "y"), new SignalPhase(24, "r")]))
                .ToArray();
        _controllerSummary.Text = _selectedScenario is null
            ? "Selecione um cenário de exemplo."
            : $"Cenário de demonstração · {_selectedScenario.ControllerCount} semáforos fictícios · ambiente SUMO";
    }

    private void FillDemoConfiguration()
    {
        var random = Random.Shared;
        _configurationName.Text = $"Demonstração aleatória {random.Next(100, 999)}";
        _seed.Value = random.Next(100_000, 9_999_999);
        _horizon.Value = random.Next(5, 21) * 60;
        _totalTimesteps.Value = random.Next(8, 65) * 256;
        _checkpointEvery.Value = random.Next(0, 17) * 256;
        _rolloutSteps.Value = random.Next(4, 17) * 16;
        _batchSize.Value = 64;
        _epochs.Value = random.Next(5, 21);
        _learningRate.Value = random.Next(1, 10) / 10_000m;
        _gamma.Value = random.Next(94, 100) / 100m;
        _gaeLambda.Value = random.Next(90, 100) / 100m;
        _clipRange.Value = random.Next(10, 31) / 100m;
        _entropy.Value = random.Next(1, 10) / 1_000m;
        _minimumGreen.Value = random.Next(5, 16);
        _maximumGreen.Value = random.Next((int)_minimumGreen.Value, 61);
        _blockingThreshold.Value = random.Next(75, 99) / 100m;
        _confirmationSamples.Value = random.Next(2, 8);
        _measurementAge.Value = random.Next(2, 15);
    }

    private void GenerateDemoData()
    {
        _demoRuns = DemoDataFactory.CreateRuns();
        _historyRuns = _demoRuns.Select(item => item.Run).ToArray();
        FillHistoryGrid(_historyRuns);
        UpdateDashboard();
        var active = _demoRuns.FirstOrDefault(item => item.Run.Status == "running") ?? _demoRuns[0];
        DisplayRun(active.Run);
    }

    private static Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 82,
            Padding = new Padding(18, 12, 18, 12),
            Margin = new Padding(0, 0, 0, 12),
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
            Text = "fluxAI | Controle de treinamento",
            AutoSize = true,
            Tag = "header-title",
            Margin = new Padding(0, 0, 0, 1)
        }, 0, 0);
        content.Controls.Add(new Label
        {
            Text = "Configure, execute e acompanhe os treinamentos da IA de trânsito.",
            AutoSize = true,
            Tag = "header-subtitle",
            Margin = Padding.Empty
        }, 0, 1);
        header.Controls.Add(content);
        return header;
    }

    private Control BuildConnectionPanel()
    {
        var group = new GroupBox
        {
            Text = "Conexão com a API",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 10)
        };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        flow.Controls.Add(Field("Endereço", _apiUrl));
        flow.Controls.Add(Field("Token de operador", _operatorToken));
        flow.Controls.Add(_connectButton);
        flow.Controls.Add(_connectionState);
        group.Controls.Add(flow);
        return group;
    }

    private Control BuildSetupPanel()
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
            Tag = "canvas"
        };
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var steps = new TabControl { Dock = DockStyle.Fill, Margin = Padding.Empty };

        var scenarioGroup = new GroupBox
        {
            Text = "1. Cenário e execução",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10)
        };
        var scenarioGrid = Grid(2);
        AddField(scenarioGrid, 0, "Cenário", _scenario);
        AddField(scenarioGrid, 1, "Nome da configuração", _configurationName);
        AddField(scenarioGrid, 2, "Seed", _seed);
        AddField(scenarioGrid, 3, "Horizonte da simulação (segundos)", _horizon);
        scenarioGrid.RowCount = 3;
        scenarioGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scenarioGrid.Controls.Add(_controllerSummary, 0, 2);
        scenarioGrid.SetColumnSpan(_controllerSummary, 2);
        _scenario.SelectedIndexChanged += async (_, _) => await OnScenarioChangedAsync();
        scenarioGroup.Controls.Add(scenarioGrid);
        steps.TabPages.Add(CreateSetupStep("1  Cenário", scenarioGroup));

        var ppoStep = new TabPage("2  Algoritmo")
        {
            BackColor = FluxAiTheme.Canvas,
            Padding = Padding.Empty,
            AutoScroll = true
        };
        ppoStep.Controls.Add(BuildPpoStep());
        steps.TabPages.Add(ppoStep);

        var controlGroup = new GroupBox
        {
            Text = "3. Parâmetros de controle dos semáforos",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10)
        };
        var controlGrid = Grid(2);
        AddField(controlGrid, 0, "Verde mínimo para decisão (s)", _minimumGreen);
        AddField(controlGrid, 1, "Verde máximo (s)", _maximumGreen);
        AddField(controlGrid, 2, "Limite de bloqueio observado", _blockingThreshold);
        AddField(controlGrid, 3, "Amostras para confirmar", _confirmationSamples);
        AddField(controlGrid, 4, "Idade máxima da medição (s)", _measurementAge);
        controlGroup.Controls.Add(controlGrid);
        steps.TabPages.Add(CreateSetupStep("3  Semáforos", controlGroup));

        var review = new TabPage("4  Revisão") { BackColor = Color.White, Padding = new Padding(24) };
        review.Controls.Add(_reviewText);
        steps.TabPages.Add(review);
        page.Controls.Add(steps, 0, 0);
        page.Controls.Add(BuildSetupFooter(), 0, 1);
        return page;
    }

    private static TabPage CreateSetupStep(string title, Control group)
    {
        var step = new TabPage(title) { BackColor = FluxAiTheme.Canvas, Padding = new Padding(18) };
        group.Dock = DockStyle.Top;
        step.Controls.Add(group);
        return step;
    }

    private Control BuildSetupFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12, 6, 12, 8),
            Margin = new Padding(0, 4, 0, 0),
            BackColor = FluxAiTheme.Surface
        };
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        actions.Controls.Add(_saveConfigurationButton);
        actions.Controls.Add(_startTrainingButton);
        actions.Controls.Add(_configurationState);
        footer.Controls.Add(actions, 0, 0);

        footer.Controls.Add(_setupNote, 0, 1);
        return footer;
    }

    private Control BuildPpoStep()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18, 14, 18, 12),
            BackColor = FluxAiTheme.Canvas
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var intro = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(4, 0, 4, 14),
            BackColor = FluxAiTheme.Canvas
        };
        intro.Controls.Add(new Label
        {
            Text = "CONFIGURA\u00c7\u00c3O DO ALGORITMO",
            AutoSize = true,
            ForeColor = FluxAiTheme.Brand,
            Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);
        intro.Controls.Add(new Label
        {
            Text = "Ajuste como o agente aprende",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            ForeColor = FluxAiTheme.Ink,
            Margin = Padding.Empty
        }, 0, 1);
        layout.Controls.Add(intro, 0, 0);

        layout.Controls.Add(CreateSetupSectionCard(
            "Dados de treinamento",
            "Volume do treino e frequência de coleta.",
            [
                ("Timesteps totais", _totalTimesteps, "Intera\u00e7\u00f5es do agente com o ambiente durante o treino."),
                ("Salvar checkpoint a cada", _checkpointEvery, "Intervalo para salvar o modelo; 0 desativa os checkpoints."),
                ("Passos por rollout", _rolloutSteps, "Experi\u00eancias coletadas antes de atualizar o agente."),
                ("Tamanho do batch", _batchSize, "Amostras processadas juntas em cada atualiza\u00e7\u00e3o.")
            ]));
        layout.Controls.Add(CreateSetupSectionCard(
            "Hiperpar\u00e2metros PPO",
            "Ritmo, estabilidade e exploração do agente.",
            [
                ("\u00c9pocas de atualiza\u00e7\u00e3o", _epochs, "Reutiliza\u00e7\u00f5es dos dados de cada rollout."),
                ("Taxa de aprendizado", _learningRate, "Define o tamanho de cada ajuste no modelo."),
                ("Gamma (desconto futuro)", _gamma, "Peso das recompensas futuras; perto de 1, v\u00ea mais longe."),
                ("GAE lambda", _gaeLambda, "Suaviza a estimativa de vantagem das a\u00e7\u00f5es."),
                ("Clip range", _clipRange, "Evita mudan\u00e7as grandes na estrat\u00e9gia por atualiza\u00e7\u00e3o."),
                ("Coeficiente de entropia", _entropy, "Valor maior incentiva testar a\u00e7\u00f5es variadas.")
            ]));

        return layout;
    }

    private Control CreateSetupSectionCard(
        string title,
        string description,
        IReadOnlyList<(string Label, NumericUpDown Input, string Help)> fields)
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(4, 0, 4, 14),
            Padding = new Padding(14),
            BackColor = FluxAiTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = FluxAiTheme.Surface
        };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            ForeColor = FluxAiTheme.Brand,
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);
        content.Controls.Add(new Label
        {
            Text = description,
            AutoSize = true,
            ForeColor = FluxAiTheme.Muted,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 1);

        var fieldList = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = fields.Count,
            BackColor = FluxAiTheme.Surface,
            Margin = Padding.Empty
        };
        fieldList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var index = 0; index < fields.Count; index++)
        {
            fieldList.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var field = BuildSetupField(fields[index].Label, fields[index].Input, fields[index].Help);
            _setupToolTip.SetToolTip(fields[index].Input, fields[index].Help);
            fieldList.Controls.Add(field, 0, index);
        }
        content.Controls.Add(fieldList, 0, 2);
        card.Controls.Add(content);
        return card;
    }

    private Control BuildSetupField(string label, NumericUpDown input, string help)
    {
        input.Visible = false;
        input.AccessibleName = label;
        input.AccessibleDescription = help;

        var card = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(12, 10, 12, 11),
            BackColor = Color.FromArgb(248, 251, 252)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(FluxAiTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            using var accent = new SolidBrush(Color.FromArgb(183, 216, 218));
            e.Graphics.FillRectangle(accent, 0, 1, 3, card.Height - 2);
        };

        var field = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            BackColor = card.BackColor
        };
        field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        field.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        field.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        field.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var fieldLabel = new Label
        {
            Text = label,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = FluxAiTheme.Ink,
            Margin = new Padding(0, 0, 0, 3)
        };
        field.Controls.Add(fieldLabel, 0, 0);
        var helpLabel = new Label
        {
            Text = help,
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Tag = "muted",
            Font = new Font("Segoe UI", 9F),
            ForeColor = FluxAiTheme.Muted,
            Margin = new Padding(0, 0, 0, 1)
        };
        var selectorRow = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Margin = new Padding(0, 9, 0, 0),
            BackColor = card.BackColor
        };
        var stepper = BuildPpoNumberStepper(input, help, helpLabel);
        stepper.Dock = DockStyle.Left;
        selectorRow.Controls.Add(stepper);
        field.Controls.Add(helpLabel, 0, 1);
        field.Controls.Add(selectorRow, 0, 2);
        card.Controls.Add(field);
        _setupToolTip.SetToolTip(stepper, help);
        return card;
    }

    private static Control BuildPpoNumberStepper(NumericUpDown valueModel, string help, Label helpLabel)
    {
        var shell = new Panel
        {
            Width = 272,
            Height = 48,
            Padding = new Padding(4),
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            Margin = Padding.Empty,
            AccessibleName = valueModel.AccessibleName,
            AccessibleDescription = help
        };
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.White,
            Margin = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var decrement = new Button
        {
            Text = "\u2212",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            ForeColor = FluxAiTheme.Muted,
            BackColor = Color.FromArgb(244, 248, 249),
            Margin = Padding.Empty,
            Tag = "ppo-stepper-minus",
            AccessibleName = $"Diminuir {valueModel.AccessibleName}"
        };
        decrement.FlatAppearance.BorderSize = 0;
        decrement.FlatAppearance.MouseOverBackColor = Color.FromArgb(228, 239, 241);

        var editor = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            ForeColor = FluxAiTheme.Ink,
            BackColor = Color.White,
            Margin = new Padding(8, 3, 8, 3),
            Tag = "ppo-number-editor",
            AccessibleName = valueModel.AccessibleName,
            AccessibleDescription = help
        };

        var increment = new Button
        {
            Text = "+",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            ForeColor = FluxAiTheme.Brand,
            BackColor = Color.FromArgb(230, 245, 244),
            Margin = Padding.Empty,
            Tag = "ppo-stepper-plus",
            AccessibleName = $"Aumentar {valueModel.AccessibleName}"
        };
        increment.FlatAppearance.BorderSize = 0;
        increment.FlatAppearance.MouseOverBackColor = Color.FromArgb(211, 237, 234);

        var hasError = false;

        void RefreshFromModel()
        {
            if (!editor.Focused)
                editor.Text = valueModel.Text;
            decrement.Enabled = valueModel.Enabled && valueModel.Value > valueModel.Minimum;
            increment.Enabled = valueModel.Enabled && valueModel.Value < valueModel.Maximum;
            hasError = false;
            helpLabel.Text = help;
            helpLabel.ForeColor = FluxAiTheme.Muted;
            shell.Invalidate();
        }
        void CommitEditor()
        {
            var parsed = decimal.TryParse(editor.Text, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture, out var value);
            if (parsed && value >= valueModel.Minimum && value <= valueModel.Maximum)
            {
                valueModel.Value = value;
                hasError = false;
                helpLabel.Text = help;
                helpLabel.ForeColor = FluxAiTheme.Muted;
                editor.Text = valueModel.Text;
            }
            else
            {
                helpLabel.Text = $"Digite um valor entre {valueModel.Minimum} e {valueModel.Maximum}.";
                helpLabel.ForeColor = Color.FromArgb(179, 57, 64);
                hasError = true;
            }
            shell.Invalidate();
        }
        void ChangeBy(decimal direction)
        {
            var next = valueModel.Value + (valueModel.Increment * direction);
            valueModel.Value = Math.Clamp(next, valueModel.Minimum, valueModel.Maximum);
            editor.Text = valueModel.Text;
        }

        decrement.Click += (_, _) => ChangeBy(-1);
        increment.Click += (_, _) => ChangeBy(1);
        valueModel.ValueChanged += (_, _) => RefreshFromModel();
        valueModel.EnabledChanged += (_, _) =>
        {
            shell.Enabled = valueModel.Enabled;
            RefreshFromModel();
        };
        editor.Leave += (_, _) => CommitEditor();
        editor.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitEditor();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                editor.Text = valueModel.Text;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode is Keys.Up or Keys.Down)
            {
                ChangeBy(e.KeyCode == Keys.Up ? 1 : -1);
                e.SuppressKeyPress = true;
            }
        };
        shell.Paint += (_, e) =>
        {
            var color = hasError
                ? Color.FromArgb(179, 57, 64)
                : editor.Focused ? FluxAiTheme.Brand : Color.FromArgb(190, 207, 211);
            using var pen = new Pen(color, editor.Focused || hasError ? 2F : 1F);
            e.Graphics.DrawRectangle(pen, 1, 1, shell.Width - 3, shell.Height - 3);
        };
        editor.Enter += (_, _) => shell.Invalidate();
        editor.Leave += (_, _) => shell.Invalidate();
        RefreshFromModel();

        row.Controls.Add(decrement, 0, 0);
        row.Controls.Add(editor, 1, 0);
        row.Controls.Add(increment, 2, 0);
        shell.Controls.Add(row);
        return shell;
    }

    private Control BuildMonitorPanel()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 3, AutoSize = true, Padding = new Padding(4), Tag = "canvas" };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _trainingState.Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold);
        var monitorCard = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 4, Padding = new Padding(22), BackColor = Color.White, Margin = new Padding(0, 0, 0, 14) };
        monitorCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        monitorCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        monitorCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        monitorCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _progressBar.Height = 18;
        _progressBar.Margin = new Padding(0, 18, 0, 12);
        monitorCard.Controls.Add(_trainingState, 0, 0);
        monitorCard.Controls.Add(_progressBar, 0, 1);
        monitorCard.Controls.Add(_progressText, 0, 2);
        _lastUpdate.Tag = "muted";
        _lastUpdate.Margin = new Padding(0, 12, 0, 0);
        monitorCard.Controls.Add(_lastUpdate, 0, 3);
        layout.Controls.Add(monitorCard, 0, 0);

        var infoCard = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, AutoSize = true, Padding = new Padding(22), BackColor = Color.White };
        infoCard.Controls.Add(new Label { Text = "O que você está vendo", AutoSize = true, Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold), ForeColor = FluxAiTheme.Ink, Margin = new Padding(0, 0, 0, 8) }, 0, 0);
        infoCard.Controls.Add(_monitorHelp, 0, 1);
        layout.Controls.Add(infoCard, 0, 1);
        var historyShortcut = new Button { Text = "Abrir execuções", AutoSize = true, Margin = new Padding(0, 18, 0, 0) };
        historyShortcut.Click += (_, _) => ShowPage("runs");
        layout.Controls.Add(historyShortcut, 0, 2);
        return layout;
    }

    private Control BuildHistoryPanel()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(4), Tag = "canvas" };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddGridTextColumn("Situação", "Status", 15);
        AddGridTextColumn("Tipo", "RunType", 12);
        AddGridTextColumn("Cenário", "ScenarioId", 25);
        AddGridTextColumn("Seed", "Seed", 10);
        AddGridTextColumn("Criado em", "CreatedAt", 25);
        AddGridTextColumn("Execução", "Id", 25);

        var historyActions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 12), Tag = "canvas" };
        var refreshButton = new Button { Text = "Atualizar", AutoSize = true };
        refreshButton.Click += async (_, _) => await LoadHistoryAsync();
        var monitorButton = new Button { Text = "Acompanhar", AutoSize = true };
        monitorButton.Click += async (_, _) => { ShowPage("monitor"); await MonitorSelectedRunAsync(); };
        var resultsButton = new Button { Text = "Abrir análise", AutoSize = true };
        resultsButton.Click += async (_, _) => await OpenRunResultsAsync();
        historyActions.Controls.Add(refreshButton);
        historyActions.Controls.Add(monitorButton);
        historyActions.Controls.Add(resultsButton);
        layout.Controls.Add(historyActions, 0, 0);
        layout.Controls.Add(_historyGrid, 0, 1);
        return layout;
    }

    private async Task ConnectAsync()
    {
        if (_demoMode)
        {
            SetConnectionState("Desative o modo de demonstração para conectar à API.", false);
            return;
        }
        if (!Uri.TryCreate(_apiUrl.Text.Trim(), UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            SetConnectionState("Informe uma URL HTTP ou HTTPS válida.", false);
            return;
        }

        SetBusy(true);
        try
        {
            var client = new FluxAiApiClient(baseUri.ToString(), _operatorToken.Text);
            await client.CheckHealthAsync();
            var scenarios = await client.GetScenariosAsync();

            _pollTimer.Stop();
            _activeRunId = null;
            _lastRunStatus = null;
            _api?.Dispose();
            _api = client;
            _loadingScenarioList = true;
            try
            {
                _scenario.DataSource = scenarios.Where(scenario => scenario.Enabled).ToList();
                _scenario.DisplayMember = nameof(ScenarioItem.Name);
                _scenario.ValueMember = nameof(ScenarioItem.Id);
            }
            finally
            {
                _loadingScenarioList = false;
            }
            _savedConfiguration = null;
            _savedConfigurationSignature = null;
            _configurationVerified = false;
            _historyRuns = [];
            _historyGrid.Rows.Clear();
            UpdateDashboard();
            _configurationState.Text = "Nenhuma configuração salva.";
            SetBusy(false);

            var enabledScenarioCount = scenarios.Count(scenario => scenario.Enabled);
            if (enabledScenarioCount == 0)
                _controllerSummary.Text = "A API conectou, mas não há cenários ativos para configurar.";

            SetConnectionState(
                string.IsNullOrWhiteSpace(_operatorToken.Text)
                    ? $"API conectada; informe token para operações de operador. Cenários ativos: {enabledScenarioCount}."
                    : $"API conectada. Cenários ativos: {enabledScenarioCount}.",
                true);
            await OnScenarioChangedAsync();
            if (!string.IsNullOrWhiteSpace(_operatorToken.Text))
                await LoadHistoryAsync();
        }
        catch (Exception exception)
        {
            SetConnectionState(DescribeException(exception), false);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task OnScenarioChangedAsync()
    {
        if (_loadingScenarioList)
            return;

        _selectedScenario = _scenario.SelectedItem as ScenarioItem;
        if (_demoMode)
        {
            ConfigureDemoScenarioSelection();
            return;
        }
        _loadedControllers = [];
        _savedConfiguration = null;
        _savedConfigurationSignature = null;
        _configurationVerified = false;
        _configurationState.Text = "Salve uma configuração para este cenário antes de iniciar.";
        _saveConfigurationButton.Enabled = false;
        _startTrainingButton.Enabled = false;

        if (_selectedScenario is null || _api is null)
        {
            _controllerSummary.Text = "Selecione um cenário após conectar.";
            return;
        }

        var scenario = _selectedScenario;
        _controllerSummary.Text = "Carregando controladores do cenário...";

        try
        {
            var controllers = await _api.GetSignalControllersAsync(scenario.Id);
            if (_selectedScenario?.Id != scenario.Id)
                return;

            _loadedControllers = controllers;
            var adaptiveCount = controllers.Count(controller => controller.Adaptive);
            var controllerIds = string.Join(", ", controllers.Take(5).Select(controller => controller.Id));
            var remainingCount = controllers.Count - Math.Min(controllers.Count, 5);
            var idSummary = controllers.Count == 0
                ? "Nenhum controlador retornado pela API."
                : $"IDs: {controllerIds}{(remainingCount > 0 ? $" e mais {remainingCount}" : "")}";
            _controllerSummary.Text =
                $"Cenário: {scenario.Name} · revisão {scenario.Revision} · ambiente {scenario.Environment}" +
                $"\nControladores carregados: {controllers.Count} ({adaptiveCount} adaptativos) · {idSummary}";
            _saveConfigurationButton.Enabled = controllers.Count > 0;
        }
        catch (Exception exception)
        {
            if (_selectedScenario?.Id != scenario.Id)
                return;

            _controllerSummary.Text = "Não foi possível carregar os controladores.";
            SetConnectionState(DescribeException(exception), false);
        }
    }

    private async Task SaveConfigurationAsync()
    {
        if (_demoMode)
        {
            MessageBox.Show(this, "Os valores mostrados são aleatórios e servem apenas para demonstração. Desative o modo para salvar uma configuração real.", "Modo demonstração ativo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_api is null)
        {
            MessageBox.Show(this, "Conecte o painel à API primeiro.", "Sem conexão", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_selectedScenario is null)
        {
            MessageBox.Show(this, "Selecione um cenário ativo.", "Cenário necessário", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var request = BuildConfigurationRequest();
        var validationErrors = ValidateConfiguration(request);
        if (validationErrors.Count > 0)
        {
            MessageBox.Show(this,
                "Revise estes campos antes de salvar:\n\n• " + string.Join("\n• ", validationErrors),
                "Configuração inválida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var requestSignature = ConfigurationSignature(request);
        _configurationVerified = false;
        _startTrainingButton.Enabled = false;
        SetBusy(true);
        try
        {
            ConfigurationResponse saved;
            if (_savedConfiguration is null)
            {
                saved = await _api.CreateConfigurationAsync(request);
            }
            else if (string.Equals(requestSignature, _savedConfigurationSignature, StringComparison.Ordinal))
            {
                saved = _savedConfiguration;
            }
            else
            {
                saved = await _api.ReplaceConfigurationAsync(
                    _savedConfiguration.Id,
                    _savedConfiguration.Revision,
                    request);
            }

            _savedConfiguration = saved;
            _savedConfigurationSignature = requestSignature;
            var verified = await _api.GetConfigurationAsync(saved.Id);
            if (!ConfigurationMatchesRequest(verified, request))
                throw new InvalidDataException("A API salvou a configuração, mas os valores consultados não correspondem ao que foi enviado.");

            _savedConfiguration = verified;
            _savedConfigurationSignature = requestSignature;
            _configurationVerified = true;
            _configurationState.Text = $"Configuração salva e consultada · revisão {verified.Revision} · ID {verified.Id:D}";
            _startTrainingButton.Enabled = true;
        }
        catch (Exception exception)
        {
            _configurationVerified = false;
            _configurationState.Text = "Não foi possível salvar a configuração.";
            MessageBox.Show(this, DescribeException(exception), "Falha ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task StartTrainingAsync()
    {
        if (_demoMode)
        {
            MessageBox.Show(this, "A execução exibida é fictícia e não será enviada à API. Desative o modo de demonstração para iniciar um treinamento real.", "Modo demonstração ativo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_api is null || _selectedScenario is null || _savedConfiguration is null || !_configurationVerified)
        {
            MessageBox.Show(this, "Conecte à API, selecione um cenário e salve a configuração.", "Treinamento não preparado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var currentSignature = ConfigurationSignature(BuildConfigurationRequest());
        if (!string.Equals(currentSignature, _savedConfigurationSignature, StringComparison.Ordinal))
        {
            MessageBox.Show(this, "Os parâmetros mudaram depois de salvar. Salve novamente antes de iniciar.", "Configuração alterada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var validationErrors = ValidateConfiguration(BuildConfigurationRequest());
        if (validationErrors.Count > 0)
        {
            MessageBox.Show(this,
                "Revise a configuração antes de iniciar:\n\n• " + string.Join("\n• ", validationErrors),
                "Configuração inválida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var request = new RunRequest(
            _savedConfiguration.Id,
            "training",
            (long)_seed.Value,
            (int)_horizon.Value);
        var confirmation = MessageBox.Show(
            this,
            BuildTrainingSummary(request),
            "Confirmar configuração do treinamento",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes)
            return;

        // The API resolves configurationId to its current revision at submission
        // time. Include that revision in the local fingerprint so a changed
        // configuration never reuses an idempotency key from an earlier attempt.
        var fingerprint = JsonSerializer.Serialize(new
        {
            Request = request,
            ConfigurationRevision = _savedConfiguration.Revision
        });
        if (!string.Equals(fingerprint, _pendingRunFingerprint, StringComparison.Ordinal) || _pendingIdempotencyKey is null)
        {
            _pendingRunFingerprint = fingerprint;
            _pendingIdempotencyKey = Guid.NewGuid().ToString("N");
        }

        SetBusy(true);
        try
        {
            var run = await _api.StartRunAsync(request, _pendingIdempotencyKey);
            _pendingRunFingerprint = null;
            _pendingIdempotencyKey = null;
            _activeRunId = run.Id;
            DisplayRun(run);
            _pollTimer.Start();
            await PollActiveRunAsync();
        }
        catch (ApiRequestException exception) when (
            exception.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.MethodNotAllowed)
        {
            _trainingState.Text = "A API ainda não habilitou o início de treinamentos.";
            MessageBox.Show(this,
                $"A API respondeu HTTP {(int)exception.StatusCode} para POST /api/v1/runs. Esta versão do backend ainda não oferece o endpoint de fila e worker, então a solicitação não foi registrada. O painel preservou a chave de idempotência para permitir uma tentativa segura quando o backend estiver pronto.\n\nDetalhe: {exception.Message}",
                "Início de treinamento indisponível",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            _trainingState.Text = "A solicitação de treinamento não foi confirmada.";
            MessageBox.Show(this, DescribeException(exception), "Falha ao iniciar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task PollActiveRunAsync()
    {
        if (_pollInProgress || _api is null || _activeRunId is null)
            return;

        _pollInProgress = true;
        try
        {
            var run = await _api.GetRunAsync(_activeRunId.Value);
            DisplayRun(run);
            if (IsTerminal(run.Status))
            {
                _pollTimer.Stop();
                await LoadHistoryAsync();
            }
        }
        catch (Exception exception)
        {
            _lastUpdate.Text = "Última tentativa de atualização: " + DateTime.Now.ToString("HH:mm:ss");
            _trainingState.Text = "Sem atualização da execução: " + DescribeException(exception);
        }
        finally
        {
            _pollInProgress = false;
        }
    }

    private async Task LoadHistoryAsync()
    {
        if (_demoMode)
        {
            FillDemoConfiguration();
            GenerateDemoData();
            return;
        }
        if (_api is null)
        {
            MessageBox.Show(this, "Conecte o painel à API primeiro.", "Sem conexão", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var runs = await _api.GetRunsAsync();
            _historyRuns = runs;
            FillHistoryGrid(runs);
            UpdateDashboard();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, DescribeException(exception), "Falha ao carregar histórico", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task MonitorSelectedRunAsync()
    {
        if (_historyGrid.SelectedRows.Count == 0 || _historyGrid.SelectedRows[0].Tag is not Guid runId)
        {
            MessageBox.Show(this, "Selecione uma execução no histórico.", "Execução não selecionada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _activeRunId = runId;
        if (_demoMode)
        {
            var demoRun = _demoRuns.FirstOrDefault(item => item.Run.Id == runId);
            if (demoRun is not null)
            {
                DisplayRun(demoRun.Run);
                _lastUpdate.Text = "Amostra fictícia gerada localmente";
            }
            return;
        }
        await PollActiveRunAsync();
        if (_activeRunId is not null && _lastRunStatus is not null && !IsTerminal(_lastRunStatus))
            _pollTimer.Start();
    }

    private async Task OpenRunResultsAsync()
    {
        if (!_demoMode && _api is null)
        {
            MessageBox.Show(this, "Conecte o painel à API primeiro.", "Sem conexão", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_historyGrid.SelectedRows.Count == 0 || _historyGrid.SelectedRows[0].Tag is not Guid runId)
        {
            MessageBox.Show(this, "Selecione uma execução no histórico para analisar.", "Execução não selecionada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!_demoMode && _historyRuns.Count == 0)
            await LoadHistoryAsync();
        if (_historyRuns.Count == 0)
        {
            MessageBox.Show(this, "O histórico não retornou execuções para análise.", "Sem resultados", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var demoResult = _demoMode ? _demoRuns.FirstOrDefault(item => item.Run.Id == runId) : null;
        using var resultsForm = new RunResultsForm(_demoMode ? null : _api, _historyRuns, runId, demoResult);
        resultsForm.ShowDialog(this);
    }

    private void FillHistoryGrid(IReadOnlyList<TrainingRun> runs)
    {
        _historyGrid.Rows.Clear();
        foreach (var run in runs.OrderByDescending(item => item.CreatedAtUtc))
        {
            var index = _historyGrid.Rows.Add(
                TranslateStatusShort(run.Status),
                run.RunType,
                run.ScenarioId,
                run.Seed,
                run.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
                run.Id.ToString("N")[..8]);
            _historyGrid.Rows[index].Tag = run.Id;
        }
        if (_historyGrid.Rows.Count > 0)
            _historyGrid.Rows[0].Selected = true;
    }

    private void DisplayRun(TrainingRun run)
    {
        _lastRunStatus = run.Status;
        UpdateDashboard(run);
        _trainingState.Text = _demoMode ? $"Amostra fictícia | {TranslateStatusShort(run.Status)} | {run.ScenarioId}" : $"Execução {run.Id.ToString("N")[..8]} | {TranslateStatusShort(run.Status)} | cenário {run.ScenarioId}";
        var progress = run.Progress;
        if (progress is null)
        {
            _progressBar.Value = 0;
            _progressText.Text = run.Status switch
            {
                "queued" => "Solicitação na fila; aguardando o worker iniciar.",
                "running" => "Worker em andamento; a API ainda não informou progresso detalhado.",
                "succeeded" => "Execução concluída; a API não informou progresso temporal.",
                "failed" => "Execução marcada como falha; a API não informou progresso temporal.",
                "interrupted" => "Execução interrompida; a API não informou progresso temporal.",
                _ => "A API não informou dados de progresso para esta execução."
            };
        }
        else
        {
            var total = Math.Max(0, progress.TotalTimesteps ?? 0);
            var completed = Math.Max(0, progress.CompletedTimesteps ?? 0);
            _progressBar.Value = total > 0
                ? (int)Math.Clamp((double)completed / total * _progressBar.Maximum, 0, _progressBar.Maximum)
                : 0;
            var episode = progress.Episode is null ? "" : $" · episódio {progress.Episode}";
            var note = string.IsNullOrWhiteSpace(progress.Note) ? "" : $" · {progress.Note}";
            _progressText.Text = $"Fase: {progress.Phase}{episode} · {completed:N0}/{(total > 0 ? total.ToString("N0") : "?")} timesteps{note}";
        }

        if (run.Failure is not null)
            _progressText.Text += $" · Falha {run.Failure.Code}: {run.Failure.Message}";
        _lastUpdate.Text = _demoMode ? "Amostra fictícia gerada localmente" : "Atualizado em " + DateTime.Now.ToString("HH:mm:ss");
    }

    private void UpdateDashboard(TrainingRun? latestOverride = null)
    {
        var ordered = _historyRuns.OrderByDescending(run => run.CreatedAtUtc).ToArray();
        _dashboardRunCount.Text = ordered.Length.ToString("N0");
        _dashboardSuccessCount.Text = ordered.Count(run => run.Status == "succeeded").ToString("N0");
        var latest = ordered.FirstOrDefault();
        if (latestOverride is not null &&
            (latest is null || latestOverride.Id == latest.Id || latestOverride.CreatedAtUtc > latest.CreatedAtUtc))
            latest = latestOverride;
        _dashboardLatestStatus.Text = latest is null ? "Sem dados" : TranslateStatusShort(latest.Status);
        _dashboardScenario.Text = latest is null ? "-" : CompactScenarioName(latest.ScenarioId);
        _dashboardToolTip.SetToolTip(_dashboardScenario, latest?.ScenarioId ?? "");
        if (ordered.Length == 0)
            _dashboardRecentText.Text = _api is null
                ? "Conecte-se à API para carregar as execuções."
                : "Ainda não há execuções registradas para esta API.";
        else
            _dashboardRecentText.Text = string.Join(Environment.NewLine + Environment.NewLine, ordered.Take(5).Select(run =>
                $"{TranslateStatus(run.Status)}  ·  {run.ScenarioId}\r\n{run.CreatedAtUtc.ToLocalTime():dd/MM/yyyy HH:mm}  ·  seed {run.Seed}"));
        if (_demoMode && ordered.Length > 0)
            _dashboardRecentText.Text = "EXEMPLOS FICTÍCIOS · gerados localmente\r\n\r\n" + _dashboardRecentText.Text;

        var statuses = new[] { "queued", "running", "succeeded", "failed", "interrupted" };
        _dashboardChart.Points = ordered.Length == 0 ? [] : statuses
            .Select(status => new FluxAiChartPoint(status switch
            {
                "queued" => "Fila",
                "running" => "Rodando",
                "succeeded" => "Concluídas",
                "failed" => "Falhas",
                _ => "Interrompidas"
            }, ordered.Count(run => run.Status == status)))
            .ToArray();
        _dashboardChart.EmptyMessage = ordered.Length == 0
            ? "As execuções aparecerão aqui depois da conexão com a API."
            : "Nenhuma execução para exibir.";
    }

    private static string CompactScenarioName(string scenarioId)
    {
        const string intersectionPrefix = "Interse\u00e7\u00e3o ";
        const string centerPrefix = "Centro - ";
        const string eastPrefix = "Zona Leste - ";
        if (scenarioId.StartsWith(intersectionPrefix, StringComparison.OrdinalIgnoreCase))
            return scenarioId[intersectionPrefix.Length..];
        if (scenarioId.StartsWith(centerPrefix, StringComparison.OrdinalIgnoreCase))
            return scenarioId[centerPrefix.Length..];
        if (scenarioId.StartsWith(eastPrefix, StringComparison.OrdinalIgnoreCase))
            return scenarioId[eastPrefix.Length..] + " Leste";
        return scenarioId;
    }

    private ConfigurationWriteRequest BuildConfigurationRequest() => new(
        _configurationName.Text.Trim(),
        _selectedScenario?.Id ?? "",
        new GreenTimingWrite(
            new GreenTimingDefault((double)_minimumGreen.Value, (double)_maximumGreen.Value),
            Array.Empty<object>()),
        new BlockingWrite(
            (double)_blockingThreshold.Value,
            (int)_confirmationSamples.Value,
            (double)_measurementAge.Value),
        new PpoWrite(
            (int)_totalTimesteps.Value,
            (int)_checkpointEvery.Value,
            (int)_rolloutSteps.Value,
            (int)_batchSize.Value,
            (int)_epochs.Value,
            (double)_learningRate.Value,
            (double)_gamma.Value,
            (double)_gaeLambda.Value,
            (double)_clipRange.Value,
            (double)_entropy.Value));

    private static string ConfigurationSignature(ConfigurationWriteRequest request) => JsonSerializer.Serialize(request);

    private IReadOnlyList<string> ValidateConfiguration(ConfigurationWriteRequest request)
    {
        var errors = new List<string>();
        var nameLength = request.Name.Trim().Length;
        if (nameLength is < 1 or > 120)
            errors.Add("Nome da configuração: informe de 1 a 120 caracteres.");
        if (_loadedControllers.Count == 0)
            errors.Add("Cenário: conecte à API e carregue pelo menos um controlador.");

        var green = request.GreenTiming.Default;
        if (green.DecisionMinimumSeconds is <= 0 or > 90)
            errors.Add("Verde mínimo: use um valor maior que 0 e até 90 segundos.");
        if (green.MaximumSeconds is <= 0 or > 90)
            errors.Add("Verde máximo: use um valor maior que 0 e até 90 segundos.");
        if (green.DecisionMinimumSeconds > green.MaximumSeconds)
            errors.Add("Verde mínimo: não pode ser maior que o verde máximo.");

        var blocking = request.Blocking;
        if (blocking.ObservedCapacityFractionThreshold is <= 0 or > 1)
            errors.Add("Limite de bloqueio: use um valor maior que 0 e até 1.");
        if (blocking.ConfirmationSamples is < 2 or > 20)
            errors.Add("Amostras para confirmar: use um valor entre 2 e 20.");
        if (blocking.MaximumMeasurementAgeSeconds is <= 0 or > 60)
            errors.Add("Idade máxima da medição: use um valor maior que 0 e até 60 segundos.");

        var ppo = request.Ppo;
        if (ppo.TotalTimesteps is < 128 or > 100_000)
            errors.Add("Timesteps totais: use um valor entre 128 e 100000.");
        if (ppo.CheckpointEvery is < 0 or > 100_000)
            errors.Add("Checkpoint: use 0 para desativar ou um valor entre 1 e 100000.");
        if (ppo.RolloutSteps is < 16 or > 4096)
            errors.Add("Passos por rollout: use um valor entre 16 e 4096.");
        if (ppo.BatchSize is < 8 or > 4096)
            errors.Add("Tamanho do batch: use um valor entre 8 e 4096.");
        else if (ppo.BatchSize > ppo.RolloutSteps)
            errors.Add("Tamanho do batch: não pode exceder os passos por rollout.");
        if (ppo.Epochs is < 1 or > 100)
            errors.Add("Épocas de atualização: use um valor entre 1 e 100.");
        if (ppo.LearningRate is <= 0 or > 0.1)
            errors.Add("Taxa de aprendizado: use um valor maior que 0 e até 0,1.");
        if (ppo.Gamma is < 0 or > 1)
            errors.Add("Gamma: use um valor entre 0 e 1.");
        if (ppo.GaeLambda is < 0 or > 1)
            errors.Add("GAE lambda: use um valor entre 0 e 1.");
        if (ppo.ClipRange is <= 0 or > 1)
            errors.Add("Clip range: use um valor maior que 0 e até 1.");
        if (ppo.EntropyCoefficient is < 0 or > 1)
            errors.Add("Coeficiente de entropia: use um valor entre 0 e 1.");

        return errors;
    }

    private static bool ConfigurationMatchesRequest(
        ConfigurationResponse response,
        ConfigurationWriteRequest request) =>
        response.ScenarioId == request.ScenarioId &&
        response.Name == request.Name &&
        response.GreenTiming.Default == request.GreenTiming.Default &&
        response.GreenTiming.PhaseOverrides.Count == request.GreenTiming.PhaseOverrides.Count &&
        response.Blocking == request.Blocking &&
        response.Ppo == request.Ppo;

    private string BuildTrainingSummary(RunRequest run) => string.Join(Environment.NewLine,
    [
        $"Cenário: {_selectedScenario!.Name} (revisão {_selectedScenario.Revision})",
        $"Ambiente: {_selectedScenario.Environment} · controladores: {_loadedControllers.Count} ({_loadedControllers.Count(controller => controller.Adaptive)} adaptativos)",
        $"Configuração: {_savedConfiguration!.Name} · revisão {_savedConfiguration.Revision}",
        $"Seed: {run.Seed} · horizonte: {run.HorizonSeconds} segundos",
        $"PPO: {_totalTimesteps.Value:N0} timesteps · rollout {_rolloutSteps.Value:N0} · batch {_batchSize.Value:N0} · épocas {_epochs.Value:N0}",
        $"Aprendizado: taxa {_learningRate.Value:G} · gamma {_gamma.Value:G} · GAE {_gaeLambda.Value:G} · clip {_clipRange.Value:G} · entropia {_entropy.Value:G}",
        $"Semáforos: verde {_minimumGreen.Value:G}–{_maximumGreen.Value:G} s · bloqueio {_blockingThreshold.Value:P0} · amostras {_confirmationSamples.Value:N0} · medição até {_measurementAge.Value:N0} s",
        "\nA execução usa simulação. Deseja solicitar o treinamento à API?"
    ]);

    private static bool IsTerminal(string status) => status is "succeeded" or "failed" or "interrupted";

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
        "succeeded" => "concluído tecnicamente",
        "failed" => "falhou",
        "interrupted" => "interrompido",
        _ => status
    };

    private static string DescribeException(Exception exception) => exception switch
    {
        ApiRequestException apiException => apiException.Message,
        TaskCanceledException => "Tempo de resposta excedido.",
        HttpRequestException => "Não foi possível alcançar a API. Confira o endereço e se o servidor está iniciado.",
        InvalidDataException => exception.Message,
        _ => exception.Message
    };

    private void SetConnectionState(string message, bool connected)
    {
        _connectionState.Text = message;
        _connectionState.ForeColor = connected ? Color.DarkGreen : Color.DarkRed;
        _sidebarConnectionState.Text = _demoMode ? "Demonstração local" : connected ? "API conectada" : "Desconectado";
        _sidebarConnectionState.ForeColor = _demoMode || connected ? Color.FromArgb(150, 220, 188) : Color.FromArgb(229, 167, 151);
    }

    private void SetBusy(bool busy)
    {
        _connectButton.Enabled = !busy && !_demoMode;
        _saveConfigurationButton.Enabled = !busy && !_demoMode && _api is not null && _selectedScenario is not null && _loadedControllers.Count > 0;
        _startTrainingButton.Enabled = !busy && !_demoMode && _configurationVerified && _savedConfiguration is not null;
        FluxAiTheme.StylePrimaryButton(_startTrainingButton);
        FluxAiTheme.StyleSecondaryButton(_saveConfigurationButton);
        UseWaitCursor = busy;
    }

    private static NumericUpDown Number(decimal minimum, decimal maximum, decimal value, decimal increment, int decimalPlaces = 0) => new()
    {
        Minimum = minimum,
        Maximum = maximum,
        Value = value,
        Increment = increment,
        DecimalPlaces = decimalPlaces,
        ThousandsSeparator = true,
        Width = 160
    };

    private static Control Field(string label, Control input)
    {
        var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = new Padding(6, 5, 16, 9), Padding = new Padding(2), BackColor = FluxAiTheme.Surface };
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Tag = "field-label", Margin = new Padding(0, 0, 0, 5) }, 0, 0);
        panel.Controls.Add(input, 0, 1);
        return panel;
    }

    private static TableLayoutPanel Grid(int columns)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = columns,
            RowCount = 0,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            Padding = new Padding(4),
            BackColor = FluxAiTheme.Surface
        };
        for (var index = 0; index < columns; index++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
        return grid;
    }

    private static void AddField(TableLayoutPanel grid, int index, string label, Control input)
    {
        var row = index / grid.ColumnCount;
        var column = index % grid.ColumnCount;
        if (row >= grid.RowCount)
        {
            grid.RowCount = row + 1;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        grid.Controls.Add(Field(label, input), column, row);
    }

    private void AddGridTextColumn(string header, string property, float fill)
    {
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            Name = property,
            FillWeight = fill,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }
}
