"""Diagnostic variants only. Production shaders are read, never edited."""
import re

TES_RECORD = '''
struct SampleRecord { dvec4 body;dvec4 direction;vec4 clip;vec4 barycentric;vec4 nearField;uvec4 identity; };
layout(set=0,binding=59,std430) buffer Samples {uint count;uint enabled;uint spare0;uint spare1;SampleRecord values[];} samples;
layout(location=16) flat out uint sampleIdentity;
'''
TES_STORE = '''
  sampleIdentity=0u;
  if(samples.enabled!=0u){
    uint id=atomicAdd(samples.count,1u);sampleIdentity=id;
    if(id<16777216u){
      samples.values[id].body=dvec4(preparedBody+direction*localDisplacement,height);
      samples.values[id].direction=dvec4(direction,localWeight);
      samples.values[id].clip=gl_Position;
      samples.values[id].barycentric=vec4(gl_TessCoord,0);
      samples.values[id].nearField=vec4(float(nearHeight),vec3(nearGradient));
      samples.values[id].identity=uvec4(gl_PrimitiveID,any(equal(gl_TessCoord,vec3(0)))?1u:0u,0u,0u);
    }
  }
'''

PRIMITIVES = '''
struct PrimitiveRecord {vec4 clip0;vec4 clip1;vec4 clip2;uvec4 samples;uvec4 counters;};
layout(set=0,binding=60,std430) buffer Primitives {uint count;uint enabled;uint spare0;uint spare1;PrimitiveRecord values[];} primitives;
'''

def tessellation(source):
    source=source.replace('void main()',TES_RECORD+'\nvoid main()')
    end=source.rfind('}')
    return source[:end]+TES_STORE+source[end:]

def geometry(tes):
    # Exact interface passthrough, including flat qualifiers and built-in position.
    declarations=re.findall(r'layout\(location=(\d+)\)\s*(flat\s+)?out\s+(\w+)\s+(\w+);',tes)
    assert len(declarations)==16
    s='#version 460\nlayout(triangles) in;layout(triangle_strip,max_vertices=3) out;\n'+PRIMITIVES
    for loc,flat,typ,name in declarations:
        s+=f'layout(location={loc}) {flat}in {typ} source_{name}[];layout(location={loc}) {flat}out {typ} {name};\n'
    s+='layout(location=16) flat in uint sampleIdentity[];\nvoid main(){uint id=0u;\n'
    s+='if(primitives.enabled!=0u){id=atomicAdd(primitives.count,1u);if(id<8388608u){primitives.values[id].clip0=gl_in[0].gl_Position;primitives.values[id].clip1=gl_in[1].gl_Position;primitives.values[id].clip2=gl_in[2].gl_Position;primitives.values[id].samples=uvec4(sampleIdentity[0],sampleIdentity[1],sampleIdentity[2],0);}}\n'
    s+='for(int i=0;i<3;++i){gl_Position=gl_in[i].gl_Position;gl_PrimitiveID=int(id);\n'
    for _,_,_,name in declarations:s+=f'{name}=source_{name}[i];\n'
    return s+'EmitVertex();}EndPrimitive();}\n'

def fragment_capture(source, ids=False):
    source=source.replace('void main()',PRIMITIVES+'\nvoid main()')
    source=source.replace('  bool anchored=', '  if(primitives.enabled!=0u)atomicAdd(primitives.values[gl_PrimitiveID].counters.x,1u);\n  bool anchored=')
    end=source.rfind('}')
    code='\nif(primitives.enabled!=0u){atomicAdd(primitives.values[gl_PrimitiveID].counters.y,1u);'
    if ids:code+='uint id=uint(gl_PrimitiveID)+1u;outColor=vec4(float(id&1023u),float((id>>10u)&1023u),float(id>>20u),16384.0);'
    return source[:end]+code+'}\n'+source[end:]

def variant(source, probe):
    if probe=='normal':return source
    if probe in ['fragment-diagnostics','fragment-ncsm1']:
        needle='uint diagnostic=floatBitsToUint(lighting.radianceGlowEnabled.w)>>16;'
        assert source.count(needle)==1
        source=source.replace(needle,'uint diagnostic=0u;')
        if probe=='fragment-ncsm1':source=source.replace('bool anchored=(productionLayer&0x40000000u)!=0u;','bool anchored=true;')
    elif probe=='no-facility':
        source=source.replace('float facilitySunVisibility=AuthoredFacilitySunVisibility(-viewDirection,lightDirection,facilityTests,facilityEntered);','float facilitySunVisibility=1.0;')
    else:raise ValueError(probe)
    return source
