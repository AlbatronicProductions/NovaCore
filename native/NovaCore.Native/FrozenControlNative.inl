bool FrozenAdmissionsEnabled(App& a){
  if(!a.frozen.Active())return false;
  auto request=a.startup.CaptureDisableRequest();
  if(request&&a.frozen.AdmissionsEnabled()){
    a.frozen.SuspendAdmissions();a.frozenDisableRequest=request;
    a.causal.Emit(Phase(30),Kind::Info,0,{1,request,a.frozen.LastAdmittedIdentity(),a.frozen.lastDurableIdentity.load(),
      a.frozen.slots[0].state.load(),a.frozen.slots[1].state.load(),a.productionBillboardGeneration,a.productionBillboardPreparedFrameIdentity});
    a.startup.AcknowledgeCaptureDisable(request);
  }
  if(a.frozenDisableRequest&&request!=a.frozenDisableRequest)throw std::runtime_error("capture disable request changed");
  return a.frozen.AdmissionsEnabled();
}
void PollFrozenDrain(App& a){
  if(!a.frozenDisableRequest||a.frozenDrainReported)return;
  if(a.frozen.Drained()){
    a.frozenDrainReported=true;
    a.causal.Emit(Phase(30),Kind::Info,0,{2,a.frozenDisableRequest,a.frozen.LastAdmittedIdentity(),a.frozen.lastDurableIdentity.load(),
      a.frozen.slots[0].state.load(),a.frozen.slots[1].state.load(),a.productionBillboardGeneration,a.productionBillboardPreparedFrameIdentity});
  }
}
