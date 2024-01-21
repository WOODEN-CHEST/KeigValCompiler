#include "KGMemory.h"
#include "KGErrors.h"
#include <stdlib.h>

// Functions.
void* Memory_SafeMalloc(size_t size)
{
	void* Pointer = malloc(size);

	if (!Pointer)
	{
		Errors_AbortProgram("Failed to allocate memory.");
		return NULL;
	}

	return Pointer;
}

void* Memory_SafeRealloc(void* pointer, size_t newSize)
{
	void* Pointer = realloc(pointer, newSize);

	if (!Pointer)
	{
		Errors_AbortProgram("Failed to reallocate memory.");
		return NULL;
	}

	return Pointer;
}

void* Memory_Free(void* pointer)
{
	free(pointer);
}