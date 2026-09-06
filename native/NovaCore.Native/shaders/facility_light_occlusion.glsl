#ifndef NOVACORE_FACILITY_LIGHT_OCCLUSION
#define NOVACORE_FACILITY_LIGHT_OCCLUSION
struct FacilityCaster { vec4 minimum; vec4 maximum; };
layout(std430,set=0,binding=58) readonly buffer FacilityVisibilityData {
  uvec4 control; vec4 boundsMin; vec4 boundsMax; vec4 padding;
  dvec4 cameraLocal; dvec4 east; dvec4 north; dvec4 up;
  FacilityCaster boxes[8];
  vec4 receiverRelativeMin; vec4 receiverRelativeMax;
} facilityVisibility;
bool FacilityRayBox(vec3 p,vec3 l,FacilityCaster box,float maximum) {
  float enter=0.0,leave=maximum;
  for(int k=0;k<3;k++) {
    if(l[k]==0.0) { if(p[k]<box.minimum[k]||p[k]>box.maximum[k])return false; }
    else { float a=(box.minimum[k]-p[k])/l[k],b=(box.maximum[k]-p[k])/l[k];enter=max(enter,min(a,b));leave=min(leave,max(a,b)); }
  }
  return leave>enter;
}
float AuthoredFacilitySunVisibility(vec3 finalRelativeBody,vec3 sunBody,out uint tests,out bool entered) {
  tests=0u;entered=false;
  if(facilityVisibility.control.x==0u)return 1.0;
  if(any(lessThan(finalRelativeBody,facilityVisibility.receiverRelativeMin.xyz))||any(greaterThan(finalRelativeBody,facilityVisibility.receiverRelativeMax.xyz)))return 1.0;
  vec3 e=vec3(facilityVisibility.east.xyz),n=vec3(facilityVisibility.north.xyz),u=vec3(facilityVisibility.up.xyz);
  vec3 light=normalize(vec3(dot(sunBody,e),dot(sunBody,n),dot(sunBody,u)));
  float maximum=facilityVisibility.boundsMin.w;
  vec3 sweep=light*maximum;
  vec3 lo=facilityVisibility.boundsMin.xyz-max(sweep,vec3(0)),hi=facilityVisibility.boundsMax.xyz-min(sweep,vec3(0));
  vec3 approximate=vec3(facilityVisibility.cameraLocal.xyz)+vec3(dot(finalRelativeBody,e),dot(finalRelativeBody,n),dot(finalRelativeBody,u));
  // Conservative rejection only. Eight FP32 roundoff units cover the transform,
  // camera transport and swept-bound operations; this never inflates a caster.
  float guard=8.0*1.1920928955078125e-7*(length(finalRelativeBody)+length(vec3(facilityVisibility.cameraLocal.xyz))+maximum);
  if(any(lessThan(approximate,lo-guard))||any(greaterThan(approximate,hi+guard)))return 1.0;
  dvec3 relative=dvec3(finalRelativeBody);
  vec3 p=vec3(facilityVisibility.cameraLocal.xyz+dvec3(dot(relative,facilityVisibility.east.xyz),dot(relative,facilityVisibility.north.xyz),dot(relative,facilityVisibility.up.xyz)));
  if(any(lessThan(p,lo))||any(greaterThan(p,hi)))return 1.0;
  entered=true;
  for(uint i=0u;i<facilityVisibility.control.x;i++){tests++;if(FacilityRayBox(p,light,facilityVisibility.boxes[i],maximum))return 0.0;}
  return 1.0;
}
// Keep the original lighting evaluation bit-for-bit for clear receivers.
// For binary occlusion only the existing ambient/emissive terms remain.
vec3 FacilityVisibleLighting(vec3 original,vec3 albedo,float ambient,float emissive,float visibility) {
  return visibility==1.0?original:albedo*ambient+albedo*emissive;
}
#endif
