#include <windows.h>
#include <stdio.h>
#include <atomic>
#include <intrin.h>
#include <stdint.h>
#include "cor.h"
#include "corprof.h"
static ICorProfilerInfo5* g_info=nullptr;
static std::atomic<bool> g_active=false;
static DWORD g_owner=0;
static ULONG64 g_objects=0,g_objectBytes=0;
static std::atomic<LONG> g_gcStarted=0,g_gcFinished=0;
struct Entry { LONG kind; DWORD thread; LONGLONG timestamp; UINT_PTR a,b; };
static Entry g_entries[8192]; static std::atomic<LONG> g_next=0;
static void record(LONG kind, UINT_PTR a, UINT_PTR b) { if(!g_active) return; LONG i=g_next.fetch_add(1); if(i<8192) { LARGE_INTEGER time; QueryPerformanceCounter(&time); g_entries[i]={kind,GetCurrentThreadId(),time.QuadPart,a,b}; } }
static DWORD g_tlsIndex=0,g_tlsOffset=0; static volatile UINT_PTR* g_context=nullptr;
struct ContextSample {LONG phase;DWORD thread;LONGLONG qpc;UINT_PTR address,ptr,limit,bytes,uoh;};
static ContextSample g_contextSamples[32]; static std::atomic<LONG> g_contextNext=0;
extern "C" __declspec(dllexport) int ConfigureCounter(void* function) {
 const unsigned char expectedStart[]={0x65,0x48,0x8b,0x04,0x25,0x58,0,0,0,0x8b,0x0d};
 const unsigned char expectedEnd[]={0x48,0x8b,0x0c,0xc8,0x48,0x03,0xca,0x48,0x8b,0x41,0x20,0x48,0x2b,0x41,0x10,0x48,0x03,0x41,0x18,0x48,0x03,0x41,0x08,0xc3};
 auto p=(unsigned char*)function;
 if(memcmp(p,expectedStart,sizeof(expectedStart))||p[15]!=0xba||memcmp(p+20,expectedEnd,sizeof(expectedEnd)))return 0;
 g_tlsIndex=*(DWORD*)(p+15+*(int32_t*)(p+11));g_tlsOffset=*(DWORD*)(p+16);return 1;
}
static void contextSample(LONG phase){if(!g_active||!g_context)return;LONG i=g_contextNext.fetch_add(1);if(i>=32)return;LARGE_INTEGER time;QueryPerformanceCounter(&time);auto p=g_context;g_contextSamples[i]={phase,GetCurrentThreadId(),time.QuadPart,(UINT_PTR)p,p[1],p[2],p[3],p[4]};}
extern "C" __declspec(dllexport) void BeginWindow() { g_owner=GetCurrentThreadId();g_context=(volatile UINT_PTR*)((unsigned char*)((void**)__readgsqword(0x58))[g_tlsIndex]+g_tlsOffset);g_contextNext=0;g_objects=g_objectBytes=0;g_next=0;g_active=true;record(0,g_gcStarted.load(),g_gcFinished.load());contextSample(0); }
extern "C" __declspec(dllexport) void EndWindow(long long delta) { contextSample(7);record(7,g_gcStarted.load(),g_gcFinished.load()); g_active=false; printf("NATIVE_ATTRIBUTION owner=%lu; delta=%lld; objectCount=%llu; objectBytes=%llu; entries=%ld\n",g_owner,delta,g_objects,g_objectBytes,g_next.load()); for(LONG i=0;i<g_next && i<8192;i++){const auto& e=g_entries[i]; printf("NATIVE_EVENT kind=%ld; thread=%lu; qpc=%lld; a=%llu; b=%llu\n",e.kind,e.thread,e.timestamp,(unsigned long long)e.a,(unsigned long long)e.b);} for(LONG i=0;i<g_contextNext&&i<32;i++){const auto& s=g_contextSamples[i];printf("CONTEXT phase=%ld; thread=%lu; qpc=%lld; address=%llu; ptr=%llu; limit=%llu; bytes=%llu; uoh=%llu; unused=%llu\n",s.phase,s.thread,s.qpc,(unsigned long long)s.address,(unsigned long long)s.ptr,(unsigned long long)s.limit,(unsigned long long)s.bytes,(unsigned long long)s.uoh,(unsigned long long)(s.limit-s.ptr));} fflush(stdout); }
class Profiler : public ICorProfilerCallback4 {
public:
HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** value) override { if(iid==__uuidof(IUnknown)||iid==__uuidof(ICorProfilerCallback)||iid==__uuidof(ICorProfilerCallback2)||iid==__uuidof(ICorProfilerCallback3)||iid==__uuidof(ICorProfilerCallback4)){*value=this;AddRef();return S_OK;}*value=nullptr;return E_NOINTERFACE; }
ULONG STDMETHODCALLTYPE AddRef() override {return 2;}
ULONG STDMETHODCALLTYPE Release() override {return 1;}
HRESULT STDMETHODCALLTYPE Initialize(/* [in] */ IUnknown *pICorProfilerInfoUnk) override { HRESULT hr = pICorProfilerInfoUnk->QueryInterface(__uuidof(ICorProfilerInfo5), (void**)&g_info); if (FAILED(hr)) return hr; return g_info->SetEventMask2(COR_PRF_MONITOR_OBJECT_ALLOCATED | COR_PRF_ENABLE_OBJECT_ALLOCATED | COR_PRF_MONITOR_SUSPENDS | COR_PRF_MONITOR_JIT_COMPILATION, COR_PRF_HIGH_BASIC_GC); }
HRESULT STDMETHODCALLTYPE Shutdown(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE AppDomainCreationStarted(/* [in] */ AppDomainID appDomainId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE AppDomainCreationFinished(/* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus) override { return S_OK; }
HRESULT STDMETHODCALLTYPE AppDomainShutdownStarted(/* [in] */ AppDomainID appDomainId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE AppDomainShutdownFinished(/* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus) override { return S_OK; }
HRESULT STDMETHODCALLTYPE AssemblyLoadStarted(/* [in] */ AssemblyID assemblyId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE AssemblyLoadFinished(/* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus) override { return S_OK; }
HRESULT STDMETHODCALLTYPE AssemblyUnloadStarted(/* [in] */ AssemblyID assemblyId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE AssemblyUnloadFinished(/* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ModuleLoadStarted(/* [in] */ ModuleID moduleId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ModuleLoadFinished(/* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ModuleUnloadStarted(/* [in] */ ModuleID moduleId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ModuleUnloadFinished(/* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ModuleAttachedToAssembly(/* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ClassLoadStarted(/* [in] */ ClassID classId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ClassLoadFinished(/* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ClassUnloadStarted(/* [in] */ ClassID classId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ClassUnloadFinished(/* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus) override { return S_OK; }
HRESULT STDMETHODCALLTYPE FunctionUnloadStarted(/* [in] */ FunctionID functionId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE JITCompilationStarted(/* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock) override { record(6,functionId,0); return S_OK; }
HRESULT STDMETHODCALLTYPE JITCompilationFinished(/* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock) override { return S_OK; }
HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchStarted(/* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction) override { return S_OK; }
HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchFinished(/* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result) override { return S_OK; }
HRESULT STDMETHODCALLTYPE JITFunctionPitched(/* [in] */ FunctionID functionId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE JITInlining(/* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ThreadCreated(/* [in] */ ThreadID threadId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ThreadDestroyed(/* [in] */ ThreadID threadId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread(/* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RemotingClientInvocationStarted(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RemotingClientSendingMessage(/* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RemotingClientReceivingReply(/* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RemotingClientInvocationFinished(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RemotingServerReceivingMessage(/* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RemotingServerInvocationStarted(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RemotingServerInvocationReturned(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RemotingServerSendingReply(/* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync) override { return S_OK; }
HRESULT STDMETHODCALLTYPE UnmanagedToManagedTransition(/* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ManagedToUnmanagedTransition(/* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RuntimeSuspendStarted(/* [in] */ COR_PRF_SUSPEND_REASON suspendReason) override { contextSample(4);record(4,suspendReason,0); return S_OK; }
HRESULT STDMETHODCALLTYPE RuntimeSuspendFinished(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RuntimeSuspendAborted(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RuntimeResumeStarted(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RuntimeResumeFinished(void) override { contextSample(5);record(5,0,0); return S_OK; }
HRESULT STDMETHODCALLTYPE RuntimeThreadSuspended(/* [in] */ ThreadID threadId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RuntimeThreadResumed(/* [in] */ ThreadID threadId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE MovedReferences(/* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ObjectAllocated(/* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId) override { if (g_active && GetCurrentThreadId() == g_owner) { ULONG bytes=0; g_info->GetObjectSize(objectId, &bytes); ++g_objects; g_objectBytes += bytes; record(1, classId, bytes); } return S_OK; }
HRESULT STDMETHODCALLTYPE ObjectsAllocatedByClass(/* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ObjectReferences(/* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RootReferences(/* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionThrown(/* [in] */ ObjectID thrownObjectId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionEnter(/* [in] */ FunctionID functionId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionLeave(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionSearchFilterEnter(/* [in] */ FunctionID functionId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionSearchFilterLeave(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionSearchCatcherFound(/* [in] */ FunctionID functionId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionOSHandlerEnter(/* [in] */ UINT_PTR __unused) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionOSHandlerLeave(/* [in] */ UINT_PTR __unused) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionEnter(/* [in] */ FunctionID functionId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionLeave(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyEnter(/* [in] */ FunctionID functionId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyLeave(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionCatcherEnter(/* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionCatcherLeave(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE COMClassicVTableCreated(/* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots) override { return S_OK; }
HRESULT STDMETHODCALLTYPE COMClassicVTableDestroyed(/* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherFound(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherExecute(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ThreadNameChanged(/* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */
            _In_reads_opt_(cchName)  WCHAR name[  ]) override { return S_OK; }
HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(/* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason) override { ++g_gcStarted; record(2, cGenerations, reason); return S_OK; }
HRESULT STDMETHODCALLTYPE SurvivingReferences(/* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]) override { return S_OK; }
HRESULT STDMETHODCALLTYPE GarbageCollectionFinished(void) override { ++g_gcFinished; record(3,0,0); return S_OK; }
HRESULT STDMETHODCALLTYPE FinalizeableObjectQueued(/* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID) override { return S_OK; }
HRESULT STDMETHODCALLTYPE RootReferences2(/* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]) override { return S_OK; }
HRESULT STDMETHODCALLTYPE HandleCreated(/* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE HandleDestroyed(/* [in] */ GCHandleID handleId) override { return S_OK; }
HRESULT STDMETHODCALLTYPE InitializeForAttach(/* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ProfilerAttachComplete(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded(void) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ReJITCompilationStarted(/* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock) override { return S_OK; }
HRESULT STDMETHODCALLTYPE GetReJITParameters(/* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ReJITCompilationFinished(/* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock) override { return S_OK; }
HRESULT STDMETHODCALLTYPE ReJITError(/* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus) override { return S_OK; }
HRESULT STDMETHODCALLTYPE MovedReferences2(/* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]) override { return S_OK; }
HRESULT STDMETHODCALLTYPE SurvivingReferences2(/* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]) override { return S_OK; }
};
class Factory : public IClassFactory {
public:
HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid,void** value) override {if(iid==__uuidof(IUnknown)||iid==__uuidof(IClassFactory)){*value=this;return S_OK;}*value=nullptr;return E_NOINTERFACE;}
ULONG STDMETHODCALLTYPE AddRef() override{return 2;} ULONG STDMETHODCALLTYPE Release() override{return 1;}
HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* outer,REFIID iid,void** value) override {if(outer)return CLASS_E_NOAGGREGATION;static Profiler profiler;return profiler.QueryInterface(iid,value);}
HRESULT STDMETHODCALLTYPE LockServer(BOOL) override{return S_OK;}
};
extern "C" __declspec(dllexport) HRESULT STDAPICALLTYPE ProfilerGetClassObject(REFCLSID,REFIID iid,void** value){static Factory factory;return factory.QueryInterface(iid,value);}
extern "C" __declspec(dllexport) HRESULT STDAPICALLTYPE ProfilerCanUnloadNow(){return S_FALSE;}