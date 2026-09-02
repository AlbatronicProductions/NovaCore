#ifndef NOVACORE_PLANETARY_NATURAL_TERRAIN_FIELD_GLSL
#define NOVACORE_PLANETARY_NATURAL_TERRAIN_FIELD_GLSL

// Isolated M12D-P2A proof library. No production shader includes this file.
const uint NOVACORE_NATURAL_FIELD_HASH_VERSION=1u;
const uint NOVACORE_NATURAL_FIELD_HASH_INITIAL=0x9B0425F0u;
const uint NOVACORE_NATURAL_FIELD_HASH_LANE_MULTIPLIER=0xE46DA8FBu;
const uint NOVACORE_NATURAL_FIELD_HASH_LANE_INCREMENT=0x8C83052Fu;
const uint NOVACORE_NATURAL_FIELD_HASH_FINAL_MULTIPLIER=0x78232465u;
const double NOVACORE_NATURAL_FIELD_INVERSE_SQRT_FIVE=0.447213595499957939281834733746;

struct NaturalTerrainFieldIdentityD
{
  uvec2 bodyId;
  uvec2 physicalFieldGeneration;
  uint familyId;
  uint octaveId;
  uint seed;
};
struct NaturalTerrainSignedCellD { uvec2 x;uvec2 y;uvec2 z; };
struct NaturalTerrainDomainD { NaturalTerrainSignedCellD cell; dvec3 fraction; };
struct NaturalTerrainFieldSampleD { double height; dvec3 bodyGradient; };

uint NaturalTerrainRotateLeft(uint value,uint count){return (value<<count)|(value>>(32u-count));}
uint NaturalTerrainMixLane(uint hash,uint value)
{
  hash=NaturalTerrainRotateLeft(hash^value,13u);
  hash=hash*NOVACORE_NATURAL_FIELD_HASH_LANE_MULTIPLIER+NOVACORE_NATURAL_FIELD_HASH_LANE_INCREMENT;
  return hash^(hash>>15u);
}
uint NaturalTerrainIdentityHash(NaturalTerrainFieldIdentityD identity)
{
  uint hash=NOVACORE_NATURAL_FIELD_HASH_INITIAL;
  hash=NaturalTerrainMixLane(hash,NOVACORE_NATURAL_FIELD_HASH_VERSION);
  hash=NaturalTerrainMixLane(hash,identity.bodyId.x);
  hash=NaturalTerrainMixLane(hash,identity.bodyId.y);
  hash=NaturalTerrainMixLane(hash,identity.physicalFieldGeneration.x);
  hash=NaturalTerrainMixLane(hash,identity.physicalFieldGeneration.y);
  hash=NaturalTerrainMixLane(hash,identity.familyId);
  hash=NaturalTerrainMixLane(hash,identity.octaveId);
  return NaturalTerrainMixLane(hash,identity.seed);
}
uint NaturalTerrainCoordinateWord(uint value,uint lane)
{
  uint hash=value^(NOVACORE_NATURAL_FIELD_HASH_LANE_INCREMENT+lane*NOVACORE_NATURAL_FIELD_HASH_FINAL_MULTIPLIER);
  hash^=hash>>16u;hash*=NOVACORE_NATURAL_FIELD_HASH_LANE_MULTIPLIER;hash^=hash>>15u;
  return NaturalTerrainRotateLeft(hash,lane*5u+4u);
}
uint NaturalTerrainCoordinateHash(uvec2 value,uint axis)
{return NaturalTerrainCoordinateWord(value.x,axis*2u)^NaturalTerrainCoordinateWord(value.y,axis*2u+1u);}
uint NaturalTerrainFinalizeHash(uint hash)
{
  hash^=hash>>16u;hash*=NOVACORE_NATURAL_FIELD_HASH_LANE_INCREMENT;
  hash^=hash>>15u;hash*=NOVACORE_NATURAL_FIELD_HASH_FINAL_MULTIPLIER;
  return hash^(hash>>16u);
}
uint NaturalTerrainHashCellFromIdentity(uint identityHash,NaturalTerrainSignedCellD cell)
{return NaturalTerrainFinalizeHash(identityHash^NaturalTerrainCoordinateHash(cell.x,0u)^NaturalTerrainCoordinateHash(cell.y,1u)^NaturalTerrainCoordinateHash(cell.z,2u));}
uint NaturalTerrainHashCell(NaturalTerrainFieldIdentityD identity,NaturalTerrainSignedCellD cell)
{return NaturalTerrainHashCellFromIdentity(NaturalTerrainIdentityHash(identity),cell);}
dvec3 NaturalTerrainSelectGradient(uint hash)
{
  uint index=hash%24u,zeroAxis=index/8u,lane=index%8u;
  bool swap=(lane&4u)!=0u;
  double first=(swap?1.0:2.0)*NOVACORE_NATURAL_FIELD_INVERSE_SQRT_FIVE;
  double second=(swap?2.0:1.0)*NOVACORE_NATURAL_FIELD_INVERSE_SQRT_FIVE;
  if((lane&1u)!=0u)first=-first;if((lane&2u)!=0u)second=-second;
  if(zeroAxis==0u)return dvec3(0.0,first,second);
  if(zeroAxis==1u)return dvec3(first,0.0,second);
  return dvec3(first,second,0.0);
}
double NaturalTerrainFade(double value){return value*value*value*(value*(value*6.0-15.0)+10.0);}
double NaturalTerrainFadeDerivative(double value){return 30.0*value*value*(value-1.0)*(value-1.0);}
NaturalTerrainDomainD NaturalTerrainReduceBodyPoint(dvec3 bodyFixedPoint,double cellSizeMetres)
{
  dvec3 q=bodyFixedPoint/cellSizeMetres,floored=floor(q);NaturalTerrainSignedCellD cell;
  for(uint axis=0u;axis<3u;axis++)
  {
    double magnitude=abs(floored[int(axis)]),highValue=floor(magnitude/4294967296.0);
    uvec2 words=uvec2(uint(magnitude-highValue*4294967296.0),uint(highValue));
    if(floored[int(axis)]<0.0){words.x=~words.x+1u;words.y=~words.y+(words.x==0u?1u:0u);}
    if(axis==0u)cell.x=words;else if(axis==1u)cell.y=words;else cell.z=words;
  }
  return NaturalTerrainDomainD(cell,q-floored);
}
uvec2 NaturalTerrainCellAddOne(uvec2 value){value.x+=1u;if(value.x==0u)value.y+=1u;return value;}
NaturalTerrainFieldSampleD EvaluateNaturalTerrainFieldD(dvec3 bodyFixedPoint,double cellSizeMetres,
  double amplitudeMetres,NaturalTerrainFieldIdentityD identity)
{
  NaturalTerrainDomainD domain=NaturalTerrainReduceBodyPoint(bodyFixedPoint,cellSizeMetres);
  dvec3 u=dvec3(NaturalTerrainFade(domain.fraction.x),NaturalTerrainFade(domain.fraction.y),NaturalTerrainFade(domain.fraction.z));
  dvec3 du=dvec3(NaturalTerrainFadeDerivative(domain.fraction.x),NaturalTerrainFadeDerivative(domain.fraction.y),NaturalTerrainFadeDerivative(domain.fraction.z));
  double value=0.0;dvec3 gradientQ=dvec3(0.0);uint identityHash=NaturalTerrainIdentityHash(identity);
  uint coordinateHashes[6];
  coordinateHashes[0]=NaturalTerrainCoordinateHash(domain.cell.x,0u);coordinateHashes[1]=NaturalTerrainCoordinateHash(NaturalTerrainCellAddOne(domain.cell.x),0u);
  coordinateHashes[2]=NaturalTerrainCoordinateHash(domain.cell.y,1u);coordinateHashes[3]=NaturalTerrainCoordinateHash(NaturalTerrainCellAddOne(domain.cell.y),1u);
  coordinateHashes[4]=NaturalTerrainCoordinateHash(domain.cell.z,2u);coordinateHashes[5]=NaturalTerrainCoordinateHash(NaturalTerrainCellAddOne(domain.cell.z),2u);
  for(int cornerZ=0;cornerZ<=1;cornerZ++)for(int cornerY=0;cornerY<=1;cornerY++)for(int cornerX=0;cornerX<=1;cornerX++)
  {
    ivec3 corner=ivec3(cornerX,cornerY,cornerZ);
    uint cornerHash=identityHash^coordinateHashes[uint(cornerX)]^coordinateHashes[2u+uint(cornerY)]^coordinateHashes[4u+uint(cornerZ)];
    cornerHash=NaturalTerrainFinalizeHash(cornerHash);
    dvec3 gradient=NaturalTerrainSelectGradient(cornerHash);
    dvec3 offset=domain.fraction-dvec3(corner);double cornerValue=dot(gradient,offset);
    dvec3 weight=dvec3(cornerX==0?1.0-u.x:u.x,cornerY==0?1.0-u.y:u.y,cornerZ==0?1.0-u.z:u.z);
    dvec3 derivative=dvec3(cornerX==0?-du.x:du.x,cornerY==0?-du.y:du.y,cornerZ==0?-du.z:du.z);
    double combined=weight.x*weight.y*weight.z;value+=combined*cornerValue;
    gradientQ+=combined*gradient+cornerValue*dvec3(derivative.x*weight.y*weight.z,
      weight.x*derivative.y*weight.z,weight.x*weight.y*derivative.z);
  }
  return NaturalTerrainFieldSampleD(value*amplitudeMetres,gradientQ*(amplitudeMetres/cellSizeMetres));
}

#endif
