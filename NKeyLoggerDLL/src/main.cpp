#include <iostream>
#include <array>
#include <map>
#include <io.h>
#include <fcntl.h>
#include <atomic>
#include <algorithm>
#include <Windows.h>
#include <psapi.h>

//Constants and DataTypes
struct KeyResult {
	std::wstring key = L"";
	std::wstring langName = L"";
	std::wstring procName = L"";
	std::string error = "";
};

static const short langNameSize = 0x100;
static const short procNameSize = 0x400;
static const short keySize = 0x10;
static const short maxErrorSize = 0x400;

struct NativeKeyResult {
	wchar_t key[keySize] = L"";
	wchar_t langName[langNameSize] = L"";
	wchar_t procName[procNameSize] = L"";
	char error[maxErrorSize] = "";
};

using CallbackFunction = void (*)(NativeKeyResult result, const bool isUpper);

static std::atomic<bool> isStopped;
static std::atomic<bool> isListen;
static HHOOK hookKeyboard;
static CallbackFunction callback = nullptr;

//Function defines
NativeKeyResult toNative(const KeyResult&);
std::wstring getLang(HKL layout);
std::wstring getProgramName();
std::wstring getSpecialKey(const std::wstring& key);
KeyResult getUnicodeKey(unsigned int code, unsigned int scanCode);
LRESULT CALLBACK keyBoardProc(int code, WPARAM w, LPARAM l);

//Functions
NativeKeyResult toNative(const KeyResult& key) {
	short sizeofChar = sizeof(wchar_t);
	NativeKeyResult result;
	memcpy_s(result.key, key.key.size() * sizeofChar, key.key.c_str(), key.key.size() * sizeofChar);
	memcpy_s(result.langName, key.langName.size() * sizeofChar, key.langName.c_str(), key.langName.size() * sizeofChar);
	memcpy_s(result.procName, key.procName.size() * sizeofChar, key.procName.c_str(), key.procName.size() * sizeofChar);
	memcpy_s(result.error, key.error.size() * sizeofChar, key.error.c_str(), key.error.size() * sizeofChar);
	return result;
}

std::wstring getLang(HKL layout) {
	std::shared_ptr<std::array<wchar_t, langNameSize>> pLangName = std::make_shared<std::array<wchar_t, langNameSize>>();
	GetLocaleInfoW(MAKELCID(LOWORD(layout), SORT_DEFAULT),
		LOCALE_SENGLISHLANGUAGENAME,
		pLangName->data(),
		langNameSize / sizeof(wchar_t));
	return std::wstring(pLangName->data(), pLangName->size());
}

std::wstring getProgramName() {
	DWORD pid = 0;
	GetWindowThreadProcessId(GetForegroundWindow(), &pid);
	if (pid <= 0)
		return L"";

	HANDLE targetProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
	if (targetProc == INVALID_HANDLE_VALUE)
		return L"";

	std::shared_ptr<std::array<wchar_t, procNameSize>> bufferName = std::make_shared<std::array<wchar_t, procNameSize>>();
	if (!GetModuleFileNameExW(targetProc, 0, bufferName->data(), procNameSize))
		return L"";

	return std::wstring(bufferName->data(), procNameSize);
}

std::wstring getSpecialKey(const std::wstring& key) {
	static std::map<std::wstring, std::wstring> map = {
		std::pair<std::wstring,std::wstring>(L"\b",L"Backspace"),
		std::pair<std::wstring,std::wstring>(L"\r",L"Enter"),
		std::pair<std::wstring,std::wstring>(L"\t",L"Tab"),
		std::pair<std::wstring,std::wstring>(L"\x1b",L"Esc"),
	};
	auto it = map.find(key);
	if (it == map.end())
		return key;
	return it->second;
}

KeyResult getUnicodeKey(unsigned int code, unsigned int scanCode) {
	KeyResult result;

	static const int keyboardStateSize = 0x100;
	std::shared_ptr<std::array<byte, keyboardStateSize>> keyboardState = std::make_shared<std::array<byte, keyboardStateSize>>();
	if (!GetKeyboardState(keyboardState->data()))
		return KeyResult{};

	HKL layout = GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), NULL));
	result.langName = getLang(layout);
	if (result.langName.empty())
		result.langName = L"Unknown language";

	result.procName = getProgramName();
	if (result.procName.empty())
		result.procName = L"Unknown process name";

	std::array<wchar_t, keySize> buffer;
	if(ToUnicodeEx(code, scanCode, keyboardState->data(), buffer.data(), keySize, 0, layout) <= 0)
		GetKeyNameTextW(scanCode << 0x10, buffer.data(), keySize);

	result.key = std::wstring(buffer.data());
	result.key = getSpecialKey(result.key);

	return result;
}

LRESULT CALLBACK keyBoardProc(int code, WPARAM w, LPARAM l) {
	try {
		static auto isUpper = []()->bool {
			bool isActive = GetKeyState(VK_CAPITAL) & 0x0001;
			bool isPush = GetAsyncKeyState(VK_SHIFT) & 0x8000;
			return isActive ^ isPush;
		};

		if (code >= 0 && (w == WM_KEYDOWN || w == WM_SYSKEYDOWN)) {
			KBDLLHOOKSTRUCT* hookStruct = (KBDLLHOOKSTRUCT*)l;
			KeyResult key = getUnicodeKey(hookStruct->vkCode, hookStruct->scanCode);

			if (key.key.empty())
				goto End;

			if (callback != nullptr)
				callback(toNative(key), isUpper());
		}
	}
	catch (std::exception ex) {
		KeyResult res;
		res.error = ex.what();
		callback(toNative(res), false);
	}
	End:
	return CallNextHookEx(NULL, code, w, l);
}

//Interface
extern "C" {
	bool _stdcall start(CallbackFunction callBack) {
		callback = callBack;
		hookKeyboard = SetWindowsHookEx(WH_KEYBOARD_LL, keyBoardProc, NULL, 0);
		if (hookKeyboard == NULL)
			return false;
		isStopped.store(false);
		isListen.store(true);
		MSG msg;
		while (GetMessage(&msg, NULL, 0, 0) && isListen.load()) {
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}
		UnhookWindowsHookEx(hookKeyboard);
		isStopped.store(true);
		return true;
	}

	void _stdcall stop() {
		isListen.store(false);
	}

	void _stdcall waitForStopped() {
		while (!isStopped.load())
			Sleep(1);
	}
}

#ifdef DEBUG

KeyResult toKeyResult(NativeKeyResult nativeKey) {
	KeyResult res;
	res.key = std::wstring(nativeKey.key);
	res.langName = std::wstring(nativeKey.langName);
	res.procName = std::wstring(nativeKey.procName);
	res.error = std::string(nativeKey.error);
	return res;
}

void testCallBack(NativeKeyResult nativeRes, const bool isUpper) {
	KeyResult res = toKeyResult(nativeRes);
	static short upperShift = 0x20;
	if (isUpper)
		for (int i = 0; i < res.key.size(); ++i)
			res.key[i] = towupper(res.key[i]);
	std::wcout << res.key << " " << res.langName << " " << res.procName << std::endl;
}

int main() {
	_setmode(_fileno(stdout), _O_U16TEXT);
	bool work = true;
	start(testCallBack);
	return 0;
}
#endif