struct Checkpoint { int id,managedThread,c0,c1,c2,region; DWORD osThread; long long counter,total; UINT_PTR address,ptr,limit,bytes,uoh; long long objects,objectBytes; LONG starts,finishes; };
static Checkpoint checkpoints[16]; static int checkpointCount=0;
struct Window { int id; DWORD owner; long long delta; ULONG64 objects,bytes; LONG entries,contextCount; ContextSample contexts[32]; };
static Window windows[4]; static int windowCount=0;
extern "C" __declspec(dllexport) void ResetRecords(){ checkpointCount=windowCount=0; }
extern "C" __declspec(dllexport) void CheckpointRecord(int id,long long counter,long long total,int c0,int c1,int c2,int managedThread,int region){
 if(checkpointCount>=16)return;
 auto p=(volatile UINT_PTR*)((unsigned char*)((void**)__readgsqword(0x58))[g_tlsIndex]+g_tlsOffset);
 checkpoints[checkpointCount++]={id,managedThread,c0,c1,c2,region,GetCurrentThreadId(),counter,total,(UINT_PTR)p,p[1],p[2],p[3],p[4],g_active?(long long)g_objects:-1,g_active?(long long)g_objectBytes:-1,g_gcStarted.load(),g_gcFinished.load()};
}
extern "C" __declspec(dllexport) void StopCapture(int id,long long delta){
 contextSample(7);record(7,g_gcStarted.load(),g_gcFinished.load());g_active=false;
 if(windowCount>=4)return;
 auto& w=windows[windowCount++];w.id=id;w.owner=g_owner;w.delta=delta;w.objects=g_objects;w.bytes=g_objectBytes;w.entries=g_next;w.contextCount=g_contextNext;
 memcpy(w.contexts,g_contextSamples,sizeof(w.contexts));
}
extern "C" __declspec(dllexport) void DumpCapture(){
 for(int i=0;i<checkpointCount;i++){auto&s=checkpoints[i];printf("CHECKPOINT id=%d counter=%lld totalApprox=%lld gc0=%d gc1=%d gc2=%d managedThread=%d osThread=%lu callerRegion=%d address=%llu ptr=%llu limit=%llu bytes=%llu uoh=%llu unused=%llu objects=%lld objectBytes=%lld gcStarts=%ld gcFinishes=%ld\n",s.id,s.counter,s.total,s.c0,s.c1,s.c2,s.managedThread,s.osThread,s.region,(unsigned long long)s.address,(unsigned long long)s.ptr,(unsigned long long)s.limit,(unsigned long long)s.bytes,(unsigned long long)s.uoh,(unsigned long long)(s.limit-s.ptr),s.objects,s.objectBytes,s.starts,s.finishes);}
 for(int i=0;i<windowCount;i++){auto&w=windows[i];printf("WINDOW id=%d owner=%lu delta=%lld objects=%llu objectBytes=%llu entries=%ld contextCount=%ld\n",w.id,w.owner,w.delta,w.objects,w.bytes,w.entries,w.contextCount);for(int j=0;j<w.contextCount&&j<32;j++){auto&s=w.contexts[j];printf("CONTEXT window=%d phase=%ld thread=%lu qpc=%lld address=%llu ptr=%llu limit=%llu bytes=%llu uoh=%llu unused=%llu\n",w.id,s.phase,s.thread,s.qpc,(unsigned long long)s.address,(unsigned long long)s.ptr,(unsigned long long)s.limit,(unsigned long long)s.bytes,(unsigned long long)s.uoh,(unsigned long long)(s.limit-s.ptr));}}
 for(LONG i=0;i<g_next&&i<8192;i++){auto&e=g_entries[i];if(e.kind!=6)printf("EVENT kind=%ld thread=%lu qpc=%lld a=%llu b=%llu\n",e.kind,e.thread,e.timestamp,(unsigned long long)e.a,(unsigned long long)e.b);}
 fflush(stdout);
}
