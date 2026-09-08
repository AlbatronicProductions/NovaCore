"""Private same-index exact-copy economics and full GPU recomputation oracle."""
import os,sys,subprocess
from gauntlet import HERE,ROOT,OUT,HOST,assess,execute
SHADERS=ROOT/'native/NovaCore.Native/shaders'
NAME='production_spherical_billboard_incoming_prepare.comp'

def source(oracle,mode='copy'):
    s=(SHADERS/NAME).read_text()
    anchor='layout(set=0,binding=45,std430) buffer Physical{PhysicalVertex values[];} physical;'
    s=s.replace(anchor,anchor+'''
layout(set=0,binding=38,std430) readonly buffer PublishedPhysical{PhysicalVertex values[];} publishedPhysical;
layout(set=0,binding=44,std430) readonly buffer PublishedLattice{ivec4 values[];} publishedLattice;
bool sameBits(double a,double b){return all(equal(unpackDouble2x32(a),unpackDouble2x32(b)));}
''')
    anchor='  double radius=frame.transition.y;'
    s=s.replace(anchor,anchor+'''
  bool reusable=false;
  PupilFrame published=pupilFrames.current;
  if(preparation.ranges[1].z!=0u && published.metadata.x!=0u && vertex<published.metadata.z &&
     sameBits(radius,published.transition.y) && regionalCatalog.entries[0].storage.w==0u){
    dvec3 previous=canonicalDirection(publishedLattice.values[vertex].xyz,published);
    reusable=all(notEqual(direction,dvec3(0))) && sameBits(direction.x,previous.x) &&
      sameBits(direction.y,previous.y) && sameBits(direction.z,previous.z);
  }
'''+('' if oracle else '''  if(reusable){PhysicalVertex value=publishedPhysical.values[vertex];
'''+({'height':'double h=CandidatePhysicalHeightD(direction);value.body=dvec4(direction*(radius+h),h);',
      'normal':'value.normal=vec4(CandidatePhysicalNormalD(direction,radius),1.0);'}.get(mode,''))+'''
physical.values[vertex]=value;return;}
'''))
    if oracle:
        # Compute the unchanged full production result, then compare all 16 words.
        # Marker is host-checked and cleared after the final fence before publication.
        anchor='  physical.values[vertex].reserved=vec4(0.0);'
        s=s.replace(anchor,anchor+'''
  if(reusable){
    PhysicalVertex a=physical.values[vertex],b=publishedPhysical.values[vertex];
    bool exact=sameBits(a.body.x,b.body.x)&&sameBits(a.body.y,b.body.y)&&sameBits(a.body.z,b.body.z)&&sameBits(a.body.w,b.body.w)&&
      all(equal(floatBitsToUint(a.normal),floatBitsToUint(b.normal)))&&all(equal(floatBitsToUint(a.reserved),floatBitsToUint(b.reserved)));
    physical.values[vertex].reserved=uintBitsToFloat(uvec4(1u,exact?0u:1u,0u,0u));
  }
''')
    return s

if __name__=='__main__':
    mode=sys.argv[1];oracle=mode=='oracle'
    assert mode in ['oracle','copy','height','normal']
    path=OUT/('same-index-'+mode+'.comp');path.write_text(source(oracle,mode))
    output=path.with_suffix('.spv')
    subprocess.run([str(__import__('pathlib').Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(SHADERS),str(path),'-o',str(output)],check=True)
    deployed=HOST/'shaders'/(NAME+'.spv');saved=deployed.read_bytes()
    try:
        deployed.write_bytes(output.read_bytes())
        env=assess.environment();env.update(NOVACORE_EARTH_ROUTE_VALIDATION='regional',NOVACORE_PERFORMANCE_FRAME_LOG='1')
        if oracle:env['NOVACORE_PREP_GPU_ORACLE']='1';env['NOVACORE_PREP_HEAPS']='1'
        execute('same-index-'+mode,env)
    finally:deployed.write_bytes(saved)
