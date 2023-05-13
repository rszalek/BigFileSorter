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

    public ChunkFile(IConfigurationRoot config, ISortingProvider<Row> sortingProvider)
    {
        _config = config;
        _sortingProvider = sortingProvider;
        var columnSeparatorValue = _config.GetSection("InputFileOptions:ColumnSeparator").Value;
        if (columnSeparatorValue != null) _separator = columnSeparatorValue;
    }

    public async Task ReadFromFileAsync(string inputPath)
    {
        using var reader = File.OpenText(inputPath);
        var buffer = await reader.ReadToEndAsync();
        LoadContent(buffer);
    }
    
    public void LoadContent(string buffer)
    {
        _lines.Clear();
        _lines = buffer.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
    
    public async Task SortContent()
    {
        foreach (var cells in _lines.Select(line => line.Split(_separator)))
        {
            _content.Add(new Row(cells[0].ConvertToLong(), cells[1]));
        }
        await _sortingProvider.Sort(_content);
        _lines.Clear();
        foreach (var line in _content.Select(row => $"{row.Number}{_separator}{row.Text}"))
        {
            _lines.Add(line);
        }
    }

    public async Task WriteToFileAsync(string outPath)
    {
        await using var writer = new StreamWriter(outPath); 
        var buffer = new StringBuilder();
        try
        {
            foreach (var line in _lines)
            {
                buffer.AppendLine(line);
            }
            await writer.WriteLineAsync(buffer.ToString());
        }
        finally
        {
            buffer.Clear();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _lines.Clear();
        _content.Clear();
    }
}