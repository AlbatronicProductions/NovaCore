// Diagnostic TCS bound. Input corners are the actual accepted VS outputs.
// See bound-proof.md for operation counts and the physical-envelope proof.
#extension GL_ARB_gpu_shader_fp64 : require
struct BoundPresentation {vec4 centerRadius;vec4 colorDistant;vec4 blendMetricState;uvec4 identity;vec4 surface;uvec4 hooks;vec4 ringGeometry;vec4 ringOrientation;vec4 ringColor;vec4 bodyOrientation;vec4 localDetail;vec4 centerLow;};
layout(set=0,binding=6,std430) readonly buffer BoundPresentations {BoundPresentation values[];} boundPresentations;
const double BoundU32=1.1920928955078125e-7LF;
const double BoundU64=2.220446049250313080847263336181640625e-16LF;
double BoundGamma(double n,double u){return n*u/(1.0-n*u);}
dvec3 BoundCrossMagnitude(dvec3 a,dvec3 b){return dvec3(a.y*b.z+a.z*b.y,a.z*b.x+a.x*b.z,a.x*b.y+a.y*b.x);}
dvec3 BoundRotate(dvec3 p,dvec4 q){return p+2.0*cross(q.xyz,cross(q.xyz,p)+q.w*p);}
float BoundRoundUp(double x){float v=float(x);uint b=floatBitsToUint(v);return uintBitsToFloat(v>=0.0?b+1u:b-1u);}
int ConservativePatchBound(out vec4 upper0,out vec4 upper1){
  upper0=vec4(3.402823e38);upper1=upper0;
  double envelope=double(uintBitsToFloat(counters.values[9]));
  // Upward FP32 encoding of 11 * sqrt(3)/2 plus the derived field-operation bound.
  // Current authoritative envelope is 11.8125868; fail closed on another contract.
  if(envelope<9.526279449462891LF||any(lessThan(vec3(i7[0],i7[1],i7[2]),vec3(0))))return -1;
  dvec3 camera=dvec3(inputData.a.xyz)+dvec3(inputData.b.xyz);
  dvec3 v0=dvec3(i5[0]),v1=dvec3(i5[1]),v2=dvec3(i5[2]);
  dvec3 vc=(v0+v1+v2)/3.0,center=camera-vc;
  dvec3 viewMagnitude=max(abs(v0),max(abs(v1),abs(v2)));
  dvec3 viewError=BoundGamma(8.0,BoundU32)*viewMagnitude;
  double radius=max(length(v0-vc),max(length(v1-vc),length(v2-vc)))+length(viewError);
  double centerLength=length(center);
  if(!(centerLength>radius)||isnan(centerLength)||isinf(centerLength))return -1;
  dvec3 axis=center/centerLength;
  double directionError=2.0*radius/(centerLength-radius)+BoundGamma(32.0,BoundU64);
  dvec4 q=dvec4(boundPresentations.values[0].bodyOrientation);
  dvec3 pointMagnitude=envelope*(abs(axis)+dvec3(directionError));
  dvec3 aq=abs(q.xyz);
  dvec3 rotateMagnitude=pointMagnitude+2.0*BoundCrossMagnitude(aq,BoundCrossMagnitude(aq,pointMagnitude)+abs(q.w)*pointMagnitude);
  // One FP64->FP32 displacement conversion plus <=30 scalar rotation operations.
  dvec3 rotateError=BoundGamma(31.0,BoundU32)*rotateMagnitude;
  dmat4 m=dmat4(frameData.camera.viewProjection);
  dvec4 c0=dvec4(gl_in[0].gl_Position),c1=dvec4(gl_in[1].gl_Position),c2=dvec4(gl_in[2].gl_Position);
  dvec4 baseMagnitude=max(abs(c0),max(abs(c1),abs(c2)));
  dvec4 baseError=BoundGamma(8.0,BoundU32)*baseMagnitude;
  dvec4 displacementError,clipMagnitude;
  for(int k=0;k<4;k++){
    dvec3 row=dvec3(m[0][k],m[1][k],m[2][k]);
    clipMagnitude[k]=dot(abs(row),rotateMagnitude);
    displacementError[k]=dot(abs(row),rotateError)+BoundGamma(7.0,BoundU32)*clipMagnitude[k];
  }
  dvec4 finalError=baseError+displacementError+BoundGamma(1.0,BoundU32)*(baseMagnitude+baseError+clipMagnitude+displacementError);
  int reject=-1;
  for(int plane=0;plane<6;plane++){
    int component=plane/2;double signValue=(plane%2==0)?1.0:-1.0;
    bool nearPlane=plane==4;
    dvec3 row=dvec3(m[0][component],m[1][component],m[2][component])*signValue;
    if(!nearPlane)row+=dvec3(m[0][3],m[1][3],m[2][3]);
    dvec3 rotatedAxis=BoundRotate(axis,q);
    // Rotation is linear for the exact stored quaternion, including its norm error.
    dvec3 bodyRow=dvec3(dot(row,BoundRotate(dvec3(1,0,0),q)),dot(row,BoundRotate(dvec3(0,1,0),q)),dot(row,BoundRotate(dvec3(0,0,1),q)));
    double support=envelope*(abs(dot(row,rotatedAxis))+length(bodyRow)*directionError);
    double b0=signValue*c0[component]+(nearPlane?0.0:c0.w);
    double b1=signValue*c1[component]+(nearPlane?0.0:c1.w);
    double b2=signValue*c2[component]+(nearPlane?0.0:c2.w);
    double rounding=finalError[component]+(nearPlane?0.0:finalError.w);
    // Forward-error allowance for this bounded double-precision expression graph.
    double scale=abs(b0)+abs(b1)+abs(b2)+support+rounding+envelope*length(bodyRow);
    double upper=max(b0,max(b1,b2))+support+rounding+BoundGamma(2048.0,BoundU64)*scale;
    float stored=BoundRoundUp(upper);
    if(plane<4)upper0[plane]=stored;else upper1[plane-4]=stored;
    if(stored<0.0&&reject<0)reject=plane;
  }
  return reject;
}
