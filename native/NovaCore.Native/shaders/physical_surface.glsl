#ifndef NOVACORE_PHYSICAL_SURFACE_GLSL
#define NOVACORE_PHYSICAL_SURFACE_GLSL

// NovaCore M12B schema 2 / generation 2. This FP64 control and modifier bank
// mirrors PlanetaryBiomeControlAuthority and PlanetaryPhysicalSurface.
const uint NOVACORE_MODIFIER_SCHEMA_VERSION=2u;
const uint NOVACORE_MODIFIER_GENERATION=2u;
const double NOVACORE_EARTH_REFERENCE_RADIUS=6371008.8;
const double NOVACORE_NORMAL_SAMPLE_RADIUS=9774.0;
const double NOVACORE_TILED_AMPLITUDE=8.0,NOVACORE_TILED_WAVELENGTH=2500000.0;
const double NOVACORE_ROLLING_AMPLITUDE=18.0,NOVACORE_ROCKY_AMPLITUDE=65.0;
const double NOVACORE_DESERT_AMPLITUDE=7.0,NOVACORE_COASTAL_AMPLITUDE=2.0;
const double NOVACORE_GLACIAL_AMPLITUDE=14.0,NOVACORE_NEAR_AMPLITUDE=.9;
const double NOVACORE_EROSION_AMPLITUDE=1.5,NOVACORE_EROSION_WAVELENGTH=1200.0;
const double NOVACORE_EROSION_RADIUS=24000.0,NOVACORE_LAUNCH_RESERVATION_RADIUS=275.0;
const double NOVACORE_LAUNCH_RESERVATION_TRANSITION=125.0;

const uint NOVACORE_BIOME_OCEAN=0u,NOVACORE_BIOME_BEACH=1u,NOVACORE_BIOME_WETLAND=2u;
const uint NOVACORE_BIOME_GRASS=3u,NOVACORE_BIOME_SCRUB=4u,NOVACORE_BIOME_DESERT=5u;
const uint NOVACORE_BIOME_ROCKY=6u,NOVACORE_BIOME_ALPINE=7u,NOVACORE_BIOME_SNOW=8u,NOVACORE_BIOME_DEVELOPED=9u;

struct BiomeBlendD{uvec4 ids;dvec4 weights;dvec4 eligibility;double glacialEligibility;double materialEligibility;};
struct NearPhysicalEvaluationD{double height;double eastGradient;double northGradient;};
struct PhysicalModifierEvaluationD
{
  double tiledHeight;double erosionHeight;double mesoHeight;double nearHeight;
  double eastGradient;double northGradient;double nearEastGradient;double nearNorthGradient;
  double geographicWeight;uint dominantId;BiomeBlendD biomes;
};

double PhysicalSinD(double value)
{
  const double pi=3.1415926535897932384626433832795,tau=6.283185307179586476925286766559;
  value-=floor((value+pi)/tau)*tau;double square=value*value;
  return value*(1.0+square*(-.16666666666666666666666666666667+square*(.00833333333333333333333333333333+
    square*(-.00019841269841269841269841269841+square*(.00000275573192239858906525573192+
    square*(-.00000002505210838544171877505211+square*(.00000000016059043836821614599392+
    square*(-.00000000000076471637318198164759))))))));
}
double PhysicalCosD(double value){return PhysicalSinD(value+1.5707963267948966192313216916398);}
double PhysicalSaturateD(double value){return clamp(value,0.0,1.0);}
double PhysicalSmoothStepD(double startValue,double endValue,double value){double t=PhysicalSaturateD((value-startValue)/(endValue-startValue));return t*t*(3.0-2.0*t);}
double PhysicalWrappedPhaseD(double coordinate,double wavelength,double phase){double cell=floor(coordinate/wavelength),local=coordinate-cell*wavelength;return local*(6.283185307179586476925286766559/wavelength)+phase;}
double PhysicalWrappedSinD(dvec3 point,dvec3 axis,double wavelength,double phase){return PhysicalSinD(PhysicalWrappedPhaseD(dot(point,axis),wavelength,phase));}
dvec3 PhysicalEastD(dvec3 direction){direction=normalize(direction);double horizontalSquared=direction.x*direction.x+direction.z*direction.z;return horizontalSquared>1e-24?dvec3(direction.z,0.0,-direction.x)/sqrt(horizontalSquared):dvec3(1.0,0.0,0.0);}
double BiomeWeightD(BiomeBlendD blend,uint biome){double value=0.0;for(uint index=0u;index<4u;index++)if(blend.ids[index]==biome)value+=blend.weights[index];return value;}

BiomeBlendD EvaluateBiomeBlendD(dvec3 bodyFixedDirection,double geographicHeight)
{
  dvec3 direction=normalize(bodyFixedDirection),point=direction*NOVACORE_EARTH_REFERENCE_RADIUS;
  const dvec3 climateA=dvec3(.7427813527082074,.5570860145311556,-.3713906763541037);
  const dvec3 climateB=dvec3(-.4364357804719848,.2182178902359924,.8728715609439696);
  const dvec3 climateC=dvec3(.2672612419124244,-.8017837257372732,.5345224838248488);
  double latitude=abs(direction.y),temperature=PhysicalSaturateD(1.0-latitude*.82-max(geographicHeight,0.0)/8500.0);
  double climateValueA=.5+.5*PhysicalWrappedSinD(point,climateA,1850000.0,.37);
  double climateValueB=.5+.5*PhysicalWrappedSinD(point,climateB,620000.0,2.11);
  double climateValueC=.5+.5*PhysicalWrappedSinD(point,climateC,210000.0,-1.43);
  double moisture=PhysicalSaturateD(.18+.46*climateValueA+.24*climateValueB+.12*climateValueC-.18*temperature);
  double aridity=PhysicalSaturateD((1.0-moisture)*(.55+.45*temperature));
  double coast=1.0-PhysicalSmoothStepD(18.0,420.0,abs(geographicHeight)),land=PhysicalSmoothStepD(-2.0,8.0,geographicHeight);
  double highland=PhysicalSmoothStepD(420.0,2400.0,geographicHeight),alpineGate=PhysicalSmoothStepD(1400.0,3600.0,geographicHeight);
  double cold=PhysicalSaturateD(latitude*.9+max(geographicHeight,0.0)/7500.0+(1.0-temperature)*.25),snowGate=PhysicalSmoothStepD(.72,.94,cold);
  double wet=PhysicalSmoothStepD(.58,.86,moisture)*(1.0-PhysicalSmoothStepD(130.0,900.0,geographicHeight));
  double developed=land*(1.0-highland)*PhysicalSmoothStepD(.78,.94,.5+.5*PhysicalWrappedSinD(point,climateB,145000.0,.91))*.18;
  double raw[10];
  raw[0]=1.0-land+land*coast*.18;raw[1]=land*coast*(1.0-.55*wet)*(1.0-snowGate);raw[2]=land*wet*(1.0-.6*highland);
  raw[3]=land*moisture*temperature*(1.0-coast)*(1.0-highland)*(1.0-snowGate);
  raw[4]=land*(1.0-abs(moisture-.38)*1.8)*temperature*(1.0-.7*highland)*(1.0-coast);
  raw[5]=land*PhysicalSmoothStepD(.48,.82,aridity)*(1.0-highland)*(1.0-coast)*(1.0-snowGate);
  raw[6]=land*highland*(1.0-.55*snowGate);raw[7]=land*alpineGate*(1.0-snowGate);raw[8]=land*snowGate*(.35+.65*highland);raw[9]=developed;
  for(uint index=0u;index<10u;index++)raw[index]=max(raw[index],0.0);
  uvec4 ids=uvec4(0);dvec4 weights=dvec4(0);bool chosen[10];for(uint index=0u;index<10u;index++)chosen[index]=false;
  for(uint slot=0u;slot<4u;slot++){uint best=0u;double bestWeight=-1.0;for(uint index=0u;index<10u;index++)if(!chosen[index]&&raw[index]>bestWeight){best=index;bestWeight=raw[index];}ids[slot]=best;weights[slot]=bestWeight;chosen[best]=true;}
  double total=weights.x+weights.y+weights.z+weights.w;if(!(total>1e-15)){ids=uvec4(NOVACORE_BIOME_SCRUB,0,1,2);weights=dvec4(1,0,0,0);total=1.0;}weights/=total;
  BiomeBlendD blend;blend.ids=ids;blend.weights=weights;
  blend.eligibility.x=PhysicalSaturateD(BiomeWeightD(blend,NOVACORE_BIOME_GRASS)+.45*BiomeWeightD(blend,NOVACORE_BIOME_SCRUB));
  blend.eligibility.y=PhysicalSaturateD((BiomeWeightD(blend,NOVACORE_BIOME_ROCKY)+.65*BiomeWeightD(blend,NOVACORE_BIOME_ALPINE))*PhysicalSmoothStepD(220.0,1800.0,geographicHeight));
  blend.eligibility.z=PhysicalSaturateD(BiomeWeightD(blend,NOVACORE_BIOME_DESERT));
  blend.eligibility.w=PhysicalSaturateD(BiomeWeightD(blend,NOVACORE_BIOME_BEACH)+.7*BiomeWeightD(blend,NOVACORE_BIOME_WETLAND));
  blend.glacialEligibility=PhysicalSaturateD(BiomeWeightD(blend,NOVACORE_BIOME_SNOW)+.35*BiomeWeightD(blend,NOVACORE_BIOME_ALPINE));blend.materialEligibility=land;return blend;
}

double PhysicalBandD(dvec3 point,dvec3 axis,double wavelength,double phase,double amplitude,out dvec3 gradient)
{double angle=PhysicalWrappedPhaseD(dot(point,axis),wavelength,phase);gradient=axis*(amplitude*(6.283185307179586476925286766559/wavelength)*PhysicalCosD(angle));return amplitude*PhysicalSinD(angle);}

NearPhysicalEvaluationD EvaluateNearPhysicalD(dvec3 bodyFixedDirection,double geographicHeight)
{
  dvec3 direction=normalize(bodyFixedDirection),point=direction*NOVACORE_EARTH_REFERENCE_RADIUS,east=PhysicalEastD(direction),north=normalize(cross(direction,east));
  const dvec3 axisA=dvec3(.7715167498104595,-.1543033499620919,.6172133998483676),axisB=dvec3(-.3244428422615251,.8111071056538127,.4866642633922876),axisC=dvec3(.1690308509457033,.8451542547285166,-.50709255283711);
  BiomeBlendD biome=EvaluateBiomeBlendD(direction,geographicHeight);
  double amplitude=NOVACORE_NEAR_AMPLITUDE*biome.materialEligibility*clamp(.17*BiomeWeightD(biome,NOVACORE_BIOME_GRASS)+.10*BiomeWeightD(biome,NOVACORE_BIOME_WETLAND)+.12*BiomeWeightD(biome,NOVACORE_BIOME_BEACH)+.62*BiomeWeightD(biome,NOVACORE_BIOME_DESERT)+.92*BiomeWeightD(biome,NOVACORE_BIOME_ROCKY)+.74*BiomeWeightD(biome,NOVACORE_BIOME_ALPINE)+.28*BiomeWeightD(biome,NOVACORE_BIOME_SNOW)+.14*BiomeWeightD(biome,NOVACORE_BIOME_SCRUB),0.0,1.0);
  dvec3 gradient,next;double height=PhysicalBandD(point,axisC,32.0,.53,amplitude*.62,gradient)+PhysicalBandD(point,axisA,7.0,-1.13,amplitude*.27,next);gradient+=next;height+=PhysicalBandD(point,axisB,1.4,2.31,amplitude*.11,next);gradient+=next;
  NearPhysicalEvaluationD result;result.height=height;result.eastGradient=dot(gradient,east);result.northGradient=dot(gradient,north);return result;
}

PhysicalModifierEvaluationD EvaluateTerrainModifiersD(dvec3 bodyFixedDirection,double geographicHeight)
{
  dvec3 direction=normalize(bodyFixedDirection),eastFrame=PhysicalEastD(direction),northFrame=normalize(cross(direction,eastFrame)),point=direction*NOVACORE_EARTH_REFERENCE_RADIUS;
  const dvec3 axisA=dvec3(.8728715609439696,.4364357804719848,-.2182178902359924),axisB=dvec3(-.1690308509457033,.50709255283711,.8451542547285166),axisC=dvec3(.3903600291794133,-.6506000486323555,.6506000486323555);
  const dvec3 detailA=dvec3(.7715167498104595,-.1543033499620919,.6172133998483676),detailB=dvec3(-.3244428422615251,.8111071056538127,.4866642633922876),detailC=dvec3(.1690308509457033,.8451542547285166,-.50709255283711);
  BiomeBlendD biome=EvaluateBiomeBlendD(direction,geographicHeight);dvec3 gradient,next;
  double tiled=PhysicalBandD(point,axisA,NOVACORE_TILED_WAVELENGTH,.713,NOVACORE_TILED_AMPLITUDE*.5,gradient)+PhysicalBandD(point,axisB,NOVACORE_TILED_WAVELENGTH*.73,2.113,NOVACORE_TILED_AMPLITUDE*.3,next);gradient+=next;tiled+=PhysicalBandD(point,axisC,NOVACORE_TILED_WAVELENGTH*.41,-1.271,NOVACORE_TILED_AMPLITUDE*.2,next);gradient+=next;dvec3 fullGradient=gradient;
  double rolling=PhysicalBandD(point,detailA,18000.0,.31,NOVACORE_ROLLING_AMPLITUDE*.58,gradient)+PhysicalBandD(point,detailB,2700.0,1.73,NOVACORE_ROLLING_AMPLITUDE*.29,next);gradient+=next;rolling+=PhysicalBandD(point,detailC,360.0,-.61,NOVACORE_ROLLING_AMPLITUDE*.13,next);gradient=(gradient+next)*biome.eligibility.x;rolling*=biome.eligibility.x;fullGradient+=gradient;
  double rocky=PhysicalBandD(point,detailC,12000.0,1.17,NOVACORE_ROCKY_AMPLITUDE*.52,gradient)+PhysicalBandD(point,axisA,1850.0,-2.03,NOVACORE_ROCKY_AMPLITUDE*.31,next);gradient+=next;rocky+=PhysicalBandD(point,detailB,190.0,.44,NOVACORE_ROCKY_AMPLITUDE*.17,next);gradient=(gradient+next)*biome.eligibility.y;rocky*=biome.eligibility.y;fullGradient+=gradient;
  double desert=PhysicalBandD(point,detailB,1400.0,2.41,NOVACORE_DESERT_AMPLITUDE*.62,gradient)+PhysicalBandD(point,detailA,310.0,-.37,NOVACORE_DESERT_AMPLITUDE*.26,next);gradient+=next;desert+=PhysicalBandD(point,axisC,64.0,1.61,NOVACORE_DESERT_AMPLITUDE*.12,next);gradient=(gradient+next)*biome.eligibility.z;desert*=biome.eligibility.z;fullGradient+=gradient;
  double coastal=PhysicalBandD(point,axisB,2800.0,-.83,NOVACORE_COASTAL_AMPLITUDE*.68,gradient)+PhysicalBandD(point,detailC,260.0,2.63,NOVACORE_COASTAL_AMPLITUDE*.32,next);gradient=(gradient+next)*biome.eligibility.w;coastal*=biome.eligibility.w;fullGradient+=gradient;
  double glacial=PhysicalBandD(point,detailA,7000.0,.67,NOVACORE_GLACIAL_AMPLITUDE*.64,gradient)+PhysicalBandD(point,axisC,840.0,-1.91,NOVACORE_GLACIAL_AMPLITUDE*.25,next);gradient+=next;glacial+=PhysicalBandD(point,detailB,120.0,2.87,NOVACORE_GLACIAL_AMPLITUDE*.11,next);gradient=(gradient+next)*biome.glacialEligibility;glacial*=biome.glacialEligibility;fullGradient+=gradient;double meso=rolling+rocky+desert+coastal+glacial;
  const dvec3 florida=dvec3(.1433224599406355,.4788205718227514,.8661348234979923);dvec3 floridaEast=PhysicalEastD(florida),floridaNorth=normalize(cross(florida,floridaEast));dvec3 delta=point-florida*NOVACORE_EARTH_REFERENCE_RADIUS;
  double localEast=dot(delta,floridaEast),localNorth=dot(delta,floridaNorth),radius=sqrt(localEast*localEast+localNorth*localNorth),weight=0.0,dWeightDr=0.0;
  if(radius<NOVACORE_EROSION_RADIUS){double q=1.0-radius/NOVACORE_EROSION_RADIUS;weight=q*q*q*(q*(q*6.0-15.0)+10.0);double derivativeQ=30.0*q*q*(q-1.0)*(q-1.0);dWeightDr=-derivativeQ/NOVACORE_EROSION_RADIUS;}
  double reservation=1.0,dReservationDr=0.0;if(radius<=NOVACORE_LAUNCH_RESERVATION_RADIUS)reservation=0.0;else if(radius<NOVACORE_LAUNCH_RESERVATION_RADIUS+NOVACORE_LAUNCH_RESERVATION_TRANSITION){double t=(radius-NOVACORE_LAUNCH_RESERVATION_RADIUS)/NOVACORE_LAUNCH_RESERVATION_TRANSITION;reservation=t*t*(3.0-2.0*t);dReservationDr=6.0*t*(1.0-t)/NOVACORE_LAUNCH_RESERVATION_TRANSITION;}
  double erosionWave=6.283185307179586476925286766559/NOVACORE_EROSION_WAVELENGTH,phase1=erosionWave*(.78*localEast+.6257795138864807*localNorth)+1.137,phase2=erosionWave*(-.35*localEast+.9367496997597597*localNorth)-.443;
  double carrier=.65*PhysicalSinD(phase1)+.35*PhysicalSinD(phase2)*PhysicalSinD(phase1*.5),carrierEast=.65*PhysicalCosD(phase1)*erosionWave*.78+.35*(PhysicalCosD(phase2)*erosionWave*-.35*PhysicalSinD(phase1*.5)+PhysicalSinD(phase2)*PhysicalCosD(phase1*.5)*erosionWave*.39),carrierNorth=.65*PhysicalCosD(phase1)*erosionWave*.6257795138864807+.35*(PhysicalCosD(phase2)*erosionWave*.9367496997597597*PhysicalSinD(phase1*.5)+PhysicalSinD(phase2)*PhysicalCosD(phase1*.5)*erosionWave*.31288975694324035);
  double radialEast=radius>1e-9?localEast/radius:0.0,radialNorth=radius>1e-9?localNorth/radius:0.0,erosion=NOVACORE_EROSION_AMPLITUDE*weight*reservation*carrier,radialDerivative=dWeightDr*reservation+weight*dReservationDr;
  double erosionEast=NOVACORE_EROSION_AMPLITUDE*(weight*reservation*carrierEast+carrier*radialDerivative*radialEast),erosionNorth=NOVACORE_EROSION_AMPLITUDE*(weight*reservation*carrierNorth+carrier*radialDerivative*radialNorth);fullGradient+=floridaEast*erosionEast+floridaNorth*erosionNorth;
  NearPhysicalEvaluationD nearValue=EvaluateNearPhysicalD(direction,geographicHeight);fullGradient+=eastFrame*nearValue.eastGradient+northFrame*nearValue.northGradient;
  uint dominant=1u;double magnitude=abs(tiled);if(abs(rolling)>magnitude){dominant=4u;magnitude=abs(rolling);}if(abs(rocky)>magnitude){dominant=5u;magnitude=abs(rocky);}if(abs(desert)>magnitude){dominant=6u;magnitude=abs(desert);}if(abs(coastal)>magnitude){dominant=7u;magnitude=abs(coastal);}if(abs(glacial)>magnitude){dominant=8u;magnitude=abs(glacial);}if(abs(erosion)>magnitude){dominant=2u;magnitude=abs(erosion);}if(abs(nearValue.height)>magnitude)dominant=9u;
  PhysicalModifierEvaluationD result;result.tiledHeight=tiled;result.erosionHeight=erosion;result.mesoHeight=meso;result.nearHeight=nearValue.height;result.eastGradient=dot(fullGradient,eastFrame);result.northGradient=dot(fullGradient,northFrame);result.nearEastGradient=nearValue.eastGradient;result.nearNorthGradient=nearValue.northGradient;result.geographicWeight=weight;result.dominantId=dominant;result.biomes=biome;return result;
}

PhysicalModifierEvaluationD EvaluateTerrainModifiersD(dvec3 direction){return EvaluateTerrainModifiersD(direction,0.0);}
double TerrainBaseModifierHeightD(dvec3 direction,double geographicHeight){PhysicalModifierEvaluationD value=EvaluateTerrainModifiersD(direction,geographicHeight);return value.tiledHeight+value.erosionHeight+value.mesoHeight;}
double TerrainModifierHeightD(dvec3 direction,double geographicHeight){PhysicalModifierEvaluationD value=EvaluateTerrainModifiersD(direction,geographicHeight);return value.tiledHeight+value.erosionHeight+value.mesoHeight+value.nearHeight;}
double TerrainModifierHeightD(dvec3 direction){return TerrainModifierHeightD(direction,0.0);}

#endif
