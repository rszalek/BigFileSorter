using System.Text;
using Microsoft.Extensions.Configuration;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class OutputFileWriter: IAsyncDisposable
{
    private readonly IConfiguration _config;
    private readonly ISortingService<string> _sortingService;
    private readonly string _inputPath;
    private readonly string _outputPath;
    private readonly string _fullOutputPath;
    private readonly string _notSortedFileExtension;
    private readonly string _sortedFileExtension;
    private readonly int _mergeBufferMaxLines;
    private readonly int _mergeMaxFilesCount;
    private readonly int _outputBufferMaxLines;
    private readonly bool _deleteAfterSorting;
    private readonly int _iterationsAllowed;

    public OutputFileWriter(IConfiguration config, ISortingService<string> sortingService)
    {
        _config = config;
        _sortingService = sortingService;
        _inputPath = _config.GetSection("InputFileOptions").GetValue<string>("Path") ?? string.Empty;
        _outputPath = _config.GetSection("OutputFileOptions").GetValue<string>("Path") ?? string.Empty;
        _fullOutputPath = string.Concat(Directory.GetCurrentDirectory(), _outputPath);
        _sortedFileExtension = _config.GetSection("ChunkFileOptions").GetValue<string>("SortedExtension") ?? string.Empty;
        _notSortedFileExtension = _config.GetSection("ChunkFileOptions").GetValue<string>("NotSortedExtension") ?? string.Empty;
        _mergeBufferMaxLines = _config.GetSection("OutputFileOptions").GetValue<int>("MergeBufferMaxLines");
        _mergeMaxFilesCount = _config.GetSection("OutputFileOptions").GetValue<int>("MergeMaxFilesCount");
        _outputBufferMaxLines = _config.GetSection("OutputFileOptions").GetValue<int>("OutputBufferMaxLines");
        _deleteAfterSorting = _config.GetSection("ChunkFileOptions").GetValue<bool>("DeleteNotSortedChunks");
        _iterationsAllowed = _config.GetSection("OutputFileOptions").GetValue<int>("IterationsAllowed");
        if (File.Exists(_fullOutputPath))
        {
            File.Delete(_fullOutputPath);
        }
        
    }

    public async Task ProcessChunks()
    {
        var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(_inputPath));
        var sortedFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{_sortedFileExtension}")).ToList();
        var iterationLevel = 1;
        while (sortedFilePathList.Count > 1)
        {
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} Iteration {iterationLevel} started.");
            // 1. group files into chunks of 5
            var groupedPathLists = sortedFilePathList.Chunk(_mergeMaxFilesCount).ToList();
            // 2. loop on every group:
            var iterator = 0;
            foreach (var pathList in groupedPathLists)
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Merging {pathList.Length} files...");
                //   a) get 5 files from the list and merge to 1 file
                //   b) save the file as file_1.sorted
                await MergeFilesWithSorting(pathList, Path.Combine(chunkDirectory, $"file_{iterator++}_{DateTime.Now.ToString("yyMMddHHmmss")}.sorted"));
                //   c) rename already processed .sorted files to .tmp
                foreach (var processedPath in pathList)
                {
                    var tempPath = Path.ChangeExtension(processedPath, ".tmp");
                    File.Move(processedPath, tempPath);
                }
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Files merged.");
            }
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} Iteration {iterationLevel} ended.");
            if (_iterationsAllowed > 0 && _iterationsAllowed == iterationLevel)
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Stopped after iteration {iterationLevel}, according to the configuration.");
                break;
            }
            iterationLevel++;
            // 3. get sortedChunkFilePathList again, there will be 1 or more new file_x.sorted files now
            sortedFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{_sortedFileExtension}")).ToList();
            // 4. if still more than 1 .sorted files found, repeat the process
        }
        // 5. if only one file left, that means this is the final sorted file
        var sortedFilePath = sortedFilePathList.First();
        File.Move(sortedFilePath, _fullOutputPath);
        // 6. clean temporary files at the end
    }

    private async Task<Queue<string>> LoadInputBuffer(StreamReader reader, int numberOfLines)
    {
        var bufferQueue = new Queue<string>(numberOfLines);
        if (reader.EndOfStream) return bufferQueue;
        for (var i = 0; i < numberOfLines; i++)
        {
            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                bufferQueue.Enqueue(line);
            }
            if (reader.EndOfStream) break;
        }
        return bufferQueue;
    }
    
    private async Task MergeFilesWithSorting(string[] filePaths, string outputPath)
    {
        var inputBuffers = new Queue<string>[filePaths.Length]; //array of FIFO buffers
        var outputBuffer = new StringBuilder();
        var bufferIndex = 0;
        // Creating chunk file readers for not empty chunks
        var readers = filePaths.Select(chunkPath => new StreamReader(chunkPath)).Where(streamReader => !streamReader.EndOfStream).ToArray();
        var chunkFirstLines = new string[readers.Length];
        // Load x lines of content of files to a buffer
        for(var i = 0; i < readers.Length; i++)
        {
            // buffer x lines from input files
            inputBuffers[i] = await LoadInputBuffer(readers[i], _mergeBufferMaxLines);
            chunkFirstLines[i] = inputBuffers[i].Dequeue(); // get first items from the buffer queue
        }
        await using var writer = new StreamWriter(outputPath);
        try
        {
            while (true)
            {
                var smallestValueChunkIndex = -1;
                string? smallestValueRow = null;
                // Find smallest value from the first lines of chunks
                for (var chunkFileIndex = 0; chunkFileIndex < readers.Length; chunkFileIndex++)
                {
                    if (chunkFirstLines[chunkFileIndex] == "" && readers[chunkFileIndex] == StreamReader.Null) continue;
                    if (smallestValueRow != null && _sortingService.Comparison(chunkFirstLines[chunkFileIndex], smallestValueRow) >= 0) continue;
                    smallestValueRow = chunkFirstLines[chunkFileIndex];
                    smallestValueChunkIndex = chunkFileIndex;
                }
                if (smallestValueChunkIndex == -1 || smallestValueRow == null) // no more chunk data to compare
                {
                    break;
                }
                if (bufferIndex >= _outputBufferMaxLines)
                {
                    await writer.WriteAsync(outputBuffer);
                    outputBuffer.Clear();
                    bufferIndex = 0;
                    outputBuffer = new StringBuilder();
                    continue;
                }
                outputBuffer.AppendLine(smallestValueRow);
                // Refill the buffer with next lines from the chunk file
                if (inputBuffers[smallestValueChunkIndex].Count == 0)
                {
                    inputBuffers[smallestValueChunkIndex] = await LoadInputBuffer(readers[smallestValueChunkIndex], _mergeBufferMaxLines);
                }
                if (inputBuffers[smallestValueChunkIndex].Count == 0) // still empty = empty buffer (end of file)
                {
                    readers[smallestValueChunkIndex].Dispose();
                    readers[smallestValueChunkIndex] = StreamReader.Null;
                    chunkFirstLines[smallestValueChunkIndex] = "";
                    continue;
                }
                // Read next line from the chunk which had the smallest value
                chunkFirstLines[smallestValueChunkIndex] = inputBuffers[smallestValueChunkIndex].Dequeue();
                bufferIndex++;
            }
            //if still anything in the buffer
            if (outputBuffer.Length > 0)
            {
                await writer.WriteAsync(outputBuffer);
            }
        }
        finally
        {
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

    public async ValueTask DisposeAsync()
    {
        //await _streamWriter.DisposeAsync();
        if (_deleteAfterSorting)
        {
            DeleteNotSortedChunkFiles();
        }
    }
}