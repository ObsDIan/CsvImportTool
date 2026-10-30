using System.Globalization;
using System.IO;
using CsvHelper;
using CsvImportTool.Models;

namespace CsvImportTool.Services;

public sealed class ErrorExportService
{
    public void ExportErrors(string filePath, IEnumerable<ImportError> errors)
    {
        using var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteField(nameof(ImportError.RowNumber));
        csv.WriteField(nameof(ImportError.ColumnName));
        csv.WriteField(nameof(ImportError.RawValue));
        csv.WriteField(nameof(ImportError.Message));
        csv.NextRecord();

        foreach (var error in errors)
        {
            csv.WriteField(error.RowNumber);
            csv.WriteField(EscapeSpreadsheetFormula(error.ColumnName));
            csv.WriteField(EscapeSpreadsheetFormula(error.RawValue));
            csv.WriteField(EscapeSpreadsheetFormula(error.Message));
            csv.NextRecord();
        }
    }

    internal static string EscapeSpreadsheetFormula(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        return value[0] is '=' or '+' or '-' or '@'
            ? $"'{value}"
            : value;
    }
}
