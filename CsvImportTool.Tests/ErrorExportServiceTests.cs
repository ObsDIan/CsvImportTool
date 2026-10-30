using CsvImportTool.Models;
using CsvImportTool.Services;

namespace CsvImportTool.Tests;

public sealed class ErrorExportServiceTests
{
    [Fact]
    public void ExportErrors_EscapesSpreadsheetFormulas()
    {
        var path = Path.GetTempFileName();
        try
        {
            new ErrorExportService().ExportErrors(path,
            [
                new ImportError
                {
                    RowNumber = 2,
                    ColumnName = "Name",
                    RawValue = "=2+2",
                    Message = "@danger"
                }
            ]);

            var output = File.ReadAllText(path);

            Assert.Contains("'=2+2", output);
            Assert.Contains("'@danger", output);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
