const W='11111111-1111-4111-8111-111111111111',S='33333333-3333-4333-8333-333333333333',P='44444444-4444-4444-8444-444444444444',B='a'.repeat(64);
function rawEvent(sequence,qpc){
  const b=Buffer.alloc(256);b.writeBigUInt64LE(BigInt(sequence),0);b.writeBigUInt64LE(BigInt(qpc),8);b.writeBigUInt64LE(8n,32);b.writeBigUInt64LE(1n,40);b.writeBigUInt64LE(18446744073709551615n,152);
  let h=14695981039346656037n;for(const byte of b.subarray(0,240))h=BigInt.asUintN(64,(h^BigInt(byte))*1099511628211n);
  b.writeBigUInt64LE(h,240);b.writeBigUInt64LE(BigInt(sequence),248);return b;
}
function packet(nonce,t,serial=1,witness=W){
  const qpc=String(Math.floor(t*10000)),raw=rawEvent(serial,qpc),beacon={schema:'NovaCore.CpuBeacon/1',witness,probeId:P,pid:2,startUtcTicks:'639261612520224860',port:58762,url:'http://fixture:58762/token/'};
  return {schema:'NovaCore.BlackoutWitness/1',witness,nonce,serverMonotonicNs:String(Math.floor(t*1e6)),serverUtcNs:'1790000000000000000',mode:'fixture',localCommittedSerial:serial,localSkippedSamples:0,committedSession:S,committedMonotonicNs:String(Math.floor(t*1e6)),diskError:null,receiverBuild:B,remainingSeconds:null,beacon,
    sample:{schema:'NovaCore.BlackoutWitness/1',witness,serial,sampleMonotonicNs:String(Math.floor(t*1e6)),utcNs:'1790000000000000000',observation:{fixture:true,session:S,producerPid:1,observerPid:2,producerStartUtcTicks:'639261612505478296',observerStartUtcTicks:'639261612520224860',qpc,qpcFrequency:10000000,producedBefore:String(serial),producedAfter:String(serial),headerProduced:String(serial),consumed:String(serial),durable:String(serial),observerHeartbeat:qpc,producerHeartbeat:qpc,producerProcess:'alive',observerProcess:'alive',producerFault:'0',observerFault:'0',ready:'1',done:'0',dropped:'0',headerWithinBracket:true,stableProduced:true,eventValid:true,admissionVerified:true,reservedBytes:'87138304',latestEventBase64:raw.toString('base64'),latestEventWords:Array.from({length:30},(_,i)=>raw.readBigUInt64LE(i*8).toString())}}};
}

module.exports={packet,W,S,P,B};
