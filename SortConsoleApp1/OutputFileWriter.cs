using Microsoft.Extensions.Configuration;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class OutputFileWriter: IAsyncDisposable
{
    private readonly StreamWriter _streamWriter;
    private IConfigurationRoot _config;
    private ISortingProvider<Row> _sortingProvider;
    private StreamReader[] _readers;
    private Row?[] _chunkRows;

    public OutputFileWriter(IConfigurationRoot config, ISortingProvider<Row> sortingProvider)
    {
        _config = config;
        _sortingProvider = sortingProvider;
        
        var outputPathValue = config.GetSection("OutputFileOptions:Path").Value;
        if (outputPathValue == null) return; //todo log or exception
        var outPath = string.Concat(Directory.GetCurrentDirectory(), outputPathValue);
        _streamWriter = File.CreateText(outPath);
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Output file initiated");
    }

    public async Task ProcessChunks()
    {
        var inputPathValue = _config.GetSection("InputFileOptions:Path").Value;
        if (inputPathValue == null) return; //todo log or exception
        var columnSeparatorValue = _config.GetSection("InputFileOptions:ColumnSeparator").Value;
        if (columnSeparatorValue == null) return; //todo log or exception;
        var chunkSortedExtensionValue = _config.GetSection("ChunkFileOptions:SortedExtension").Value;
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
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Sorted chunk files found: {sortedChunkFilePathList.Count}");
        // Merging chunks into one output file
        while (true)
        {
            var smallestIndex = -1;
            Row smallestRow = null;
            for (var index = 0; index < sortedChunkFilePathList.Count; index++)
            {
                if (_chunkRows[index] == null) continue;
                if (smallestRow != null && _sortingProvider.Comparison(_chunkRows[index], smallestRow) >= 0) continue;
                smallestIndex = index;
                smallestRow = _chunkRows[index];
            }
            if (smallestIndex == -1) break;
            await WriteOneLineAsync(smallestRow, columnSeparatorValue);
            // Read next line
            if (_readers[smallestIndex].EndOfStream)
            {
                _chunkRows[smallestIndex] = null;
                _readers[smallestIndex].Dispose();
                continue;
            }
            var line = await _readers[smallestIndex].ReadLineAsync();
            if (line == null) continue;
            var cells = line.Split(columnSeparatorValue);
            _chunkRows[smallestIndex] = new Row(cells[0].ConvertToLong(), cells[1]);
        }
    }

    private async Task WriteOneLineAsync(Row row, string separator)
    {
        var formattedLine = $"{row.Number}{separator}{row.Text}";
        await _streamWriter.WriteLineAsync(formattedLine);
    }

    public async Task FlushAllAsync()
    {
        await _streamWriter.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _streamWriter.DisposeAsync();
    }
}