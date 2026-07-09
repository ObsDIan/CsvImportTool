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
        csv.WriteRecords(errors);
    }
}
