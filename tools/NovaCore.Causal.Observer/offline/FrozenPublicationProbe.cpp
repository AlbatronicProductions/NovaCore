// CPU-only exact Writer/Win32 publication ownership regression. No Vulkan calls.
#define main FrozenMockEntry
#include "FrozenCaptureMock.cpp"
#undef main
#include <fstream>
static unsigned checks{};
static void Check(bool ok,const char* why){if(!ok)throw std::runtime_error(why);++checks;}
static uint64_t IdentityAt(HANDLE handle){Header h;DWORD got=0;LARGE_INTEGER zero{};Check(SetFilePointerEx(handle,zero,nullptr,FILE_BEGIN)!=0,"pin seek");Check(ReadFile(handle,&h,sizeof(h),&got,nullptr)&&got==sizeof(h),"pin header");return h[Identity];}
static HANDLE OpenPin(const std::filesystem::path& path,DWORD share){auto h=CreateFileW(path.c_str(),GENERIC_READ,share,nullptr,OPEN_EXISTING,FILE_ATTRIBUTE_NORMAL,nullptr);Check(h!=INVALID_HANDLE_VALUE,"pin open");return h;}
static void WaitDurable(Writer& writer,uint64_t id){auto start=GetTickCount64();while(writer.lastDurableIdentity<id&&!writer.failure&&GetTickCount64()-start<2000)Sleep(1);Check(writer.lastDurableIdentity==id&&!writer.failure,"publication did not complete");}
int main(int argc,char** argv){try{
 Check(argc==2,"output required");std::filesystem::path root=argv[1];Check(!std::filesystem::exists(root),"fresh output required");std::filesystem::create_directories(root);
 // Hold the exact alarm/audit sharing modes throughout the old/new call.
 for(DWORD share:{DWORD(FILE_SHARE_READ|FILE_SHARE_DELETE),DWORD(FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE)}){
  auto folder=root/std::to_string(share);std::filesystem::create_directories(folder);auto old=folder/L"snapshot.bin",next=folder/L"snapshot.tmp";
  Header before;before.Begin();before[Identity]=1;Header after=before;after[Identity]=2;
  {std::ofstream out(old,std::ios::binary);out.write(reinterpret_cast<char*>(&before),sizeof(before));}
  {auto file=CreateFileW(next.c_str(),GENERIC_WRITE,FILE_SHARE_READ,nullptr,CREATE_NEW,FILE_ATTRIBUTE_NORMAL,nullptr);Check(file!=INVALID_HANDLE_VALUE,"temporary create");FileWrite(file,&after,sizeof(after));Check(FlushFileBuffers(file)!=0,"temporary flush");CloseHandle(file);}
  auto pin=OpenPin(old,share);
  BOOL moved=MoveFileExW(next.c_str(),old.c_str(),MOVEFILE_REPLACE_EXISTING|MOVEFILE_WRITE_THROUGH);DWORD error=GetLastError();
  Check(!moved&&error==ERROR_ACCESS_DENIED,"old pinned replacement witness differs");
  PublishFile(next,old);Check(IdentityAt(pin)==1,"reader's immutable identity changed");auto fresh=OpenPin(old,share);Check(IdentityAt(fresh)==2,"new path not published");CloseHandle(fresh);CloseHandle(pin);
 }
 for(unsigned transport:{0u,3u,4u})for(bool permitted:{true,false}){
  auto folder=root/((permitted?"worker-overlap-":"worker-denied-")+std::to_string(transport));std::filesystem::create_directories(folder);
  Writer writer;std::array<std::vector<uint8_t>,SlotCount> storage;for(size_t i=0;i<SlotCount;i++){storage[i].resize(16384);writer.slots[i].mapped=storage[i].data();}writer.Start(folder);
  std::vector<uint8_t> topologyBytes(336);
  auto now=GetTickCount64();auto queue=[&](uint64_t id){auto* slot=writer.Reserve(now+(id-1)*1001);Check(slot!=nullptr,"reserve");Fill(*slot,id*100,false);
    if(transport>=3){auto& h=slot->header;auto& t=writer.topologies[0];h[CaptureTransport]=transport;h[IndexHandle]=101;h[Reserved55]=10;h[IndexReadbackFrame]=1;h[GpuCopiedBytes]=64*h[Vertices]+16*h[Triangles]+188;
      if(id==1){t.state=Proven;t.mapped=topologyBytes.data();t.authority={1,101,10,h[TopologyHash],h[TopologyFamily],30,28,2,0,336,4,201,11,1,72,64,1,64,20,30,0,0};std::memcpy(t.mapped,slot->mapped+h.sections[1].offset-HeaderBytes,336);}
      slot->topology=&writer.PinTopology(h);std::memset(slot->mapped+h.sections[1].offset-HeaderBytes,0xcc,336);Check(t.pins==1,"topology pin before handoff");
    }
    if(transport==4){auto& h=slot->header;auto* b=slot->mapped+h.sections[0].offset-HeaderBytes;std::array<uint8_t,64> extra{};
      for(size_t i=0;i<4;++i)std::memcpy(extra.data()+i*16,b+i*64+48,16);
      for(size_t i=0;i<h[Vertices];++i)std::memmove(b+i*48,b+i*64,48);
      std::memcpy(b+h[Vertices]*48,extra.data(),64);h[HeaderFlags]|=4;
    }
    writer.Submitted(id*100,id+63);writer.Presented(id*100,0);slot->header[Completed]=id*100;slot->header[CompletedSubmission]=id+63;slot->state.store(Ready,std::memory_order_release);};
  queue(1);WaitDurable(writer,1);queue(2);WaitDurable(writer,2);
  auto pin=OpenPin(folder/L"snapshot-1.bin",permitted?FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE:FILE_SHARE_READ);
  queue(3);writer.Stop(); // Retain the reader until the real worker has exited.
  if(permitted){Check(!writer.failure&&writer.lastDurableIdentity==3,"overlap prevented publication");Check(IdentityAt(pin)==1,"writer mutated reader bytes");auto fresh=OpenPin(folder/L"snapshot-1.bin",FILE_SHARE_READ|FILE_SHARE_DELETE);Check(IdentityAt(fresh)==3,"worker publication absent");CloseHandle(fresh);}
  else {Check(writer.failure==1&&writer.lastDurableIdentity==2,"failed storage falsely marked durable");Check(writer.failureIdentity==3&&writer.failureFrame==300&&writer.failureOperation==Publish&&writer.failureNativeError==ERROR_SHARING_VIOLATION,"shutdown fault boundary lost");Check(std::string(writer.FailureText())=="frozen replacement failed","shutdown fault text lost");Check(std::filesystem::exists(folder/L"snapshot-1.tmp"),"unpublished capture discarded");}
  if(transport>=3){Check(writer.topologies[0].pins==0,"topology pin survived worker success/failure");auto* slot=&writer.slots[1];Check(slot->topology==nullptr,"worker retained stale witness pointer");}
  CloseHandle(pin);
 }
 std::cout<<"PASS publication checks="<<checks<<"; old pinned failure reproduced; exact Writer overlap and shutdown fault identity; GPU calls=0\n";return 0;
}catch(const std::exception& error){std::cerr<<"FAIL "<<error.what()<<'\n';return 1;}}
