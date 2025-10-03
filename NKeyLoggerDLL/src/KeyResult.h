#ifndef KEYRESULTH
#define KEYRESULTH

#include <string>

static const short langNameSize = 0x100;
static const short procNameSize = 0x400;
static const short keySize = 0x10;
static const short maxErrorSize = 0x400;

struct KeyResult {
	std::wstring key = L"";
	std::wstring langName = L"";
	std::wstring procName = L"";
	std::string error = "";
};

struct NativeKeyResult {
	wchar_t key[keySize] = L"";
	wchar_t langName[langNameSize] = L"";
	wchar_t procName[procNameSize] = L"";
	char error[maxErrorSize] = "";
};

#endif // !KEYRESULTH