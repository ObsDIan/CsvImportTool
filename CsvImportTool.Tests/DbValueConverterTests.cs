using CsvImportTool.Models;
using CsvImportTool.Services;

namespace CsvImportTool.Tests;

public sealed class DbValueConverterTests
{
    [Fact]
    public void TextTypes_DoNotUsePointerLengthAsCharacterLimit()
    {
        var mapping = Mapping("ntext", maxLength: 16);

        var success = DbValueConverter.TryConvert(mapping, new string('字', 100), out var value, out var error);

        Assert.True(success, error);
        Assert.Equal(new string('字', 100), value);
    }

    [Theory]
    [InlineData("123.45", true)]
    [InlineData("1234.56", false)]
    [InlineData("12.345", false)]
    public void Decimal_ValidatesPrecisionAndScale(string raw, bool expected)
    {
        var mapping = Mapping("decimal", precision: 5, scale: 2);

        var success = DbValueConverter.TryConvert(mapping, raw, out _, out _);

        Assert.Equal(expected, success);
    }

    [Theory]
    [InlineData("yes", true)]
    [InlineData("否", false)]
    public void Bit_AcceptsSupportedLocalizedValues(string raw, bool expected)
    {
        var mapping = Mapping("bit");

        var success = DbValueConverter.TryConvert(mapping, raw, out var value, out var error);

        Assert.True(success, error);
        Assert.Equal(expected, value);
    }

    private static ColumnMapping Mapping(
        string dataType,
        int maxLength = 0,
        byte precision = 0,
        int scale = 0) =>
        new()
        {
            DbColumn = "Value",
            DataType = dataType,
            MaxLength = maxLength,
            NumericPrecision = precision,
            NumericScale = scale
        };
}
