namespace SortConsoleApp1.Interfaces;

public interface ISortingService<T>
{
    public void Sort(List<T> inputList);
    public Task SortAsync(List<T> inputList);

    public int Comparison(T row1, T item2);
    
    
}

