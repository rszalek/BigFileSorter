using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class SortingProvider: ISortingProvider<Row>
{
    public async Task Sort(List<Row> inputList)
    {
        await Task.Run(() => { inputList.Sort(Comparison); });
    }

    public int Comparison(Row row1, Row row2)
    {
        var compare = string.Compare(row1.Text, row2.Text, StringComparison.Ordinal);
        if (compare != 0) return compare;
        if (row1.Number > row2.Number) return 1;
        if (row1.Number < row2.Number) return -1;
        return 0;
    }
}