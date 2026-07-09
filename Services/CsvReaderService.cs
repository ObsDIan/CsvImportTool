using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace CsvImportTool.Services;

public sealed class CsvReaderService
{
    public DataTable ReadCsv(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("找不到 CSV 檔案。", filePath);

        var encoding = DetectEncoding(filePath);

        using var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            BadDataFound = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        });

        if (!csv.Read())
            throw new InvalidOperationException("CSV 沒有任何資料列。  ");

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        if (headers.Length == 0)
            throw new InvalidOperationException("CSV 找不到標題列，請確認第一列是否為欄位名稱。  ");

        var table = new DataTable(Path.GetFileNameWithoutExtension(filePath));

        foreach (var header in NormalizeDuplicateHeaders(headers))
            table.Columns.Add(header, typeof(string));

        while (csv.Read())
        {
            var row = table.NewRow();

            for (var i = 0; i < table.Columns.Count; i++)
            {
                row[i] = csv.GetField(i) ?? string.Empty;
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private static Encoding DetectEncoding(string filePath)
    {
        var bom = new byte[4];
        using (var fs = File.OpenRead(filePath))
        {
            fs.Read(bom, 0, bom.Length);
        }

        if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        if (bom[0] == 0xFF && bom[1] == 0xFE)
            return Encoding.Unicode;

        if (bom[0] == 0xFE && bom[1] == 0xFF)
            return Encoding.BigEndianUnicode;

        try
        {
            var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            _ = utf8Strict.GetString(File.ReadAllBytes(filePath));
            return utf8Strict;
        }
        catch (DecoderFallbackException)
        {
            // 台灣常見舊系統 CSV 可能是 Big5 / CP950。
            return Encoding.GetEncoding(950);
        }
    }

    private static IEnumerable<string> NormalizeDuplicateHeaders(IEnumerable<string> headers)
    {
        var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawHeader in headers)
        {
            var header = string.IsNullOrWhiteSpace(rawHeader) ? "未命名欄位" : rawHeader.Trim();

            if (!used.TryAdd(header, 1))
            {
                used[header]++;
                header = $"{header}_{used[header]}";
            }

            yield return header;
        }
    }
}
