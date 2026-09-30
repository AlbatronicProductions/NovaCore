// Main/UI-thread lifecycle and timer/checkpoint polling. Read is a cached copy.
extern "C" NC_API NcResult __cdecl nc_player_gpu_memory_begin(uint64_t parent){
  try{
    if(playerGpuMemory||gApp||!IsWindow(reinterpret_cast<HWND>(parent))||GetWindowThreadProcessId(reinterpret_cast<HWND>(parent),nullptr)!=GetCurrentThreadId())return NC_INVALID_ARGUMENT;
    auto owner=std::make_unique<PlayerGpuMemory>();owner->Start(reinterpret_cast<HWND>(parent));
    playerGpuMemory=std::move(owner);return NC_SUCCESS;
  }catch(...){return NC_FAILURE;}
}
extern "C" NC_API NcResult __cdecl nc_player_gpu_memory_read(NcPlayerGpuMemorySnapshot* snapshot){
  if(!snapshot||snapshot->size!=sizeof(*snapshot)||snapshot->version!=1)return NC_INVALID_ARGUMENT;
  try{
    if(!ReadPlayerGpuMemory(*snapshot)){*snapshot={};snapshot->size=sizeof(*snapshot);snapshot->version=1;snapshot->status=4;}
    return NC_SUCCESS;
  }catch(...){return NC_FAILURE;}
}
extern "C" NC_API NcResult __cdecl nc_player_gpu_memory_end(){
  if(gApp||(playerGpuMemory&&!playerGpuMemory->OnOwnerThread()))return NC_INVALID_ARGUMENT;
  try{playerGpuMemory.reset();return NC_SUCCESS;}catch(...){return NC_FAILURE;}
}
extern "C" NC_API NcResult __cdecl nc_player_gpu_memory_poll(){
  try{if(!playerGpuMemory)return NC_INVALID_ARGUMENT;playerGpuMemory->Poll();return NC_SUCCESS;}
  catch(...){return NC_FAILURE;}
}
