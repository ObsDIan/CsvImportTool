using CsvImportTool.Models;
using Microsoft.Data.SqlClient;

namespace CsvImportTool.Services;

public sealed class DatabaseSchemaService
{
    public async Task<IReadOnlyList<TableInfo>> GetTablesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var result = new List<TableInfo>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_SCHEMA, TABLE_NAME;
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TableInfo
            {
                SchemaName = reader.GetString(0),
                TableName = reader.GetString(1)
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<DbColumnInfo>> GetColumnsAsync(
        string connectionString,
        TableInfo table,
        CancellationToken cancellationToken = default)
    {
        var result = new List<DbColumnInfo>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                s.name AS SchemaName,
                t.name AS TableName,
                c.name AS ColumnName,
                ty.name AS DataType,
                c.max_length AS MaxLength,
                c.precision AS NumericPrecision,
                c.scale AS NumericScale,
                c.is_nullable AS IsNullable,
                c.is_identity AS IsIdentity,
                c.is_computed AS IsComputed,
                CASE WHEN pk.column_id IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS IsPrimaryKey,
                CASE WHEN dc.object_id IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasDefault,
                c.column_id AS OrdinalPosition
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            INNER JOIN sys.columns c ON t.object_id = c.object_id
            INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
            LEFT JOIN (
                SELECT ic.object_id, ic.column_id
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic
                    ON i.object_id = ic.object_id
                    AND i.index_id = ic.index_id
                WHERE i.is_primary_key = 1
            ) pk
                ON c.object_id = pk.object_id
                AND c.column_id = pk.column_id
            WHERE s.name = @SchemaName
              AND t.name = @TableName
            ORDER BY c.column_id;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SchemaName", table.SchemaName);
        command.Parameters.AddWithValue("@TableName", table.TableName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DbColumnInfo
            {
                SchemaName = reader.GetString(0),
                TableName = reader.GetString(1),
                ColumnName = reader.GetString(2),
                DataType = reader.GetString(3),
                MaxLength = reader.GetInt16(4),
                NumericPrecision = reader.GetByte(5),
                NumericScale = reader.GetByte(6),
                IsNullable = reader.GetBoolean(7),
                IsIdentity = reader.GetBoolean(8),
                IsComputed = reader.GetBoolean(9),
                IsPrimaryKey = reader.GetBoolean(10),
                HasDefault = reader.GetBoolean(11),
                OrdinalPosition = reader.GetInt32(12)
            });
        }

        return result;
    }
}
