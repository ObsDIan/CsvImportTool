using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CsvImportTool.Models;

public sealed class ColumnMapping : INotifyPropertyChanged
{
    private bool _isEnabled;
    private string? _csvColumn;
    private string? _fixedValue;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public string DbColumn { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string LengthDisplay { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsRequired { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsComputed { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool HasDefault { get; set; }
    public int MaxLength { get; set; }
    public byte NumericPrecision { get; set; }
    public int NumericScale { get; set; }

    public string? CsvColumn
    {
        get => _csvColumn;
        set => SetField(ref _csvColumn, value);
    }

    public string? FixedValue
    {
        get => _fixedValue;
        set => SetField(ref _fixedValue, value);
    }

    public static ColumnMapping FromDbColumn(DbColumnInfo column)
    {
        return new ColumnMapping
        {
            IsEnabled = !column.IsIdentity && !column.IsComputed,
            DbColumn = column.ColumnName,
            DataType = column.DataType,
            LengthDisplay = column.LengthDisplay,
            IsNullable = column.IsNullable,
            IsRequired = column.IsRequired,
            IsIdentity = column.IsIdentity,
            IsComputed = column.IsComputed,
            IsPrimaryKey = column.IsPrimaryKey,
            HasDefault = column.HasDefault,
            MaxLength = column.MaxLength,
            NumericPrecision = column.NumericPrecision,
            NumericScale = column.NumericScale
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
