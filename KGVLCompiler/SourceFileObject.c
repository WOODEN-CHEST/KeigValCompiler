#include "SourceFileObject.h"
#include "KGMemory.h"
#include <string.h>


// Static functions.



// Functions.
void SourceFileObject_Construct(SourceFileObject* self, const char* filePath)
{
	self->FilePath = (char*)Memory_SafeMalloc(strlen(filePath) + 1);
	strcpy(self->FilePath, filePath);
}

void SourceFileObject_Parse(SourceFileObject* self)
{

}