namespace AIprnScrAnalizerToText;

public sealed partial class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly ActiveWindowCaptureService _captureService = new();
    private readonly IAiAgent[] _visionAgents;
    private readonly ITextAiAgent[] _textAgents;
    private readonly HotkeyService _hotkeyService = new();

    private readonly TabControl tabControl = new();

    private readonly ComboBox cmbVisionAgent = new();
    private readonly ComboBox cmbTextAgent = new();

    private readonly TextBox txtOllamaVisionUrl = new();
    private readonly TextBox txtOllamaVisionModel = new();
    private readonly TextBox txtOllamaTextUrl = new();
    private readonly TextBox txtOllamaTextModel = new();
    private readonly TextBox txtOpenAiKey = new();
    private readonly TextBox txtOpenAiBaseUrl = new();
    private readonly TextBox txtOpenAiVisionModel = new();
    private readonly TextBox txtOpenAiTextModel = new();
    private readonly NumericUpDown nudTemperature = new();
    private readonly NumericUpDown nudTimeout = new();
    private readonly CheckBox chkIncludeVisionPrompt = new();

    private readonly RichTextBox rtbVisionPrompt = new();
    private readonly RichTextBox rtbTextPrompt = new();
    private readonly RichTextBox rtbVisionResult = new();
    private readonly RichTextBox rtbFinalResult = new();
    private readonly TextBox txtStatus = new();

    private readonly Button btnActivate = new();
    private readonly Button btnExecuteNow = new();
    private readonly Button btnSaveSettings = new();
    private readonly Button btnCancel = new();

    private CancellationTokenSource? _currentRunCts;
    private bool _isRunning;

    public MainForm()
    {
        _settings = AppSettings.Load();
        _visionAgents = new IAiAgent[] { new OllamaVisionAgent(), new OpenAICompatibleAgent() };
        _textAgents = new ITextAiAgent[] { new OllamaTextAgent(), new OpenAITextAgent() };

        Text = "AIprnScrAnalizerToText";
        Width = 1180;
        Height = 820;
        MinimumSize = new Size(980, 650);
        StartPosition = FormStartPosition.CenterScreen;

        InitializeUi();
        LoadSettingsToUi();

        _hotkeyService.HotkeyPressed += async (_, _) => await ExecutePipelineAsync();
        _hotkeyService.Register();

        SetStatus("Skrót CTRL + ALT + I jest aktywny. Ustaw aktywne okno i naciśnij skrót.");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _currentRunCts?.Cancel();
        _currentRunCts?.Dispose();
        _hotkeyService.Dispose();
        base.OnFormClosed(e);
    }

    private void InitializeUi()
    {
        tabControl.Dock = DockStyle.Fill;

        var workTab = new TabPage("Praca");
        var promptTab = new TabPage("Prompty");
        var configTab = new TabPage("Konfiguracja");

        BuildWorkTab(workTab);
        BuildPromptTab(promptTab);
        BuildConfigTab(configTab);

        tabControl.TabPages.Add(workTab);
        tabControl.TabPages.Add(promptTab);
        tabControl.TabPages.Add(configTab);

        Controls.Add(tabControl);
    }

    private void BuildWorkTab(TabPage tab)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // przyciski
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));   // status
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));  // Wynik Vision — opis obrazu
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Wynik końcowy Text LLM

        var buttonsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        btnActivate.Text = "Start / Aktywuj skrót";
        btnActivate.Width = 160;
        btnActivate.Height = 32;
        btnActivate.Click += (_, _) =>
        {
            ReadSettingsFromUi();
            _settings.Save();
            SetStatus("Aktywne. Ustaw okno docelowe i naciśnij CTRL + ALT + I.");
        };

        btnExecuteNow.Text = "Wykonaj teraz";
        btnExecuteNow.Width = 130;
        btnExecuteNow.Height = 32;
        btnExecuteNow.Click += async (_, _) => await ExecutePipelineAsync();

        btnCancel.Text = "Przerwij";
        btnCancel.Width = 100;
        btnCancel.Height = 32;
        btnCancel.Enabled = false;
        btnCancel.Click += (_, _) => _currentRunCts?.Cancel();

        btnSaveSettings.Text = "Zapisz konfigurację";
        btnSaveSettings.Width = 150;
        btnSaveSettings.Height = 32;
        btnSaveSettings.Click += (_, _) =>
        {
            ReadSettingsFromUi();
            _settings.Save();
            SetStatus($"Zapisano konfigurację: {AppSettings.SettingsFilePath}");
        };

        buttonsPanel.Controls.AddRange(new Control[] { btnActivate, btnExecuteNow, btnCancel, btnSaveSettings });

        txtStatus.Dock = DockStyle.Fill;
        txtStatus.ReadOnly = true;

        root.Controls.Add(buttonsPanel, 0, 0);
        root.Controls.Add(txtStatus, 0, 1);

        root.Controls.Add(
            CreateLabeledRichTextBox("Wynik Vision — opis obrazu", rtbVisionResult, readOnly: true),
            0,
            2);

        root.Controls.Add(
            CreateLabeledRichTextBox("Wynik końcowy Text LLM", rtbFinalResult, readOnly: true),
            0,
            3);

        tab.Controls.Add(root);
    }

    private void BuildPromptTab(TabPage tab)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 340,
            Padding = new Padding(10)
        };

        split.Panel1.Controls.Add(CreateLabeledRichTextBox("Prompt dla Vision — co model ma odczytać z obrazu", rtbVisionPrompt, readOnly: false));
        split.Panel2.Controls.Add(CreateLabeledRichTextBox("Prompt dla zwykłego LLM — co zrobić z opisem Vision", rtbTextPrompt, readOnly: false));

        tab.Controls.Add(split);
    }

    private void BuildConfigTab(TabPage tab)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 16,
            Padding = new Padding(14),
            AutoScroll = true
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddComboRow(root, "Vision Agent:", cmbVisionAgent, _visionAgents.Select(a => a.Name).ToArray());
        AddTextRow(root, "Ollama Vision URL:", txtOllamaVisionUrl);
        AddTextRow(root, "Ollama Vision Model:", txtOllamaVisionModel);
        AddComboRow(root, "Text LLM Agent:", cmbTextAgent, _textAgents.Select(a => a.Name).ToArray());
        AddTextRow(root, "Ollama Text URL:", txtOllamaTextUrl);
        AddTextRow(root, "Ollama Text Model:", txtOllamaTextModel);
        AddTextRow(root, "OpenAI API Key:", txtOpenAiKey, password: true);
        AddTextRow(root, "OpenAI Base URL:", txtOpenAiBaseUrl);
        AddTextRow(root, "OpenAI Vision Model:", txtOpenAiVisionModel);
        AddTextRow(root, "OpenAI Text Model:", txtOpenAiTextModel);
        AddNumericRow(root, "Temperature:", nudTemperature, 0, 2, 0.1m, 1);
        AddNumericRow(root, "Timeout [s]:", nudTimeout, 30, 1800, 300, 0);

        chkIncludeVisionPrompt.Text = "Dołącz prompt Vision do kontekstu końcowego LLM";
        chkIncludeVisionPrompt.Dock = DockStyle.Fill;
        root.Controls.Add(new Label { Text = "Kontekst:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, root.RowCountUsed());
        root.Controls.Add(chkIncludeVisionPrompt, 1, root.RowCountUsed() - 1);

        var info = new Label
        {
            Text = "Pipeline: CTRL+ALT+I → screenshot aktywnego okna → Vision → Text LLM → wynik końcowy.",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0)
        };
        root.Controls.Add(info, 0, root.RowCountUsed());
        root.SetColumnSpan(info, 2);

        tab.Controls.Add(root);
    }

    private static Control CreateLabeledRichTextBox(string labelText, RichTextBox richTextBox, bool readOnly)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        richTextBox.Dock = DockStyle.Fill;
        richTextBox.ReadOnly = readOnly;
        richTextBox.Font = new Font("Consolas", 10F);
        richTextBox.WordWrap = true;

        panel.Controls.Add(label, 0, 0);
        panel.Controls.Add(richTextBox, 0, 1);
        return panel;
    }

    private static void AddComboRow(TableLayoutPanel root, string labelText, ComboBox comboBox, string[] items)
    {
        var row = root.RowCountUsed();
        comboBox.Dock = DockStyle.Fill;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Items.Clear();
        comboBox.Items.AddRange(items.Cast<object>().ToArray());
        AddLabel(root, labelText, row);
        root.Controls.Add(comboBox, 1, row);
    }

    private static void AddTextRow(TableLayoutPanel root, string labelText, TextBox textBox, bool password = false)
    {
        var row = root.RowCountUsed();
        textBox.Dock = DockStyle.Fill;
        textBox.UseSystemPasswordChar = password;
        AddLabel(root, labelText, row);
        root.Controls.Add(textBox, 1, row);
    }

    private static void AddNumericRow(TableLayoutPanel root, string labelText, NumericUpDown numeric, decimal minimum, decimal maximum, decimal increment, int decimalPlaces)
    {
        var row = root.RowCountUsed();
        numeric.Dock = DockStyle.Left;
        numeric.Width = 120;
        numeric.Minimum = minimum;
        numeric.Maximum = maximum;
        numeric.Increment = increment;
        numeric.DecimalPlaces = decimalPlaces;
        AddLabel(root, labelText, row);
        root.Controls.Add(numeric, 1, row);
    }

    private static void AddLabel(TableLayoutPanel root, string text, int row)
    {
        root.Controls.Add(new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        }, 0, row);
    }

    private void LoadSettingsToUi()
    {
        SelectComboValue(cmbVisionAgent, _settings.SelectedVisionAgent);
        SelectComboValue(cmbTextAgent, _settings.SelectedTextAgent);

        txtOllamaVisionUrl.Text = _settings.OllamaVisionUrl;
        txtOllamaVisionModel.Text = _settings.OllamaVisionModel;
        txtOllamaTextUrl.Text = _settings.OllamaTextUrl;
        txtOllamaTextModel.Text = _settings.OllamaTextModel;
        txtOpenAiKey.Text = _settings.OpenAIApiKey;
        txtOpenAiBaseUrl.Text = _settings.OpenAIBaseUrl;
        txtOpenAiVisionModel.Text = _settings.OpenAIVisionModel;
        txtOpenAiTextModel.Text = _settings.OpenAITextModel;
        nudTemperature.Value = Math.Clamp(_settings.Temperature, nudTemperature.Minimum, nudTemperature.Maximum);
        nudTimeout.Value = Math.Clamp(_settings.RequestTimeoutSeconds, (int)nudTimeout.Minimum, (int)nudTimeout.Maximum);
        chkIncludeVisionPrompt.Checked = _settings.IncludeVisionPromptInFinalContext;

        rtbVisionPrompt.Text = _settings.VisionPrompt;
        rtbTextPrompt.Text = _settings.TextAgentPrompt;
    }

    private void ReadSettingsFromUi()
    {
        _settings.SelectedVisionAgent = cmbVisionAgent.Text;
        _settings.SelectedTextAgent = cmbTextAgent.Text;
        _settings.OllamaVisionUrl = txtOllamaVisionUrl.Text.Trim();
        _settings.OllamaVisionModel = txtOllamaVisionModel.Text.Trim();
        _settings.OllamaTextUrl = txtOllamaTextUrl.Text.Trim();
        _settings.OllamaTextModel = txtOllamaTextModel.Text.Trim();
        _settings.OpenAIApiKey = txtOpenAiKey.Text.Trim();
        _settings.OpenAIBaseUrl = txtOpenAiBaseUrl.Text.Trim();
        _settings.OpenAIVisionModel = txtOpenAiVisionModel.Text.Trim();
        _settings.OpenAITextModel = txtOpenAiTextModel.Text.Trim();
        _settings.Temperature = nudTemperature.Value;
        _settings.RequestTimeoutSeconds = (int)nudTimeout.Value;
        _settings.IncludeVisionPromptInFinalContext = chkIncludeVisionPrompt.Checked;
        _settings.VisionPrompt = rtbVisionPrompt.Text.Trim();
        _settings.TextAgentPrompt = rtbTextPrompt.Text.Trim();
    }

    private async Task ExecutePipelineAsync()
    {
        if (_isRunning)
        {
            SetStatus("Poprzednie wykonanie nadal trwa. Użyj Przerwij albo poczekaj na zakończenie.");
            return;
        }

        ReadSettingsFromUi();
        _settings.Save();

        _currentRunCts?.Dispose();
        _currentRunCts = new CancellationTokenSource();
        var ct = _currentRunCts.Token;

        try
        {
            _isRunning = true;
            SetRunUiState(isRunning: true);

            rtbVisionResult.Clear();
            rtbFinalResult.Clear();

            SetStatus("Przechwytywanie aktywnego okna...");
            await Task.Delay(150, ct).ConfigureAwait(true);
            var image = _captureService.CaptureActiveWindow();

            var visionAgent = _visionAgents.FirstOrDefault(a => a.Name == _settings.SelectedVisionAgent)
                ?? throw new InvalidOperationException($"Nie znaleziono Vision Agent: {_settings.SelectedVisionAgent}");

            SetStatus($"Wysyłanie obrazu do Vision Agent: {visionAgent.Name}...");
            var visionText = await visionAgent.AnalyzeImageAsync(image, _settings.VisionPrompt, _settings, ct).ConfigureAwait(true);
            rtbVisionResult.Text = visionText;

            var textAgent = _textAgents.FirstOrDefault(a => a.Name == _settings.SelectedTextAgent)
                ?? throw new InvalidOperationException($"Nie znaleziono Text Agent: {_settings.SelectedTextAgent}");

            var finalPrompt = BuildFinalPrompt(_settings, visionText);

            SetStatus($"Wysyłanie opisu Vision do Text LLM: {textAgent.Name}...");
            var finalText = await textAgent.CompleteAsync(finalPrompt, _settings, ct).ConfigureAwait(true);
            rtbFinalResult.Text = finalText;

            SetStatus("Zakończono.");
            tabControl.SelectedIndex = 0;
        }
        catch (OperationCanceledException)
        {
            SetStatus("Przerwano.");
        }
        catch (Exception ex)
        {
            var error = "Błąd: " + ex.Message;
            SetStatus(error);
            if (string.IsNullOrWhiteSpace(rtbFinalResult.Text))
            {
                rtbFinalResult.Text = error;
            }
        }
        finally
        {
            _isRunning = false;
            SetRunUiState(isRunning: false);
        }
    }

    private static string BuildFinalPrompt(AppSettings settings, string visionText)
    {
        var parts = new List<string>();

        parts.Add("PROMPT UŻYTKOWNIKA DLA TEXT LLM:");
        parts.Add(settings.TextAgentPrompt.Trim());

        if (settings.IncludeVisionPromptInFinalContext)
        {
            parts.Add("PROMPT UŻYTY DLA VISION:");
            parts.Add(settings.VisionPrompt.Trim());
        }

        parts.Add("OPIS OBRAZU ZWRÓCONY PRZEZ VISION:");
        parts.Add(visionText.Trim());

        parts.Add("ODPOWIEDŹ KOŃCOWA:");

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    private void SetRunUiState(bool isRunning)
    {
        btnExecuteNow.Enabled = !isRunning;
        btnActivate.Enabled = !isRunning;
        btnSaveSettings.Enabled = !isRunning;
        btnCancel.Enabled = isRunning;
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatus(text)));
            return;
        }

        txtStatus.Text = text;
    }

    private static void SelectComboValue(ComboBox comboBox, string value)
    {
        var index = comboBox.Items.IndexOf(value);
        comboBox.SelectedIndex = index >= 0 ? index : 0;
    }
}

internal static class TableLayoutPanelExtensions
{
    public static int RowCountUsed(this TableLayoutPanel panel)
    {
        var used = 0;
        foreach (Control control in panel.Controls)
        {
            used = Math.Max(used, panel.GetRow(control) + 1);
        }

        while (panel.RowStyles.Count <= used)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        }

        if (panel.RowCount <= used)
        {
            panel.RowCount = used + 1;
        }

        return used;
    }
}
