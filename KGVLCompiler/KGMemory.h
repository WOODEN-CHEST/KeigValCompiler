#pragma once
#include <stddef.h>

// Functions.
void* Memory_SafeMalloc(size_t size);

void* Memory_SafeRealloc(void* pointer, size_t newSize);

void* Memory_Free(void* pointer);