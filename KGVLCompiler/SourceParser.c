#include "SourceParser.h"
#include "KGFile.h"
#include "KGErrors.h"
#include "KGMemory.h"
#include <stdio.h>
#include <Windows.h>
#include <stdbool.h>


// Macros.
#define FILE_COLLECTION_DEFAULT_CAPACITY 16
#define SOURCE_FILE_COLLECTION_CAPACITY_GROWTH 2
#define SOURCE_CODE_FILE_EXTENSION ".kgvl"
#define SOURCE_CODE_FILE_EXTENSION_LENGTH 5

#define IsCharPathSeparator(character) (character == '\\')  || (character == '/')


// Static functions.
/* SourceFileCollection. */
static void SourceFileCollectionEnsureCapacity(SourceFileCollection* self, size_t capacity)
{
	if (self->_objectCapacity < capacity)
	{
		while (self->_objectCapacity < capacity)
		{
			self->_objectCapacity *= SOURCE_FILE_COLLECTION_CAPACITY_GROWTH;
		}

		self->SourceFileObjectArray = (SourceFileObject*)Memory_SafeRealloc(self->SourceFileObjectArray,
			sizeof(SourceFileObject) * self->_objectCapacity);
	}
}

static void SourceFileCollectionExpand(SourceFileCollection* self)
{
	self->Length++;
	SourceFileCollectionEnsureCapacity(self, self->Length);
}


/* Parsing files. */
static bool IsSourceCodeFile(const char* filePath)
{
	const char Extension[] = SOURCE_CODE_FILE_EXTENSION;
	int ExtensionIndex = SOURCE_CODE_FILE_EXTENSION_LENGTH - 1;

	for (int PathIndex = strlen(filePath) - 1; (PathIndex >= 0) && (ExtensionIndex >= 0); PathIndex--, ExtensionIndex--)
	{
		if (filePath[PathIndex] != Extension[ExtensionIndex])
		{
			return false;
		}
	}

	return ExtensionIndex < 0;
}

static void ParseFilesInDirectory(SourceFileCollection* fileCollection, const char* directory)
{
	WIN32_FIND_DATA FileData;
	HANDLE SearchHandle = FindFirstFileA(directory, &FileData);

	while ((SearchHandle != INVALID_HANDLE_VALUE)  && (SearchHandle))
	{
		if (FileData.cFileName[0] == '.' && FileData.cFileName[1] == '\0')
		{
			SearchHandle = FindNextFileA(SearchHandle, &FileData);
			continue;
		}
		if (FileData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
		{
			const char* FullDirectoryPath = File_JoinPaths(directory, FileData.cFileName);
			ParseFilesInDirectory(fileCollection, FullDirectoryPath);
			Memory_Free(FullDirectoryPath);
			SearchHandle = FindNextFileA(SearchHandle, &FileData);
			continue;
		}
		if (!IsSourceCodeFile(FileData.cFileName))
		{
			SearchHandle = FindNextFileA(SearchHandle, &FileData);
			continue;
		}

		SourceFileCollectionExpand(fileCollection);

		const char* FullFilePath = File_JoinPaths(directory, FileData.cFileName);
		SourceFileObject* FileObject = fileCollection->SourceFileObjectArray + fileCollection->Length - 1;
		SourceFileObject_Construct(FileObject, FullFilePath);
		SourceFileObject_Parse(FileObject);
		Memory_Free(FullFilePath);

		SearchHandle = FindNextFileA(SearchHandle, &FileData);
	}

	FindClose(SearchHandle);
}

// Functions.
SourceFileObject* SourceFile_ParseFile(const char* path)
{
	const char* SourceData = File_ReadAllText(path);
	if (!SourceData)
	{
		return NULL;
	}
}

void SourceFile_ParseAllFiles(SourceFileCollection* fileCollection, const char* sourceDirectory)
{
	fileCollection->Length = 0;
	fileCollection->SourceFileObjectArray = (SourceFileObject*)Memory_SafeMalloc(sizeof(SourceFileObject) * FILE_COLLECTION_DEFAULT_CAPACITY);
	fileCollection->_objectCapacity = FILE_COLLECTION_DEFAULT_CAPACITY;

	ParseFilesInDirectory(fileCollection, sourceDirectory);
}