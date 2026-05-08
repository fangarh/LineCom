using LineCom.CatalogImport.Core.Database;
using LineCom.CatalogImport.Core.Images;
using LineCom.CatalogImport.Core.Planning;
using LineCom.CatalogImport.Core.Reporting;
using LineCom.CatalogImport.Core.Source;

namespace LineCom.CatalogImport.WinForms;

public sealed class MainForm : Form
{
    private readonly TextBox _sourcePath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _manifestPath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _reportPath = new() { Dock = DockStyle.Fill };
    private readonly TextBox _connectionString = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly TextBox _storageRootPath = new() { Dock = DockStyle.Fill };
    private readonly Label _summaryLabel = new() { AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
    private readonly CheckBox _resetCatalog = new() { Text = "Reset catalog then import", AutoSize = true };
    private readonly CheckBox _allowDevQaReset = new() { Text = "I confirm this is a dev/QA environment", AutoSize = true, Enabled = false };
    private readonly CheckBox _replaceMainImages = new() { Text = "Replace existing main images", AutoSize = true };
    private readonly Button _dryRunButton = new() { Text = "Dry-run", AutoSize = true };
    private readonly Button _writeReportButton = new() { Text = "Write report", AutoSize = true, Enabled = false };
    private readonly Button _applyButton = new() { Text = "Apply", AutoSize = true, Enabled = false };
    private readonly DataGridView _productsGrid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };
    private readonly TextBox _log = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        ReadOnly = true
    };

    private CatalogImportPlanSnapshot? _currentPlanSnapshot;

    public MainForm()
    {
        Text = "LineCom Catalog Importer";
        MinimumSize = new Size(1000, 700);
        Width = 1200;
        Height = 800;

        _sourcePath.Text = Path.Combine("Assets", "1c_export_41_01_nomenclature_by_category.json");
        _manifestPath.Text = Path.Combine("Assets", "product-images", "tktdf_manifest.json");
        _reportPath.Text = Path.Combine(".codex-tmp", "catalog-import-reports");

        _sourcePath.TextChanged += (_, _) => InvalidateCurrentPlan();
        _manifestPath.TextChanged += (_, _) => InvalidateCurrentPlan();
        _resetCatalog.CheckedChanged += (_, _) => _allowDevQaReset.Enabled = _resetCatalog.Checked;
        _dryRunButton.Click += async (_, _) => await RunDryRunAsync();
        _writeReportButton.Click += (_, _) => WriteCurrentReport("dry-run");
        _applyButton.Click += async (_, _) => await ApplyAsync();

        Controls.Add(BuildLayout());
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 68));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 32));

        root.Controls.Add(BuildInputs(), 0, 0);
        root.Controls.Add(BuildControls(), 0, 1);
        root.Controls.Add(_summaryLabel, 0, 2);
        root.Controls.Add(_productsGrid, 0, 3);
        root.Controls.Add(_log, 0, 4);

        return root;
    }

    private Control BuildInputs()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        AddFileRow(panel, "Source JSON", _sourcePath, "JSON files (*.json)|*.json|All files (*.*)|*.*");
        AddFileRow(panel, "Image manifest", _manifestPath, "JSON files (*.json)|*.json|All files (*.*)|*.*");
        AddFolderRow(panel, "Report folder", _reportPath);
        AddFolderRow(panel, "Storage root", _storageRootPath);
        AddTextRow(panel, "Connection string", _connectionString);

        return panel;
    }

    private Control BuildControls()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        panel.Controls.Add(_dryRunButton);
        panel.Controls.Add(_writeReportButton);
        panel.Controls.Add(_applyButton);
        panel.Controls.Add(_replaceMainImages);
        panel.Controls.Add(_resetCatalog);
        panel.Controls.Add(_allowDevQaReset);

        return panel;
    }

    private void AddFileRow(TableLayoutPanel panel, string label, TextBox textBox, string filter)
    {
        var button = new Button { Text = "Browse", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = filter,
                FileName = textBox.Text
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                textBox.Text = dialog.FileName;
            }
        };

        AddRow(panel, label, textBox, button);
    }

    private void AddFolderRow(TableLayoutPanel panel, string label, TextBox textBox)
    {
        var button = new Button { Text = "Browse", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = Directory.Exists(textBox.Text) ? textBox.Text : string.Empty
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                textBox.Text = dialog.SelectedPath;
            }
        };

        AddRow(panel, label, textBox, button);
    }

    private void AddTextRow(TableLayoutPanel panel, string label, TextBox textBox)
    {
        AddRow(panel, label, textBox, new Label());
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control input, Control action)
    {
        var row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(
            new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
            0,
            row);
        panel.Controls.Add(input, 1, row);
        panel.Controls.Add(action, 2, row);
    }

    private async Task RunDryRunAsync()
    {
        var sourcePath = _sourcePath.Text;
        var manifestPath = _manifestPath.Text;

        try
        {
            SetBusy(true);

            var plan = await Task.Run(() =>
            {
                var source = OneCExportReader.Read(sourcePath);
                var images = ProductImageManifestReader.ReadAcceptedBySourceRow(manifestPath);
                return CatalogImportPlanner.BuildPlan(source, images);
            });

            if (!string.Equals(_sourcePath.Text, sourcePath, StringComparison.Ordinal)
                || !string.Equals(_manifestPath.Text, manifestPath, StringComparison.Ordinal))
            {
                InvalidateCurrentPlan();
                Log("Dry-run result ignored because source inputs changed during processing.");
                return;
            }

            _currentPlanSnapshot = new CatalogImportPlanSnapshot(plan, sourcePath, manifestPath);
            _productsGrid.DataSource = plan.Products.Select(product => new ProductPreviewRow(
                product.SourceRow,
                product.ExternalId,
                product.Name,
                product.Slug,
                product.CategorySlug,
                product.PublishStatus,
                product.Image is not null)).ToArray();

            _writeReportButton.Enabled = true;
            _applyButton.Enabled = true;
            UpdateSummary(plan.Summary);
            LogWarnings(plan.Warnings);
            Log("Dry-run complete. No database writes were performed.");
        }
        catch (Exception exception)
        {
            InvalidateCurrentPlan();
            ShowError("Dry-run failed", exception);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void WriteCurrentReport(string mode)
    {
        if (_currentPlanSnapshot is null)
        {
            return;
        }

        WriteReport(
            _currentPlanSnapshot,
            _reportPath.Text,
            mode,
            string.IsNullOrWhiteSpace(_connectionString.Text) ? null : "configured");
    }

    private void WriteReport(
        CatalogImportPlanSnapshot snapshot,
        string reportPath,
        string mode,
        string? targetDatabase)
    {
        try
        {
            var result = CatalogImportReportWriter.WriteReports(
                snapshot.Plan,
                reportPath,
                new CatalogImportReportContext(
                    snapshot.SourcePath,
                    snapshot.ManifestPath,
                    mode,
                    targetDatabase));

            Log($"Reports written: {result.MarkdownPath}");
        }
        catch (Exception exception)
        {
            ShowError("Report failed", exception);
        }
    }

    private async Task ApplyAsync()
    {
        var snapshot = _currentPlanSnapshot;
        if (snapshot is null)
        {
            return;
        }

        var connectionString = _connectionString.Text;
        var reportPath = _reportPath.Text;
        var resetCatalog = _resetCatalog.Checked;
        var allowDevQaReset = _allowDevQaReset.Checked;
        var replaceMainImages = _replaceMainImages.Checked;
        var storageRootPath = string.IsNullOrWhiteSpace(_storageRootPath.Text) ? null : _storageRootPath.Text;
        var targetDatabase = string.IsNullOrWhiteSpace(connectionString) ? null : "configured";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show(this, "Connection string is required before apply.", "Apply blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (resetCatalog && !allowDevQaReset)
        {
            MessageBox.Show(this, "Reset requires explicit dev/QA allowance.", "Reset blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (resetCatalog && !ConfirmReset())
        {
            return;
        }

        try
        {
            SetBusy(true);
            var database = new CatalogImportDatabase(connectionString);
            var result = await database.ApplyAsync(
                snapshot.Plan,
                new CatalogImportApplyOptions(
                    ResetCatalog: resetCatalog,
                    AllowResetInCurrentEnvironment: allowDevQaReset,
                    ReplaceExistingMainImages: replaceMainImages,
                    StorageRootPath: storageRootPath),
                CancellationToken.None);

            Log(
                $"Apply complete. Categories: {result.CategoriesProcessed}, products: {result.ProductsProcessed}, images: {result.ImagesProcessed}.");
            if (result.ResetImpact is not null)
            {
                Log(
                    "Reset impact: "
                    + $"categories {result.ResetImpact.Categories}, products {result.ResetImpact.Products}, "
                    + $"product images {result.ResetImpact.ProductImages}, stored files {result.ResetImpact.StoredProductImageFiles}.");
            }

            WriteReport(snapshot, reportPath, resetCatalog ? "reset-apply" : "upsert-apply", targetDatabase);
        }
        catch (Exception exception)
        {
            ShowError("Apply failed", exception);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private bool ConfirmReset()
    {
        var message = "Reset catalog then import will delete existing catalog rows before import. Continue only for dev/QA databases.";
        return MessageBox.Show(
            this,
            message,
            "Confirm catalog reset",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    private void SetBusy(bool isBusy)
    {
        _dryRunButton.Enabled = !isBusy;
        _writeReportButton.Enabled = !isBusy && _currentPlanSnapshot is not null;
        _applyButton.Enabled = !isBusy && _currentPlanSnapshot is not null;
        _resetCatalog.Enabled = !isBusy;
        _allowDevQaReset.Enabled = !isBusy && _resetCatalog.Checked;
        _replaceMainImages.Enabled = !isBusy;
    }

    private void UpdateSummary(CatalogImportSummary summary)
    {
        _summaryLabel.Text =
            $"Categories: {summary.Categories} | Products: {summary.Products} | "
            + $"Published: {summary.PublishableProducts} | Draft: {summary.DraftProducts} | "
            + $"Images: {summary.ImageAssignments} | Warnings: {summary.Warnings}";
    }

    private void LogWarnings(IReadOnlyList<CatalogImportWarning> warnings)
    {
        if (warnings.Count == 0)
        {
            Log("Warnings: 0");
            return;
        }

        foreach (var warning in warnings)
        {
            var row = warning.SourceRow is null ? "n/a" : warning.SourceRow.Value.ToString();
            Log($"Warning {warning.Code} at row {row}: {warning.Message}");
        }
    }

    private void ShowError(string caption, Exception exception)
    {
        Log($"{caption}: {exception.Message}");
        MessageBox.Show(this, exception.Message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void InvalidateCurrentPlan()
    {
        if (_currentPlanSnapshot is null)
        {
            return;
        }

        _currentPlanSnapshot = null;
        _writeReportButton.Enabled = false;
        _applyButton.Enabled = false;
        _productsGrid.DataSource = null;
        _summaryLabel.Text = string.Empty;
        Log("Dry-run plan invalidated because source inputs changed.");
    }

    private void Log(string message)
    {
        _log.AppendText($"[{DateTimeOffset.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private sealed record ProductPreviewRow(
        int SourceRow,
        string ExternalId,
        string Name,
        string Slug,
        string CategorySlug,
        string PublishStatus,
        bool HasImage);

    private sealed record CatalogImportPlanSnapshot(
        CatalogImportPlan Plan,
        string SourcePath,
        string? ManifestPath);
}
