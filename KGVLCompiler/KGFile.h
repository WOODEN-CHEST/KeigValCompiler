#pragma once
#include <stdio.h>

// Macros.
#define PATH_SEPARATOR '/'


// Functions.
char* File_ReadAllText(const char* path);

char* File_JoinPaths(const char* path1, const char* path2);