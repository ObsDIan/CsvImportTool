using System.Data;
using CsvImportTool.Models;

namespace CsvImportTool.Services;

public sealed class ValidationService
{
    public IReadOnlyList<ImportError> Validate(DataTable csvTable, IEnumerable<ColumnMapping> mappings)
    {
        var errors = new List<ImportError>();
        var enabledMappings = mappings.Where(x => x.IsEnabled).ToList();

        foreach (var mapping in mappings.Where(x => x.IsRequired))
        {
            var hasSource = !string.IsNullOrWhiteSpace(mapping.CsvColumn) || !string.IsNullOrWhiteSpace(mapping.FixedValue);
            if (!mapping.IsEnabled || !hasSource)
            {
                errors.Add(new ImportError
                {
                    RowNumber = 0,
                    ColumnName = mapping.DbColumn,
                    RawValue = string.Empty,
                    Message = "資料庫必填欄位尚未啟用或尚未設定 CSV 欄位 / 固定值。"
                });
            }
        }

        for (var rowIndex = 0; rowIndex < csvTable.Rows.Count; rowIndex++)
        {
            var row = csvTable.Rows[rowIndex];
            var displayRowNumber = rowIndex + 2; // CSV 第 1 列通常是標題列，所以資料第 1 筆是第 2 列。

            foreach (var mapping in enabledMappings)
            {
                var rawValue = ResolveRawValue(row, mapping);

                if (!DbValueConverter.TryConvert(mapping, rawValue, out _, out var errorMessage))
                {
                    errors.Add(new ImportError
                    {
                        RowNumber = displayRowNumber,
                        ColumnName = mapping.DbColumn,
                        RawValue = rawValue ?? string.Empty,
                        Message = errorMessage
                    });
                }
            }
        }

        return errors;
    }

    internal static string? ResolveRawValue(DataRow row, ColumnMapping mapping)
    {
        if (!string.IsNullOrWhiteSpace(mapping.FixedValue))
            return mapping.FixedValue;

        if (string.IsNullOrWhiteSpace(mapping.CsvColumn))
            return null;

        if (!row.Table.Columns.Contains(mapping.CsvColumn))
            return null;

        return row[mapping.CsvColumn]?.ToString();
    }
}
