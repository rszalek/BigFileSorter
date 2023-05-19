using System.Text;
using Microsoft.Extensions.Configuration;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class OutputFileWriter: IAsyncDisposable
{
    private readonly IConfiguration _config;
    private readonly ISortingService<string> _sortingService;
    private readonly StreamWriter _streamWriter;
    private readonly string _inputPath;
    private readonly string _outputPath;
    private readonly string _notSortedFileExtension;
    private readonly string _sortedFileExtension;
    private readonly int _outputBufferMaxLines;
    private readonly bool _deleteAfterSorting;
    //private List<StreamReader> _readers;

    public OutputFileWriter(IConfiguration config, ISortingService<string> sortingService)
    {
        _config = config;
        _sortingService = sortingService;
        _inputPath = _config.GetSection("InputFileOptions").GetValue<string>("Path") ?? string.Empty;
        _outputPath = _config.GetSection("OutputFileOptions").GetValue<string>("Path") ?? string.Empty;
        var fullOutputPath = string.Concat(Directory.GetCurrentDirectory(), _outputPath);
        _sortedFileExtension = _config.GetSection("ChunkFileOptions").GetValue<string>("SortedExtension") ?? string.Empty;
        _notSortedFileExtension = _config.GetSection("ChunkFileOptions").GetValue<string>("NotSortedExtension") ?? string.Empty;
        _outputBufferMaxLines = _config.GetSection("OutputFileOptions").GetValue<int>("OutputBufferMaxLines");
        _deleteAfterSorting = _config.GetSection("ChunkFileOptions").GetValue<bool>("DeleteNotSortedChunks");
        // _readers = new List<StreamReader>();
        // // Creating chunk file readers for not empty chunks
        // foreach (var streamReader in sortedChunkFilePathList.Select(chunkPath => new StreamReader(chunkPath)).Where(streamReader => !streamReader.EndOfStream))
        // {
        //     _readers.Add(streamReader);
        // }
        _streamWriter = File.CreateText(fullOutputPath);
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Output file initiated");
    }

    public async Task ProcessChunks()
    {
        var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(_inputPath));
        var sortedFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{_sortedFileExtension}")).ToList();
        while (sortedFilePathList.Count > 1)
        {
            // 1. group files into chunks of 5
            var groupedPathLists = sortedFilePathList.Chunk(5).ToList();
            // 2. loop on every group:
            var iterator = 0;
            foreach (var pathList in groupedPathLists)
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Merging {pathList.Length} files...");
                //   a) get 5 files from the list and merge to 1 file
                //   b) save the file as file_1.sorted
                await MergeFilesWithSorting(pathList, Path.Combine(chunkDirectory, $"file_{iterator++}.sorted"));
                //   c) delete already processed .sorted files
                foreach (var processedPath in pathList)
                {
                    var tempPath = Path.ChangeExtension(processedPath, ".tmp");
                    File.Move(processedPath, tempPath);
                    //File.Delete(tempPath);
                }
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Files merged.");
            }
            // 3. get sortedChunkFilePathList again, there will be 1 or more new file_x.sorted files now
            sortedFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{_sortedFileExtension}")).ToList();
            // 4. if still more than 1 .sorted files found, repeat the process
        }
    }

    private async Task MergeFilesWithSorting(string[] filePaths, string outputPath)
    {
        var inputBuffers = new List<string>(filePaths.Length);
        var outputBuffer = new StringBuilder();
        var bufferIndex = 0;
        // Creating chunk file readers for not empty chunks
        var readers = filePaths.Select(chunkPath => new StreamReader(chunkPath)).Where(streamReader => !streamReader.EndOfStream).ToList();
        await using var writer = new StreamWriter(outputPath);
        var chunkFirstLines = new List<string>(readers.Count);
        // Load all content of files to memory
        foreach (var chunkReader in readers)
        {
            var inputContent = await chunkReader.ReadToEndAsync();
            inputBuffers.Add(inputContent);
        }
        // Get first lines of chunk files
        chunkFirstLines.AddRange(
            from content in inputBuffers
            let firstNewLineIndex = content.IndexOf(Environment.NewLine, StringComparison.Ordinal)
            select content.Substring(0, firstNewLineIndex));

        try
        {
            while (true)
            {
                var smallestValueChunkIndex = -1;
                string? smallestValueRow = null;
                // Find smallest value from the first lines of chunks
                for (var chunkFileIndex = 0; chunkFileIndex < readers.Count; chunkFileIndex++)
                {
                    if (smallestValueRow != null && _sortingService.Comparison(chunkFirstLines[chunkFileIndex], smallestValueRow) >= 0) continue;
                    smallestValueRow = chunkFirstLines[chunkFileIndex];
                    smallestValueChunkIndex = chunkFileIndex;
                }
                if (smallestValueChunkIndex == -1 || smallestValueRow == null) // no more chunk data to compare
                {
                    break;
                }
                // if (bufferIndex >= _outputBufferMaxLines)
                // {
                //     await writer.WriteLineAsync(buffer);
                //     //await File.AppendAllTextAsync(outputPath, buffer.ToString());
                //     buffer.Clear();
                //     bufferIndex = 0;
                //     buffer = new StringBuilder();
                //     continue;
                // }
                outputBuffer.AppendLine(smallestValueRow);
                // Read next line from the chunk which had the smallest value
                var line = "";
                do
                {
                    line = await readers[smallestValueChunkIndex].ReadLineAsync();
                } while (line == "");
                if (line == null) // end of file
                {
                    readers[smallestValueChunkIndex].Dispose();
                    readers.RemoveAt(smallestValueChunkIndex);
                    chunkFirstLines.RemoveAt(smallestValueChunkIndex);
                    continue;
                }
                chunkFirstLines[smallestValueChunkIndex] = line;
                bufferIndex++;
            }
            // if still anything in the buffer
            // if (buffer.Length > 0)
            // {
            //     await writer.WriteAsync(buffer);
            //     //await File.AppendAllTextAsync(outputPath, buffer.ToString());
            // }
        }
        finally
        {
            //await writer.FlushAsync();
            await writer.WriteAsync(outputBuffer);
            outputBuffer.Clear();
            writer.Close();
        }
    }
    
    private void DeleteNotSortedChunkFiles()
    {
        try
        {
            var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(_inputPath));
            var chunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{_notSortedFileExtension}")).ToList();
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
        if (_deleteAfterSorting)
        {
            DeleteNotSortedChunkFiles();
        }
    }
}