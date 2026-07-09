namespace CsvImportTool.Models;

public sealed class ImportError
{
    public int RowNumber { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string RawValue { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
