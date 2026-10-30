using System.Text;
using CsvImportTool.Services;

namespace CsvImportTool.Tests;

public sealed class CsvReaderServiceTests
{
    [Fact]
    public void ReadCsv_NormalizesDuplicateAndEmptyHeaders()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "Name,Name,\nAlice,A.,value\n", new UTF8Encoding(false));

            var table = new CsvReaderService().ReadCsv(path);

            Assert.Equal(new[] { "Name", "Name_2", "未命名欄位" },
                table.Columns.Cast<System.Data.DataColumn>().Select(column => column.ColumnName));
            Assert.Single(table.Rows);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
