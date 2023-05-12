using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class ChunkFile: IAsyncDisposable
{
    private IConfigurationRoot _config;
    private ISortingProvider<Row> _sortingProvider;
    private readonly List<Row> _content = new List<Row>();
    private readonly string _separator = ". ";
    private List<string> _lines;

    public ChunkFile(IConfigurationRoot config, ISortingProvider<Row> sortingProvider, IList<string> lines)
    {
        _config = config;
        _sortingProvider = sortingProvider;
        _lines = lines.ToList();

        var columnSeparatorValue = _config.GetSection("InputFileOptions:ColumnSeparator").Value;
        if (columnSeparatorValue != null) _separator = columnSeparatorValue;
    }

    public async Task ReadFromFile(string path)
    {
        using var fileReader = File.OpenText(path);
        var allData = await fileReader.ReadToEndAsync();
        _lines = allData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
    
    public async Task SortContent()
    {
        foreach (var line in _lines)
        {
            var cells = line.Split(_separator);
            _content.Add(new Row(cells[0].ConvertToLong(), cells[1]));
        }
        await _sortingProvider.Sort(_content);
        _lines.Clear();
        foreach (var line in _content.Select(row => $"{row.Number}{_separator}{row.Text}"))
        {
            _lines.Add(line);
        }
    }

    public async Task WriteToFile(string path)
    {
        var buffer = new StringBuilder();
        await using var fileWriter = File.CreateText(path);
        foreach (var line in _lines)
        {
            buffer.AppendLine(line);
        }
        await fileWriter.WriteAsync(buffer.ToString());
        await fileWriter.FlushAsync();
        await fileWriter.DisposeAsync();
    }

    public ValueTask DisposeAsync()
    {
        _content.Clear();
        return default;
    }
}