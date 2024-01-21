#pragma once
#include <stddef.h>
#include "KGErrors.h"

// Macros.
#define STRINGBUILDER_DEFAULT_CAPACITY 128


//Types.
typedef struct StringBuilderStruct
{
	char* Data;
	size_t Length;
	size_t _capacity;
} StringBuilder;


// Functions.
void StringBuilder_Construct(StringBuilder* builder, size_t capacity);

StringBuilder* StringBuilder_Construct2(size_t capacity);

void StringBuilder_AppendChar(StringBuilder* self, char character);

void StringBuilder_AppendString(StringBuilder* self, const char* string);

ErrorCode StringBuilder_Remove(StringBuilder* self, size_t startIndex, size_t endIndex);

void StringBuilder_Clear(StringBuilder* self);

void StringBuilder_Deconstruct(StringBuilder* self);