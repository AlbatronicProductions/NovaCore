#ifndef NOVACORE_FACILITY_SUPPORT_GLSL
#define NOVACORE_FACILITY_SUPPORT_GLSL
// GPU equivalent of Core.Surface.FloridaFacilitySupport, immutable physical v1.
// These are NovaCore survey-derived physical parameters, not a render-only decal.
const uvec2 FACILITY_SUPPORT_ID=uvec2(1u,0x4E434653u);
const dvec3 FACILITY_UP=dvec3(.1433224599406355LF,.4788205718227514LF,.8661348234979923LF);
const dvec3 FACILITY_EAST=dvec3(.9865841313746494LF,0.0,-.163253642286255LF);
const dvec3 FACILITY_NORTH=dvec3(-.07816920235165153LF,.8779127861008367LF,-.47239677793606216LF);
const double FACILITY_RADIUS=6371008.8LF,FACILITY_PLANE=15.134892258793116LF;
struct FacilitySample{double weight;dvec3 gradient;double plane;};
double FacilityWeight(double coordinate,double inner,out double derivative){
  derivative=0.0;double distance=abs(coordinate);
  if(distance<=inner)return 1.0;if(distance>=inner+128.0)return 0.0;
  double t=(distance-inner)/128.0;
  derivative=-30.0*t*t*(t-1.0)*(t-1.0)*sign(coordinate)/128.0;
  return 1.0-t*t*t*(t*(t*6.0-15.0)+10.0);
}
FacilitySample FacilitySupport(dvec3 direction){
  double cosine=dot(direction,FACILITY_UP);
  if(cosine<1.0-(192.0*192.0+184.0*184.0)/(FACILITY_RADIUS*FACILITY_RADIUS))return FacilitySample(0.0,dvec3(0),0.0);
  double e=dot(direction,FACILITY_EAST),n=dot(direction,FACILITY_NORTH),dx,dy;
  double wx=FacilityWeight(FACILITY_RADIUS*e/cosine,64.0,dx),wy=FacilityWeight(FACILITY_RADIUS*n/cosine,56.0,dy);
  if(wx==0.0||wy==0.0)return FacilitySample(0.0,dvec3(0),0.0);
  dvec3 gx=(FACILITY_EAST*cosine-FACILITY_UP*e)/(cosine*cosine);
  dvec3 gy=(FACILITY_NORTH*cosine-FACILITY_UP*n)/(cosine*cosine);
  return FacilitySample(wx*wy,gx*(dx*wy)+gy*(dy*wx),(FACILITY_RADIUS+FACILITY_PLANE)/cosine-FACILITY_RADIUS);
}
double FacilityBaseHeight(dvec3 direction,double naturalBase){FacilitySample s=FacilitySupport(direction);return s.weight==0.0?naturalBase:naturalBase+(s.plane-naturalBase)*s.weight;}
dvec3 FacilityBaseNormal(dvec3 direction,double naturalBase,dvec3 naturalNormal){
  FacilitySample s=FacilitySupport(direction);if(s.weight==0.0)return naturalNormal;if(s.weight==1.0)return FACILITY_UP;
  dvec3 naturalSlope=direction-naturalNormal/max(dot(naturalNormal,direction),1e-9);
  dvec3 planeSlope=direction-FACILITY_UP/dot(FACILITY_UP,direction);
  return normalize(direction-(naturalSlope*(1.0-s.weight)+planeSlope*s.weight+s.gradient*(s.plane-naturalBase)));
}
#endif
