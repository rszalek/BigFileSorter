namespace SortConsoleApp1.Interfaces;

public interface ISortingService<T>
{
    public Task Sort(List<T> inputList);

    public int Comparison(T row1, T item2);
    
    
}

