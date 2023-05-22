using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1.Services;

internal sealed class LineSortingService: ISortingService<string>
{
    private string _separator = ". ";
    public void Sort(List<string> inputList, string columnSeparator)
    {
        _separator = columnSeparator;
        var comparer = new TwoColumnsComparer(columnSeparator);
        inputList.Sort(comparer);
    }

    public async Task SortAsync(List<string> inputList, string columnSeparator = "")
    {
        var comparer = new TwoColumnsComparer(columnSeparator);
        await Task.Run(() => { inputList.Sort(comparer); });
    }

    public int Comparison(string row1, string row2)
    {
        var line1Cells = row1.Split(_separator);
        var line2Cells = row2.Split(_separator);
        var compare = string.Compare(line1Cells[1], line2Cells[1], StringComparison.Ordinal);
        if (compare != 0) return compare;
        var line1Col0Value = line1Cells[0].ConvertToLong();
        var line2Col0Value = line2Cells[0].ConvertToLong();
        if (line1Col0Value > line2Col0Value) return 1;
        if (line1Col0Value < line2Col0Value) return -1;
        return 0;
    }
    
    
}