using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using CsvImportTool.Models;
using CsvImportTool.Services;
using Microsoft.Win32;

namespace CsvImportTool;

public partial class MainWindow : Window
{
    private readonly CsvReaderService _csvReaderService = new();
    private readonly DatabaseSchemaService _databaseSchemaService = new();
    private readonly MappingService _mappingService = new();
    private readonly ValidationService _validationService = new();
    private readonly ImportService _importService = new();
    private readonly ErrorExportService _errorExportService = new();

    private readonly ObservableCollection<ColumnMapping> _mappings = new();
    private readonly ObservableCollection<ImportError> _errors = new();
    private List<string> _csvHeaders = new();
    private DataTable? _csvTable;

    public MainWindow()
    {
        InitializeComponent();
        MappingGrid.ItemsSource = _mappings;
        ErrorsGrid.ItemsSource = _errors;
    }

    private async void LoadTables_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            var connectionString = GetConnectionString();
            SetStatus("正在讀取資料表...");

            var tables = await _databaseSchemaService.GetTablesAsync(connectionString);
            TablesComboBox.ItemsSource = tables;

            if (tables.Count > 0)
                TablesComboBox.SelectedIndex = 0;

            SetStatus($"已讀取 {tables.Count} 張資料表。  ");
        });
    }

    private async void LoadColumns_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            var connectionString = GetConnectionString();
            var table = GetSelectedTable();

            SetStatus($"正在讀取 {table.DisplayName} 欄位...");

            var columns = await _databaseSchemaService.GetColumnsAsync(connectionString, table);

            _mappings.Clear();
            foreach (var column in columns)
                _mappings.Add(ColumnMapping.FromDbColumn(column));

            AutoMapIfReady();

            SetStatus($"已讀取 {table.DisplayName} 共 {columns.Count} 個欄位。Identity / Computed 欄位預設不匯入。  ");
        });
    }

    private void BrowseCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "選擇 CSV 檔案",
            Filter = "CSV 檔案 (*.csv)|*.csv|所有檔案 (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
            CsvPathTextBox.Text = dialog.FileName;
    }

    private async void LoadCsv_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(CsvPathTextBox.Text))
                throw new InvalidOperationException("請先選擇 CSV 檔案。  ");

            SetStatus("正在讀取 CSV...");

            _csvTable = _csvReaderService.ReadCsv(CsvPathTextBox.Text);
            _csvHeaders = _csvTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToList();

            CsvPreviewGrid.ItemsSource = _csvTable.DefaultView;
            CsvColumnComboColumn.ItemsSource = new[] { string.Empty }.Concat(_csvHeaders).ToList();

            AutoMapIfReady();
            ResetProgress();

            SetStatus($"已讀取 CSV：{_csvTable.Rows.Count} 筆資料，{_csvTable.Columns.Count} 個欄位。  ");
            return Task.CompletedTask;
        });
    }

    private void AutoMap_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureCsvLoaded();
            EnsureMappingsLoaded();

            AutoMapIfReady();
            SetStatus("已依照欄位名稱嘗試自動對應，請仍然人工確認一次。  ");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void Validate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ValidateAndDisplayErrors();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            var connectionString = GetConnectionString();
            var table = GetSelectedTable();
            var csvTable = EnsureCsvLoaded();
            EnsureMappingsLoaded();

            var errors = ValidateAndDisplayErrors(showSuccessMessage: false);
            if (errors.Count > 0)
            {
                SetStatus($"驗證失敗，共 {errors.Count} 筆錯誤。請修正後再匯入。  ");
                MessageBox.Show(this, "驗證失敗，資料尚未匯入。請查看「驗證錯誤」頁籤。", "無法匯入", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                this,
                $"確認要將 {csvTable.Rows.Count} 筆資料 Insert 到 {table.DisplayName}？\n\n第一版採整批交易：任一筆失敗會全部 Rollback。",
                "確認匯入",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            ImportProgressBar.Minimum = 0;
            ImportProgressBar.Maximum = csvTable.Rows.Count;
            ImportProgressBar.Value = 0;
            ProgressTextBlock.Text = $"0 / {csvTable.Rows.Count}";

            var progress = new Progress<int>(done =>
            {
                ImportProgressBar.Value = done;
                ProgressTextBlock.Text = $"{done} / {csvTable.Rows.Count}";
            });

            SetStatus("正在匯入資料...");
            var summary = await _importService.InsertAsync(connectionString, table, csvTable, _mappings, progress);

            SetStatus($"匯入完成：成功 {summary.SuccessRows} 筆，失敗 {summary.FailedRows} 筆。  ");
            MessageBox.Show(this, $"匯入完成：成功 {summary.SuccessRows} 筆。", "匯入完成", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    private void ExportErrors_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_errors.Count == 0)
            {
                MessageBox.Show(this, "目前沒有錯誤資料可匯出。", "無錯誤", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "匯出錯誤 CSV",
                Filter = "CSV 檔案 (*.csv)|*.csv",
                FileName = $"ImportErrors_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            _errorExportService.ExportErrors(dialog.FileName, _errors);
            SetStatus($"已匯出錯誤資料：{dialog.FileName}");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private IReadOnlyList<ImportError> ValidateAndDisplayErrors(bool showSuccessMessage = true)
    {
        var csvTable = EnsureCsvLoaded();
        EnsureMappingsLoaded();

        var errors = _validationService.Validate(csvTable, _mappings);

        _errors.Clear();
        foreach (var error in errors)
            _errors.Add(error);

        if (errors.Count == 0)
        {
            SetStatus($"驗證通過，共 {csvTable.Rows.Count} 筆資料可匯入。  ");
            if (showSuccessMessage)
                MessageBox.Show(this, "驗證通過，可以匯入。", "驗證成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            SetStatus($"驗證失敗，共 {errors.Count} 筆錯誤。  ");
            if (showSuccessMessage)
                MessageBox.Show(this, "驗證失敗，請查看「驗證錯誤」頁籤。", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return errors;
    }

    private void AutoMapIfReady()
    {
        if (_csvHeaders.Count == 0 || _mappings.Count == 0)
            return;

        _mappingService.AutoMap(_mappings, _csvHeaders);
        MappingGrid.Items.Refresh();
    }

    private string GetConnectionString()
    {
        var connectionString = ConnectionStringTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("請輸入 SQL Server 連線字串。  ");

        return connectionString;
    }

    private TableInfo GetSelectedTable()
    {
        if (TablesComboBox.SelectedItem is not TableInfo table)
            throw new InvalidOperationException("請先選擇資料表。  ");

        return table;
    }

    private DataTable EnsureCsvLoaded()
    {
        if (_csvTable is null)
            throw new InvalidOperationException("請先讀取 CSV。  ");

        return _csvTable;
    }

    private void EnsureMappingsLoaded()
    {
        if (_mappings.Count == 0)
            throw new InvalidOperationException("請先讀取資料庫欄位並設定欄位對應。  ");
    }

    private async Task RunUiActionAsync(Func<Task> action)
    {
        try
        {
            SetUiEnabled(false);
            await action();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetUiEnabled(true);
        }
    }

    private void SetUiEnabled(bool isEnabled)
    {
        ConnectionStringTextBox.IsEnabled = isEnabled;
        TablesComboBox.IsEnabled = isEnabled;
        CsvPathTextBox.IsEnabled = isEnabled;
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private void ResetProgress()
    {
        ImportProgressBar.Value = 0;
        ImportProgressBar.Maximum = 100;
        ProgressTextBlock.Text = "0 / 0";
    }

    private void ShowError(Exception ex)
    {
        SetStatus($"發生錯誤：{ex.Message}");
        MessageBox.Show(this, ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
