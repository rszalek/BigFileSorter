using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class ChunkFile: IDisposable
{
    private readonly IConfiguration _config;
    private readonly ISortingService<string> _sortingService;
    private readonly List<Row> _content = new List<Row>();
    private readonly string _separator = ". ";
    private List<string> _lines = new List<string>();

    public ChunkFile(IConfiguration config, ISortingService<string> sortingService)
    {
        _config = config;
        _sortingService = sortingService;
        _separator = _config.GetSection("InputFileOptions").GetValue<string>("ColumnSeparator") ?? ". ";
    }

    public void ReadFromFile(string inputPath)
    {
        _lines.Clear();
        var readLines = File.ReadAllLines(inputPath);
        _lines = readLines.ToList();
    }

    public async Task ReadFromFileAsync(string inputPath)
    {
        _lines.Clear();
        var readLines = await File.ReadAllLinesAsync(inputPath);
        _lines = readLines.ToList();

        // using var reader = File.OpenText(inputPath);
        // while (!reader.EndOfStream)
        // {
        //     var line = await reader.ReadLineAsync();
        //     if (string.IsNullOrEmpty(line)) continue;
        //     _lines.Add(line);
        // }
        //var buffer = await reader.ReadToEndAsync();
        //LoadContent(buffer);
    }

    private void LoadContent(string buffer)
    {
        _lines.Clear();
        _lines = buffer.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
    
    public void SortContent()
    {
        // foreach (var cells in _lines.Select(line => line.Split(_separator)))
        // {
        //     if (cells.Length < 1) continue;
        //     _content.Add(new Row(cells[0].ConvertToLong(), cells[1]));
        // }
        // _sortingService.Sort(_content);
        // _lines.Clear();
        // foreach (var line in _content.Select(row => $"{row.Number}{_separator}{row.Text}"))
        // {
        //     _lines.Add(line);
        // }
        _sortingService.Sort(_lines, _separator);
    }

    public async Task SortContentAsync()
    {
        // foreach (var cells in _lines.Select(line => line.Split(_separator)))
        // {
        //     _content.Add(new Row(cells[0].ConvertToLong(), cells[1]));
        // }
        // await _sortingService.SortAsync(_content);
        // _lines.Clear();
        // foreach (var line in _content.Select(row => $"{row.Number}{_separator}{row.Text}"))
        // {
        //     _lines.Add(line);
        // }
        await _sortingService.SortAsync(_lines, _separator);
    }

    public void WriteToFile(string outPath)
    {
        File.WriteAllLines(outPath, _lines);
    }

    public async Task WriteToFileAsync(string outPath)
    {
        await File.WriteAllLinesAsync(outPath, _lines);
        
        
        // await using var writer = new StreamWriter(outPath); 
        // var buffer = new StringBuilder();
        // try
        // {
        //     foreach (var line in _lines)
        //     {
        //         buffer.AppendLine(line);
        //     }
        //     await writer.WriteLineAsync(buffer.ToString());
        // }
        // finally
        // {
        //     await writer.FlushAsync();
        //     buffer.Clear();
        //     writer.Close();
        // }
    }

    public void Dispose()
    {
        _lines.Clear();
        _content.Clear();
        GC.Collect();
    }
}