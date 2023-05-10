namespace SortConsoleApp1.Interfaces;

public interface ISortingProvider<T>
{
    public void Sort(List<T> inputList);

    public int Comparison(T row1, T item2);
    
    
}

