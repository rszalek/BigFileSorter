namespace SortConsoleApp1;

public class OutputFileWriter: IDisposable
{
    private readonly StreamWriter _streamWriter;

    public OutputFileWriter(string path)
    {
        _streamWriter = File.CreateText(path);
    }

    public async Task WriteOneLineAsync(string text)
    {
        await _streamWriter.WriteLineAsync(text);
    }

    public async Task FlushAll()
    {
        await _streamWriter.FlushAsync();
    }

    public void Dispose()
    {
        _streamWriter.Dispose();
    }
}