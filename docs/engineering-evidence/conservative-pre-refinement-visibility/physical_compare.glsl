// Diagnostic-only exact records, keyed within one immutable submitted draw.
struct PhysicalRecord {dvec4 body;dvec4 direction;vec4 clip;vec4 barycentric;vec4 nearField;uvec4 identity;};
layout(set=0,binding=59,std430) buffer PhysicalSamples {uint count;uint enabled;uint spare0;uint spare1;PhysicalRecord records[];} samples;
layout(set=0,binding=64,std430) buffer PhysicalTable {uint values[];} physicalTable;
layout(set=0,binding=65,std430) buffer PhysicalResult {uint values[];} physicalResult;
const uint PHYSICAL_CAPACITY=4194304u,TABLE_CAPACITY=8388608u;
uint PhysicalHash(uint patchId,vec3 bary){uint h=2166136261u;h=(h^patchId)*16777619u;uvec3 b=floatBitsToUint(bary);for(int i=0;i<3;i++)h=(h^b[i])*16777619u;return h&(TABLE_CAPACITY-1u);}
bool PhysicalKey(PhysicalRecord r,uint patchId,vec3 bary){return r.identity.x==patchId&&all(equal(floatBitsToUint(r.barycentric.xyz),floatBitsToUint(bary)));}
bool ExactDouble4(dvec4 a,dvec4 b){for(int i=0;i<4;i++)if(any(notEqual(unpackDouble2x32(a[i]),unpackDouble2x32(b[i]))))return false;return true;}
bool ExactPhysical(PhysicalRecord a,PhysicalRecord b){return ExactDouble4(a.body,b.body)&&ExactDouble4(a.direction,b.direction)&&all(equal(floatBitsToUint(a.clip),floatBitsToUint(b.clip)))&&all(equal(floatBitsToUint(a.barycentric),floatBitsToUint(b.barycentric)))&&all(equal(floatBitsToUint(a.nearField),floatBitsToUint(b.nearField)));}
