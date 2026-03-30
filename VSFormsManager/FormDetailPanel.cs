using VSFormsManager.Models;

namespace VSFormsManager.Controls
{
    /// <summary>
    /// Reusable UserControl that displays all metadata for a <see cref="FormRecord"/>:
    /// name, project, namespace, type, location, controls list, and code dependencies.
    ///
    /// Used by both <see cref="frmMain"/> (main window) and <see cref="frmGetForm"/>
    /// (form browser) so both show an identical detail view.
    ///
    /// Call <see cref="LoadRecord"/> to populate; pass <c>null</c> to clear.
    /// </summary>
    public class FormDetailPanel : UserControl
    {
        // ── Controls ──────────────────────────────────────────────────────────
        private TextBox  txtFormName    = null!;
        private TextBox  txtProject     = null!;
        private TextBox  txtNamespace   = null!;
        private TextBox  txtType        = null!;
        private TextBox  txtLastScanned = null!;
        private TextBox  txtLocation    = null!;
        private Button   btnOpenFolder  = null!;
        private Label    lblNoFile      = null!;
        private Label    lblPlaceholder = null!;
        private Panel    pnlInfo        = null!;
        private TabControl tcDetails   = null!;
        private TabPage  tabControls   = null!;
        private TabPage  tabDeps       = null!;
        private ListView lvControls    = null!;
        private ListView lvDeps        = null!;

        private FormRecord? _record;

        // ── Constructor ───────────────────────────────────────────────────────

        public FormDetailPanel()
        {
            Dock = DockStyle.Fill;
            Font = new Font("Segoe UI", 9f);
            BuildUi();
            ShowPlaceholder();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates every field from <paramref name="record"/>.
        /// Pass <c>null</c> to clear the panel and show the placeholder.
        /// </summary>
        public void LoadRecord(FormRecord? record)
        {
            _record = record;
            if (record == null) { ShowPlaceholder(); return; }
            HidePlaceholder();

            txtFormName.Text    = record.FormName;
            txtProject.Text     = record.ProjectName;
            txtNamespace.Text   = record.Namespace;
            txtType.Text        = record.FormTypeLabel;
            txtLastScanned.Text = record.LastScanned.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            txtLocation.Text    = record.FormFilePath;

            bool missing          = !record.FileExists;
            lblNoFile.Visible     = missing;
            btnOpenFolder.Enabled = !missing;

            lvControls.BeginUpdate();
            lvControls.Items.Clear();
            foreach (var c in record.Controls)
            {
                var item = new ListViewItem(c.Name);
                item.SubItems.Add(c.ControlType);
                lvControls.Items.Add(item);
            }
            lvControls.EndUpdate();
            tabControls.Text = $"Controls ({record.Controls.Count})";

            lvDeps.BeginUpdate();
            lvDeps.Items.Clear();
            foreach (var d in record.CodeDependencies)
            {
                var item = new ListViewItem(d.Namespace);
                item.SubItems.Add(d.Category);
                item.ForeColor = d.IsFramework ? Color.DimGray : Color.DarkGreen;
                lvDeps.Items.Add(item);
            }
            lvDeps.EndUpdate();
            tabDeps.Text = $"Code Dependencies ({record.CodeDependencies.Count})";
        }

        /// <summary>Clears all fields and shows the placeholder message.</summary>
        public void ClearRecord() => LoadRecord(null);

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUi()
        {
            // ── Placeholder (shown when no record loaded) ─────────────────────
            lblPlaceholder = new Label
            {
                Text      = "No form selected.\r\n\r\nUse File → Browse for Form  or  " +
                             "click the Browse button above to get started.",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Font      = new Font("Segoe UI", 10f),
                Visible   = false
            };

            // ── Info section ──────────────────────────────────────────────────
            pnlInfo = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 210,
                Padding = new Padding(12, 8, 12, 0)
            };

            const int labelW = 104;
            const int rowH   = 30;
            const int startX = 12;
            const int startY = 8;

            (_, txtFormName)    = AddRow(pnlInfo, "Form Name:",    startX, startY,            labelW);
            (_, txtProject)     = AddRow(pnlInfo, "Project:",      startX, startY + rowH,     labelW);
            (_, txtNamespace)   = AddRow(pnlInfo, "Namespace:",    startX, startY + rowH * 2, labelW);
            (_, txtType)        = AddRow(pnlInfo, "Form Type:",    startX, startY + rowH * 3, labelW);
            (_, txtLastScanned) = AddRow(pnlInfo, "Last Scanned:", startX, startY + rowH * 4, labelW);

            // Location row (has an extra "Open Folder" button)
            var lblLoc = MakeLabel("Location:", startX, startY + rowH * 5, labelW);

            txtLocation = MakeReadOnlyBox(startX + labelW + 4, startY + rowH * 5, 10);
            txtLocation.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            btnOpenFolder = new Button
            {
                Text    = "📂 Open",
                Size    = new Size(82, 23),
                Anchor  = AnchorStyles.Right | AnchorStyles.Top,
                Font    = new Font("Segoe UI", 8.5f),
                Enabled = false
            };
            btnOpenFolder.Click += BtnOpenFolder_Click;

            lblNoFile = new Label
            {
                Text      = "⚠  File no longer exists at this location.",
                ForeColor = Color.Firebrick,
                AutoSize  = false,
                Height    = 20,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Visible   = false,
                Font      = new Font("Segoe UI", 8.5f)
            };

            pnlInfo.Controls.AddRange(new Control[]
                { lblLoc, txtLocation, btnOpenFolder, lblNoFile });

            pnlInfo.Resize += (_, _) => LayoutLocationRow();
            Load            += (_, _) => LayoutLocationRow();

            // ── Separator ─────────────────────────────────────────────────────
            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = SystemColors.ControlDark };

            // ── Tab control ───────────────────────────────────────────────────
            tcDetails = new TabControl { Dock = DockStyle.Fill };

            tabControls = new TabPage("Controls");
            tabDeps     = new TabPage("Code Dependencies");

            lvControls = new ListView
            {
                Dock          = DockStyle.Fill,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = true,
                HideSelection = false,
                Font          = new Font("Segoe UI", 9f)
            };
            lvControls.Columns.Add("Name",         200);
            lvControls.Columns.Add("Control Type", 200);
            tabControls.Controls.Add(lvControls);

            lvDeps = new ListView
            {
                Dock          = DockStyle.Fill,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = true,
                HideSelection = false,
                Font          = new Font("Segoe UI", 9f)
            };
            lvDeps.Columns.Add("Namespace", 340);
            lvDeps.Columns.Add("Category",  120);
            tabDeps.Controls.Add(lvDeps);

            tcDetails.TabPages.AddRange(new[] { tabControls, tabDeps });

            // Add in reverse dock order
            Controls.Add(tcDetails);
            Controls.Add(sep);
            Controls.Add(pnlInfo);
            Controls.Add(lblPlaceholder);
        }

        private void LayoutLocationRow()
        {
            const int labelW = 104;
            const int startX = 12;
            const int rowH   = 30;
            const int startY = 8;
            const int btnW   = 88;
            const int margin = 12;

            int right = pnlInfo.ClientSize.Width - margin;
            int btnX  = right - btnW;
            int txtX  = startX + labelW + 4;
            int txtW  = btnX - 4 - txtX;
            int y     = startY + rowH * 5;

            if (txtW < 40) return;

            txtLocation.SetBounds(txtX, y, txtW, 23);
            btnOpenFolder.Location = new Point(btnX, y);
            lblNoFile.SetBounds(txtX, y + 26, right - txtX, 20);
        }

        // ── Placeholder ───────────────────────────────────────────────────────

        private void ShowPlaceholder()
        {
            lblPlaceholder.Visible = true;
            pnlInfo.Visible        = false;
            tcDetails.Visible      = false;

            txtFormName.Text = txtProject.Text = txtNamespace.Text =
            txtType.Text     = txtLastScanned.Text = txtLocation.Text = string.Empty;
            lblNoFile.Visible     = false;
            btnOpenFolder.Enabled = false;
            lvControls.Items.Clear();
            lvDeps.Items.Clear();
            tabControls.Text = "Controls";
            tabDeps.Text     = "Code Dependencies";
        }

        private void HidePlaceholder()
        {
            lblPlaceholder.Visible = false;
            pnlInfo.Visible        = true;
            tcDetails.Visible      = true;
        }

        // ── Events ────────────────────────────────────────────────────────────

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            if (_record == null) return;
            var dir = _record.FormDirectory;
            if (Directory.Exists(dir))
                System.Diagnostics.Process.Start("explorer.exe", dir);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private (Label, TextBox) AddRow(Panel parent, string labelText, int x, int y, int lw)
        {
            var lbl = MakeLabel(labelText, x, y, lw);
            var txt = MakeReadOnlyBox(x + lw + 4, y,
                          parent.ClientSize.Width - x - lw - 20);
            txt.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            parent.Controls.Add(lbl);
            parent.Controls.Add(txt);
            return (lbl, txt);
        }

        private static Label MakeLabel(string text, int x, int y, int width) =>
            new Label
            {
                Text      = text,
                Location  = new Point(x, y + 3),
                Size      = new Size(width, 22),
                TextAlign = ContentAlignment.MiddleRight
            };

        private static TextBox MakeReadOnlyBox(int x, int y, int width) =>
            new TextBox
            {
                Location    = new Point(x, y),
                Size        = new Size(width, 23),
                ReadOnly    = true,
                BackColor   = SystemColors.ControlLight,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 9f)
            };
    }
}
