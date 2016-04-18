#include "stdafx.h"
#include "ClassFactory.h"

#include "ContextMenu.h"

ClassFactory::ClassFactory()
{
    m_ref = 1;
    InterlockedIncrement(&g_ref);
}

ClassFactory::~ClassFactory()
{
    InterlockedDecrement(&g_ref);
}

IFACEMETHODIMP ClassFactory::QueryInterface(REFIID riid, void **ppvObject)
{
    *ppvObject = 0;

    if (IsEqualIID(riid, IID_IUnknown))
        *ppvObject = this;
    else if (IsEqualIID(riid, IID_IClassFactory))
        *ppvObject = (IClassFactory*)this;

    if (*ppvObject)
    {
        LPUNKNOWN pUnk = (LPUNKNOWN)(*ppvObject);
        pUnk->AddRef();
        return S_OK;
    }

    return E_NOINTERFACE;
}

IFACEMETHODIMP ClassFactory::CreateInstance(LPUNKNOWN pUnkOuter, REFIID riid, LPVOID *ppvObject)
{
    *ppvObject = 0;
    if (pUnkOuter != NULL)
        return CLASS_E_NOAGGREGATION;

    ContextMenu* pExt = new ContextMenu();
    if (pExt == 0)
        return E_OUTOFMEMORY;

    HRESULT hResult = pExt->QueryInterface(riid, ppvObject);
    pExt->Release();
    return hResult;
}

IFACEMETHODIMP_(ULONG) ClassFactory::AddRef()
{
    return InterlockedIncrement(&m_ref);
}

IFACEMETHODIMP_(ULONG) ClassFactory::Release()
{
    ULONG cRef = InterlockedDecrement(&m_ref);
    if (cRef == 0)
        delete this;

    return cRef;
}

IFACEMETHODIMP ClassFactory::LockServer(BOOL fLock)
{
    return E_NOTIMPL;
}
