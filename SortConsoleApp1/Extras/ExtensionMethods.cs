namespace SortConsoleApp1.Extras;

public static class ExtensionMethods
{
    //https://cc.davelozinski.com/c-sharp/fastest-way-to-convert-a-string-to-an-int
    public static long ConvertToLong(this string str)
    {
        long y = 0;
        foreach (var t in str)
        {
            y = y * 10 + (t - '0');
        }
        return y;
    }
    
    public static int Comparison(Row row1, Row row2)
    {
        var compare = string.Compare(row1.Text, row2.Text, StringComparison.Ordinal);
        if (compare != 0) return compare;
        if (row1.Number > row2.Number) return 1;
        if (row1.Number < row2.Number) return -1;
        return 0;
    }
}