using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class App
{
    private readonly IConfiguration _config;
    private readonly ISortingService<Row> _sortingService;
    private string inputPath = "";
    private string notSortedFileExtension = "notsorted";
    private string sortedFileExtension = "sorted";
    private string columnSeparator = ". ";
    private long maxChunkFileSize = 1000;
    private string outputPath = "";

    public App(IConfiguration config, ISortingService<Row> sortingService)
    {
        _config = config;
        _sortingService = sortingService;
        inputPath = config.GetSection("InputFileOptions").GetValue<string>("Path") ?? string.Empty;
        columnSeparator = config.GetSection("InputFileOptions").GetValue<string>("ColumnSeparator") ?? string.Empty;
        notSortedFileExtension = config.GetSection("ChunkFileOptions").GetValue<string>("NotSortedExtension") ?? string.Empty;
        sortedFileExtension = config.GetSection("ChunkFileOptions").GetValue<string>("SortedExtension") ?? string.Empty;
        maxChunkFileSize = config.GetSection("ChunkFileOptions").GetValue<long>("MaxChunkFileSizeInMB") * 1024L * 1024L;
        outputPath = config.GetSection("OutputFileOptions").GetValue<string>("Path") ?? string.Empty;
    }

    public async Task Run()
    {
        Console.WriteLine($"{DateTime.Now:hh:mm:ss} Creating chunk files:");
        try
        {
            var inPath = string.Concat(Directory.GetCurrentDirectory(), inputPath);
            var outPath = string.Concat(Directory.GetCurrentDirectory(), outputPath);
            var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(inputPath));

            // Splitting large input file into smaller files (chunks)
            using var inputFile = new StreamReader(inPath);
            var chunkFileNumber = 0;
            while (!inputFile.EndOfStream)
            {
                var readBuffer = new StringBuilder();
                long chunkFileSizeBytes = 0;
                while (!inputFile.EndOfStream && chunkFileSizeBytes < maxChunkFileSize)
                {
                    var line = await inputFile.ReadLineAsync();
                    if (line == null) continue;
                    //lines.Add(line);
                    readBuffer.AppendLine(line);
                    chunkFileSizeBytes += line.Length; // * sizeof(char);
                }
                var notSortedChunkFileName = $"{Path.GetFileNameWithoutExtension(inputPath)}_{chunkFileNumber++}.{notSortedFileExtension}";
                var notSortedChunkFilePath = Path.Combine(chunkDirectory, notSortedChunkFileName);
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Not sorted chunk data ({chunkFileNumber}) created");
                await using var chunkFile = new StreamWriter(notSortedChunkFilePath);
                // Writing all lines to file
                await chunkFile.WriteAsync(readBuffer);
                await chunkFile.FlushAsync();
                //lines.Clear();
                readBuffer.Clear();
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Not sorted chunk ({chunkFileNumber}) written to file {notSortedChunkFileName}");
            }
            
            // Sorting chunk files
            Console.WriteLine();
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} Sorting chunk files");
            try
            {
                async Task ParallelSort(string notSortedChunkFilePath)
                {
                    var sortedChunkFileName = Path.GetFileNameWithoutExtension(notSortedChunkFilePath);
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Sorting chunk file {sortedChunkFileName} started");
                    var sortedChunkFilePath = $"{chunkDirectory}\\{sortedChunkFileName}.{sortedFileExtension}";
                    await using var notSortedChunkFile = new ChunkFile(_config, _sortingService);
                    await notSortedChunkFile.ReadFromFileAsync(notSortedChunkFilePath);
                    await notSortedChunkFile.SortContent();
                    await notSortedChunkFile.WriteToFileAsync(sortedChunkFilePath);
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Chunk file {sortedChunkFileName} has been sorted");
                }

                // Multi threaded sorting action
                var chunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{notSortedFileExtension}")).ToList();
                var sortTasks = chunkFilePathList.Select(ParallelSort).ToList();
                await Task.WhenAll(sortTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} ERROR: {ex.Message}, {ex.StackTrace}");
            }
            
            // Merging sorted chunk files
            Console.WriteLine();
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} Merging sorted chunk files");
            try
            {
                await using var outputFileWriter = new OutputFileWriter(_config, _sortingService);
                await outputFileWriter.ProcessChunks();
                //await outputFileWriter.FlushAllAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} ERROR: {ex.Message}, {ex.StackTrace}");
            }
            finally
            {
                DeleteSortedChunkFiles();
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
    
    private void DeleteSortedChunkFiles()
    {
        try
        {
            var inputPath = _config.GetSection("InputFileOptions").GetValue<string>("Path");
            var sortedFileExtension = _config.GetSection("ChunkFileOptions").GetValue<string>("SortedExtension");
            var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(inputPath));
            var chunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{sortedFileExtension}")).ToList();
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