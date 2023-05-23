# BigFileSorter
## Big text file sorting tool application
This is a tool console application to sort very big text files consisted of 2 columns: Number. String

For example:
```
415. Apple
30432. Something something something
1. Apple
32. Cherry is the best
2. Banana is yellow
```
The result needs to be sorted. Sort criterion: the first part of String is compared, if it matches, then Number.
Expected result:
```
1. Apple
415. Apple
2. Banana is yellow
32. Cherry is the best
30432. Something something something
```
Sorting method: **External Merge Sort**

See: [External Merge Sort](https://en.wikipedia.org/wiki/External_sorting)

[Josef Ottosson's article](https://josef.codes/sorting-really-large-files-with-c-sharp/) was very helpful.

Type: **Console app.**

Framework: **.NET7**

Configuration file: **appsettings.json**

### Configuration parameters' description:

```
{
    "InputFileOptions": {
        "Path": "\\InputFiles\\TestFile.txt",           -- path to the input not sorted test file
        "ColumnSeparator": ". "                         -- separator for columns of data
    },
    "ChunkFileOptions": {
        "NotSortedExtension": "notsorted",              -- extension for temporary not sorted chunk files
        "SortedExtension": "sorted",                    --
        "MaxChunkFileSizeInMB": 20,                     --
        "RunSplittingModule": true,                     --
        "RunSortingModule": true,                       --
        "RunMergingModule": true,                       --
        "DeleteNotSortedChunks": false,                 --
        "DeleteSortedChunks": false                     --
    },
    "OutputFileOptions": {
        "Path": "\\OutputFiles\\SortedTestFile.txt",    --
        "ColumnSeparator": ". ",                        --
        "MergeBufferMaxLines": 1000000,                 --
        "MergeMaxFilesCount": 5,                        --
        "OutputBufferMaxLines": 1000000,                --
        "IterationsAllowed": -1                         --
    },
    "Sorting": {
        "TasksPerGroup": 16,                            --
        "InputBufferMaxLines": 100000                   --
    }
}
```
