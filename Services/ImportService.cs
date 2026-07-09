using System.Data;
using CsvImportTool.Models;
using Microsoft.Data.SqlClient;

namespace CsvImportTool.Services;

public sealed class ImportService
{
    public async Task<ImportSummary> InsertAsync(
        string connectionString,
        TableInfo table,
        DataTable csvTable,
        IEnumerable<ColumnMapping> mappings,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var selectedMappings = mappings
            .Where(x => x.IsEnabled && !x.IsIdentity && !x.IsComputed)
            .Where(x => !string.IsNullOrWhiteSpace(x.CsvColumn) || !string.IsNullOrWhiteSpace(x.FixedValue))
            .ToList();

        if (selectedMappings.Count == 0)
            throw new InvalidOperationException("沒有任何可匯入欄位，請先設定欄位對應。  ");

        var columnSql = string.Join(", ", selectedMappings.Select(x => QuoteName(x.DbColumn)));
        var parameterSql = string.Join(", ", selectedMappings.Select((_, index) => $"@p{index}"));
        var insertSql = $"INSERT INTO {QuoteName(table.SchemaName)}.{QuoteName(table.TableName)} ({columnSql}) VALUES ({parameterSql});";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var successRows = 0;

            for (var rowIndex = 0; rowIndex < csvTable.Rows.Count; rowIndex++)
            {
                var row = csvTable.Rows[rowIndex];

                await using var command = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
                command.CommandTimeout = 120;

                for (var columnIndex = 0; columnIndex < selectedMappings.Count; columnIndex++)
                {
                    var mapping = selectedMappings[columnIndex];
                    var rawValue = ValidationService.ResolveRawValue(row, mapping);

                    if (!DbValueConverter.TryConvert(mapping, rawValue, out var value, out var errorMessage))
                    {
                        throw new InvalidOperationException($"第 {rowIndex + 2} 列欄位 {mapping.DbColumn} 驗證失敗：{errorMessage}");
                    }

                    command.Parameters.AddWithValue($"@p{columnIndex}", value == DBNull.Value ? DBNull.Value : value);
                }

                await command.ExecuteNonQueryAsync(cancellationToken);
                successRows++;
                progress?.Report(successRows);
            }

            await transaction.CommitAsync(cancellationToken);

            return new ImportSummary
            {
                TotalRows = csvTable.Rows.Count,
                SuccessRows = successRows,
                FailedRows = 0
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string QuoteName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("SQL 名稱不可為空。", nameof(name));

        return $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
