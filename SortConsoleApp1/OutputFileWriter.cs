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
    private List<StreamReader> _readers;
    private List<Row> _chunkRows;
    private string inputPath = "";
    private string outputPath = "";
    private string notSortedFileExtension = "notsorted";
    private string sortedFileExtension = "sorted";
    private bool deleteAfterSorting = true;

    public OutputFileWriter(IConfiguration config, ISortingService<Row> sortingService)
    {
        _config = config;
        _sortingService = sortingService;
        inputPath = _config.GetSection("InputFileOptions").GetValue<string>("Path") ?? string.Empty;
        outputPath = _config.GetSection("OutputFileOptions").GetValue<string>("Path") ?? string.Empty;
        var fullOutputPath = string.Concat(Directory.GetCurrentDirectory(), outputPath);
        sortedFileExtension = _config.GetSection("ChunkFileOptions").GetValue<string>("SortedExtension") ?? string.Empty;
        notSortedFileExtension = _config.GetSection("ChunkFileOptions").GetValue<string>("NotSortedExtension") ?? string.Empty;
        deleteAfterSorting = _config.GetSection("ChunkFileOptions").GetValue<bool>("DeleteNotSortedChunks");
        var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(inputPath));
        var sortedChunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{sortedFileExtension}")).ToList();
        _readers = new List<StreamReader>();
        // Creating chunk file readers for not empty chunks
        foreach (var streamReader in sortedChunkFilePathList.Select(chunkPath => new StreamReader(chunkPath)).Where(streamReader => !streamReader.EndOfStream))
        {
            _readers.Add(streamReader);
        }
        _chunkRows = new List<Row>();
        _streamWriter = File.CreateText(fullOutputPath);
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Output file initiated");
    }

    public async Task ProcessChunks()
    {
        var columnSeparatorValue = _config.GetSection("InputFileOptions").GetValue<string>("ColumnSeparator");
        if (columnSeparatorValue == null) return; //todo log or exception;
        var chunkSortedExtensionValue = _config.GetSection("ChunkFileOptions").GetValue<string>("SortedExtension");
        if (chunkSortedExtensionValue == null)  return; //todo log or exception;

        // Get Row values from the first lines of chunks
        foreach (var chunkReader in _readers)
        {
            var line = await chunkReader.ReadLineAsync();
            if (line == null) continue; // should not happen, because it means EndOfStream = true and these are filtered out
            var cells = line.Split(columnSeparatorValue);
            _chunkRows.Add(new Row(cells[0].ConvertToLong(), cells[1]));
        }

        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Sorted chunk files found: {_readers.Count}. Processing...");
        // Merging chunks into one output file
        var buffer = new StringBuilder();
        var maxOutBufferLines = 10000;
        var bufferIndex = 0;
        while (true)
        {
            var smallestValueChunkIndex = -1;
            Row? smallestValueRow = null;
            // Find smallest Row value from the first lines of chunks
            for (var chunkFileIndex = 0; chunkFileIndex < _readers.Count; chunkFileIndex++)
            {
                if (smallestValueRow != null && _sortingService.Comparison(_chunkRows[chunkFileIndex], smallestValueRow) >= 0) continue;
                smallestValueRow = _chunkRows[chunkFileIndex];
                smallestValueChunkIndex = chunkFileIndex;
            }
            if (smallestValueChunkIndex == -1 || smallestValueRow == null) // no more chunk data to compare
            {
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
            buffer.AppendLine($"{smallestValueRow.Number}{columnSeparatorValue}{smallestValueRow.Text}");
            // Read next line from the chunk which had the smallest value
            var line = "";
            do
            {
                line = await _readers[smallestValueChunkIndex].ReadLineAsync();
            } while (line == "");
            if (line == null) // end of file
            {
                _readers[smallestValueChunkIndex].Dispose();
                _readers.RemoveAt(smallestValueChunkIndex);
                _chunkRows.RemoveAt(smallestValueChunkIndex);
                continue;
            }
            var cells = line.Split(columnSeparatorValue);
            _chunkRows[smallestValueChunkIndex] = new Row(cells[0].ConvertToLong(), cells[1]);
            bufferIndex++;
        }
        // if still anything in the buffer
        if (buffer.Length > 0)
        {
            await WriteBufferAsync(buffer.ToString());
            buffer.Clear();
        }
        Console.WriteLine();
    }

    private void DeleteNotSortedChunkFiles()
    {
        try
        {
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
        if (deleteAfterSorting)
        {
            DeleteNotSortedChunkFiles();
        }
    }
}