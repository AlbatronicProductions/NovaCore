// Diagnostic-only displaced-hull bound. Rounded outward at every FP64 bound operation.
// Production prepared corners already include all base physical authorities.
#extension GL_ARB_gpu_shader_fp64 : require
struct BoundPresentation {vec4 centerRadius;vec4 colorDistant;vec4 blendMetricState;uvec4 identity;vec4 surface;uvec4 hooks;vec4 ringGeometry;vec4 ringOrientation;vec4 ringColor;vec4 bodyOrientation;vec4 localDetail;vec4 centerLow;};
layout(set=0,binding=6,std430) readonly buffer BoundPresentations {BoundPresentation values[];} boundPresentations;
const double BU32=1.1920928955078125e-7LF,BU64=2.220446049250313080847263336181640625e-16LF;
// Treat possible denormal flushing as an interval of the smallest normal value.
double BUp(double x){
  if(isnan(x)||isinf(x))return x;
  if(abs(x)<2.2250738585072014e-308LF)return 2.2250738585072014e-308LF;
  uvec2 b=unpackDouble2x32(x);
  if(x>=0.0){b.x++;if(b.x==0u)b.y++;}else{if(b.x==0u)b.y--;b.x--;}
  return packDouble2x32(b);
}
double BDown(double x){return -BUp(-x);}
double BAdd(double a,double b){return BUp(a+b);}
double BMul(double a,double b){return BUp(a*b);}
double BGamma(double n,double u){return BMul(2.0,BMul(n,u));}
dvec2 BI(double v){return dvec2(v);}
dvec2 BIAdd(dvec2 a,dvec2 b){return dvec2(BDown(a.x+b.x),BUp(a.y+b.y));}
dvec2 BINeg(dvec2 a){return -a.yx;}
dvec2 BIMul(dvec2 a,dvec2 b){dvec4 p=dvec4(a.x*b.x,a.x*b.y,a.y*b.x,a.y*b.y);return dvec2(BDown(min(min(p.x,p.y),min(p.z,p.w))),BUp(max(max(p.x,p.y),max(p.z,p.w))));}
double BIAbs(dvec2 a){return max(abs(a.x),abs(a.y));}
double BSqrt(double a){return BMul(BUp(sqrt(a)),BAdd(1.0,BMul(8.0,BU32)));}
// Exact stored-quaternion linear rotation, evaluated as intervals for a basis vector.
void BRotateBasis(int basis,dvec4 q,out dvec2 x,out dvec2 y,out dvec2 z){
  dvec2 p[3]=dvec2[3](BI(basis==0?1.0:0.0),BI(basis==1?1.0:0.0),BI(basis==2?1.0:0.0));
  dvec2 t[3];for(int i=0;i<3;i++){int j=(i+1)%3,k=(i+2)%3;t[i]=BIAdd(BIAdd(BIMul(BI(q[j]),p[k]),BINeg(BIMul(BI(q[k]),p[j]))),BIMul(BI(q.w),p[i]));}
  dvec2 v[3];for(int i=0;i<3;i++){int j=(i+1)%3,k=(i+2)%3;v[i]=BIAdd(p[i],BIMul(BI(2.0),BIAdd(BIMul(BI(q[j]),t[k]),BINeg(BIMul(BI(q[k]),t[j])))));}
  x=v[0];y=v[1];z=v[2];
}
float BFloatUp(double x){float v=float(x);uint b=floatBitsToUint(v);if(abs(v)<1.1754943508222875e-38)return 1.1754943508222875e-38;return uintBitsToFloat(v>=0.0?b+1u:b-1u);}
int ConservativePatchBound(out vec4 upper0,out vec4 upper1){
  upper0=vec4(3.402823e38);upper1=upper0;
  double envelope=double(uintBitsToFloat(counters.values[9]));
  double baseMaximum=double(max(i7[0],max(i7[1],i7[2])));
  // Certified field bound plus FP64 add/subtract cancellation against interpolated base.
  double heightRoundoff=BMul(BGamma(2.0,BU64),BAdd(BMul(2.0,BMul(baseMaximum,BAdd(1.0,BGamma(8.0,BU32)))),9.536805152893066LF));
  if(!(envelope>=BAdd(9.536805152893066LF,heightRoundoff))||isinf(envelope)||any(lessThan(vec3(i7[0],i7[1],i7[2]),vec3(0))))return -1;
  envelope=BAdd(9.536805152893066LF,heightRoundoff); // Derived field authority, not a measured maximum.
  // A normalized FP64 direction has length <=1+gamma(32), including inverse-sqrt error.
  double e=BMul(envelope,BAdd(1.0,BGamma(32.0,BU32)));
  dvec4 q=dvec4(boundPresentations.values[0].bodyOrientation);dvec3 aq=abs(q.xyz);
  // Absolute expression graph for p+2*cross(q,cross(q,p)+q.w*p), |p_i|<=e.
  double inner[3],rotMagnitude[3],rotError[3];
  for(int i=0;i<3;i++){int j=(i+1)%3,k=(i+2)%3;inner[i]=BMul(e,BAdd(BAdd(aq[j],aq[k]),abs(q.w)));}
  for(int i=0;i<3;i++){int j=(i+1)%3,k=(i+2)%3;rotMagnitude[i]=BAdd(e,BMul(2.0,BAdd(BMul(aq[j],inner[k]),BMul(aq[k],inner[j]))));rotError[i]=BAdd(BMul(BGamma(31.0,BU32),rotMagnitude[i]),BMul(32.0,1.1754943508222875e-38LF));}
  dmat4 m=dmat4(frameData.camera.viewProjection);dvec4 c[3]=dvec4[3](dvec4(gl_in[0].gl_Position),dvec4(gl_in[1].gl_Position),dvec4(gl_in[2].gl_Position));
  double finalError[4];
  for(int k=0;k<4;k++){
    double bm=max(abs(c[0][k]),max(abs(c[1][k]),abs(c[2][k]))),be=BAdd(BMul(BGamma(8.0,BU32),bm),BMul(8.0,1.1754943508222875e-38LF)),cm=0.0,de=0.0;
    for(int j=0;j<3;j++){cm=BAdd(cm,BMul(abs(m[j][k]),rotMagnitude[j]));de=BAdd(de,BMul(abs(m[j][k]),rotError[j]));}
    de=BAdd(de,BAdd(BMul(BGamma(7.0,BU32),cm),BMul(8.0,1.1754943508222875e-38LF)));
    finalError[k]=BAdd(BAdd(be,de),BAdd(BMul(BGamma(1.0,BU32),BAdd(BAdd(bm,be),BAdd(cm,de))),1.1754943508222875e-38LF));
  }
  int reject=-1;
  for(int plane=0;plane<6;plane++){
    int k=plane/2;double s=plane%2==0?1.0:-1.0;bool zOnly=plane==4;
    dvec2 row[3];for(int j=0;j<3;j++)row[j]=BIAdd(BI(s*m[j][k]),BI(zOnly?0.0:m[j][3]));
    double normSquared=0.0;
    for(int b=0;b<3;b++){dvec2 x,y,z;BRotateBasis(b,q,x,y,z);dvec2 dotValue=BIAdd(BIAdd(BIMul(row[0],x),BIMul(row[1],y)),BIMul(row[2],z));double a=BIAbs(dotValue);normSquared=BAdd(normSquared,BMul(a,a));}
    double support=BMul(e,BSqrt(normSquared)),cornerUpper=-1.7976931348623157e308LF;
    for(int v=0;v<3;v++)cornerUpper=max(cornerUpper,BAdd(s*c[v][k],zOnly?0.0:c[v].w));
    double error=BAdd(finalError[k],zOnly?0.0:finalError[3]);
    float upper=BFloatUp(BAdd(BAdd(cornerUpper,support),error));
    if(plane<4)upper0[plane]=upper;else upper1[plane-4]=upper;
    if(upper<0.0&&reject<0)reject=plane;
  }
  return reject;
}
