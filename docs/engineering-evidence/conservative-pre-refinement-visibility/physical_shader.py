"""Exact post-TES physical-record A/B; no raw archive, bounded GPU table."""
from pathlib import Path
HERE=Path(__file__).resolve().parent

def variant(source,compare):
    source=source.replace('void main()', (HERE/'physical_compare.glsl').read_text()+'\nvoid main()')
    record="""
  PhysicalRecord current;
  current.body=dvec4(preparedBody+direction*localDisplacement,height);
  current.direction=dvec4(direction,localWeight);current.clip=gl_Position;
  current.barycentric=vec4(gl_TessCoord,0);current.nearField=vec4(float(nearHeight),vec3(nearGradient));
  current.identity=uvec4(gl_PrimitiveID,any(equal(gl_TessCoord,vec3(0)))?1u:0u,0u,0u);
"""
    if not compare:
        code=record+"""
  uint id=atomicAdd(samples.count,1u);
  if(id<PHYSICAL_CAPACITY)samples.records[id]=current;
  else atomicAdd(physicalResult.values[5],1u);
"""
    else:
        code=record+"""
  atomicAdd(physicalResult.values[0],1u);
  uint slot=PhysicalHash(uint(gl_PrimitiveID),gl_TessCoord);bool found=false;
  for(uint probe=0u;probe<256u;probe++){
    uint index=physicalTable.values[slot];if(index==0xffffffffu)break;
    PhysicalRecord previous=samples.records[index];
    if(PhysicalKey(previous,uint(gl_PrimitiveID),gl_TessCoord)){
      found=true;
      if(!ExactPhysical(previous,current)){atomicAdd(physicalResult.values[1],1u);atomicMin(physicalResult.values[6],uint(gl_PrimitiveID));}
      break;
    }
    slot=(slot+1u)&(TABLE_CAPACITY-1u);
  }
  if(!found)atomicAdd(physicalResult.values[2],1u);
"""
    end=source.rfind('}');return source[:end]+code+source[end:]
