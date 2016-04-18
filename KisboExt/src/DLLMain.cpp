#include "stdafx.h"

#include <windows.h>
#include <Olectl.h>

#include "ClassFactory.h"
#include "DebugLog.h"
#include "resource.h"

HINSTANCE g_hInst;
UINT g_ref;

HANDLE m_menuImage = NULL;
WCHAR m_exePath[MAX_PATH] = { 0 };

// {AD896442-13D3-4855-9739-22C9ECBECFBD}
#define SHELLEXT_GUID   { 0xad896442, 0x13d3, 0x4855, { 0x97, 0x39, 0x22, 0xc9, 0xec, 0xbe, 0xcf, 0xbd } }

BOOL APIENTRY DllMain(HMODULE hModule, DWORD dwReason, LPVOID lpReserved)
{
	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
        g_hInst = hModule;

        if (m_menuImage == NULL)
            m_menuImage = LoadImage(g_hInst, MAKEINTRESOURCEW(IDB_ICON), IMAGE_BITMAP, 0, 0, LR_DEFAULTSIZE | LR_LOADTRANSPARENT | LR_LOADMAP3DCOLORS);

        if (m_exePath[0] == 0)
        {
            HKEY hKey;
            if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"Software\\RyuaNerin", NULL, KEY_READ, &hKey) == NO_ERROR)
            {
                DWORD len = sizeof(m_exePath);
                RegGetValueW(hKey, NULL, L"Kisbo", REG_SZ, NULL, reinterpret_cast<LPBYTE>(&m_exePath), &len);
                RegCloseKey(hKey);

                DebugLog(L"GetExePath [%d] %s", len, m_exePath);
            }
        }

        DisableThreadLibraryCalls(hModule);
        break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void **ppReturn)
{    
    *ppReturn = 0;
    if (!IsEqualCLSID(SHELLEXT_GUID, rclsid))
        return CLASS_E_CLASSNOTAVAILABLE;

    ClassFactory *fac = new ClassFactory();
    if (fac == 0)
        return E_OUTOFMEMORY;

    HRESULT result = fac->QueryInterface(riid, ppReturn);
    fac->Release();
    return result;
}

STDAPI DllCanUnloadNow(void)
{
    return g_ref > 0 ? S_FALSE : S_OK;
}

BOOL Regist_Key(HKEY rootKey, LPWSTR subKey, LPWSTR keyName, LPWSTR keyData)
{
    HKEY hKey;
    DWORD dwDisp;

    if (RegCreateKeyExW(HKEY_CLASSES_ROOT, subKey, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_WRITE, NULL, &hKey, &dwDisp) != NOERROR)
        return FALSE;

    RegSetValueExW(hKey, keyName, 0, REG_SZ, (LPBYTE)keyData, (lstrlen(keyData) + 1) * sizeof(TCHAR));
    RegCloseKey(hKey);
    return TRUE;
}
BOOL Regist_CLSID(LPWSTR clsid)
{
    TCHAR dllPath[MAX_PATH];
    GetModuleFileNameW(g_hInst, dllPath, ARRAYSIZE(dllPath));

    TCHAR subKey[MAX_PATH];
    wsprintf(subKey, L"CLSID\\%s", clsid);
    if (!Regist_Key(HKEY_CLASSES_ROOT, subKey, NULL, L"Shell Extension for Kisbo"))
        return FALSE;
    
    wsprintf(subKey, L"CLSID\\%s\\InprocServer32", clsid);
    if (!Regist_Key(HKEY_CLASSES_ROOT, subKey, NULL, dllPath))
        return FALSE;

    if (!Regist_Key(HKEY_CLASSES_ROOT, subKey, L"ThreadingModel", L"Apartment"))
        return FALSE;

    return TRUE;
}

STDAPI DllRegisterServer(void)
{
    DebugLog(L"Registering Server");
    
    TCHAR clsid[MAX_PATH];
    if (StringFromGUID2(SHELLEXT_GUID, clsid, ARRAYSIZE(clsid)) == 0)
        return SELFREG_E_CLASS;

    if (!Regist_CLSID(clsid))
        return SELFREG_E_CLASS;

    // 0.0.0.1 old
    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"*\\ShellEx\\ContextMenuHandlers\\KisboExt");
    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"Directory\\ShellEx\\ContextMenuHandlers\\KisboExt");

    // 0.0.0.2 new
    if (!Regist_Key(HKEY_CLASSES_ROOT, L"*\\ShellEx\\ContextMenuHandlers\\00KisboExt", NULL, clsid))
        return SELFREG_E_CLASS;

    if (!Regist_Key(HKEY_CLASSES_ROOT, L"Directory\\ShellEx\\ContextMenuHandlers\\00KisboExt", NULL, clsid))
        return SELFREG_E_CLASS;

    if (!Regist_Key(HKEY_LOCAL_MACHINE, L"Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved", NULL, L"00KisboExt"))
        return SELFREG_E_CLASS;

    return S_OK;
}

void Unregist_CLSID(LPWSTR clsid)
{
    TCHAR subKey[MAX_PATH];
    wsprintf(subKey, L"CLSID\\%s\\InprocServer32", clsid);
    RegDeleteKeyW(HKEY_CLASSES_ROOT, subKey);

    wsprintf(subKey, L"CLSID\\%s", clsid);
    RegDeleteKeyW(HKEY_CLASSES_ROOT, subKey);
}

STDAPI DllUnregisterServer(void)
{
    DebugLog(L"Unregistering server");

    TCHAR clsid[MAX_PATH];
    StringFromGUID2(SHELLEXT_GUID, clsid, ARRAYSIZE(clsid));
    
    Unregist_CLSID(clsid);

    // 0.0.0.1 old
    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"*\\ShellEx\\ContextMenuHandlers\\KisboExt");
    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"Directory\\ShellEx\\ContextMenuHandlers\\KisboExt");

    // 0.0.0.2 new
    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"*\\ShellEx\\ContextMenuHandlers\\00KisboExt");
    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"Directory\\ShellEx\\ContextMenuHandlers\\00KisboExt");

    HKEY hTmpKey;
    if (RegOpenKeyW(HKEY_LOCAL_MACHINE, L"Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved", &hTmpKey) == ERROR_SUCCESS)
    {
        RegDeleteValueW(hTmpKey, clsid);
        RegCloseKey(hTmpKey);
    }

    if (RegOpenKeyW(HKEY_LOCAL_MACHINE, L"Software\\RyuaNerin", &hTmpKey) == ERROR_SUCCESS)
    {
        RegDeleteValueW(hTmpKey, L"Kisbo");
        RegCloseKey(hTmpKey);
    }


    return S_OK;
}
