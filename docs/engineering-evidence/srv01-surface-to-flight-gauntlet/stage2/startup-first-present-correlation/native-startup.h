// Temporary bounded diagnostic. Included inside the existing anonymous namespace.
struct StartupEvent { long long tick; unsigned code,frame; long long value; };
static StartupEvent startupEvents[256]{};
static unsigned startupCount{},startupFrame=~0u;
static bool startupPresented{},startupPostPresentCallback{};
static void StartupStamp(unsigned code,long long value=0) {
  if(startupFrame!=~0u && startupFrame>=8)return;
  if(startupCount>=256)return;
  LARGE_INTEGER t;QueryPerformanceCounter(&t);
  startupEvents[startupCount++]={t.QuadPart,code,startupFrame,value};
}
static void StartupDump() {
  LARGE_INTEGER f;QueryPerformanceFrequency(&f);
  std::printf("STARTUP_NATIVE {\"frequency\":%lld,\"events\":[",f.QuadPart);
  for(unsigned i=0;i<startupCount;i++){auto &e=startupEvents[i];std::printf("%s[%lld,%u,%u,%lld]",i?",":"",e.tick,e.code,e.frame,e.value);}
  std::printf("]}\n");std::fflush(stdout);
}
