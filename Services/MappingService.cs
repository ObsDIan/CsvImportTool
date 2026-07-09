using System.Text.RegularExpressions;
using CsvImportTool.Models;

namespace CsvImportTool.Services;

public sealed class MappingService
{
    public void AutoMap(IEnumerable<ColumnMapping> mappings, IEnumerable<string> csvHeaders)
    {
        var headerList = csvHeaders.ToList();
        var normalizedLookup = headerList
            .GroupBy(NormalizeName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            if (!mapping.IsEnabled)
                continue;

            var normalizedDbColumn = NormalizeName(mapping.DbColumn);

            if (normalizedLookup.TryGetValue(normalizedDbColumn, out var exactHeader))
            {
                mapping.CsvColumn = exactHeader;
                continue;
            }

            var containsHeader = headerList.FirstOrDefault(header =>
                NormalizeName(header).Contains(normalizedDbColumn, StringComparison.OrdinalIgnoreCase) ||
                normalizedDbColumn.Contains(NormalizeName(header), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(containsHeader))
                mapping.CsvColumn = containsHeader;
        }
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim().ToLowerInvariant();
        return Regex.Replace(text, "[^a-z0-9\u4e00-\u9fff]", string.Empty);
    }
}
