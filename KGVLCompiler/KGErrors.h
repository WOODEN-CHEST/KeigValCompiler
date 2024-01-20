#pragma once

// Types.
typedef enum ErrorCodeEnum
{
	ErrorCode_Success,

	ErrorCode_IO,

	ErrorCode_Unknown
} ErrorCode;


// Functions.
void Errors_InitErrorHandling();

void Errors_AbortProgram(const char* message);

ErrorCode Errors_SetError(ErrorCode code, const char* message);

const char* Errors_GetLastErrorMessage();

ErrorCode Errors_GetLastErrorCode();