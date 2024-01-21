#include "StringBuilder.h"
#include "KGMemory.h"
#include <string.h>


// Macros.
#define STRINGBUILDER_CAPACITY_GROWTH 2

// Static functions.
static void EnsureCapacity(StringBuilder* self, size_t requiredCapacity)
{
	requiredCapacity++; // Account for null terminator.

	if (self->_capacity >= requiredCapacity)
	{
		return;
	}

	while (self->_capacity < requiredCapacity)
	{
		self->_capacity *= 2;
	}

	self->Data = (char*)Memory_SafeRealloc(self->Data, self->_capacity);
}


// Functions.
void StringBuilder_Construct(StringBuilder* builder, size_t capacity)
{
	if (capacity == 0)
	{
		capacity = STRINGBUILDER_DEFAULT_CAPACITY;
	}

	builder->Data = Memory_SafeMalloc(capacity);
	builder->Length = 0;
	builder->_capacity = capacity;
}


StringBuilder* StringBuilder_Construct2(size_t capacity)
{
	StringBuilder* Builder = (StringBuilder*)Memory_SafeMalloc(sizeof(StringBuilder));
	StringBuilder_Construct(Builder, capacity);
	return Builder;
}

void StringBuilder_AppendChar(StringBuilder* self, char character)
{
	EnsureCapacity(self, self->Length + 1);

	self->Data[self->Length] = character;
	self->Length++;
	self->Data[self->Length] = '\0';
}

void StringBuilder_AppendString(StringBuilder* self, const char* string)
{
	size_t AppendedStringLength = strlen(string);
	EnsureCapacity(self, self->Length + AppendedStringLength);

	strcpy(self->Data + self->Length, string);
	self->Length += AppendedStringLength;
}

ErrorCode StringBuilder_Remove(StringBuilder* self, size_t startIndex, size_t endIndex)
{
	if (startIndex > endIndex)
	{
		return Errors_SetError(ErrorCode_ArgumentError, "StringBuilder_Remove: startIndex is greater than endIndex.");
	}
	if (startIndex >= self->Length)
	{
		return Errors_SetError(ErrorCode_IndexOutOfRange, "StringBuilder_Remove: startIndex is out of string bounds.");
	}
	if (endIndex > self->Length)
	{
		return Errors_SetError(ErrorCode_IndexOutOfRange, "StringBuilder_Remove: endIndex is out of string bounds.");
	}

	size_t RemovedLength = endIndex - startIndex;
	if (RemovedLength == 0)
	{
		return;
	}

	for (size_t i = endIndex; i <= self->Length; i++)
	{
		self->Data[i - RemovedLength] = self->Data[i];
	}

	return ErrorCode_Success;
}

void StringBuilder_Clear(StringBuilder* self)
{
	self->Length = 0;
	self->Data[0] = '\0';
}

void StringBuilder_Deconstruct(StringBuilder* self)
{
	Memory_Free(self->Data);
}