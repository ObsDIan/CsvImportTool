namespace CsvImportTool.Models;

public sealed class ImportSummary
{
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows { get; set; }
}
