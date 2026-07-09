namespace CsvImportTool.Models;

public sealed class TableInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;

    public string DisplayName => $"[{SchemaName}].[{TableName}]";

    public override string ToString() => DisplayName;
}
