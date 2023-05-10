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
    private string chunkFileExtension = "";
    private string columnSeparator = "";
    private long maxChunkFileSize = 0;
    private string outputPath = "";

    public App(IConfigurationRoot config, ISortingProvider<Row> sortingProvider)
    {
        _config = config;
        _sortingProvider = sortingProvider;
        var inputPathValue = config.GetSection("InputFileOptions:Path").Value;
        if (inputPathValue != null) inputPath = inputPathValue;
        var columnSeparatorValue = config.GetSection("InputFileOptions:ColumnSeparator").Value;
        if (columnSeparatorValue != null) columnSeparator = columnSeparatorValue;
        var chunkExtensionValue = config.GetSection("ChunkFileOptions:Extension").Value;
        if (chunkExtensionValue != null) chunkFileExtension = chunkExtensionValue;
        var maxSize = config.GetSection("ChunkFileOptions:MaxChunkFileSizeInMB").Value;
        if (maxSize != null) maxChunkFileSize = maxSize.ConvertToLong() * 1024L * 1024L; // in MB
        var outputPathValue = config.GetSection("OutputFileOptions:Path").Value;
        if (outputPathValue != null) outputPath = outputPathValue;
    }

    public async Task Run()
    {
        try
        {
            var inPath = string.Concat(Directory.GetCurrentDirectory(), inputPath);
            var outPath = string.Concat(Directory.GetCurrentDirectory(), outputPath);
            var chunkDirectory = string.Concat(Directory.GetCurrentDirectory(), Path.GetDirectoryName(inputPath));
            
            // Splitting large input file into smaller files (chunks)
            var chunkFilePathList = new List<string>();
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
                     chunkFileSizeBytes += line.Length;// * sizeof(char);
                 }
                 var chunkFilePath = $"{chunkDirectory}\\{Path.GetFileNameWithoutExtension(inputPath)}_{chunkFileNumber++}.{chunkFileExtension}";
                 using var chunkFile = new ChunkFile(_config, _sortingProvider);
                 // Getting lines
                 chunkFile.Lines = lines;
                 // Sorting
                 chunkFile.SortContent();
                 // Writing all lines to file
                 await chunkFile.WriteToFile(chunkFilePath); 
                 chunkFilePathList.Add(chunkFilePath);
             }
            
            // Merging chunks into one output file
            //var chunkFilePathList = Directory.GetFiles(chunkDirectory).Where(file => Path.GetExtension(file).Equals(".chunk")).ToList();
            await using var outputFile = new StreamWriter(outPath);
            var readers = new StreamReader[chunkFilePathList.Count];
            var chunkLines = new string[chunkFilePathList.Count];

            for (var i = 0; i < chunkFilePathList.Count; i++)
            {
                readers[i] = new StreamReader(chunkFilePathList[i]);
                chunkLines[i] = await readers[i].ReadLineAsync();
            }

            // while (true)
            // {
            //     var smallestIndex = -1;
            //     var smallestLine = string.Empty;
            //     for (var index = 0; index < chunkFilePathList.Count; index++)
            //     {
            //         if (smallestLine.Length > 0 && string.CompareOrdinal(chunkLines[index], smallestLine) >= 0) continue;
            //         smallestIndex = index;
            //         smallestLine = chunkLines[index];
            //     }
            //     if (smallestIndex == -1) break;
            //     var textValue = smallestLine.Substring(0, smallestLine.Length - 24);
            //     var numberValue = smallestLine.Substring(smallestLine.Length - 24).ConvertToLong();
            //     var formattedLine = $"{numberValue}{columnSeparator}{textValue}";
            //     await outputFile.WriteLineAsync(formattedLine);
            //     chunkLines[smallestIndex] = await readers[smallestIndex].ReadLineAsync();
            //     if (chunkLines[smallestIndex] == null)
            //     {
            //         readers[smallestIndex].Dispose();
            //         //todo uncomment File.Delete(chunkFilePathList[smallestIndex]);
            //     }
            // }
            /*var outputFileWriter = new OutputFileWriter(outPath);
            try
            {

                await outputFileWriter.FlushAll();
            }
            finally
            {
                outputFileWriter.Dispose();
            }*/
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}, {ex.StackTrace}");
        }
    }
}