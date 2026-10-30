using System.Globalization;
using CsvImportTool.Models;

namespace CsvImportTool.Services;

public static class DbValueConverter
{
    public static bool TryConvert(ColumnMapping mapping, string? rawValue, out object value, out string errorMessage)
    {
        value = DBNull.Value;
        errorMessage = string.Empty;

        var text = rawValue?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            if (mapping.IsRequired)
            {
                errorMessage = "必填欄位不可空白。";
                return false;
            }

            return true;
        }

        var type = mapping.DataType.ToLowerInvariant();

        try
        {
            switch (type)
            {
                case "nvarchar":
                case "varchar":
                case "nchar":
                case "char":
                case "text":
                case "ntext":
                case "xml":
                    if (!CheckStringLength(mapping, text, out errorMessage))
                        return false;
                    value = text;
                    return true;

                case "int":
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                    {
                        value = intValue;
                        return true;
                    }
                    break;

                case "bigint":
                    if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                    {
                        value = longValue;
                        return true;
                    }
                    break;

                case "smallint":
                    if (short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var shortValue))
                    {
                        value = shortValue;
                        return true;
                    }
                    break;

                case "tinyint":
                    if (byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var byteValue))
                    {
                        value = byteValue;
                        return true;
                    }
                    break;

                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue) ||
                        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimalValue))
                    {
                        if (!CheckDecimalPrecision(mapping, decimalValue, out errorMessage))
                            return false;

                        value = decimalValue;
                        return true;
                    }
                    break;

                case "float":
                    if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue) ||
                        double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out doubleValue))
                    {
                        value = doubleValue;
                        return true;
                    }
                    break;

                case "real":
                    if (float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatValue) ||
                        float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out floatValue))
                    {
                        value = floatValue;
                        return true;
                    }
                    break;

                case "bit":
                    if (TryParseBoolean(text, out var boolValue))
                    {
                        value = boolValue;
                        return true;
                    }
                    break;

                case "date":
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                    if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dateTimeValue) ||
                        DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeValue))
                    {
                        value = dateTimeValue;
                        return true;
                    }
                    break;

                case "datetimeoffset":
                    if (DateTimeOffset.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dateTimeOffsetValue) ||
                        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeOffsetValue))
                    {
                        value = dateTimeOffsetValue;
                        return true;
                    }
                    break;

                case "time":
                    if (TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out var timeValue) ||
                        TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out timeValue))
                    {
                        value = timeValue;
                        return true;
                    }
                    break;

                case "uniqueidentifier":
                    if (Guid.TryParse(text, out var guidValue))
                    {
                        value = guidValue;
                        return true;
                    }
                    break;

                case "binary":
                case "varbinary":
                case "image":
                    errorMessage = "第一版暫不支援二進位欄位匯入。";
                    return false;

                default:
                    // 無法辨識的 SQL 型別先以字串交給 SQL Server 嘗試處理。
                    value = text;
                    return true;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        errorMessage = $"值 '{text}' 無法轉換成資料型別 {mapping.DataType}。";
        return false;
    }

    private static bool CheckStringLength(ColumnMapping mapping, string text, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (mapping.MaxLength <= 0 || mapping.MaxLength == -1 ||
            mapping.DataType.Equals("text", StringComparison.OrdinalIgnoreCase) ||
            mapping.DataType.Equals("ntext", StringComparison.OrdinalIgnoreCase) ||
            mapping.DataType.Equals("xml", StringComparison.OrdinalIgnoreCase))
            return true;

        var maxChars = mapping.DataType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                       mapping.DataType.Equals("nchar", StringComparison.OrdinalIgnoreCase)
            ? mapping.MaxLength / 2
            : mapping.MaxLength;

        if (text.Length <= maxChars)
            return true;

        errorMessage = $"文字長度 {text.Length} 超過欄位上限 {maxChars}。";
        return false;
    }

    private static bool CheckDecimalPrecision(ColumnMapping mapping, decimal value, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (mapping.NumericPrecision == 0)
            return true;

        var normalized = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
        var parts = normalized.Split('.');
        var integerDigits = parts[0].TrimStart('0').Length;
        var fractionDigits = parts.Length > 1 ? parts[1].TrimEnd('0').Length : 0;
        var maxIntegerDigits = mapping.NumericPrecision - mapping.NumericScale;

        if (integerDigits > maxIntegerDigits)
        {
            errorMessage = $"整數位數 {integerDigits} 超過欄位上限 {maxIntegerDigits}。";
            return false;
        }

        if (fractionDigits > mapping.NumericScale)
        {
            errorMessage = $"小數位數 {fractionDigits} 超過欄位上限 {mapping.NumericScale}。";
            return false;
        }

        return true;
    }

    private static bool TryParseBoolean(string text, out bool value)
    {
        value = false;

        if (bool.TryParse(text, out value))
            return true;

        switch (text.Trim().ToLowerInvariant())
        {
            case "1":
            case "y":
            case "yes":
            case "是":
            case "真":
            case "啟用":
                value = true;
                return true;
            case "0":
            case "n":
            case "no":
            case "否":
            case "假":
            case "停用":
                value = false;
                return true;
            default:
                return false;
        }
    }
}
