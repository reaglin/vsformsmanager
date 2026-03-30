using VSFormsManager.Controls;
using VSFormsManager.Models;
using VSFormsManager.Services;
using VSFormsManager.Services.Parsing;

namespace VSFormsManager
{
    /// <summary>
    /// Form browser with a tree of previously discovered forms on the left and a
    /// <see cref="FormDetailPanel"/> on the right.
    ///
    /// Opened modally from <see cref="frmMain"/> via <see cref="AppSession.OpenFormBrowser"/>.
    /// When the user selects a form and clicks "Use This Form",
    /// <see cref="AppSession.CurrentForm"/> is set and the dialog closes with
    /// <see cref="DialogResult.OK"/>. Closing without clicking the button leaves
    /// <see cref="AppSession.CurrentForm"/> unchanged.
    /// </summary>
    public partial class frmGetForm : Form
    {
        // ── State ─────────────────────────────────────────────────────────────

        private readonly FormsRepository _repo = new();
        private FormRecord? _selectedRecord;

        // ── Controls ──────────────────────────────────────────────────────────

        private TreeView        tvForms      = null!;
        private FormDetailPanel _detailPanel = null!;
        private Button          btnBrowse    = null!;
        private Button          btnRescan    = null!;
        private Button          btnRemove    = null!;
        private Button          btnUse       = null!;

        // ── Constructor ───────────────────────────────────────────────────────

        public frmGetForm()
        {
            InitializeComponent();
            BuildUi();
            RefreshTree();
        }

        // ═════════════════════════════════════════════════════════════════════
        // UI CONSTRUCTION
        // ═════════════════════════════════════════════════════════════════════

        private void BuildUi()
        {
            Text            = "Form Browser";
            Size            = new Size(1000, 680);
            MinimumSize     = new Size(760, 520);
            StartPosition   = FormStartPosition.CenterScreen;
            Font            = new Font("Segoe UI", 9f);

            // ── Toolbar ───────────────────────────────────────────────────────
            var pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 46,
                BackColor = SystemColors.ControlLight,
                Padding   = new Padding(8, 8, 8, 0)
            };

            btnBrowse = MakeToolBtn("Browse for Form\u2026", 8);
            btnBrowse.Click += BtnBrowse_Click;

            btnRescan = MakeToolBtn("Rescan Selected", btnBrowse.Right + 6);
            btnRescan.Click += BtnRescan_Click;

            btnRemove = MakeToolBtn("Remove Selected", btnRescan.Right + 6);
            btnRemove.ForeColor = Color.Firebrick;
            btnRemove.Click += BtnRemove_Click;

            // "Use This Form" is prominent and right-aligned
            btnUse = new Button
            {
                Text      = "\u2714  Use This Form",
                Size      = new Size(148, 30),
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                Enabled   = false
            };
            btnUse.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 50);
            btnUse.Click += BtnUse_Click;
            pnlToolbar.Resize += (_, _) =>
                btnUse.Location = new Point(pnlToolbar.ClientSize.Width - btnUse.Width - 8, 8);

            pnlToolbar.Controls.AddRange(
                new Control[] { btnBrowse, btnRescan, btnRemove, btnUse });

            var toolSep = new Panel
                { Dock = DockStyle.Top, Height = 1, BackColor = SystemColors.ControlDark };

            // ── Main split ────────────────────────────────────────────────────
            var split = new SplitContainer
            {
                Dock             = DockStyle.Fill,
                FixedPanel       = FixedPanel.Panel1,
                SplitterWidth    = 5,
                SplitterDistance = 280
            };

            BuildLeftPanel(split.Panel1);
            BuildRightPanel(split.Panel2);

            Controls.Add(split);
            Controls.Add(toolSep);
            Controls.Add(pnlToolbar);
        }

        private void BuildLeftPanel(SplitterPanel panel)
        {
            var header = MakeSectionHeader("Previously Found Forms");
            header.Dock = DockStyle.Top;

            tvForms = new TreeView
            {
                Dock          = DockStyle.Fill,
                HideSelection = false,
                ShowLines     = true,
                ShowPlusMinus = true,
                BorderStyle   = BorderStyle.None,
                Font          = new Font("Segoe UI", 9f),
                ItemHeight    = 22,
                Indent        = 18
            };
            tvForms.AfterSelect += TvForms_AfterSelect;

            // Context menu
            var ctx      = new ContextMenuStrip();
            var miRescan = new ToolStripMenuItem("Rescan");
            var miRemove = new ToolStripMenuItem("Remove");
            miRescan.Click += (_, _) => RescanSelected();
            miRemove.Click += (_, _) => RemoveSelected();
            ctx.Items.AddRange(new ToolStripItem[]
                { miRescan, new ToolStripSeparator(), miRemove });
            tvForms.ContextMenuStrip = ctx;
            ctx.Opening += (_, _) =>
            {
                miRescan.Enabled = _selectedRecord != null;
                miRemove.Enabled = _selectedRecord != null;
            };

            panel.Controls.Add(tvForms);
            panel.Controls.Add(header);
        }

        private void BuildRightPanel(SplitterPanel panel)
        {
            var header = MakeSectionHeader("Form Details");
            header.Dock = DockStyle.Top;

            _detailPanel = new FormDetailPanel { Dock = DockStyle.Fill };

            panel.Controls.Add(_detailPanel);
            panel.Controls.Add(header);
        }

        // ═════════════════════════════════════════════════════════════════════
        // TREE MANAGEMENT
        // ═════════════════════════════════════════════════════════════════════

        private void RefreshTree(FormRecord? selectAfter = null)
        {
            tvForms.BeginUpdate();
            tvForms.Nodes.Clear();

            foreach (var group in _repo.GetGroupedByProject())
            {
                var projectNode = new TreeNode(
                    string.IsNullOrEmpty(group.Key) ? "(No Project)" : group.Key)
                {
                    NodeFont  = new Font(tvForms.Font, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 60, 120)
                };

                foreach (var record in group)
                {
                    var node = new TreeNode(record.FormName) { Tag = record };
                    if (!record.FileExists)
                    {
                        node.ForeColor   = Color.Gray;
                        node.ToolTipText = "File not found — right-click to remove.";
                    }
                    projectNode.Nodes.Add(node);
                }

                tvForms.Nodes.Add(projectNode);
                projectNode.Expand();
            }

            tvForms.EndUpdate();

            if (selectAfter != null) SelectFormNode(selectAfter);
        }

        private void SelectFormNode(FormRecord target)
        {
            foreach (TreeNode proj in tvForms.Nodes)
                foreach (TreeNode form in proj.Nodes)
                    if (form.Tag is FormRecord r &&
                        r.FormFilePath.Equals(target.FormFilePath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        tvForms.SelectedNode = form;
                        form.EnsureVisible();
                        return;
                    }
        }

        private void TvForms_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is FormRecord record)
            {
                _selectedRecord   = record;
                _detailPanel.LoadRecord(record);
                btnRescan.Enabled = true;
                btnRemove.Enabled = true;
                btnUse.Enabled    = true;
            }
            else
            {
                _selectedRecord   = null;
                _detailPanel.ClearRecord();
                btnRescan.Enabled = false;
                btnRemove.Enabled = false;
                btnUse.Enabled    = false;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // TOOLBAR ACTIONS
        // ═════════════════════════════════════════════════════════════════════

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title           = "Select a Visual Studio Form File",
                Filter          = "Form Files (*.cs;*.xaml)|*.cs;*.xaml" +
                                  "|C# Files (*.cs)|*.cs" +
                                  "|XAML Files (*.xaml;*.xaml.cs)|*.xaml;*.xaml.cs" +
                                  "|All Files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                ParseAndSave(dlg.FileName);
        }

        private void BtnRescan_Click(object? sender, EventArgs e) => RescanSelected();
        private void BtnRemove_Click(object? sender, EventArgs e) => RemoveSelected();

        /// <summary>
        /// Sets <see cref="AppSession.CurrentForm"/> to the selected record and
        /// closes with <see cref="DialogResult.OK"/> so <see cref="frmMain"/>
        /// refreshes its detail panel.
        /// </summary>
        private void BtnUse_Click(object? sender, EventArgs e)
        {
            if (_selectedRecord == null) return;
            AppSession.CurrentForm = _selectedRecord;
            DialogResult = DialogResult.OK;
            Close();
        }

        // ── Shared logic ──────────────────────────────────────────────────────

        private void ParseAndSave(string filePath)
        {
            try
            {
                var (parser, primaryPath) = FormParserFactory.GetParser(filePath);
                var record                = parser.Parse(primaryPath);

                _repo.AddOrUpdate(record);
                RefreshTree(record);

                MessageBox.Show(
                    $"'{record.FormName}' from '{record.ProjectName}' was found.\r\n\r\n" +
                    $"Controls: {record.Controls.Count}    " +
                    $"Dependencies: {record.CodeDependencies.Count}",
                    "Form Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (NotSupportedException ex) { ShowWarn("File Not Supported", ex.Message); }
            catch (Exception ex) { ShowWarn("Parse Error", ex.Message); }
        }

        private void RescanSelected()
        {
            if (_selectedRecord == null) return;
            if (!_selectedRecord.FileExists)
            {
                ShowWarn("File Not Found",
                    $"The file no longer exists:\r\n{_selectedRecord.FormFilePath}");
                return;
            }
            ParseAndSave(_selectedRecord.FormFilePath);
        }

        private void RemoveSelected()
        {
            if (_selectedRecord == null) return;
            if (MessageBox.Show(
                    $"Remove '{_selectedRecord.FormName}' from the list?\r\n" +
                    "The source file will not be deleted.",
                    "Remove Form", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;

            _repo.Remove(_selectedRecord);
            _selectedRecord   = null;
            btnRescan.Enabled = false;
            btnRemove.Enabled = false;
            btnUse.Enabled    = false;
            RefreshTree();
            _detailPanel.ClearRecord();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Button MakeToolBtn(string text, int x) =>
            new Button { Text = text, Location = new Point(x, 8), Size = new Size(140, 30) };

        private static Label MakeSectionHeader(string text) =>
            new Label
            {
                Text      = text,
                Height    = 28,
                Padding   = new Padding(8, 6, 0, 0),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(230, 236, 245),
                ForeColor = Color.FromArgb(30, 60, 120),
                AutoSize  = false
            };

        private void ShowWarn(string title, string msg) =>
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
