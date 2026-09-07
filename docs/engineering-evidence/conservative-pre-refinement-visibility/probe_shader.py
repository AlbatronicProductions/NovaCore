"""Diagnostic sideband only; no geometry shader and no culling behavior."""
from pathlib import Path
HERE=Path(__file__).resolve().parent
RECORD='''
struct VisibilityPatch {vec4 upper0;vec4 upper1;uvec4 counts;uvec4 identity;};
layout(set=0,binding=61,std430) buffer VisibilityProbe {uint patchCount;uint enabled;uint frame;uint generation;VisibilityPatch patches[];} visibilityProbe;
'''
def control(source):
    source=source.replace('#version 460','#version 460\n#extension GL_ARB_gpu_shader_fp64 : require')
    bound=(HERE/'conservative_bound.glsl').read_text().replace('#extension GL_ARB_gpu_shader_fp64 : require','')
    source=source.replace('void main(){',RECORD+'\nlayout(location=18) patch out vec4 visibilityUpper0;\nlayout(location=19) patch out vec4 visibilityUpper1;\nlayout(location=20) patch out int visibilityReject;\n'+bound+'\nvoid main(){')
    source=source.replace('  if(i==0u){','''  if(i==0u){
    vec4 upper0,upper1;int rejected=ConservativePatchBound(upper0,upper1);
    visibilityUpper0=upper0;visibilityUpper1=upper1;visibilityReject=rejected;
    if(visibilityProbe.enabled!=0u){
      uint id=uint(gl_PrimitiveID);atomicMax(visibilityProbe.patchCount,id+1u);
      if(id<1500000u){
        visibilityProbe.patches[id].upper0=upper0;visibilityProbe.patches[id].upper1=upper1;
        visibilityProbe.patches[id].identity.x=uint(rejected+1);
        bool before=false;
        for(int p=0;p<6;p++){bool outside=true;for(int v=0;v<3;v++){vec4 c=gl_in[v].gl_Position;float d=(p%2==0?c[p/2]:-c[p/2])+(p==4?0.0:c.w);outside=outside&&d<0.0;}before=before||outside;}
        visibilityProbe.patches[id].identity.z=before?1u:0u;
      }
    }
''')
    return source
def evaluation(source):
    source=source.replace('void main()',RECORD+'\nlayout(location=18) patch in vec4 visibilityUpper0;\nlayout(location=19) patch in vec4 visibilityUpper1;\nlayout(location=20) patch in int visibilityReject;\nvoid main()')
    store='''
  if(visibilityProbe.enabled!=0u&&uint(gl_PrimitiveID)<1500000u){
    uint id=uint(gl_PrimitiveID),insideMask=0u,violations=0u;dvec4 c=dvec4(gl_Position);
    for(int plane=0;plane<6;plane++){
      double d=(plane%2==0?c[plane/2]:-c[plane/2])+(plane==4?0.0:c.w);
      double bound=double(plane<4?visibilityUpper0[plane]:visibilityUpper1[plane-4]);
      if(d>=0.0)insideMask|=1u<<uint(plane);
      if(d>bound||isnan(d)||isinf(d))violations++;
      if(plane==visibilityReject&&d>=0.0)atomicAdd(visibilityProbe.patches[id].identity.w,1u);
    }
    atomicAdd(visibilityProbe.patches[id].counts.x,1u);
    atomicAdd(visibilityProbe.patches[id].counts.y,violations);
    if(insideMask==63u)atomicAdd(visibilityProbe.patches[id].counts.z,1u);
    if(localWeight>0.0)atomicAdd(visibilityProbe.patches[id].counts.w,1u);
    atomicOr(visibilityProbe.patches[id].identity.y,insideMask);
  }
'''
    end=source.rfind('}');return source[:end]+store+source[end:]


def rejection(source):
    """Only after final-bound oracle gates: no sideband, unchanged production TES."""
    source=source.replace('#version 460','#version 460\n#extension GL_ARB_gpu_shader_fp64 : require')
    bound=(HERE/'conservative_bound.glsl').read_text().replace('#extension GL_ARB_gpu_shader_fp64 : require','')
    source=source.replace('void main(){',bound+'\nvoid main(){')
    marker='    gl_TessLevelOuter[0]=a;'
    replacement="""    if(max(a,max(b,c))>1.0){
      vec4 upper0,upper1;
      if(ConservativePatchBound(upper0,upper1)>=0){
        gl_TessLevelOuter[0]=0;gl_TessLevelOuter[1]=0;gl_TessLevelOuter[2]=0;gl_TessLevelInner[0]=0;
        return;
      }
    }
"""+marker
    assert marker in source
    return source.replace(marker,replacement)
