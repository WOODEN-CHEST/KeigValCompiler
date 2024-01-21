#pragma once


// Types.
typedef struct SourceFileObjectStruct
{
	const char* FilePath;
} SourceFileObject;


// Functions.
void SourceFileObject_Construct(SourceFileObject* self, const char* filePath);

void SourceFileObject_Parse(SourceFileObject* self);