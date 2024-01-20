#include "KGFile.h"
#include "KGErrors.h"
#include "KGMemory.h"
#include <string.h>


// Macros.
#define EXTRA_FILE_ERROR_MESSAGE_BYTES 32


// Static functions.
static void SetErrorForFile(const char* message, const char* path)
{
	size_t RequiredMemory = strlen(message) + strlen(path) + EXTRA_FILE_ERROR_MESSAGE_BYTES;
	char Message = (char*)Memory_SafeMalloc(RequiredMemory);
	sprintf(Message, "%s File: \"%s\"", message, path);

	Errors_SetError(ErrorCode_IO, Message);
	Memory_Free(Message);
}


// Functions.
char* File_ReadAllText(const char* path)
{
	FILE* File = fopen(path, "r");
	if (!File)
	{
		SetErrorForFile("Failed to open file!", path);
		return NULL;
	}

	long FileSize;
	if (fseek(File, 0, SEEK_END))
	{
		SetErrorForFile("Failed to seek file to end!", path);
		return NULL;
	}

	FileSize = ftell(File);
	if (FileSize == -1)
	{
		SetErrorForFile("Failed to tell position in file!", path);
		return NULL;
	}

	if (fseek(File, 0, SEEK_SET))
	{
		SetErrorForFile("Failed to seek file to start!", path);
		return NULL;
	}

	char* FileData = (char*)Memory_SafeMalloc(FileSize + 1);
	fread(FileData, sizeof(char), FileSize, File);
	FileData[FileSize] = '\0';
	fclose(File);

	return FileData;
}