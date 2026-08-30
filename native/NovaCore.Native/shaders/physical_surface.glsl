#ifndef NOVACORE_PHYSICAL_SURFACE_GLSL
#define NOVACORE_PHYSICAL_SURFACE_GLSL

// NovaCore 11B-7H modifier schema 1 / generation 1. These constants mirror
// PlanetaryPhysicalSurface and are immutable members of one compatible shader
// generation; they are never published per patch.
const uint NOVACORE_MODIFIER_SCHEMA_VERSION=1u;
const uint NOVACORE_MODIFIER_GENERATION=1u;
const double NOVACORE_EARTH_REFERENCE_RADIUS=6371008.8;
const double NOVACORE_NORMAL_SAMPLE_RADIUS=9774.0;
const double NOVACORE_TILED_AMPLITUDE=8.0;
const double NOVACORE_TILED_WAVELENGTH=2500000.0;
const double NOVACORE_EROSION_AMPLITUDE=10.0;
const double NOVACORE_EROSION_WAVELENGTH=40000.0;
const double NOVACORE_EROSION_RADIUS=24000.0;

struct PhysicalModifierEvaluationD
{
  double tiledHeight;
  double erosionHeight;
  double eastGradient;
  double northGradient;
  double geographicWeight;
  uint dominantId;
};

double PhysicalSinD(double value)
{
  const double pi=3.1415926535897932384626433832795;
  const double tau=6.283185307179586476925286766559;
  value-=floor((value+pi)/tau)*tau;
  double square=value*value;
  return value*(1.0+square*(-.16666666666666666666666666666667+
    square*(.00833333333333333333333333333333+
    square*(-.00019841269841269841269841269841+
    square*(.00000275573192239858906525573192+
    square*(-.00000002505210838544171877505211+
    square*(.00000000016059043836821614599392+
    square*(-.00000000000076471637318198164759))))))));
}

double PhysicalCosD(double value)
{
  return PhysicalSinD(value+1.5707963267948966192313216916398);
}

dvec3 PhysicalEastD(dvec3 direction)
{
  direction=normalize(direction);
  double horizontalSquared=direction.x*direction.x+direction.z*direction.z;
  return horizontalSquared>1e-24
    ?dvec3(direction.z,0.0,-direction.x)/sqrt(horizontalSquared)
    :dvec3(1.0,0.0,0.0);
}

PhysicalModifierEvaluationD EvaluateTerrainModifiersD(dvec3 bodyFixedDirection)
{
  dvec3 direction=normalize(bodyFixedDirection),eastFrame=PhysicalEastD(direction);
  dvec3 northFrame=normalize(cross(direction,eastFrame));
  dvec3 point=direction*NOVACORE_EARTH_REFERENCE_RADIUS;
  const dvec3 axisA=dvec3(.8728715609439696,.4364357804719848,-.2182178902359924);
  const dvec3 axisB=dvec3(-.1690308509457033,.50709255283711,.8451542547285166);
  const dvec3 axisC=dvec3(.3903600291794133,-.6506000486323555,.6506000486323555);
  double wave=6.283185307179586476925286766559/NOVACORE_TILED_WAVELENGTH;
  double a=wave*dot(point,axisA)+.713;
  double b=wave*dot(point,axisB)+2.113;
  double c=wave*dot(point,axisC)-1.271;
  double tiled=NOVACORE_TILED_AMPLITUDE*(.5*PhysicalSinD(a)+.3*PhysicalSinD(b)+.2*PhysicalSinD(c));
  dvec3 tiledGradient=axisA*(NOVACORE_TILED_AMPLITUDE*.5*wave*PhysicalCosD(a))+
    axisB*(NOVACORE_TILED_AMPLITUDE*.3*wave*PhysicalCosD(b))+
    axisC*(NOVACORE_TILED_AMPLITUDE*.2*wave*PhysicalCosD(c));

  const dvec3 florida=dvec3(.1433224599406355,.4788205718227514,.8661348234979923);
  dvec3 floridaEast=PhysicalEastD(florida),floridaNorth=normalize(cross(florida,floridaEast));
  dvec3 delta=point-florida*NOVACORE_EARTH_REFERENCE_RADIUS;
  double localEast=dot(delta,floridaEast),localNorth=dot(delta,floridaNorth);
  double radius=sqrt(localEast*localEast+localNorth*localNorth),weight=0.0,dWeightDr=0.0;
  if(radius<NOVACORE_EROSION_RADIUS)
  {
    double q=1.0-radius/NOVACORE_EROSION_RADIUS;
    weight=q*q*q*(q*(q*6.0-15.0)+10.0);
    double derivativeQ=30.0*q*q*(q-1.0)*(q-1.0);
    dWeightDr=-derivativeQ/NOVACORE_EROSION_RADIUS;
  }
  double erosionWave=6.283185307179586476925286766559/NOVACORE_EROSION_WAVELENGTH;
  double phase1=erosionWave*(.78*localEast+.6257795138864807*localNorth)+1.137;
  double phase2=erosionWave*(-.35*localEast+.9367496997597597*localNorth)-.443;
  double carrier=.65*PhysicalSinD(phase1)+.35*PhysicalSinD(phase2)*PhysicalSinD(phase1*.5);
  double carrierEast=.65*PhysicalCosD(phase1)*erosionWave*.78+
    .35*(PhysicalCosD(phase2)*erosionWave*-.35*PhysicalSinD(phase1*.5)+
      PhysicalSinD(phase2)*PhysicalCosD(phase1*.5)*erosionWave*.39);
  double carrierNorth=.65*PhysicalCosD(phase1)*erosionWave*.6257795138864807+
    .35*(PhysicalCosD(phase2)*erosionWave*.9367496997597597*PhysicalSinD(phase1*.5)+
      PhysicalSinD(phase2)*PhysicalCosD(phase1*.5)*erosionWave*.31288975694324035);
  double radialEast=radius>1e-9?localEast/radius:0.0;
  double radialNorth=radius>1e-9?localNorth/radius:0.0;
  double erosion=NOVACORE_EROSION_AMPLITUDE*weight*carrier;
  double erosionEast=NOVACORE_EROSION_AMPLITUDE*(weight*carrierEast+carrier*dWeightDr*radialEast);
  double erosionNorth=NOVACORE_EROSION_AMPLITUDE*(weight*carrierNorth+carrier*dWeightDr*radialNorth);
  dvec3 erosionGradient=floridaEast*erosionEast+floridaNorth*erosionNorth;
  double finalEast=dot(tiledGradient+erosionGradient,eastFrame);
  double finalNorth=dot(tiledGradient+erosionGradient,northFrame);
  uint dominant=abs(erosion)>abs(tiled)?2u:1u;
  return PhysicalModifierEvaluationD(tiled,erosion,finalEast,finalNorth,weight,dominant);
}

double TerrainModifierHeightD(dvec3 direction)
{
  PhysicalModifierEvaluationD value=EvaluateTerrainModifiersD(direction);
  return value.tiledHeight+value.erosionHeight;
}

#endif
