using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class ChunkFile: IDisposable
{
    private IConfigurationRoot _config;
    private ISortingProvider<Row> _sortingProvider;
    private readonly List<Row> _content = new List<Row>();
    private string _separator = ". ";

    public List<string> Lines { get; set; }

    public ChunkFile(IConfigurationRoot config, ISortingProvider<Row> sortingProvider)
    {
        _config = config;
        _sortingProvider = sortingProvider;
        
        var columnSeparatorValue = _config.GetSection("InputFileOptions:ColumnSeparator").Value;
        if (columnSeparatorValue != null) _separator = columnSeparatorValue;
    }

    public async Task ReadAllLines(string path)
    {
        using var fileReader = File.OpenText(path);
        while (!fileReader.EndOfStream)
        {
            var line = await fileReader.ReadLineAsync();
            if (line == null) continue;
            var cells = line.Split(_separator);
            _content.Add(new Row(cells[0].ConvertToLong(), cells[1]));
        }
    }
    
    public void SortContent()
    {
        foreach (var line in Lines)
        {
            var cells = line.Split(_separator);
            _content.Add(new Row(cells[0].ConvertToLong(), cells[1]));
        }
        _sortingProvider.Sort(_content);
    }

    public async Task WriteToFile(string path)
    {
        await using var fileWriter = File.CreateText(path);
        // write lines in a special format [Text]|[0..0Number] to be able to treat every line as text for sorting
        foreach (var line in _content.Select(row => $"{row.Text}|{row.Number}"))
        {
            await fileWriter.WriteLineAsync(line);
        }
    }
    
    public void Dispose()
    {
        _content.Clear();
    }
}