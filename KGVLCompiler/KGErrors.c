#include "KGErrors.h"
#include <stdlib.h>
#include <stdio.h>
#include <stdbool.h>
#include <string.h>

// Macros.
#define ERROR_MESSAGE_CAPACITY_GROWTH 2
#define ERROR_MESSAGE_DEFAULT_CAPACITY 512


// Variables.
static bool _isInitialized = false;

static ErrorCode _lastErrorCode;

static char* _lastErrorMessage;
static size_t _lastErrorMessageCapacity;


// Functions.
void Errors_InitErrorHandling()
{
	if (_isInitialized)
	{
		return;
	}

	_lastErrorCode = ErrorCode_Success;

	_lastErrorMessageCapacity = ERROR_MESSAGE_DEFAULT_CAPACITY;
	_lastErrorMessage = malloc(_lastErrorMessageCapacity);
	if (!_lastErrorMessage)
	{
		Errors_AbortProgram("Failed to allocate memory for error handling.");
		return;
	}
	*_lastErrorMessage = '\0';
}

void Errors_AbortProgram(const char* message)
{
	printf("Program aborted: %s", message);
	exit(EXIT_FAILURE);
}

ErrorCode Errors_SetError(ErrorCode code, const char* message)
{
	if ((code < ErrorCode_Success) || (code > ErrorCode_Unknown))
	{
		code = ErrorCode_Unknown;
	}
	_lastErrorCode = code;

	char ErrorTypeName[128];
	switch (code)
	{
		case ErrorCode_IO:
			strcpy(ErrorTypeName, "IO Error");
			break;

		default:
			strcpy(ErrorTypeName, "Unknown Error");
			break;
	}
	

	size_t ErrorNameLength = strlen(ErrorTypeName);
	size_t MessageLength = strlen(message);
	size_t RequiredCapacity = ErrorNameLength + MessageLength + 3; // 3 accounts for null terminator, dot and space.

	if (RequiredCapacity > _lastErrorMessageCapacity)
	{
		while (RequiredCapacity > _lastErrorMessageCapacity)
		{
			_lastErrorMessageCapacity *= ERROR_MESSAGE_CAPACITY_GROWTH;
		}

		char* NewPtr = (char*)realloc(_lastErrorMessage, _lastErrorMessageCapacity);
		if (!NewPtr)
		{
			free(_lastErrorMessage);
			Errors_AbortProgram("Failed to reallocate memory for error message.");
			return;
		}

		_lastErrorMessage = NewPtr;
	}

	sprintf(_lastErrorMessage, "%s. %s", ErrorTypeName, message);

	return code;
}

const char* Errors_GetLastErrorMessage()
{
	return _lastErrorMessage;
}

ErrorCode Errors_GetLastErrorCode()
{
	return _lastErrorCode;
}