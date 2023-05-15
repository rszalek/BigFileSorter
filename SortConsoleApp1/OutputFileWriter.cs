using System.Text;
using Microsoft.Extensions.Configuration;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class OutputFileWriter: IAsyncDisposable
{
    private readonly IConfiguration _config;
    private readonly ISortingService<Row> _sortingService;
    private readonly StreamWriter _streamWriter;
    private StreamReader[] _readers;
    private Row?[] _chunkRows;

    public OutputFileWriter(IConfiguration config, ISortingService<Row> sortingService)
    {
        _config = config;
        _sortingService = sortingService;
        
        var outputPathValue = config.GetSection("OutputFileOptions:Path").Value;
        if (outputPathValue == null) return; //todo log or exception
        var outPath = string.Concat(Directory.GetCurrentDirectory(), outputPathValue);
        _streamWriter = File.CreateText(outPath);
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Output file initiated");
    }

    public async Task ProcessChunks()
    {
        var inputPathValue = _config.GetSection("InputFileOptions").GetValue<string>("Path");
        if (inputPathValue == null) return; //todo log or exception
        var columnSeparatorValue = _config.GetSection("InputFileOptions").GetValue<string>("ColumnSeparator");
        if (columnSeparatorValue == null) return; //todo log or exception;
        var chunkSortedExtensionValue = _config.GetSection("ChunkFileOptions").GetValue<string>("SortedExtension");
        if (chunkSortedExtensionValue == null)  return; //todo log or exception;

        var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(inputPathValue));

        // Creating chunk file readers
        var sortedChunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{chunkSortedExtensionValue}")).ToList();
        
        _readers = new StreamReader[sortedChunkFilePathList.Count];
        _chunkRows = new Row[sortedChunkFilePathList.Count];
        for (var i = 0; i < sortedChunkFilePathList.Count; i++)
        {
            _readers[i] = new StreamReader(sortedChunkFilePathList[i]);
            var line = await _readers[i].ReadLineAsync();
            if (line != null)
            {
                var cells = line.Split(columnSeparatorValue);
                _chunkRows[i] = new Row(cells[0].ConvertToLong(), cells[1]);
            }
            else
            {
                _chunkRows[i] = null;
            }
        }
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Sorted chunk files found: {sortedChunkFilePathList.Count}. Processing...");
        // Merging chunks into one output file
        var buffer = new StringBuilder();
        var maxOutBufferLines = 10000;
        var bufferIndex = 0;
        while (true)
        {
            var smallestIndex = -1;
            Row smallestRow = null;
            for (var index = 0; index < sortedChunkFilePathList.Count; index++)
            {
                if (_chunkRows[index] == null) continue;
                if (smallestRow != null && _sortingService.Comparison(_chunkRows[index], smallestRow) >= 0) continue;
                smallestIndex = index;
                smallestRow = _chunkRows[index];
            }
            if (smallestIndex == -1)
            {
                await WriteBufferAsync(buffer.ToString());
                buffer.Clear();
                break;
            }
            if (bufferIndex >= maxOutBufferLines)
            {
                await WriteBufferAsync(buffer.ToString());
                buffer.Clear();
                bufferIndex = 0;
                buffer = new StringBuilder();
                continue;
            }
            buffer.AppendLine($"{smallestRow.Number}{columnSeparatorValue}{smallestRow.Text}");
            // Read next line
            if (_readers[smallestIndex].EndOfStream)
            {
                _chunkRows[smallestIndex] = null;
                _readers[smallestIndex].Dispose();
                continue;
            }
            var line = await _readers[smallestIndex].ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;
            var cells = line.Split(columnSeparatorValue);
            _chunkRows[smallestIndex] = new Row(cells[0].ConvertToLong(), cells[1]);
            bufferIndex++;
        }
        Console.WriteLine();
    }

    private void DeleteUnsortedChunkFiles()
    {
        try
        {
            var inputPath = _config.GetSection("InputFileOptions").GetValue<string>("Path");
            var notSortedFileExtension = _config.GetSection("ChunkFileOptions").GetValue<string>("NotSortedExtension");
            var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(inputPath));
            var chunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{notSortedFileExtension}")).ToList();
            foreach (var path in chunkFilePathList)
            {
                var pathToDelete = Path.ChangeExtension(path, ".old");
                File.Move(path, pathToDelete);
                File.Delete(pathToDelete);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} ERROR: {ex.Message}, {ex.StackTrace}");
        }
    }

    private async Task WriteOneLineAsync(Row row, string separator)
    {
        var formattedLine = $"{row.Number}{separator}{row.Text}";
        await _streamWriter.WriteLineAsync(formattedLine);
    }

    private async Task WriteBufferAsync(string buffer)
    {
        await _streamWriter.WriteAsync(buffer);
    }

    public async Task FlushAllAsync()
    {
        await _streamWriter.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _streamWriter.DisposeAsync();
        DeleteUnsortedChunkFiles();
    }
}