// Temporary diagnostic only, inserted inside NovaCoreNative.cpp's anonymous namespace.
// Codes: pump1/2 frame3/4 fence5/6 callback7/8 acquire9/10 record11/12 submit13/14
// present15/16 recreate17/18 deviceIdle19/20 dispatch21/22 windowProc23/24
// postCallback25/26 swap27/28 window29/30 diagnosticDelegate31/32.
struct GapEvent { long long qpc; unsigned long long processCpu,threadCpu,readBytes,writeBytes,aux; unsigned frame,code,thread; };
static std::array<GapEvent,250000> gapEvents{};
static volatile LONG gapCount=0;
static unsigned gapFrame=0;
static unsigned long long GapFileTime(FILETIME f){return (static_cast<unsigned long long>(f.dwHighDateTime)<<32)|f.dwLowDateTime;}
static void GapStamp(unsigned code,unsigned long long aux=0){
  const LONG slot=InterlockedIncrement(&gapCount)-1;if(slot>=static_cast<LONG>(gapEvents.size()))return;
  LARGE_INTEGER q;QueryPerformanceCounter(&q);FILETIME c{},e{},k{},u{},tk{},tu{};IO_COUNTERS io{};
  GetProcessTimes(GetCurrentProcess(),&c,&e,&k,&u);GetThreadTimes(GetCurrentThread(),&c,&e,&tk,&tu);GetProcessIoCounters(GetCurrentProcess(),&io);
  gapEvents[slot]={q.QuadPart,GapFileTime(k)+GapFileTime(u),GapFileTime(tk)+GapFileTime(tu),io.ReadTransferCount,io.WriteTransferCount,aux,gapFrame,code,GetCurrentThreadId()};
}
struct GapScope {unsigned end;unsigned long long aux;GapScope(unsigned begin,unsigned finish,unsigned long long value=0):end(finish),aux(value){GapStamp(begin,aux);}~GapScope(){GapStamp(end,aux);}};
static void GapDump(){
  LARGE_INTEGER f;QueryPerformanceFrequency(&f);const LONG observed=gapCount;const auto count=std::min<LONG>(observed,static_cast<LONG>(gapEvents.size()));
  std::array<bool,8192> keep{};for(unsigned i=0;i<5;i++)keep[i]=true;
  unsigned slow=0;for(LONG i=1;i<count;i++)if((gapEvents[i].qpc-gapEvents[i-1].qpc)*1000.0/f.QuadPart>25){
    slow++;const auto frame=gapEvents[i].frame;for(unsigned j=frame>3?frame-3:0;j<=std::min<unsigned>(8191,frame+25);j++)keep[j]=true;
  }
  std::printf("GAP_NATIVE_META frequency=%lld records=%ld capacity=%zu dropped=%ld slowAdjacent=%u\n",f.QuadPart,count,gapEvents.size(),std::max<LONG>(0,gapCount-count),slow);
  std::printf("GAP_NATIVE_COLUMNS qpc,processCpu100ns,threadCpu100ns,readBytes,writeBytes,aux,frame,code,osThread\n");
  for(LONG i=0;i<count;i++){const auto&r=gapEvents[i];if(r.frame<keep.size()&&keep[r.frame])std::printf("GAP_NATIVE %lld,%llu,%llu,%llu,%llu,%llu,%u,%u,%u\n",r.qpc,r.processCpu,r.threadCpu,r.readBytes,r.writeBytes,r.aux,r.frame,r.code,r.thread);}
}
