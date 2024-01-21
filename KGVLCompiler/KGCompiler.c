#include <stdio.h>
#include "StringBuilder.h"
#include <stdbool.h>
#include <sys/stat.h>
#include "SourceParser.h"
#include <Windows.h>

// Macros.
#define MIN_ARG_COUNT 2


// Types.
typedef struct CompilationArgsStruct
{
    char* SourceDirectoryPath;
    char* DestinationDirectoryPath;
} CompilationArgs;


// Static functions.
static ErrorCode ReadCompilationArgs(CompilationArgs* argData, int argCount, char** args)
{
    argData->SourceDirectoryPath = NULL;
    argData->DestinationDirectoryPath = NULL;

    if (argCount < MIN_ARG_COUNT)
    {
        return Errors_SetError(ErrorCode_CommandLineArguments, 
            "No arguments found. Usage: <source directory path> <destination directory path (optional)>");
    }

    char* Path = args[1];
    argData->SourceDirectoryPath = args[1];
    argData->DestinationDirectoryPath = args[1];

    DWORD SourceDirAttributes = GetFileAttributesA(argData->SourceDirectoryPath);
    if (!(SourceDirAttributes & FILE_ATTRIBUTE_DIRECTORY))
    {
        return Errors_SetError(ErrorCode_CommandLineArguments, "Source path is not a directory.");
    }

    if (MIN_ARG_COUNT + 1 <= argCount)
    {
        argData->DestinationDirectoryPath = args[2];
        SourceDirAttributes = GetFileAttributesA(argData->DestinationDirectoryPath);
        if (!(SourceDirAttributes & FILE_ATTRIBUTE_DIRECTORY))
        {
            return Errors_SetError(ErrorCode_CommandLineArguments, "Destination path is not a directory.");
        }
    }

    return ErrorCode_Success;
}




// Functions.
int main(int argCount, char** args)
{
    Errors_InitErrorHandling();

    // Read args.
    CompilationArgs ArgData;
    if (ReadCompilationArgs(&ArgData, argCount, args) != ErrorCode_Success)
    {
        printf(Errors_GetLastErrorMessage());
        return 0;
    }

    // Parse source files.
    SourceFileCollection SourceFiles;
    SourceFile_ParseAllFiles(&SourceFiles, ArgData.SourceDirectoryPath);

    // Filter and optimize.


    // Compile.



    // Exit.
    return 0;
}