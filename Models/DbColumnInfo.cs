namespace CsvImportTool.Models;

public sealed class DbColumnInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int MaxLength { get; set; }
    public byte NumericPrecision { get; set; }
    public int NumericScale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsComputed { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool HasDefault { get; set; }
    public int OrdinalPosition { get; set; }

    public bool IsRequired => !IsNullable && !IsIdentity && !IsComputed && !HasDefault;

    public string LengthDisplay
    {
        get
        {
            if (MaxLength == -1)
                return "MAX";

            return DataType.ToLowerInvariant() switch
            {
                "nvarchar" or "nchar" => (MaxLength / 2).ToString(),
                "varchar" or "char" or "binary" or "varbinary" => MaxLength.ToString(),
                _ => string.Empty
            };
        }
    }
}
