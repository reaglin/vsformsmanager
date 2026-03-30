using VSFormsManager.Models;
using VSFormsManager.Services.Conversion;

namespace VSFormsManager
{
    /// <summary>
    /// Two-step modal dialog for saving a form in a different format.
    ///
    /// Step 1 — Format and Location
    ///   Choose the output format (WinForms Designer, WinForms Code-Only, XAML),
    ///   the output folder, the output file base name, and the target namespace.
    ///
    /// Step 2 — Dependency Review
    ///   Lists all namespaces used by the form. The user can check any that
    ///   should be commented out in the output (e.g. namespaces not available
    ///   in the target project).  Clicking "Convert and Save" triggers the
    ///   AI conversion via <see cref="AiFormConverter"/>.
    ///
    /// Open via:
    ///   using var dlg = new frmSaveAs(AppSession.CurrentForm!, AppSession.Settings);
    ///   dlg.ShowDialog(this);
    /// </summary>
    public partial class frmSaveAs : Form
    {
        // ── State ─────────────────────────────────────────────────────────────

        private readonly FormRecord _source;
        private readonly AiFormConverter _converter;

        // ── Step-1 controls ───────────────────────────────────────────────────
        private Panel        pnlStep1   = null!;
        private RadioButton  rbDesigner = null!;
        private RadioButton  rbCodeOnly = null!;
        private RadioButton  rbXaml     = null!;
        private TextBox      txtFolder  = null!;
        private TextBox      txtName    = null!;
        private TextBox      txtNs      = null!;
        private Label        lblFiles   = null!;

        // ── Step-2 controls ───────────────────────────────────────────────────
        private Panel        pnlStep2    = null!;
        private ListView     lvDeps      = null!;
        private Panel        pnlProgress = null!;
        private Label        lblProgress = null!;
        private ProgressBar  pbProgress  = null!;
        private Button       btnBack     = null!;
        private Button       btnConvert  = null!;

        // ── Constructor ───────────────────────────────────────────────────────

        public frmSaveAs(FormRecord source, AppSettings settings)
        {
            _source    = source;
            _converter = new AiFormConverter(settings);

            BuildUi();
            PopulateStep1Defaults();
        }

        // ═════════════════════════════════════════════════════════════════════
        // UI CONSTRUCTION
        // ═════════════════════════════════════════════════════════════════════

        private void BuildUi()
        {
            AutoScaleMode   = AutoScaleMode.Font;
            Text            = "Save Form As";
            Size            = new Size(640, 600);
            MinimumSize     = new Size(580, 520);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9f);

            BuildStep1Panel();
            BuildStep2Panel();

            // Only step 1 visible initially
            pnlStep1.Visible = true;
            pnlStep2.Visible = false;

            Controls.Add(pnlStep1);
            Controls.Add(pnlStep2);
        }

        // ── Step 1 ────────────────────────────────────────────────────────────

        private void BuildStep1Panel()
        {
            pnlStep1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            // Title
            var lblTitle = MakeTitle("Step 1 of 2 — Choose Format & Location");

            // Source info
            var lblSource = new Label
            {
                Text      = $"Converting:  {_source.FormName}  from project  {_source.ProjectName}",
                AutoSize  = true,
                Location  = new Point(0, 38),
                ForeColor = Color.DimGray,
                Font      = new Font("Segoe UI", 9f, FontStyle.Italic)
            };

            // ── Format group ──────────────────────────────────────────────────
            var grpFormat = new GroupBox
            {
                Text     = "Output Format",
                Location = new Point(0, 68),
                Size     = new Size(580, 100),
                Anchor   = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            rbDesigner = new RadioButton { Text = "WinForms (Designer + Code)   — creates .cs and .Designer.cs", Location = new Point(14, 22), AutoSize = true, Checked = true };
            rbCodeOnly = new RadioButton { Text = "WinForms (Code Only)           — creates single .cs file",     Location = new Point(14, 48), AutoSize = true };
            rbXaml     = new RadioButton { Text = "XAML (WPF)                        — creates .xaml and .xaml.cs", Location = new Point(14, 74), AutoSize = true };

            rbDesigner.CheckedChanged += (_, _) => UpdateFileHint();
            rbCodeOnly.CheckedChanged += (_, _) => UpdateFileHint();
            rbXaml.CheckedChanged     += (_, _) => UpdateFileHint();

            grpFormat.Controls.AddRange(new Control[] { rbDesigner, rbCodeOnly, rbXaml });

            // ── Details group ─────────────────────────────────────────────────
            var grpDet = new GroupBox
            {
                Text     = "Output Details",
                Location = new Point(0, 180),
                Size     = new Size(580, 170),
                Anchor   = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            int lw = 90, ly = 24, rh = 32;

            // Folder row
            var lblFolder = MakeLabel("Folder:",    14, ly,        lw);
            txtFolder     = MakeBox(110, ly, 330);
            txtFolder.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            var btnBrowse = new Button { Text = "Browse…", Location = new Point(447, ly - 1), Size = new Size(80, 25) };
            btnBrowse.Click += BtnBrowseFolder_Click;

            // File name row
            var lblName = MakeLabel("File Name:", 14, ly + rh,     lw);
            txtName     = MakeBox(110, ly + rh, 250);

            // Namespace row
            var lblNs = MakeLabel("Namespace:",  14, ly + rh * 2, lw);
            txtNs     = MakeBox(110, ly + rh * 2, 360);
            txtNs.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            // File hint row
            lblFiles = new Label
            {
                Location  = new Point(14, ly + rh * 3 + 4),
                AutoSize  = false,
                Size      = new Size(540, 20),
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                ForeColor = Color.DimGray,
                Font      = new Font("Segoe UI", 8.5f)
            };

            grpDet.Controls.AddRange(new Control[]
                { lblFolder, txtFolder, btnBrowse, lblName, txtName, lblNs, txtNs, lblFiles });

            // Resize grpFormat/grpDet when panel resizes
            pnlStep1.Resize += (_, _) =>
            {
                int w = pnlStep1.ClientSize.Width - 40;
                grpFormat.Width = w;
                grpDet.Width    = w;
                btnBrowse.Left  = txtFolder.Right + 6;
                txtNs.Width     = w - 100;
                lblFiles.Width  = w - 14;
            };

            // Next button
            var btnNext = new Button
            {
                Text    = "Next  →",
                Size    = new Size(110, 32),
                Anchor  = AnchorStyles.Right | AnchorStyles.Bottom,
                Font    = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnNext.Location = new Point(
                pnlStep1.ClientSize.Width - btnNext.Width - 20,
                pnlStep1.ClientSize.Height - btnNext.Height - 20);
            btnNext.Click += BtnNext_Click;
            pnlStep1.Resize += (_, _) =>
                btnNext.Location = new Point(
                    pnlStep1.ClientSize.Width - btnNext.Width - 20,
                    pnlStep1.ClientSize.Height - btnNext.Height - 20);

            pnlStep1.Controls.AddRange(new Control[]
                { lblTitle, lblSource, grpFormat, grpDet, btnNext });
        }

        // ── Step 2 ────────────────────────────────────────────────────────────

        private void BuildStep2Panel()
        {
            pnlStep2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var lblTitle = MakeTitle("Step 2 of 2 — Dependency Review");

            var lblInfo = new Label
            {
                Text     = "The form uses the namespaces listed below.\r\n" +
                            "Check any that should be commented out in the output " +
                            "(e.g. namespaces that don't exist in the target project).\r\n" +
                            "Project-specific namespaces are highlighted in green.",
                AutoSize = false,
                Location = new Point(0, 40),
                Size     = new Size(580, 58),
                Anchor   = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            // Quick-select buttons
            var btnCheckProject = new Button
            {
                Text     = "Check Project-Only",
                Size     = new Size(148, 28),
                Location = new Point(0, 106)
            };
            var btnUncheckAll = new Button
            {
                Text     = "Uncheck All",
                Size     = new Size(100, 28),
                Location = new Point(154, 106)
            };
            btnCheckProject.Click += (_, _) => SetChecks(projectOnly: true);
            btnUncheckAll.Click   += (_, _) => SetChecks(projectOnly: false, uncheckAll: true);

            // Dependencies list
            lvDeps = new ListView
            {
                Location      = new Point(0, 144),
                CheckBoxes    = true,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = true,
                HideSelection = false,
                Font          = new Font("Segoe UI", 9f),
                Anchor        = AnchorStyles.Left | AnchorStyles.Right |
                                AnchorStyles.Top  | AnchorStyles.Bottom
            };
            lvDeps.Columns.Add("Namespace", 380);
            lvDeps.Columns.Add("Category",  120);

            // Progress panel (hidden until conversion starts)
            pnlProgress = new Panel
            {
                Location  = new Point(0, 0),
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Height    = 52,
                Visible   = false
            };
            lblProgress = new Label
            {
                Location = new Point(0, 4),
                AutoSize = false,
                Height   = 20,
                Dock     = DockStyle.Top,
                Font     = new Font("Segoe UI", 9f),
                Text     = "Converting…"
            };
            pbProgress = new ProgressBar
            {
                Dock  = DockStyle.Bottom,
                Style = ProgressBarStyle.Marquee,
                Height = 18
            };
            pnlProgress.Controls.AddRange(new Control[] { lblProgress, pbProgress });

            // Bottom buttons
            btnBack = new Button
            {
                Text     = "←  Back",
                Size     = new Size(100, 32),
                Location = new Point(0, 0),
                Anchor   = AnchorStyles.Left | AnchorStyles.Bottom
            };
            btnBack.Click += (_, _) => GoToStep(1);

            btnConvert = new Button
            {
                Text     = "Convert and Save",
                Size     = new Size(148, 32),
                Anchor   = AnchorStyles.Right | AnchorStyles.Bottom,
                Font     = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnConvert.Click += BtnConvert_Click;

            pnlStep2.Resize += (_, _) => LayoutStep2(pnlStep2.ClientSize);
            Load += (_, _) => LayoutStep2(pnlStep2.ClientSize);

            pnlStep2.Controls.AddRange(new Control[]
                { lblTitle, lblInfo, btnCheckProject, btnUncheckAll,
                  lvDeps, pnlProgress, btnBack, btnConvert });
        }

        private void LayoutStep2(Size sz)
        {
            int w      = sz.Width  - 40;
            int h      = sz.Height - 40;
            int bottom = h;

            btnBack.Location    = new Point(0, bottom - btnBack.Height);
            btnConvert.Location = new Point(w - btnConvert.Width, bottom - btnConvert.Height);
            pnlProgress.SetBounds(0, bottom - btnBack.Height - 58, w, 52);
            lvDeps.SetBounds(0, 144, w, pnlProgress.Top - 144 - 8);
        }

        // ═════════════════════════════════════════════════════════════════════
        // DATA POPULATION
        // ═════════════════════════════════════════════════════════════════════

        private void PopulateStep1Defaults()
        {
            txtName.Text = _source.FormName;
            txtNs.Text   = _source.Namespace;

            // Default folder = source file's directory
            txtFolder.Text = _source.FormDirectory;

            // Pre-select the same format as source
            rbDesigner.Checked = _source.FormType is FormType.WinFormsDesigner;
            rbCodeOnly.Checked = _source.FormType is FormType.WinFormsCodeOnly;
            rbXaml.Checked     = _source.FormType is FormType.Xaml;

            // Fallback: default to Designer
            if (!rbDesigner.Checked && !rbCodeOnly.Checked && !rbXaml.Checked)
                rbDesigner.Checked = true;

            UpdateFileHint();
        }

        private void PopulateStep2()
        {
            lvDeps.Items.Clear();
            foreach (var dep in _source.CodeDependencies)
            {
                var item = new ListViewItem(dep.Namespace);
                item.SubItems.Add(dep.Category);
                item.ForeColor = dep.IsFramework ? Color.DimGray : Color.DarkGreen;
                item.Checked   = false;   // user must opt-in
                lvDeps.Items.Add(item);
            }
        }

        private void UpdateFileHint()
        {
            var fmt   = GetSelectedFormat();
            var name  = string.IsNullOrWhiteSpace(txtName.Text) ? "FormName" : txtName.Text.Trim();
            var files = ConversionPromptBuilder.ExpectedOutputFiles(name, fmt);
            lblFiles.Text = "Will create: " + string.Join(",  ", files);
        }

        // ═════════════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═════════════════════════════════════════════════════════════════════

        private void BtnBrowseFolder_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description          = "Select the output folder for the converted form files",
                SelectedPath         = txtFolder.Text,
                UseDescriptionForTitle = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                txtFolder.Text = dlg.SelectedPath;
        }

        private void BtnNext_Click(object? sender, EventArgs e)
        {
            if (!ValidateStep1()) return;
            PopulateStep2();
            GoToStep(2);
        }

        private async void BtnConvert_Click(object? sender, EventArgs e)
        {
            var namespacesToComment = lvDeps.CheckedItems
                .Cast<ListViewItem>()
                .Select(i => i.Text)
                .ToList();

            SetConvertingState(true);

            var result = await _converter.ConvertAsync(
                source:                _source,
                targetType:            GetSelectedFormat(),
                outputDirectory:       txtFolder.Text.Trim(),
                outputBaseName:        txtName.Text.Trim(),
                outputNamespace:       txtNs.Text.Trim(),
                namespacesToComment:   namespacesToComment,
                progress:              new Progress<string>(msg => lblProgress.Text = msg),
                cancellationToken:     CancellationToken.None);

            SetConvertingState(false);

            if (result.Success)
            {
                var fileList = string.Join("\r\n  • ", result.WrittenPaths.Select(Path.GetFileName));
                var commentNote = result.CommentedOutNamespaces.Count > 0
                    ? $"\r\n\r\nCommented-out namespaces:\r\n  • " +
                      string.Join("\r\n  • ", result.CommentedOutNamespaces)
                    : string.Empty;

                var answer = MessageBox.Show(
                    $"Conversion successful!\r\n\r\nFiles created:\r\n  • {fileList}" +
                    commentNote + "\r\n\r\nOpen output folder?",
                    "Conversion Complete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (answer == DialogResult.Yes)
                    System.Diagnostics.Process.Start(
                        "explorer.exe", txtFolder.Text.Trim());

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(
                    result.ErrorMessage,
                    "Conversion Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // ── Dependency check helpers ──────────────────────────────────────────

        private void SetChecks(bool projectOnly = false, bool uncheckAll = false)
        {
            foreach (ListViewItem item in lvDeps.Items)
            {
                if (uncheckAll)
                    item.Checked = false;
                else if (projectOnly)
                    item.Checked = item.SubItems[1].Text == "Project";
            }
        }

        // ── Step navigation ───────────────────────────────────────────────────

        private void GoToStep(int step)
        {
            pnlStep1.Visible = step == 1;
            pnlStep2.Visible = step == 2;
        }

        private void SetConvertingState(bool converting)
        {
            pnlProgress.Visible = converting;
            btnConvert.Enabled  = !converting;
            btnBack.Enabled     = !converting;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private bool ValidateStep1()
        {
            if (string.IsNullOrWhiteSpace(txtFolder.Text))
            {
                MessageBox.Show("Please choose an output folder.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFolder.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter a file name.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtNs.Text))
            {
                MessageBox.Show("Please enter a namespace.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNs.Focus();
                return false;
            }
            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private FormType GetSelectedFormat()
        {
            if (rbCodeOnly.Checked) return FormType.WinFormsCodeOnly;
            if (rbXaml.Checked)     return FormType.Xaml;
            return FormType.WinFormsDesigner;
        }

        private static Label MakeTitle(string text) =>
            new Label
            {
                Text     = text,
                AutoSize = true,
                Location = new Point(0, 6),
                Font     = new Font("Segoe UI", 11f, FontStyle.Bold)
            };

        private static Label MakeLabel(string text, int x, int y, int w) =>
            new Label
            {
                Text      = text,
                Location  = new Point(x, y + 3),
                Size      = new Size(w, 22),
                TextAlign = ContentAlignment.MiddleRight
            };

        private static TextBox MakeBox(int x, int y, int w) =>
            new TextBox
            {
                Location = new Point(x, y),
                Size     = new Size(w, 23),
                Font     = new Font("Segoe UI", 9f)
            };
    }
}
