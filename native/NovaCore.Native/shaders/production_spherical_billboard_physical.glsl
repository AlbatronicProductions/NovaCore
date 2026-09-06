#ifndef NOVACORE_PRODUCTION_SPHERICAL_BILLBOARD_PHYSICAL_GLSL
#define NOVACORE_PRODUCTION_SPHERICAL_BILLBOARD_PHYSICAL_GLSL

double CandidateNaturalBaseHeightD(dvec3 direction)
{
  double geographic=CanonicalGeographicHeight(direction);
  NaturalTerrainCompositionSampleD value=EvaluateNaturalCandidateD(direction);
  return max(0.0,geographic+value.macro.height+value.meso.height);
}

double CandidateBaseHeightD(dvec3 direction){return FacilityBaseHeight(direction,CandidateNaturalBaseHeightD(direction));}

double CandidatePhysicalHeightD(dvec3 direction)
{
  NaturalTerrainCompositionSampleD value=EvaluateNaturalCandidateD(direction);
  return max(0.0,CandidateBaseHeightD(direction)+(1.0-FacilitySupport(direction).weight)*value.nearField.height);
}

vec3 CandidateBaseNormalD(dvec3 direction,double radius)
{
  direction=normalize(direction);
  dvec3 east=PhysicalEastD(direction),north=normalize(cross(direction,east));
  double angle=NOVACORE_NORMAL_SAMPLE_RADIUS/radius;
  dvec3 leftDirection=normalize(direction-east*angle),rightDirection=normalize(direction+east*angle);
  dvec3 downDirection=normalize(direction-north*angle),upDirection=normalize(direction+north*angle);
  double leftHeight=CandidateNaturalBaseHeightD(leftDirection),rightHeight=CandidateNaturalBaseHeightD(rightDirection);
  double downHeight=CandidateNaturalBaseHeightD(downDirection),upHeight=CandidateNaturalBaseHeightD(upDirection);
  dvec3 left=leftDirection*(radius+leftHeight),right=rightDirection*(radius+rightHeight);
  dvec3 down=downDirection*(radius+downHeight),up=upDirection*(radius+upHeight);
  dvec3 baseNormal=normalize(cross(right-left,up-down));if(dot(baseNormal,direction)<0.0)baseNormal=-baseNormal;
  if(FacilitySupport(direction).weight==0.0)return vec3(baseNormal);
  return vec3(FacilityBaseNormal(direction,CandidateNaturalBaseHeightD(direction),baseNormal));
}

vec3 CandidatePhysicalNormalD(dvec3 direction,double radius)
{
  direction=normalize(direction);
  dvec3 east=PhysicalEastD(direction),north=normalize(cross(direction,east));
  vec3 baseNormal=CandidateBaseNormalD(direction,radius);
  NaturalTerrainFieldSampleD nearValue=EvaluateNaturalCandidateNearD(direction);
  double radial=max(dot(dvec3(baseNormal),direction),1e-9);
  double eastSlope=-dot(dvec3(baseNormal),east)/radial+dot(nearValue.bodyGradient,east);
  double northSlope=-dot(dvec3(baseNormal),north)/radial+dot(nearValue.bodyGradient,north);
  return normalize(vec3(direction-east*eastSlope-north*northSlope));
}

#endif
