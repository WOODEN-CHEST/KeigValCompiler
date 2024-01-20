#include <stdio.h>
#include <exit_codes.h>

char* ReadFile(const char path[])
{
	FILE* File = fopen(path, "r");
	
	if (File == NULL)
	{
		return ERR_SOURCE_FILE_NOT_OPENED;
	}

	fseek(File, 0, SEEK_END);
	int FileSize = ftell(File);
	fseek(File, 0, SEEK_SET);
}