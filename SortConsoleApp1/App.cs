using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class App
{
    private readonly IConfiguration _config;
    private readonly ISortingService<string> _sortingService;
    private readonly string _inputPath;
    private readonly string _notSortedFileExtension;
    private readonly string _sortedFileExtension;
    private readonly long _maxChunkFileSize;
    private readonly bool _runSplittingModule;
    private readonly bool _runSortingModule;
    private readonly bool _runMergingModule;
    private readonly bool _deleteNotSortedFiles;
    private readonly bool _deleteSortedFiles;
    private readonly int _tasksPerGroup;

    public App(IConfiguration config, ISortingService<string> sortingService)
    {
        _config = config;
        _sortingService = sortingService;
        _inputPath = config.GetSection("InputFileOptions").GetValue<string>("Path") ?? string.Empty;
        _notSortedFileExtension = config.GetSection("ChunkFileOptions").GetValue<string>("NotSortedExtension") ?? string.Empty;
        _sortedFileExtension = config.GetSection("ChunkFileOptions").GetValue<string>("SortedExtension") ?? string.Empty;
        _maxChunkFileSize = config.GetSection("ChunkFileOptions").GetValue<long>("MaxChunkFileSizeInMB") * 1024L * 1024L;
        _runSplittingModule = config.GetSection("ChunkFileOptions").GetValue<bool>("RunSplittingModule");
        _runSortingModule = config.GetSection("ChunkFileOptions").GetValue<bool>("RunSortingModule");
        _runMergingModule = config.GetSection("ChunkFileOptions").GetValue<bool>("RunMergingModule");
        _deleteNotSortedFiles = config.GetSection("ChunkFileOptions").GetValue<bool>("DeleteNotSortedChunks");
        _deleteSortedFiles = config.GetSection("ChunkFileOptions").GetValue<bool>("DeleteSortedChunks");
        _tasksPerGroup = config.GetSection("Sorting").GetValue<int>("TasksPerGroup");
    }

    public async Task Run()
    {
        try
        {
            var inPath = string.Concat(Directory.GetCurrentDirectory(), _inputPath);
            var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(_inputPath));

            if (_runSplittingModule) // for testing purpose
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss}--- Splitting module ---");
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Creating chunk files:");
                // Splitting large input file into smaller files (chunks)
                using var inputFile = new StreamReader(inPath);
                var chunkFileNumber = 0;
                while (!inputFile.EndOfStream)
                {
                    var readBuffer = new StringBuilder();
                    long chunkFileSizeBytes = 0;
                    while (!inputFile.EndOfStream && chunkFileSizeBytes < _maxChunkFileSize)
                    {
                        var line = await inputFile.ReadLineAsync();
                        if (line == null) continue;
                        readBuffer.AppendLine(line);
                        chunkFileSizeBytes += line.Length; // * sizeof(char);
                    }
                    var notSortedChunkFileName = $"{Path.GetFileNameWithoutExtension(_inputPath)}_{chunkFileNumber++}.{_notSortedFileExtension}";
                    var notSortedChunkFilePath = Path.Combine(chunkDirectory, notSortedChunkFileName);
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Not sorted chunk data ({chunkFileNumber}) created");
                    await using var writeStream = File.OpenWrite(notSortedChunkFilePath);
                    await using var notSortedChunkFile = new StreamWriter(writeStream, bufferSize: 512);
                    // Writing all lines to file
                    await notSortedChunkFile.WriteAsync(readBuffer);
                    await notSortedChunkFile.FlushAsync();
                    readBuffer.Clear();
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Not sorted chunk ({chunkFileNumber}) written to file {notSortedChunkFileName}");
                }
            }
            Console.WriteLine();
            if (_runSortingModule) // for testing purpose
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} --- Sorting module ---");
                try
                {
                    void ParallelSort(string notSortedChunkFilePath)
                    {
                        var sortedChunkFileName = Path.GetFileNameWithoutExtension(notSortedChunkFilePath);
                        var sortedChunkFilePath = $"{chunkDirectory}\\{sortedChunkFileName}.{_sortedFileExtension}";
                        using var notSortedChunkFile = new ChunkFile(_config, _sortingService);
                        notSortedChunkFile.ReadFromFile(notSortedChunkFilePath);
                        notSortedChunkFile.SortContent();
                        notSortedChunkFile.WriteToFile(sortedChunkFilePath);
                    }

                    // Multi threaded sorting action
                    var chunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{_notSortedFileExtension}")).ToList();
                    var sortTasks = new List<Task>();
                    foreach (var chunkPath in chunkFilePathList)
                    {
                        var task = new Task(() => ParallelSort(chunkPath));
                        sortTasks.Add(task);
                    }
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Tasks to be run: {sortTasks.Count}");
                    // Split tasks into groups of [tasksPerGroup]
                    var sortTaskGroups = sortTasks.Chunk(_tasksPerGroup).ToList();
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Processing {sortTaskGroups.Count} groups of tasks");
                    var groupIndex = 1;
                    foreach (var sortTaskGroup in sortTaskGroups)
                    {
                        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Sorting group Id={groupIndex} of {sortTaskGroup.Length} tasks started");
                        foreach (var task in sortTaskGroup)
                        {
                            task.Start();
                        }
                        await Task.WhenAll(sortTaskGroup);
                        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Sorting group Id={groupIndex} ended");
                        groupIndex++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} ERROR: {ex.Message}, {ex.StackTrace}");
                }
            }
            Console.WriteLine();
            if (_runMergingModule) // for testing purpose
            {
                // Merging sorted chunk files
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Merging sorted chunk files");
                try
                {
                    await using var outputFileWriter = new OutputFileWriter(_config, _sortingService);
                    await outputFileWriter.ProcessChunks();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} ERROR: {ex.Message}, {ex.StackTrace}");
                }
                finally
                {
                    if (_deleteNotSortedFiles)
                    {
                        DeleteChunkFiles(_notSortedFileExtension);
                    }
                    if (_deleteSortedFiles)
                    {
                        DeleteChunkFiles(_sortedFileExtension);
                        DeleteChunkFiles("tmp");
                    }
                }
            }
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} Output file created.");
            Console.WriteLine($"Press any key to exit.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} ERROR: {ex.Message}, {ex.StackTrace}");
        }
    }

    private void DeleteChunkFiles(string extension)
    {
        try
        {
            var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(_inputPath));
            var chunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{extension}")).ToList();
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
}