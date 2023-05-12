using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

namespace SortConsoleApp1;

public class App
{
    private readonly IConfigurationRoot _config;
    private readonly ISortingProvider<Row> _sortingProvider;
    private string inputPath = "";
    private string notSortedFileExtension = "notsorted";
    private string sortedFileExtension = "sorted";
    private string columnSeparator = ". ";
    private long maxChunkFileSize = 1000;
    private string outputPath = "";

    public App(IConfigurationRoot config, ISortingProvider<Row> sortingProvider)
    {
        _config = config;
        _sortingProvider = sortingProvider;
        var inputPathValue = config.GetSection("InputFileOptions:Path").Value;
        if (inputPathValue != null) inputPath = inputPathValue;
        var columnSeparatorValue = config.GetSection("InputFileOptions:ColumnSeparator").Value;
        if (columnSeparatorValue != null) columnSeparator = columnSeparatorValue;
        var chunkNotSortedExtensionValue = config.GetSection("ChunkFileOptions:NotSortedExtension").Value;
        if (chunkNotSortedExtensionValue != null) notSortedFileExtension = chunkNotSortedExtensionValue;
        var chunkSortedExtensionValue = config.GetSection("ChunkFileOptions:SortedExtension").Value;
        if (chunkSortedExtensionValue != null) sortedFileExtension = chunkSortedExtensionValue;
        var maxSize = config.GetSection("ChunkFileOptions:MaxChunkFileSizeInMB").Value;
        if (maxSize != null) maxChunkFileSize = maxSize.ConvertToLong() * 1024L * 1024L; // in MB
        var outputPathValue = config.GetSection("OutputFileOptions:Path").Value;
        if (outputPathValue != null) outputPath = outputPathValue;
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
                var lines = new List<string>();
                long chunkFileSizeBytes = 0;
                while (!inputFile.EndOfStream && chunkFileSizeBytes < maxChunkFileSize)
                {
                    var line = await inputFile.ReadLineAsync();
                    if (line == null) continue;
                    lines.Add(line);
                    chunkFileSizeBytes += line.Length; // * sizeof(char);
                }
                var notSortedChunkFilePath = $"{chunkDirectory}\\{Path.GetFileNameWithoutExtension(inputPath)}_{chunkFileNumber++}.{notSortedFileExtension}";
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Not sorted chunk data ({chunkFileNumber}) created");
                await using var chunkFile = new ChunkFile(_config, _sortingProvider, lines);
                // Writing all lines to file
                await chunkFile.WriteToFile(notSortedChunkFilePath);
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} Not sorted chunk ({chunkFileNumber}) written to file {notSortedChunkFilePath}");
            }
            
            // Sorting chunk files
            Console.WriteLine();
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} Sorting chunk files");
            try
            {
                // Sorting
                // Creating chunk file readers
                var chunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals($".{notSortedFileExtension}")).ToList();

                async Task ParallelSort(string notSortedChunkFilePath)
                {
                    var sortedChunkFileName = Path.GetFileNameWithoutExtension(notSortedChunkFilePath);
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Sorting chunk file {sortedChunkFileName} started");
                    var sortedChunkFilePath = $"{chunkDirectory}\\{sortedChunkFileName}.{sortedFileExtension}";
                    var notSortedChunkFile = new ChunkFile(_config, _sortingProvider, new List<string>());
                    await notSortedChunkFile.ReadFromFile(notSortedChunkFilePath);
                    await notSortedChunkFile.SortContent();
                    await notSortedChunkFile.WriteToFile(sortedChunkFilePath);
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} Chunk file {sortedChunkFileName} has been sorted");
                }

                //var parallel = Parallel.ForEach(chunkFilePathList, ParallelSort);
                //await Task.Run(() => Parallel.ForEach(chunkFilePathList, ParallelSort));
                var sortTasks = new List<Task>();
                foreach (var notSortedChunkFilePath in chunkFilePathList)
                {
                    sortTasks.Add(ParallelSort(notSortedChunkFilePath));
                }
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
                await using var outputFileWriter = new OutputFileWriter(_config, _sortingProvider);
                await outputFileWriter.ProcessChunks();
                //await outputFileWriter.FlushAllAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss} ERROR: {ex.Message}, {ex.StackTrace}");
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
}