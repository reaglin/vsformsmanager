using VSFormsManager.Models;
using VSFormsManager.Services;
using VSFormsManager.Services.Scaffolding;

namespace VSFormsManager
{
    /// <summary>
    /// Four-step wizard that scaffolds a new Visual Studio solution from
    /// a subset of forms in the VSFormsManager library.
    ///
    /// Step 1 — Select Forms
    ///   Checkbox tree of all known forms, grouped by project.
    ///
    /// Step 2 — Configure Project
    ///   Solution name, output folder, root namespace, startup form,
    ///   namespace-rewrite toggle.  Source project properties auto-populated.
    ///
    /// Step 3 — Review Files and Packages
    ///   Left: checkbox tree of all files that will be written (uncheck to exclude).
    ///   Right: checkbox list of NuGet packages to include.
    ///
    /// Step 4 — Generate
    ///   Progress log + completion summary with "Open in Explorer" shortcut.
    /// </summary>
    public partial class frmBatchProject : Form
    {
        // ── State ─────────────────────────────────────────────────────────────
        private readonly FormsRepository         _repo;
        private readonly ProjectScaffoldConfig   _config = new();
        private CsprojReader.CsprojInfo?         _sourceInfo;
        private List<ScaffoldFile>               _scaffoldFiles = new();

        // ── Step panels ───────────────────────────────────────────────────────
        private Panel pnlStep1 = null!;
        private Panel pnlStep2 = null!;
        private Panel pnlStep3 = null!;
        private Panel pnlStep4 = null!;

        // ── Shared navigation bar ─────────────────────────────────────────────
        private Button btnBack    = null!;
        private Button btnNext    = null!;
        private Label  lblStep    = null!;
        private int    _currentStep = 1;

        // ── Step 1 controls ───────────────────────────────────────────────────
        private TreeView tvFormSelect = null!;
        private Label    lblSelCount  = null!;

        // ── Step 2 controls ───────────────────────────────────────────────────
        private TextBox txtSolName   = null!;
        private TextBox txtOutDir    = null!;
        private TextBox txtNamespace = null!;
        private ComboBox cmbStartup  = null!;
        private CheckBox chkRewrite  = null!;
        private Label    lblSrcInfo  = null!;

        // ── Step 3 controls ───────────────────────────────────────────────────
        private TreeView  tvReviewFiles   = null!;
        private ListView  lvPackages      = null!;
        private Label     lblFileCount    = null!;

        // ── Step 4 controls ───────────────────────────────────────────────────
        private RichTextBox rtbLog        = null!;
        private Button      btnOpenFolder = null!;
        private string      _finalSlnPath = string.Empty;

        // ── Constructor ───────────────────────────────────────────────────────
        public frmBatchProject(FormsRepository repo)
        {
            _repo = repo;
            AutoScaleMode = AutoScaleMode.Font;
            BuildUi();
            GoToStep(1);
        }

        // ═════════════════════════════════════════════════════════════════════
        // UI CONSTRUCTION
        // ═════════════════════════════════════════════════════════════════════

        private void BuildUi()
        {
            Text            = "New Project from Library";
            Size            = new Size(860, 660);
            MinimumSize     = new Size(720, 560);
            StartPosition   = FormStartPosition.CenterScreen;
            Font            = new Font("Segoe UI", 9f);

            // ── Header band ───────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 54,
                BackColor = Color.FromArgb(0, 100, 180)
            };
            var lblTitle = new Label
            {
                Text      = "New Project from Library",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(18, 14)
            };
            lblStep = new Label
            {
                ForeColor  = Color.FromArgb(180, 220, 255),
                Font       = new Font("Segoe UI", 9f),
                AutoSize   = true,
                Location   = new Point(18, 34)
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblStep });

            // ── Nav bar ───────────────────────────────────────────────────────
            var pnlNav = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 52,
                BackColor = SystemColors.ControlLight,
                Padding   = new Padding(12, 10, 12, 0)
            };
            var navSep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = SystemColors.ControlDark };

            btnBack = new Button { Text = "← Back",  Size = new Size(96, 30), Enabled = false };
            btnNext = new Button { Text = "Next →",  Size = new Size(96, 30),
                                   Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var btnCancel = new Button
                { Text = "Cancel", Size = new Size(80, 30), DialogResult = DialogResult.Cancel };

            btnBack.Click   += (_, _) => GoToStep(_currentStep - 1);
            btnNext.Click   += BtnNext_Click;
            CancelButton     = btnCancel;

            pnlNav.Resize += (_, _) =>
            {
                btnCancel.Location = new Point(pnlNav.ClientSize.Width - btnCancel.Width - 8, 10);
                btnNext.Location   = new Point(btnCancel.Left - btnNext.Width - 6, 10);
                btnBack.Location   = new Point(btnNext.Left - btnBack.Width - 6, 10);
            };
            pnlNav.Controls.AddRange(new Control[] { btnBack, btnNext, btnCancel });

            // ── Content area ──────────────────────────────────────────────────
            var pnlContent = new Panel { Dock = DockStyle.Fill };

            pnlStep1 = BuildStep1Panel();
            pnlStep2 = BuildStep2Panel();
            pnlStep3 = BuildStep3Panel();
            pnlStep4 = BuildStep4Panel();

            foreach (var p in new[] { pnlStep1, pnlStep2, pnlStep3, pnlStep4 })
            {
                p.Dock    = DockStyle.Fill;
                p.Visible = false;
                p.Padding = new Padding(20, 16, 20, 8);
                pnlContent.Controls.Add(p);
            }

            Controls.Add(pnlContent);
            Controls.Add(navSep);
            Controls.Add(pnlNav);
            Controls.Add(pnlHeader);
        }

        // ── Step 1 panel ──────────────────────────────────────────────────────

        private Panel BuildStep1Panel()
        {
            var pnl = new Panel();

            var lbl = MakeSectionLabel("Select the forms to include in the new project.");
            lbl.Location = new Point(0, 0);
            lbl.Size     = new Size(800, 22);

            lblSelCount = new Label
            {
                Text      = "0 forms selected",
                ForeColor = Color.FromArgb(0, 100, 180),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location  = new Point(0, 26),
                AutoSize  = true
            };

            tvFormSelect = new TreeView
            {
                CheckBoxes    = true,
                HideSelection = false,
                ShowLines     = true,
                Font          = new Font("Segoe UI", 9f),
                ItemHeight    = 22,
                Indent        = 18,
                Anchor        = AnchorStyles.Left | AnchorStyles.Right |
                                AnchorStyles.Top  | AnchorStyles.Bottom,
                Location      = new Point(0, 52)
            };
            tvFormSelect.AfterCheck += TvFormSelect_AfterCheck;

            pnl.Controls.AddRange(new Control[] { lbl, lblSelCount, tvFormSelect });
            pnl.Resize += (_, _) =>
            {
                tvFormSelect.Size = new Size(
                    pnl.ClientSize.Width  - 40,
                    pnl.ClientSize.Height - 60);
            };
            return pnl;
        }

        // ── Step 2 panel ──────────────────────────────────────────────────────

        private Panel BuildStep2Panel()
        {
            var pnl = new Panel();
            var lbl = MakeSectionLabel("Configure the new project.");
            lbl.Location = new Point(0, 0);

            int lw = 120, rh = 32, y = 30;

            // Source project info (read-only)
            lblSrcInfo = new Label
            {
                Location  = new Point(0, y),
                AutoSize  = false,
                Height    = 38,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                ForeColor = Color.DimGray,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            };
            y += 48;

            // Solution name
            AddRow(pnl, "Solution Name:", lw, y, out txtSolName);
            txtSolName.TextChanged += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(txtNamespace.Text) ||
                    txtNamespace.Text == PrevSolName())
                    txtNamespace.Text = SanitizeIdentifier(txtSolName.Text);
            };
            y += rh;

            // Output directory
            var lblOut = MakeLabel("Output Folder:", 0, y, lw);
            txtOutDir  = MakeTextBox(lw + 4, y, 100);
            txtOutDir.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            var btnBrowse = new Button
                { Text = "Browse…", Size = new Size(80, 23), Anchor = AnchorStyles.Right | AnchorStyles.Top };
            btnBrowse.Click += BtnBrowseOutDir_Click;
            pnl.Controls.AddRange(new Control[] { lblOut, txtOutDir, btnBrowse });
            pnl.Resize += (_, _) =>
            {
                int w = pnl.ClientSize.Width - 40;
                txtOutDir.Width = w - lw - 4 - btnBrowse.Width - 4;
                btnBrowse.Location = new Point(lw + 4 + txtOutDir.Width + 4, y);
                lblSrcInfo.Width   = w;
            };
            y += rh;

            // Root namespace
            AddRow(pnl, "Root Namespace:", lw, y, out txtNamespace);
            y += rh;

            // Startup form
            var lblStart = MakeLabel("Startup Form:", 0, y, lw);
            cmbStartup   = new ComboBox
            {
                Location      = new Point(lw + 4, y),
                Size          = new Size(260, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 9f)
            };
            pnl.Controls.AddRange(new Control[] { lblStart, cmbStartup });
            y += rh + 8;

            // Namespace rewrite
            chkRewrite = new CheckBox
            {
                Text     = "Rewrite root namespace in all copied files (best-effort)",
                Location = new Point(lw + 4, y),
                AutoSize = true,
                Checked  = true
            };
            pnl.Controls.Add(chkRewrite);
            y += rh;

            // Source info note
            var lblNote = new Label
            {
                Text      = "ⓘ  Target framework, output type, and NuGet packages are read automatically from the source .csproj.",
                Location  = new Point(0, y + 8),
                AutoSize  = false,
                Height    = 36,
                ForeColor = Color.FromArgb(60, 80, 120),
                Font      = new Font("Segoe UI", 8.5f),
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnl.Controls.AddRange(new Control[] { lbl, lblSrcInfo, lblNote });
            return pnl;
        }

        // ── Step 3 panel ──────────────────────────────────────────────────────

        private Panel BuildStep3Panel()
        {
            var pnl = new Panel();

            var lbl = MakeSectionLabel(
                "Review the files that will be written. Uncheck any to exclude.");
            lbl.Location = new Point(0, 0);

            // Split: files tree on left, packages list on right
            var split = new SplitContainer
            {
                Dock             = DockStyle.Fill,
                SplitterDistance = 420,
                SplitterWidth    = 5,
                Anchor           = AnchorStyles.Left | AnchorStyles.Right |
                                   AnchorStyles.Top  | AnchorStyles.Bottom,
                Location         = new Point(0, 28)
            };

            // Left — file tree
            lblFileCount = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 22,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.DimGray,
                Text      = string.Empty
            };
            tvReviewFiles = new TreeView
            {
                Dock          = DockStyle.Fill,
                CheckBoxes    = true,
                HideSelection = false,
                ShowLines     = true,
                Font          = new Font("Segoe UI", 8.5f),
                ItemHeight    = 20
            };
            tvReviewFiles.AfterCheck += TvReviewFiles_AfterCheck;
            split.Panel1.Controls.AddRange(new Control[] { tvReviewFiles, lblFileCount });

            // Right — packages
            var lblPkg = new Label
            {
                Text     = "NuGet Packages:",
                Dock     = DockStyle.Top,
                Height   = 22,
                Font     = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            lvPackages = new ListView
            {
                Dock          = DockStyle.Fill,
                CheckBoxes    = true,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = true,
                Font          = new Font("Segoe UI", 9f)
            };
            lvPackages.Columns.Add("Package",  180);
            lvPackages.Columns.Add("Version",  90);
            split.Panel2.Controls.AddRange(new Control[] { lvPackages, lblPkg });

            pnl.Controls.AddRange(new Control[] { lbl, split });
            pnl.Resize += (_, _) =>
                split.Size = new Size(
                    pnl.ClientSize.Width  - 40,
                    pnl.ClientSize.Height - 36);
            return pnl;
        }

        // ── Step 4 panel ──────────────────────────────────────────────────────

        private Panel BuildStep4Panel()
        {
            var pnl = new Panel();

            var lbl = MakeSectionLabel("Generating project…");
            lbl.Location = new Point(0, 0);
            lbl.Name     = "lblStep4Title";

            rtbLog = new RichTextBox
            {
                ReadOnly    = true,
                BackColor   = Color.FromArgb(30, 30, 30),
                ForeColor   = Color.FromArgb(200, 230, 200),
                Font        = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Anchor      = AnchorStyles.Left | AnchorStyles.Right |
                              AnchorStyles.Top  | AnchorStyles.Bottom,
                Location    = new Point(0, 28)
            };

            btnOpenFolder = new Button
            {
                Text    = "Open Solution Folder",
                Size    = new Size(178, 30),
                Visible = false,
                Anchor  = AnchorStyles.Left | AnchorStyles.Bottom,
                Font    = new Font("Segoe UI", 9.5f)
            };
            btnOpenFolder.Click += (_, _) =>
            {
                if (!string.IsNullOrEmpty(_finalSlnPath))
                    System.Diagnostics.Process.Start(
                        "explorer.exe",
                        Path.GetDirectoryName(_finalSlnPath)!);
            };

            pnl.Controls.AddRange(new Control[] { lbl, rtbLog, btnOpenFolder });
            pnl.Resize += (_, _) =>
            {
                rtbLog.Size = new Size(
                    pnl.ClientSize.Width  - 40,
                    pnl.ClientSize.Height - 70);
                btnOpenFolder.Location = new Point(0, rtbLog.Bottom + 8);
            };
            return pnl;
        }

        // ═════════════════════════════════════════════════════════════════════
        // NAVIGATION
        // ═════════════════════════════════════════════════════════════════════

        private void GoToStep(int step)
        {
            _currentStep = step;

            pnlStep1.Visible = step == 1;
            pnlStep2.Visible = step == 2;
            pnlStep3.Visible = step == 3;
            pnlStep4.Visible = step == 4;

            btnBack.Enabled = step > 1 && step < 4;

            string[] titles =
            {
                "Step 1 of 4 — Select Forms",
                "Step 2 of 4 — Configure Project",
                "Step 3 of 4 — Review Files & Packages",
                "Step 4 of 4 — Generating"
            };
            lblStep.Text = titles[step - 1];

            btnNext.Text = step == 3 ? "Generate  ▶" : "Next  →";
            btnNext.Enabled = step < 4;

            // Populate steps on entry
            switch (step)
            {
                case 1: PopulateStep1(); break;
                case 2: PopulateStep2(); break;
                case 3: PopulateStep3(); break;
            }
        }

        private async void BtnNext_Click(object? sender, EventArgs e)
        {
            switch (_currentStep)
            {
                case 1:
                    if (!ValidateStep1()) return;
                    FlushStep1();
                    GoToStep(2);
                    break;

                case 2:
                    if (!ValidateStep2()) return;
                    FlushStep2();
                    GoToStep(3);
                    break;

                case 3:
                    FlushStep3();
                    GoToStep(4);
                    await RunGenerateAsync();
                    break;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // STEP 1  — populate / validate / flush
        // ═════════════════════════════════════════════════════════════════════

        private void PopulateStep1()
        {
            tvFormSelect.BeginUpdate();
            tvFormSelect.Nodes.Clear();

            foreach (var group in _repo.GetGroupedByProject())
            {
                var projNode = new TreeNode(
                    string.IsNullOrEmpty(group.Key) ? "(No Project)" : group.Key)
                {
                    NodeFont  = new Font(tvFormSelect.Font, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 60, 120)
                };
                foreach (var form in group)
                {
                    var node = new TreeNode(form.FormName)
                    {
                        Tag     = form,
                        Checked = _config.SelectedForms
                            .Any(f => f.FormFilePath == form.FormFilePath)
                    };
                    if (!form.FileExists)
                    {
                        node.ForeColor   = Color.Gray;
                        node.ToolTipText = "File not found";
                    }
                    projNode.Nodes.Add(node);
                }
                tvFormSelect.Nodes.Add(projNode);
                projNode.Expand();
            }

            tvFormSelect.EndUpdate();
            UpdateSelectionCount();
        }

        private void TvFormSelect_AfterCheck(object? sender, TreeViewEventArgs e)
        {
            // Propagate project-node check state to children
            if (e.Node?.Tag == null && e.Node != null)
            {
                foreach (TreeNode child in e.Node.Nodes)
                    child.Checked = e.Node.Checked;
            }
            UpdateSelectionCount();
        }

        private void UpdateSelectionCount()
        {
            int count = tvFormSelect.Nodes
                .Cast<TreeNode>()
                .SelectMany(p => p.Nodes.Cast<TreeNode>())
                .Count(n => n.Checked && n.Tag is FormRecord);

            lblSelCount.Text = $"{count} form{(count == 1 ? "" : "s")} selected";
        }

        private bool ValidateStep1()
        {
            var selected = GetCheckedForms();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one form.",
                    "No Forms Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void FlushStep1()
        {
            _config.SelectedForms = GetCheckedForms();
        }

        private List<FormRecord> GetCheckedForms() =>
            tvFormSelect.Nodes
                .Cast<TreeNode>()
                .SelectMany(p => p.Nodes.Cast<TreeNode>())
                .Where(n => n.Checked && n.Tag is FormRecord)
                .Select(n => (FormRecord)n.Tag!)
                .ToList();

        // ═════════════════════════════════════════════════════════════════════
        // STEP 2  — populate / validate / flush
        // ═════════════════════════════════════════════════════════════════════

        private void PopulateStep2()
        {
            // Read source .csproj from first form that has one
            var csprojPath = _config.SelectedForms
                .Select(f => f.ProjectFilePath)
                .FirstOrDefault(p => !string.IsNullOrEmpty(p) && File.Exists(p))
                ?? string.Empty;

            _sourceInfo = string.IsNullOrEmpty(csprojPath)
                ? new CsprojReader.CsprojInfo()
                : CsprojReader.Read(csprojPath);

            _config.SourceProjectFilePath = csprojPath;
            _config.SourceRootNamespace   = _sourceInfo.RootNamespace;
            _config.TargetFramework       = _sourceInfo.TargetFramework;
            _config.OutputType            = _sourceInfo.OutputType;
            _config.UseWindowsForms       = _sourceInfo.UseWindowsForms;
            _config.PackageReferences     = _sourceInfo.PackageReferences;

            lblSrcInfo.Text =
                $"Source project: {Path.GetFileName(csprojPath)}   " +
                $"Framework: {_sourceInfo.TargetFramework}   " +
                $"Type: {_sourceInfo.OutputType}   " +
                $"Packages: {_sourceInfo.PackageReferences.Count}";

            // Populate startup form combo
            cmbStartup.Items.Clear();
            foreach (var f in _config.SelectedForms)
                cmbStartup.Items.Add(f);
            if (cmbStartup.Items.Count > 0)
                cmbStartup.SelectedIndex = 0;

            // Keep existing values if user went back
            if (string.IsNullOrEmpty(txtSolName.Text))
                txtSolName.Text = string.Empty;
            if (string.IsNullOrEmpty(txtOutDir.Text))
                txtOutDir.Text = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);
        }

        private void BtnBrowseOutDir_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description           = "Select the parent folder for the new solution",
                SelectedPath          = txtOutDir.Text,
                UseDescriptionForTitle = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                txtOutDir.Text = dlg.SelectedPath;
        }

        private bool ValidateStep2()
        {
            if (string.IsNullOrWhiteSpace(txtSolName.Text))
            {
                Warn("Please enter a solution name."); txtSolName.Focus(); return false;
            }
            if (!IsValidIdentifier(txtSolName.Text.Trim()))
            {
                Warn("Solution name must be a valid C# identifier (letters, digits, underscores).");
                txtSolName.Focus(); return false;
            }
            if (string.IsNullOrWhiteSpace(txtOutDir.Text))
            {
                Warn("Please choose an output folder."); txtOutDir.Focus(); return false;
            }
            if (string.IsNullOrWhiteSpace(txtNamespace.Text))
            {
                Warn("Please enter a root namespace."); txtNamespace.Focus(); return false;
            }
            return true;
        }

        private void FlushStep2()
        {
            _config.SolutionName          = txtSolName.Text.Trim();
            _config.OutputParentDirectory = txtOutDir.Text.Trim();
            _config.RootNamespace         = txtNamespace.Text.Trim();
            _config.StartupForm           = cmbStartup.SelectedItem as FormRecord;
            _config.RewriteNamespaces     = chkRewrite.Checked;
        }

        // ═════════════════════════════════════════════════════════════════════
        // STEP 3  — populate / flush
        // ═════════════════════════════════════════════════════════════════════

        private void PopulateStep3()
        {
            // Run dependency scan
            _scaffoldFiles = DependencyScanner.Scan(
                _config.SelectedForms,
                _config.SourceProjectFilePath,
                _config.StartupForm?.FormName ?? _config.SelectedForms.First().FormName);

            // Build review tree grouped by category then directory
            tvReviewFiles.BeginUpdate();
            tvReviewFiles.Nodes.Clear();

            var grouped = _scaffoldFiles
                .GroupBy(f => f.Category)
                .OrderBy(g => (int)g.Key);

            foreach (var catGroup in grouped)
            {
                var catNode = new TreeNode(catGroup.Key.ToString())
                {
                    NodeFont  = new Font(tvReviewFiles.Font, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 60, 120),
                    Checked   = true
                };

                // Sub-group by relative directory
                foreach (var dirGroup in catGroup
                             .GroupBy(f => f.RelativeDirectory)
                             .OrderBy(g => g.Key))
                {
                    var dirLabel = string.IsNullOrEmpty(dirGroup.Key)
                        ? "(root)" : dirGroup.Key;

                    var dirNode = new TreeNode(dirLabel)
                        { ForeColor = Color.DimGray, Checked = true };

                    foreach (var file in dirGroup.OrderBy(f => f.FileName))
                    {
                        var fileNode = new TreeNode(file.FileName)
                            { Tag = file, Checked = file.IsIncluded };
                        dirNode.Nodes.Add(fileNode);
                    }

                    catNode.Nodes.Add(dirNode);
                }

                tvReviewFiles.Nodes.Add(catNode);
                catNode.Expand();
            }

            tvReviewFiles.EndUpdate();

            lblFileCount.Text =
                $"{_scaffoldFiles.Count(f => f.IsIncluded)} of {_scaffoldFiles.Count} files included";

            // Populate packages list
            lvPackages.Items.Clear();
            foreach (var pkg in _config.PackageReferences)
            {
                var item = new ListViewItem(pkg.PackageId) { Checked = pkg.IsIncluded };
                item.SubItems.Add(pkg.Version);
                item.Tag = pkg;
                lvPackages.Items.Add(item);
            }

            if (_config.PackageReferences.Count == 0)
            {
                var item = new ListViewItem("(no packages in source project)")
                    { ForeColor = Color.DimGray, Checked = false };
                lvPackages.Items.Add(item);
            }
        }

        private void TvReviewFiles_AfterCheck(object? sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;

            // Propagate downward
            SetSubtreeChecked(e.Node, e.Node.Checked);

            // Sync ScaffoldFile.IsIncluded
            foreach (TreeNode cat in tvReviewFiles.Nodes)
                foreach (TreeNode dir in cat.Nodes)
                    foreach (TreeNode file in dir.Nodes)
                        if (file.Tag is ScaffoldFile sf)
                            sf.IsIncluded = file.Checked;

            int total    = _scaffoldFiles.Count;
            int included = _scaffoldFiles.Count(f => f.IsIncluded);
            lblFileCount.Text = $"{included} of {total} files included";
        }

        private static void SetSubtreeChecked(TreeNode node, bool @checked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = @checked;
                SetSubtreeChecked(child, @checked);
            }
        }

        private void FlushStep3()
        {
            // Sync package include flags from list view
            foreach (ListViewItem item in lvPackages.Items)
            {
                if (item.Tag is PackageReferenceEntry pkg)
                    pkg.IsIncluded = item.Checked;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // STEP 4  — generate
        // ═════════════════════════════════════════════════════════════════════

        private async Task RunGenerateAsync()
        {
            btnNext.Enabled = false;
            btnBack.Enabled = false;
            rtbLog.Clear();

            var progress = new Progress<string>(msg =>
            {
                rtbLog.AppendText(msg + Environment.NewLine);
                rtbLog.ScrollToCaret();
            });

            var result = await SolutionScaffolder.ScaffoldAsync(
                _config,
                _scaffoldFiles,
                _sourceInfo ?? new CsprojReader.CsprojInfo(),
                progress,
                CancellationToken.None);

            if (result.Success)
            {
                _finalSlnPath = result.SolutionPath;

                rtbLog.AppendText(Environment.NewLine);
                rtbLog.AppendText(
                    $"✓  Solution created: {result.SolutionPath}" + Environment.NewLine);
                rtbLog.AppendText(
                    $"   {result.WrittenPaths.Count} files written." + Environment.NewLine);

                if (result.SkippedPaths.Count > 0)
                    rtbLog.AppendText(
                        $"   {result.SkippedPaths.Count} files skipped (unchecked)." +
                        Environment.NewLine);

                // Update header label
                var titleLabel = pnlStep4.Controls
                    .OfType<Label>()
                    .FirstOrDefault(l => l.Name == "lblStep4Title");
                if (titleLabel != null)
                    titleLabel.Text = "Project generated successfully!";

                btnOpenFolder.Visible = true;
            }
            else
            {
                rtbLog.AppendText(
                    $"✗  FAILED: {result.ErrorMessage}" + Environment.NewLine);

                btnBack.Enabled = true;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private string _prevSolName = string.Empty;
        private string PrevSolName() => _prevSolName;

        private void AddRow(Panel parent, string labelText, int lw, int y, out TextBox txt)
        {
            var lbl = MakeLabel(labelText, 0, y, lw);
            txt = MakeTextBox(lw + 4, y, 280);
            parent.Controls.AddRange(new Control[] { lbl, txt });
        }

        private static Label MakeSectionLabel(string text) =>
            new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", 9.5f) };

        private static Label MakeLabel(string text, int x, int y, int w) =>
            new Label
            {
                Text = text, Location = new Point(x, y + 3),
                Size = new Size(w, 22), TextAlign = ContentAlignment.MiddleRight
            };

        private static TextBox MakeTextBox(int x, int y, int w) =>
            new TextBox { Location = new Point(x, y), Size = new Size(w, 23),
                          Font = new Font("Segoe UI", 9f) };

        private static string SanitizeIdentifier(string raw) =>
            string.Concat(raw.Where(c => char.IsLetterOrDigit(c) || c == '_'));

        private static bool IsValidIdentifier(string s) =>
            !string.IsNullOrEmpty(s) &&
            s.All(c => char.IsLetterOrDigit(c) || c == '_') &&
            !char.IsDigit(s[0]);

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
