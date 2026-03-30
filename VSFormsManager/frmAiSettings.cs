using VSFormsManager.Models;
using VSFormsManager.Services;
using VSFormsManager.Services.Providers;

namespace VSFormsManager
{
    /// <summary>
    /// Modal settings form for AI provider management.
    ///
    /// Tab 1 – API Keys
    ///   One GroupBox per provider. Each box lets the user enter/reveal their
    ///   API key, choose the model string, and run a live connectivity test.
    ///
    /// Tab 2 – Task Assignment
    ///   Assigns each <see cref="AiTask"/> to a specific provider.
    ///
    /// Open via <see cref="AppSession.OpenAiSettings"/> or directly:
    ///   using var dlg = new frmAiSettings(AppSession.Settings);
    ///   if (dlg.ShowDialog(this) == DialogResult.OK) AppSession.ReloadSettings();
    /// </summary>
    public partial class frmAiSettings : Form
    {
        // ── State ─────────────────────────────────────────────────────────────

        private readonly AppSettings _settings;

        // Per-provider control references
        private readonly Dictionary<AiProviderType, TextBox> _keyBoxes   = new();
        private readonly Dictionary<AiProviderType, TextBox> _modelBoxes = new();
        private readonly Dictionary<AiProviderType, Button>  _showBtns   = new();
        private readonly Dictionary<AiProviderType, Button>  _testBtns   = new();
        private readonly Dictionary<AiProviderType, Label>   _statusLbls = new();

        // Per-task control references
        private readonly Dictionary<AiTask, ComboBox> _taskCombos = new();

        // ── Constructor ───────────────────────────────────────────────────────

        public frmAiSettings(AppSettings settings)
        {
            _settings = settings;

            InitializeComponent();   // minimal stub in Designer.cs
            BuildUi();
            PopulateFromSettings();
        }

        // ═════════════════════════════════════════════════════════════════════
        // UI CONSTRUCTION
        // ═════════════════════════════════════════════════════════════════════

        private void BuildUi()
        {
            // ── Form properties ───────────────────────────────────────────────
            Text            = "AI Provider Settings";
            Size            = new Size(700, 620);
            MinimumSize     = new Size(680, 580);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9f);

            // ── Bottom separator + button panel ───────────────────────────────
            var sepLine = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = SystemColors.ControlDark
            };

            var pnlButtons = new Panel
            {
                Dock   = DockStyle.Bottom,
                Height = 52
            };

            var btnCancel = new Button
            {
                Text         = "Cancel",
                Size         = new Size(90, 32),
                DialogResult = DialogResult.Cancel
            };

            var btnSave = new Button
            {
                Text = "Save",
                Size = new Size(90, 32)
            };
            btnSave.Click += BtnSave_Click;

            // Position buttons on the right; reposition on resize
            void PositionButtons()
            {
                btnCancel.Location = new Point(pnlButtons.ClientSize.Width - 102, 10);
                btnSave.Location   = new Point(pnlButtons.ClientSize.Width - 200, 10);
            }
            pnlButtons.Resize += (_, _) => PositionButtons();
            pnlButtons.Controls.AddRange(new Control[] { btnSave, btnCancel });

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            // ── Tab control ───────────────────────────────────────────────────
            var tabControl = new TabControl { Dock = DockStyle.Fill };

            var tabKeys  = new TabPage("API Keys");
            var tabTasks = new TabPage("Task Assignment");

            tabControl.TabPages.Add(tabKeys);
            tabControl.TabPages.Add(tabTasks);

            BuildKeysTab(tabKeys);
            BuildTasksTab(tabTasks);

            // Add controls in reverse dock order
            Controls.Add(tabControl);
            Controls.Add(sepLine);
            Controls.Add(pnlButtons);

            // Force initial button positioning after layout
            Load += (_, _) => PositionButtons();
        }

        // ── API Keys tab ──────────────────────────────────────────────────────

        private void BuildKeysTab(TabPage tab)
        {
            // Scrollable container for all four provider GroupBoxes
            var scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true
            };

            int y = 10;
            foreach (var provider in AiProviderRouter.AllProviders)
            {
                var grp = BuildProviderGroup(provider, y);
                scroll.Controls.Add(grp);
                y += grp.Height + 8;
            }

            // Keep GroupBoxes filling the panel width on resize
            scroll.Resize += (_, _) => ResizeProviderGroups(scroll);

            tab.Controls.Add(scroll);
        }

        private GroupBox BuildProviderGroup(AiProviderType provider, int topY)
        {
            // ── GroupBox shell ────────────────────────────────────────────────
            var grp = new GroupBox
            {
                Text     = AiProviderRouter.DisplayName(provider),
                Location = new Point(10, topY),
                Width    = 640,
                Height   = 118,
                Font     = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            // ── Row 1: API Key ─────────────────────────────────────────────────
            //   [API Key:] [_____txtKey_____] [Show] [Test]

            var lblKey = MakeLabel("API Key:", 12, 26, 62);
            lblKey.Font = new Font("Segoe UI", 9f);   // unbold inside GroupBox

            var txtKey = new TextBox
            {
                Location     = new Point(80, 24),
                Height       = 23,
                PasswordChar = '●',
                Anchor       = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Font         = new Font("Segoe UI", 9f)
            };
            // Width will be set by ResizeProviderGroups on first layout

            var btnShow = new Button
            {
                Text   = "Show",
                Size   = new Size(62, 25),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Font   = new Font("Segoe UI", 8.5f)
            };
            btnShow.Click += (_, _) => ToggleKeyVisibility(provider);

            var btnTest = new Button
            {
                Text   = "Test",
                Size   = new Size(62, 25),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Font   = new Font("Segoe UI", 8.5f)
            };
            btnTest.Click += async (_, _) => await TestProviderAsync(provider);

            // ── Row 2: Model ───────────────────────────────────────────────────
            //   [Model:] [_txtModel_]

            var lblModel = MakeLabel("Model:", 12, 58, 62);
            lblModel.Font = new Font("Segoe UI", 9f);

            var txtModel = new TextBox
            {
                Location = new Point(80, 56),
                Size     = new Size(280, 23),
                Font     = new Font("Segoe UI", 9f)
            };

            // ── Row 3: Status ──────────────────────────────────────────────────
            var lblStatus = new Label
            {
                Text      = string.Empty,
                Location  = new Point(80, 88),
                Height    = 20,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.DimGray,
                AutoSize  = false
            };

            grp.Controls.AddRange(new Control[]
                { lblKey, txtKey, btnShow, btnTest, lblModel, txtModel, lblStatus });

            // Store references for later use
            _keyBoxes[provider]   = txtKey;
            _modelBoxes[provider] = txtModel;
            _showBtns[provider]   = btnShow;
            _testBtns[provider]   = btnTest;
            _statusLbls[provider] = lblStatus;

            // Layout right-anchored controls once the GroupBox has a real width
            grp.Layout += (_, _) => LayoutProviderGroupRow(grp, txtKey, btnShow, btnTest, lblStatus);

            return grp;
        }

        /// <summary>
        /// Recomputes the sizes/positions of all right-anchored controls inside
        /// a provider GroupBox. Called on every Layout event so it handles both
        /// initial sizing and window resize.
        /// </summary>
        private static void LayoutProviderGroupRow(
            GroupBox grp, TextBox txtKey, Button btnShow, Button btnTest, Label lblStatus)
        {
            const int margin = 8;
            const int btnW   = 62;
            const int btnGap = 4;
            const int keyX   = 80;

            int right  = grp.ClientSize.Width - margin;
            int testX  = right - btnW;
            int showX  = testX - btnGap - btnW;
            int keyW   = showX - btnGap - keyX;

            if (keyW < 40) return;  // form too narrow to lay out sensibly

            txtKey.SetBounds(keyX, txtKey.Top, keyW, txtKey.Height);
            btnShow.Location = new Point(showX, 24);
            btnTest.Location = new Point(testX, 24);
            lblStatus.SetBounds(keyX, lblStatus.Top, right - keyX, lblStatus.Height);
        }

        /// <summary>Stretches all GroupBoxes to fill the scrollable panel.</summary>
        private static void ResizeProviderGroups(Panel scroll)
        {
            int w = scroll.ClientSize.Width - 20;
            foreach (Control c in scroll.Controls)
                c.Width = w;
        }

        // ── Task Assignment tab ───────────────────────────────────────────────

        private void BuildTasksTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Fill };

            var lblInfo = new Label
            {
                Text      = "Assign an AI provider to each task.\r\n" +
                             "Providers without a saved API key can still be selected here — " +
                             "they will prompt for a key when first used.",
                Location  = new Point(20, 18),
                Size      = new Size(600, 44),
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            pnl.Controls.Add(lblInfo);

            int y = 76;
            foreach (AiTask task in Enum.GetValues<AiTask>())
            {
                var lbl = MakeLabel(TaskDisplayName(task) + ":", 20, y + 4, 180);
                lbl.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

                var combo = new ComboBox
                {
                    Location      = new Point(210, y),
                    Size          = new Size(260, 26),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font          = new Font("Segoe UI", 9f)
                };

                foreach (var p in AiProviderRouter.AllProviders)
                    combo.Items.Add(new ProviderItem(p));

                _taskCombos[task] = combo;
                pnl.Controls.Add(lbl);
                pnl.Controls.Add(combo);
                y += 44;
            }

            tab.Controls.Add(pnl);
        }

        // ═════════════════════════════════════════════════════════════════════
        // DATA BINDING  — settings ↔ controls
        // ═════════════════════════════════════════════════════════════════════

        private void PopulateFromSettings()
        {
            // API Keys
            _keyBoxes[AiProviderType.Claude].Text  = _settings.ClaudeApiKey;
            _keyBoxes[AiProviderType.Gemini].Text  = _settings.GeminiApiKey;
            _keyBoxes[AiProviderType.OpenAi].Text  = _settings.OpenAiApiKey;
            _keyBoxes[AiProviderType.Mistral].Text = _settings.MistralApiKey;

            // Models
            _modelBoxes[AiProviderType.Claude].Text  = _settings.ClaudeModel;
            _modelBoxes[AiProviderType.Gemini].Text  = _settings.GeminiModel;
            _modelBoxes[AiProviderType.OpenAi].Text  = _settings.OpenAiModel;
            _modelBoxes[AiProviderType.Mistral].Text = _settings.MistralModel;

            // Task assignments — select the matching ComboBox item
            foreach (AiTask task in Enum.GetValues<AiTask>())
            {
                var combo    = _taskCombos[task];
                var assigned = _settings.TaskAssignment.GetProvider(task);

                foreach (ProviderItem item in combo.Items)
                {
                    if (item.Provider != assigned) continue;
                    combo.SelectedItem = item;
                    break;
                }

                if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
                    combo.SelectedIndex = 0;
            }
        }

        private void FlushToSettings()
        {
            _settings.ClaudeApiKey  = _keyBoxes[AiProviderType.Claude].Text.Trim();
            _settings.GeminiApiKey  = _keyBoxes[AiProviderType.Gemini].Text.Trim();
            _settings.OpenAiApiKey  = _keyBoxes[AiProviderType.OpenAi].Text.Trim();
            _settings.MistralApiKey = _keyBoxes[AiProviderType.Mistral].Text.Trim();

            _settings.ClaudeModel  = _modelBoxes[AiProviderType.Claude].Text.Trim();
            _settings.GeminiModel  = _modelBoxes[AiProviderType.Gemini].Text.Trim();
            _settings.OpenAiModel  = _modelBoxes[AiProviderType.OpenAi].Text.Trim();
            _settings.MistralModel = _modelBoxes[AiProviderType.Mistral].Text.Trim();

            foreach (AiTask task in Enum.GetValues<AiTask>())
            {
                if (_taskCombos[task].SelectedItem is ProviderItem pi)
                    _settings.TaskAssignment.SetProvider(task, pi.Provider);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═════════════════════════════════════════════════════════════════════

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            FlushToSettings();
            try
            {
                SettingsManager.Save(_settings);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Settings could not be saved:\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ── Show / Hide key ───────────────────────────────────────────────────

        private void ToggleKeyVisibility(AiProviderType provider)
        {
            var txt = _keyBoxes[provider];
            var btn = _showBtns[provider];

            bool visible = txt.PasswordChar == '\0';
            txt.PasswordChar = visible ? '●' : '\0';
            btn.Text         = visible ? "Show" : "Hide";
        }

        // ── Test connectivity ─────────────────────────────────────────────────

        private async Task TestProviderAsync(AiProviderType provider)
        {
            var lbl   = _statusLbls[provider];
            var btn   = _testBtns[provider];
            var key   = _keyBoxes[provider].Text.Trim();
            var model = _modelBoxes[provider].Text.Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                SetStatus(lbl, "⚠  No API key entered.", Color.DarkOrange);
                return;
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                SetStatus(lbl, "⚠  No model name entered.", Color.DarkOrange);
                return;
            }

            btn.Enabled = false;
            SetStatus(lbl, "Testing connection…", Color.DimGray);

            try
            {
                IAiProvider p = provider switch
                {
                    AiProviderType.Claude  => new ClaudeProvider(key, model),
                    AiProviderType.Gemini  => new GeminiProvider(key, model),
                    AiProviderType.OpenAi  => new OpenAiProvider(key, model),
                    AiProviderType.Mistral => new MistralProvider(key, model),
                    _ => throw new InvalidOperationException($"Unknown provider: {provider}")
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var reply = await p.GenerateAsync(
                    "You are a connectivity-test assistant.",
                    "Reply with exactly one word: OK",
                    cts.Token);

                SetStatus(lbl, $"✓  Connected — response: \"{reply.Trim()}\"", Color.DarkGreen);
            }
            catch (OperationCanceledException)
            {
                SetStatus(lbl, "✗  Request timed out after 30 seconds.", Color.Firebrick);
            }
            catch (Exception ex)
            {
                // Show only the first line of the error to keep the label readable
                var msg = ex.Message.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
                SetStatus(lbl, $"✗  {msg}", Color.Firebrick);
            }
            finally
            {
                btn.Enabled = true;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private static void SetStatus(Label lbl, string text, Color color)
        {
            lbl.ForeColor = color;
            lbl.Text      = text;
        }

        private static Label MakeLabel(string text, int x, int y, int width) =>
            new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                Size      = new Size(width, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

        private static string TaskDisplayName(AiTask task) => task switch
        {
            AiTask.FormAnalysis   => "Form Analysis",
            AiTask.CodeConversion => "Code Conversion",
            _                     => task.ToString()
        };

        // ── ComboBox item that carries both display text and enum value ────────

        private sealed class ProviderItem
        {
            public AiProviderType Provider { get; }

            public ProviderItem(AiProviderType provider) => Provider = provider;

            public override string ToString() => AiProviderRouter.DisplayName(Provider);
        }
    }
}
