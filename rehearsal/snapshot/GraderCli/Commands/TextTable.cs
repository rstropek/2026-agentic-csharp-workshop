using System.Globalization;

namespace GraderCli.Commands;

/// <summary>
/// Writes rows as a table with aligned columns for the text output. Values are formatted culture-invariant, missing values
/// as <c>-</c>.
/// </summary>
internal static class TextTable
{
    private const string ColumnSeparator = "  ";

    public static void Write(TextWriter output, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        var cells = rows.Select(row => row.Select(Format).ToList()).ToList();
        if (cells.Count == 0)
        {
            output.WriteLine("(none)");
            return;
        }

        var widths = headers.Select((header, column) => Math.Max(header.Length, cells.Max(row => row[column].Length))).ToList();
        WriteRow(output, headers, widths);
        foreach (var row in cells)
        {
            WriteRow(output, row, widths);
        }
    }

    private static string Format(object? value) => value is null ? "-" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "-";

    private static void WriteRow(TextWriter output, IReadOnlyList<string> row, List<int> widths) =>
        output.WriteLine(string.Join(ColumnSeparator, row.Select((cell, column) => cell.PadRight(widths[column]))).TrimEnd());
}
