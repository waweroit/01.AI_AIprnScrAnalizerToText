namespace AIprnScrAnalizerToText;

public sealed partial class MainForm : Form
{
    private readonly AppSettings _settings = new();
    private readonly ActiveWindowCaptureService _captureService = new();
    private readonly IAiAgent[] _agents;
    private readonly HotkeyService _hotkeyService = new();
    private readonly ComboBox cmbAgent = new(); private readonly TextBox txtOllamaUrl = new(); private readonly TextBox txtOllamaModel = new(); private readonly TextBox txtOpenAiKey = new(); private readonly TextBox txtOpenAiBaseUrl = new(); private readonly RichTextBox rtbResult = new(); private readonly Button btnRun = new();
    public MainForm() { Text = "AIprnScrAnalizerToText"; Width = 980; Height = 720; StartPosition = FormStartPosition.CenterScreen; _agents = new IAiAgent[] { new OpenAICompatibleAgent(), new OllamaVisionAgent() }; InitializeUi(); _hotkeyService.HotkeyPressed += async (_, _) => await ExecuteAsync(); _hotkeyService.Register(); }
    protected override void OnFormClosed(FormClosedEventArgs e) { _hotkeyService.Dispose(); base.OnFormClosed(e); }
    private void InitializeUi()
    {
        var lbl1 = new Label { Text = "Agent AI:", Left = 20, Top = 20, Width = 180 }; cmbAgent.SetBounds(220, 18, 300, 28); cmbAgent.DropDownStyle = ComboBoxStyle.DropDownList; cmbAgent.Items.AddRange(_agents.Select(a => a.Name).Cast<object>().ToArray()); cmbAgent.SelectedIndex = 0;
        var lbl2 = new Label { Text = "OllamaVision URL:", Left = 20, Top = 60, Width = 180 }; txtOllamaUrl.SetBounds(220, 58, 520, 28); txtOllamaUrl.Text = _settings.OllamaVisionUrl;
        var lbl3 = new Label { Text = "OllamaVision Model:", Left = 20, Top = 100, Width = 180 }; txtOllamaModel.SetBounds(220, 98, 300, 28); txtOllamaModel.Text = _settings.OllamaVisionModel;
        var lbl4 = new Label { Text = "OpenAI ApiKey:", Left = 20, Top = 140, Width = 180 }; txtOpenAiKey.SetBounds(220, 138, 520, 28); txtOpenAiKey.UseSystemPasswordChar = true;
        var lbl5 = new Label { Text = "OpenAI Base URL:", Left = 20, Top = 180, Width = 180 }; txtOpenAiBaseUrl.SetBounds(220, 178, 520, 28); txtOpenAiBaseUrl.Text = _settings.OpenAIBaseUrl;
        btnRun.Text = "Start / Aktywuj"; btnRun.SetBounds(220, 220, 180, 36); btnRun.Click += async (_, _) => await ExecuteAsync();
        rtbResult.SetBounds(20, 280, 920, 380); rtbResult.ReadOnly = true; rtbResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Controls.AddRange(new Control[] { lbl1, cmbAgent, lbl2, txtOllamaUrl, lbl3, txtOllamaModel, lbl4, txtOpenAiKey, lbl5, txtOpenAiBaseUrl, btnRun, rtbResult });
        txtOpenAiKey.Text = _settings.OpenAIApiKey;
    }
    private async Task ExecuteAsync()
    {
        try { rtbResult.Clear(); _settings.SelectedAgent = cmbAgent.Text; _settings.OllamaVisionUrl = txtOllamaUrl.Text.Trim(); _settings.OllamaVisionModel = txtOllamaModel.Text.Trim(); _settings.OpenAIApiKey = txtOpenAiKey.Text.Trim(); _settings.OpenAIBaseUrl = txtOpenAiBaseUrl.Text.Trim(); var image = _captureService.CaptureActiveWindow(); var agent = _agents.First(a => a.Name == _settings.SelectedAgent); var result = await agent.AnalyzeImageAsync(image, "Opisz, co znajduje sie na tym zrzucie ekranu.", _settings, CancellationToken.None); rtbResult.Text = result; }
        catch (Exception ex) { rtbResult.Text = "Blad: " + ex.Message; }
    }
}
