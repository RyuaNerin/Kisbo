#ifndef __CLASSFACTORY__H_
#define __CLASSFACTORY__H_

#include <unknwn.h>     // IClassFactory

extern HINSTANCE g_hInst;
extern UINT g_ref;

class ClassFactory : public IClassFactory
{
public:
    ClassFactory();
    virtual ~ClassFactory();

    // IUnknown
    STDMETHODIMP QueryInterface(REFIID, LPVOID*);
    STDMETHODIMP_(ULONG) AddRef();
    STDMETHODIMP_(ULONG) Release();

    // IClassFactory
    STDMETHODIMP CreateInstance(LPUNKNOWN, REFIID, LPVOID *);
    STDMETHODIMP LockServer(BOOL);

protected:
    ULONG m_ref;
};

#endif