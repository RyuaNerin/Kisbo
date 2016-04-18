#ifndef __DEBUGLOG__H__
#define __DEBUGLOG__H__

#ifdef _DEBUG
void DebugLog(const wchar_t *fmt, ...);
#else
#define DebugLog
#endif

#endif