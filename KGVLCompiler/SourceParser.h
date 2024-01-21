#pragma once
#include <stddef.h>
#include "SourceFileObject.h"

// Types.


typedef struct SourceFileCollectionStruct
{
	SourceFileObject* SourceFileObjectArray;
	size_t Length;
	size_t _objectCapacity;
} SourceFileCollection;


// Functions.
void SourceFile_ParseAllFiles(SourceFileCollection* fileCollection, const char* sourceDirectory);