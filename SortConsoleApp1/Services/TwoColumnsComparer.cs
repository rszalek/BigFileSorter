using SortConsoleApp1.Extras;

namespace SortConsoleApp1.Services;

class TwoColumnsComparer : IComparer<string>
{
    private readonly string _separator;
    private int _separatorLength;

    public TwoColumnsComparer(string separator)
    {
        _separator = separator;
        _separatorLength = separator.Length;
    }
    
    public int Compare(string? firstLine, string? secondLine)
    {
        var indexOfFirstLineSeparator = firstLine?.IndexOf(_separator, StringComparison.Ordinal) ?? 0;
        var indexOfSecondLineSeparator = secondLine?.IndexOf(_separator, StringComparison.Ordinal) ?? 0;
        // Getting second column
        var firstLineCol2 = firstLine?.Substring(indexOfFirstLineSeparator + _separatorLength) ?? string.Empty;
        var secondLineCol2 = secondLine?.Substring(indexOfSecondLineSeparator + _separatorLength) ?? string.Empty;
        var result = string.Compare(firstLineCol2, secondLineCol2, StringComparison.Ordinal);
        if (result != 0) return result;
        // And now the first column
        var firstLineCol1 = firstLine?.Substring(0, indexOfFirstLineSeparator) ?? string.Empty;
        var secondLineCol1 = secondLine?.Substring(0, indexOfSecondLineSeparator) ?? string.Empty;
        var firstLineCol1Value = firstLineCol1.ConvertToLong();
        var secondLineCol1Value = secondLineCol1.ConvertToLong();
        if (firstLineCol1Value > secondLineCol1Value) return 1;
        if (firstLineCol1Value < secondLineCol1Value) return -1;
        return 0;
    }
}