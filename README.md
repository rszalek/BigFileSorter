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
        "SortedExtension": "sorted",                    -- extension for temporary sorted chunk files
        "MaxChunkFileSizeInMB": 20,                     -- maximum average size of a not sorted chunk file
        "RunSplittingModule": true,                     -- first step module of the process to be run or not (splitting the input file into not sorted chunks)
        "RunSortingModule": true,                       -- second step module of the process to be run or not (sorting all chunk files)
        "RunMergingModule": true,                       -- third step module of the process to be run or not (merging sorted chunk files into output file, using K-way merging algorithm)
        "DeleteNotSortedChunks": false,                 -- do you want to delete not sorted files after the process?
        "DeleteSortedChunks": false                     -- do you want to delete sorted files after the process?
    },
    "OutputFileOptions": {
        "Path": "\\OutputFiles\\SortedTestFile.txt",    -- path to the output sorted file
        "ColumnSeparator": ". ",                        -- separator used for the output file
        "MergeBufferMaxLines": 1000000,                 -- maximum lines buffered when merging chunk files, before they are saved to a disk
        "MergeMaxFilesCount": 5,                        -- maximum count of files used to be merged on one merging iteration level
        "OutputBufferMaxLines": 1000000,                -- maximum lines buffered when building output file, before it is saved to a disk
        "IterationsAllowed": -1                         -- (for testing purpose) number of iterations after which the program stops merging files, -1 means "not used"
    },
    "Sorting": {
        "TasksPerGroup": 16,                            -- number of simultenaous tasks (multithreading) used to sort chunk files
        "InputBufferMaxLines": 100000                   -- max lines buffered per chunk file when sorting it
    }
}
```
