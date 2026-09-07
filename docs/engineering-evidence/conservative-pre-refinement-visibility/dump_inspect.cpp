// Read-only DbgEng host. Compile against the Microsoft SDK DbgEng.h, not vendored source.
#include <Windows.h>
#include <cstdio>
#include <cstdlib>
#include <DbgEng.h>
struct Sink final : IDebugOutputCallbacks {
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, void** value) override {
    if(id==__uuidof(IUnknown)||id==__uuidof(IDebugOutputCallbacks)){*value=this;return S_OK;}
    *value=nullptr;return E_NOINTERFACE;
  }
  ULONG STDMETHODCALLTYPE AddRef() override{return 1;}
  ULONG STDMETHODCALLTYPE Release() override{return 0;}
  HRESULT STDMETHODCALLTYPE Output(ULONG,PCSTR text) override {std::fputs(text,stdout);std::fflush(stdout);return S_OK;}
};
int main(int argc,char**argv){
  if(argc!=3)return 2;
  auto module=LoadLibraryW(L"C:\\Windows\\System32\\dbgeng.dll");
  auto create=reinterpret_cast<HRESULT(WINAPI*)(REFIID,PVOID*)>(GetProcAddress(module,"DebugCreate"));
  IDebugClient*client{};IDebugControl*control{};Sink output;
  auto check=[](HRESULT hr){if(FAILED(hr)){std::printf("HRESULT %08lx\n",hr);std::exit(1);}};
  check(create(__uuidof(IDebugClient),reinterpret_cast<void**>(&client)));
  check(client->SetOutputCallbacks(&output));
  check(client->QueryInterface(__uuidof(IDebugControl),reinterpret_cast<void**>(&control)));
  check(client->OpenDumpFile(argv[1]));check(control->WaitForEvent(0,INFINITE));
  check(control->Execute(DEBUG_OUTCTL_THIS_CLIENT,argv[2],DEBUG_EXECUTE_ECHO));
  client->EndSession(DEBUG_END_PASSIVE);control->Release();client->Release();return 0;
}
