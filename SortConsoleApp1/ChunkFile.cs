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
    //private readonly List<Row> _content = new List<Row>();
    private readonly string _separator;
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
        var readLines = File.ReadLines(inputPath);
        _lines = readLines.ToList();
    }

    public async Task ReadFromFileAsync(string inputPath)
    {
        _lines.Clear();
        var readLines = await File.ReadAllLinesAsync(inputPath);
        _lines = readLines.ToList();
    }

    private void LoadContent(string buffer)
    {
        _lines.Clear();
        _lines = buffer.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
    
    public void SortContent()
    {
        _sortingService.Sort(_lines, _separator);
    }

    public async Task SortContentAsync()
    {
        await _sortingService.SortAsync(_lines, _separator);
    }

    public void WriteToFile(string outPath)
    {
        File.WriteAllLines(outPath, _lines);
    }

    public async Task WriteToFileAsync(string outPath)
    {
        await File.WriteAllLinesAsync(outPath, _lines);

    }

    public void Dispose()
    {
        _lines.Clear();
        //_content.Clear();
        GC.Collect();
    }
}