#ifndef NOVACORE_PLANETARY_NATURAL_TERRAIN_FAMILIES_GLSL
#define NOVACORE_PLANETARY_NATURAL_TERRAIN_FAMILIES_GLSL

#include "planetary_natural_terrain_field.glsl"

// Canonical M12D-P2B composition, reachable only through the explicit M12D
// candidate generation. Generation 3 never includes this authority.
const uint NOVACORE_NATURAL_FAMILY_COMPOSITION_VERSION=1u;
const double NOVACORE_NATURAL_FAMILY_WARP_FRACTION=.10;
const double NOVACORE_NATURAL_FAMILY_ORIENTATION_REGULARIZER=.5;
const double NOVACORE_NATURAL_FAMILY_WARP_CELL=240000.0;
const uint NOVACORE_NATURAL_FAMILY_WARP_CONTROL_ID=0x50423210u;
const uint NOVACORE_NATURAL_FAMILY_SEED_MULTIPLIER=0x9E3779B9u;
const uint NOVACORE_NATURAL_FAMILY_OCTAVE_MULTIPLIER=0x85EBCA6Bu;
const uint NOVACORE_NATURAL_FAMILY_SEED_VERSION=0xB2F10A4Du;

struct NaturalTerrainFamilyIdentityD { uvec2 bodyId;uvec2 physicalFieldGeneration;uint seed; };
struct NaturalTerrainFamilyConfigurationD
{
  double macroCell;double macroAmplitude;double mesoCell;double mesoAmplitude;
  double nearCell;double nearAmplitude;double shapeLinear;double shapeRidge;double anisotropy;
};
struct NaturalTerrainFamilyVectorD
{
  NaturalTerrainFieldSampleD x;NaturalTerrainFieldSampleD y;NaturalTerrainFieldSampleD z;
};
struct NaturalTerrainFamilySampleD
{
  uint family;NaturalTerrainFieldSampleD macro;NaturalTerrainFieldSampleD meso;
  NaturalTerrainFieldSampleD nearField;NaturalTerrainFieldSampleD total;dvec3 orientation;
};
struct NaturalTerrainFamilyBlendD { uint firstFamily;uint secondFamily;double weight;dvec3 gradient; };
struct NaturalTerrainCompositionSampleD
{
  uint firstFamily;uint secondFamily;double secondWeight;dvec3 secondWeightGradient;
  NaturalTerrainFieldSampleD macro;NaturalTerrainFieldSampleD meso;
  NaturalTerrainFieldSampleD nearField;NaturalTerrainFieldSampleD total;
};

NaturalTerrainFamilyConfigurationD NaturalTerrainFamilyConfiguration(uint family)
{
  if(family==1u)return NaturalTerrainFamilyConfigurationD(18000.0,12.0,2700.0,5.0,180.0,1.5,1.0,0.0,0.0);
  if(family==2u)return NaturalTerrainFamilyConfigurationD(24000.0,11.0,3400.0,6.0,240.0,1.8,.92,.08,0.0);
  if(family==3u)return NaturalTerrainFamilyConfigurationD(48000.0,34.0,6500.0,21.0,520.0,11.0,.62,.38,.035);
  if(family==4u)return NaturalTerrainFamilyConfigurationD(36000.0,24.0,4200.0,14.0,320.0,6.0,.70,.30,.04);
  if(family==5u)return NaturalTerrainFamilyConfigurationD(14000.0,5.0,1600.0,2.2,120.0,.8,.82,.18,.08);
  if(family==6u)return NaturalTerrainFamilyConfigurationD(24000.0,1.5,2800.0,.8,180.0,.25,.76,-.24,0.0);
  if(family==7u)return NaturalTerrainFamilyConfigurationD(30000.0,14.0,3600.0,7.0,260.0,2.5,.68,.32,.06);
  return NaturalTerrainFamilyConfigurationD(22000.0,10.0,2800.0,4.0,220.0,1.2,1.0,0.0,0.0);
}
dvec2 NaturalTerrainFamilyBoundsD(uint family)
{
  NaturalTerrainFamilyConfigurationD configuration=NaturalTerrainFamilyConfiguration(family);
  const double rootThree=1.7320508075688772935274463415059,rootSix=2.4494897427831780981972840747059;
  double macro=configuration.macroAmplitude*rootThree*abs(configuration.shapeLinear);
  double meso=configuration.mesoAmplitude*rootThree*(abs(configuration.shapeLinear)+abs(configuration.shapeRidge));
  double nearField=configuration.nearAmplitude*rootThree*abs(configuration.shapeLinear);
  double controlGradient=12.25/NOVACORE_NATURAL_FAMILY_WARP_CELL;
  double baseContribution=NOVACORE_NATURAL_FAMILY_WARP_FRACTION*configuration.macroCell/3.0*rootSix*controlGradient;
  double orientationGradient=2.0*rootSix*controlGradient;
  double anisotropicContribution=configuration.anisotropy*configuration.macroCell/rootThree*(
    controlGradient+rootThree*rootThree*orientationGradient);
  double transform=1.0+baseContribution+anisotropicContribution;
  double gradient=transform*(12.25*configuration.macroAmplitude/configuration.macroCell+
    (abs(configuration.shapeLinear)+abs(configuration.shapeRidge))*12.25*configuration.mesoAmplitude/configuration.mesoCell+
    12.25*configuration.nearAmplitude/configuration.nearCell);
  return dvec2(macro+meso+nearField,gradient);
}
dvec2 NaturalTerrainCompositionBoundsD()
{
  double maximumHeight=0.0,maximumGradient=0.0;
  for(uint family=1u;family<=8u;family++)
  {
    dvec2 bounds=NaturalTerrainFamilyBoundsD(family);
    maximumHeight=max(maximumHeight,bounds.x);maximumGradient=max(maximumGradient,bounds.y);
  }
  double controlGradient=12.25/NOVACORE_NATURAL_FAMILY_WARP_CELL;
  double normalizedGradient=controlGradient/1.7320508075688772935274463415059;
  double outerWeightGradient=1.875/.84*normalizedGradient;
  double blendWeightGradient=1.875*6.0*outerWeightGradient;
  maximumGradient+=2.0*maximumHeight*blendWeightGradient;
  return dvec2(maximumHeight,maximumGradient);
}
uint NaturalTerrainFamilySeed(uint seed,uint family,uint octave)
{return seed^NOVACORE_NATURAL_FAMILY_SEED_VERSION^family*NOVACORE_NATURAL_FAMILY_SEED_MULTIPLIER^octave*NOVACORE_NATURAL_FAMILY_OCTAVE_MULTIPLIER;}
NaturalTerrainFieldIdentityD NaturalTerrainFamilyFieldIdentity(NaturalTerrainFamilyIdentityD identity,uint family,uint octave)
{
  return NaturalTerrainFieldIdentityD(identity.bodyId,identity.physicalFieldGeneration,family,octave,
    NaturalTerrainFamilySeed(identity.seed,family,octave));
}
NaturalTerrainFieldSampleD NaturalTerrainFamilyAdd(NaturalTerrainFieldSampleD a,NaturalTerrainFieldSampleD b)
{return NaturalTerrainFieldSampleD(a.height+b.height,a.bodyGradient+b.bodyGradient);}
NaturalTerrainFieldSampleD NaturalTerrainFamilyShape(NaturalTerrainFieldSampleD value,double linear,double ridge)
{
  if(ridge==0.0&&linear==1.0)return value;
  const double epsilon=.01;double root=sqrt(value.height*value.height+epsilon*epsilon);
  return NaturalTerrainFieldSampleD(linear*value.height+ridge*(root-epsilon),
    value.bodyGradient*(linear+ridge*value.height/root));
}
NaturalTerrainFieldSampleD NaturalTerrainFamilyControl(dvec3 point,double cell,uint family,uint octave,
  NaturalTerrainFamilyIdentityD identity)
{
  return EvaluateNaturalTerrainFieldD(point,cell,1.0,NaturalTerrainFamilyFieldIdentity(identity,family,octave));
}
NaturalTerrainFamilyVectorD NaturalTerrainFamilyControls(dvec3 point,NaturalTerrainFamilyIdentityD identity)
{
  NaturalTerrainFieldSampleD x=NaturalTerrainFamilyControl(point,NOVACORE_NATURAL_FAMILY_WARP_CELL,
    NOVACORE_NATURAL_FAMILY_WARP_CONTROL_ID,0x100u,identity);
  NaturalTerrainFieldSampleD y=NaturalTerrainFamilyControl(point,NOVACORE_NATURAL_FAMILY_WARP_CELL,
    NOVACORE_NATURAL_FAMILY_WARP_CONTROL_ID,0x101u,identity);
  double inverseBound=1.0/1.7320508075688772935274463415059;
  NaturalTerrainFieldSampleD z=NaturalTerrainFieldSampleD(x.height*y.height*inverseBound,
    (x.bodyGradient*y.height+y.bodyGradient*x.height)*inverseBound);
  return NaturalTerrainFamilyVectorD(x,y,z);
}
dvec3 NaturalTerrainFamilyVectorValue(NaturalTerrainFamilyVectorD value)
{return dvec3(value.x.height,value.y.height,value.z.height);}
NaturalTerrainFamilyVectorD NaturalTerrainFamilyOrientation(NaturalTerrainFamilyVectorD value)
{
  dvec3 components=NaturalTerrainFamilyVectorValue(value);
  double lengthValue=sqrt(dot(components,components)+NOVACORE_NATURAL_FAMILY_ORIENTATION_REGULARIZER*NOVACORE_NATURAL_FAMILY_ORIENTATION_REGULARIZER);
  double inverse=1.0/lengthValue,inverseSquared=inverse*inverse;
  dvec3 lengthGradient=(value.x.bodyGradient*value.x.height+value.y.bodyGradient*value.y.height+
    value.z.bodyGradient*value.z.height)*inverse;
  return NaturalTerrainFamilyVectorD(
    NaturalTerrainFieldSampleD(value.x.height*inverse,value.x.bodyGradient*inverse-lengthGradient*(value.x.height*inverseSquared)),
    NaturalTerrainFieldSampleD(value.y.height*inverse,value.y.bodyGradient*inverse-lengthGradient*(value.y.height*inverseSquared)),
    NaturalTerrainFieldSampleD(value.z.height*inverse,value.z.bodyGradient*inverse-lengthGradient*(value.z.height*inverseSquared)));
}
NaturalTerrainFieldSampleD NaturalTerrainFamilyScale(dvec3 point,uint family,uint octave,double cell,double amplitude,
  NaturalTerrainFamilyConfigurationD configuration,NaturalTerrainFamilyVectorD controls,
  NaturalTerrainFamilyVectorD orientation,NaturalTerrainFamilyIdentityD identity)
{
  double baseScale=NOVACORE_NATURAL_FAMILY_WARP_FRACTION*cell/3.0;
  dvec3 warped=point+NaturalTerrainFamilyVectorValue(controls)*baseScale;
  double anisotropicScale=configuration.anisotropy*cell/1.7320508075688772935274463415059;
  dvec3 orientationValue=NaturalTerrainFamilyVectorValue(orientation);
  if(anisotropicScale!=0.0)warped+=orientationValue*(controls.x.height*anisotropicScale);
  NaturalTerrainFieldSampleD evaluated=EvaluateNaturalTerrainFieldD(warped,cell,amplitude,
    NaturalTerrainFamilyFieldIdentity(identity,family,octave));
  dvec3 gradient=evaluated.bodyGradient+baseScale*(controls.x.bodyGradient*evaluated.bodyGradient.x+
    controls.y.bodyGradient*evaluated.bodyGradient.y+controls.z.bodyGradient*evaluated.bodyGradient.z);
  if(anisotropicScale!=0.0)
  {
    double projected=dot(evaluated.bodyGradient,orientationValue);
    gradient+=anisotropicScale*(controls.x.bodyGradient*projected+controls.x.height*(
      orientation.x.bodyGradient*evaluated.bodyGradient.x+orientation.y.bodyGradient*evaluated.bodyGradient.y+
      orientation.z.bodyGradient*evaluated.bodyGradient.z));
  }
  return NaturalTerrainFieldSampleD(evaluated.height,gradient);
}
NaturalTerrainFamilySampleD EvaluateNaturalTerrainFamilyWithControlsD(dvec3 point,uint family,
  NaturalTerrainFamilyVectorD controls,NaturalTerrainFamilyVectorD orientation,NaturalTerrainFamilyIdentityD identity)
{
  NaturalTerrainFamilyConfigurationD configuration=NaturalTerrainFamilyConfiguration(family);
  NaturalTerrainFieldSampleD macro=NaturalTerrainFamilyScale(point,family,0u,configuration.macroCell,
    configuration.macroAmplitude,configuration,controls,orientation,identity);
  NaturalTerrainFieldSampleD meso=NaturalTerrainFamilyShape(NaturalTerrainFamilyScale(point,family,1u,
    configuration.mesoCell,configuration.mesoAmplitude,configuration,controls,orientation,identity),
    configuration.shapeLinear,configuration.shapeRidge);
  NaturalTerrainFieldSampleD nearField=NaturalTerrainFamilyScale(point,family,2u,configuration.nearCell,
    configuration.nearAmplitude,configuration,controls,orientation,identity);
  return NaturalTerrainFamilySampleD(family,macro,meso,nearField,
    NaturalTerrainFamilyAdd(NaturalTerrainFamilyAdd(macro,meso),nearField),NaturalTerrainFamilyVectorValue(orientation));
}
NaturalTerrainFamilySampleD EvaluateNaturalTerrainFamilyD(dvec3 point,uint family,NaturalTerrainFamilyIdentityD identity)
{
  NaturalTerrainFamilyVectorD controls=NaturalTerrainFamilyControls(point,identity);
  return EvaluateNaturalTerrainFamilyWithControlsD(point,family,controls,NaturalTerrainFamilyOrientation(controls),identity);
}
double NaturalTerrainFamilySmootherStep(double value,out double derivative)
{
  if(value<=0.0){derivative=0.0;return 0.0;}if(value>=1.0){derivative=0.0;return 1.0;}
  double square=value*value;derivative=30.0*square*(value-1.0)*(value-1.0);
  return square*value*(value*(value*6.0-15.0)+10.0);
}
NaturalTerrainFamilyBlendD NaturalTerrainFamilyBiomeBlend(NaturalTerrainFieldSampleD control)
{
  double normalized=control.height/1.7320508075688772935274463415059,outerDerivative;
  double outer=NaturalTerrainFamilySmootherStep((normalized+.42)/.84,outerDerivative);
  dvec3 outerGradient=control.bodyGradient*(outerDerivative/(.84*1.7320508075688772935274463415059));
  double coordinate=outer*6.0;uint familyIndex=min(uint(floor(coordinate)),5u);
  double fraction=coordinate-double(familyIndex),blendDerivative;
  double weight=NaturalTerrainFamilySmootherStep(fraction,blendDerivative);
  return NaturalTerrainFamilyBlendD(familyIndex+1u,familyIndex+2u,weight,outerGradient*(6.0*blendDerivative));
}
NaturalTerrainFieldSampleD NaturalTerrainFamilyBlendSample(NaturalTerrainFieldSampleD first,
  NaturalTerrainFieldSampleD second,double weight,dvec3 weightGradient)
{
  return NaturalTerrainFieldSampleD(first.height*(1.0-weight)+second.height*weight,
    first.bodyGradient*(1.0-weight)+second.bodyGradient*weight+weightGradient*(second.height-first.height));
}
NaturalTerrainCompositionSampleD EvaluateNaturalTerrainCompositionD(dvec3 point,NaturalTerrainFamilyIdentityD identity)
{
  NaturalTerrainFamilyVectorD controls=NaturalTerrainFamilyControls(point,identity);
  NaturalTerrainFamilyBlendD blend=NaturalTerrainFamilyBiomeBlend(controls.x);
  NaturalTerrainFamilyVectorD orientation=NaturalTerrainFamilyOrientation(controls);
  NaturalTerrainFamilySampleD first=EvaluateNaturalTerrainFamilyWithControlsD(point,blend.firstFamily,controls,orientation,identity);
  NaturalTerrainFamilySampleD second=EvaluateNaturalTerrainFamilyWithControlsD(point,blend.secondFamily,controls,orientation,identity);
  return NaturalTerrainCompositionSampleD(blend.firstFamily,blend.secondFamily,blend.weight,blend.gradient,
    NaturalTerrainFamilyBlendSample(first.macro,second.macro,blend.weight,blend.gradient),
    NaturalTerrainFamilyBlendSample(first.meso,second.meso,blend.weight,blend.gradient),
    NaturalTerrainFamilyBlendSample(first.nearField,second.nearField,blend.weight,blend.gradient),
    NaturalTerrainFamilyBlendSample(first.total,second.total,blend.weight,blend.gradient));
}

// Renderer integration path for TES. Macro and meso are prepared once; this
// evaluates only the canonical near family while retaining the same control,
// family, blend, warp, and analytic-gradient identities as the full authority.
NaturalTerrainFieldSampleD EvaluateNaturalTerrainNearD(dvec3 point,NaturalTerrainFamilyIdentityD identity)
{
  NaturalTerrainFamilyVectorD controls=NaturalTerrainFamilyControls(point,identity);
  NaturalTerrainFamilyBlendD blend=NaturalTerrainFamilyBiomeBlend(controls.x);
  NaturalTerrainFamilyVectorD orientation=NaturalTerrainFamilyOrientation(controls);
  NaturalTerrainFamilyConfigurationD firstConfiguration=NaturalTerrainFamilyConfiguration(blend.firstFamily);
  NaturalTerrainFamilyConfigurationD secondConfiguration=NaturalTerrainFamilyConfiguration(blend.secondFamily);
  NaturalTerrainFieldSampleD first=NaturalTerrainFamilyScale(point,blend.firstFamily,2u,
    firstConfiguration.nearCell,firstConfiguration.nearAmplitude,firstConfiguration,controls,orientation,identity);
  NaturalTerrainFieldSampleD second=NaturalTerrainFamilyScale(point,blend.secondFamily,2u,
    secondConfiguration.nearCell,secondConfiguration.nearAmplitude,secondConfiguration,controls,orientation,identity);
  return NaturalTerrainFamilyBlendSample(first,second,blend.weight,blend.gradient);
}

#endif
