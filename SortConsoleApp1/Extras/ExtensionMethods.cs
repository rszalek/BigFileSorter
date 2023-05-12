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

}