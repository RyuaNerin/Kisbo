#include "stdafx.h"

#ifdef _DEBUG
#include "DebugLog.h"

#include <windows.h>
#include <cwchar>

static CRITICAL_SECTION* cs;

void DebugLog(const wchar_t *fmt, ...)
{
    if (cs == 0)
    {
        cs = new CRITICAL_SECTION();
        InitializeCriticalSection(cs);
    }

    EnterCriticalSection(cs);
    
    va_list	args;
    va_start(args, fmt);

    INT len = _scwprintf(L"KisboExt: ") + _vscwprintf(fmt, args) + 1;

    WCHAR* str = new WCHAR[len];
    len = wsprintfW(str, L"KisboExt: ");
    wvsprintfW(str + len, fmt, args);

    va_end(args);

    OutputDebugStringW(str);

    delete[] str;

    LeaveCriticalSection(cs);
}
#endif
